﻿using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Semver;
using Umbraco.Cms.Infrastructure.HostedServices;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSync.Core.Serialization;

namespace uSync.BackOffice;


/// <summary>
///  Callback event for SignalR hub
/// </summary>
public delegate void SyncEventCallback(SyncProgressSummary summary);

/// <summary>
///  the service that does all the processing,
///  this forms the entry point as an API to 
///  uSync, it is where imports, exports and reports
///  are actually ran from. 
/// </summary>
public partial class SyncService : ISyncService
{
    private readonly ILogger<SyncService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    private readonly IEventAggregator _eventAggregator;

    private readonly ISyncConfigService _uSyncConfig;
    private readonly ISyncHandlerFactory _handlerFactory;
    private readonly ISyncFileService _syncFileService;
    private readonly ISyncEventService _mutexService;

    private readonly ICoreScopeProvider _scopeProvider;

    private readonly IBackgroundTaskQueue? _backgroundTaskQueue;

    private readonly IAppCache _appCache;

    /// <summary>
    ///  Create a new uSyncService (done via DI)
    /// </summary>
    public SyncService(
        ILogger<SyncService> logger,
        IEventAggregator eventAggregator,
        ISyncConfigService uSyncConfigService,
        ISyncHandlerFactory handlerFactory,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        AppCaches appCaches,
        ICoreScopeProvider scopeProvider,
        ILoggerFactory loggerFactory,
        IBackgroundTaskQueue backgroundTaskQueue)
    {
        this._logger = logger;

        this._eventAggregator = eventAggregator;

        this._uSyncConfig = uSyncConfigService;
        this._handlerFactory = handlerFactory;
        this._syncFileService = syncFileService;
        this._mutexService = mutexService;

        this._appCache = appCaches.RuntimeCache;

        uSyncTriggers.DoExport += USyncTriggers_DoExport;
        uSyncTriggers.DoImport += USyncTriggers_DoImport;
        _scopeProvider = scopeProvider;
        _loggerFactory = loggerFactory;

        _backgroundTaskQueue = backgroundTaskQueue;

    }

    /// <inheritdoc/>>
    public bool HasContentFiles(string rootFolder)
    {
        return _syncFileService.DirectoryExists(rootFolder + "Content");
    }

    /// <inheritdoc/>>
    public bool HasContentFiles(string[] folders)
        => folders.Any(x => HasContentFiles(x));

    /// <inheritdoc/>>
    public bool HasRootFiles(string[] folders)
        => folders[..^1].Any(x => _syncFileService.DirectoryHasChildren(x));


    #region Reporting 

    /// <inheritdoc/>>
    [Obsolete("Will be removed in v16")]
    public IEnumerable<uSyncAction> Report(string folder, SyncHandlerOptions handlerOptions, uSyncCallbacks? callbacks = null)
    {
        handlerOptions ??= new SyncHandlerOptions();
        handlerOptions.Action = HandlerActions.Report;

        var handlers = _handlerFactory.GetValidHandlers(handlerOptions);
        return Report(folder, handlers, callbacks);
    }

    /// <inheritdoc/>>
    [Obsolete("Will be removed in v16")]
    public IEnumerable<uSyncAction> Report(string folder, IEnumerable<string> handlerAliases, uSyncCallbacks? callbacks)
    {
        var handlers = _handlerFactory.GetDefaultHandlers(handlerAliases);
        return Report(folder, handlers, callbacks);
    }

    /// <inheritdoc/>>
    [Obsolete("Will be removed in v16")]
    public IEnumerable<uSyncAction> Report(string folder, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks? callbacks)
    {

        var sw = Stopwatch.StartNew();

        _mutexService.FireBulkStartingAsync(new uSyncReportStartingNotification()).Wait();

        _logger.LogDebug("Reporting For [{handlers}]", string.Join(",", handlers.Select(x => x.Handler.Name)));

        var actions = new List<uSyncAction>();

        var summary = new SyncProgressSummary(handlers.Select(x => x.Handler), "Reporting", handlers.Count());

        foreach (var configuredHandler in handlers)
        {
            var handler = configuredHandler.Handler;
            var handlerSettings = configuredHandler.Settings;

            summary.Increment();

            summary.UpdateHandler(handler.Name, HandlerStatus.Processing, $"Reporting {handler.Name}", 0);

            callbacks?.Callback?.Invoke(summary);

            var handlerActions = handler.Report([$"{folder}/{handler.DefaultFolder}"], handlerSettings, callbacks?.Update);
            actions.AddRange(handlerActions);

            summary.UpdateHandler(handler.Name, HandlerStatus.Complete,
                handlerActions.CountChanges(),
                handlerActions.ContainsErrors());
        }

        summary.UpdateMessage("Report Complete");
        callbacks?.Callback?.Invoke(summary);


        _mutexService.FireBulkCompleteAsync(new uSyncReportCompletedNotification(actions)).Wait();
        sw.Stop();

        _logger.LogInformation("uSync Report: {handlerCount} handlers, processed {itemCount} items, {changeCount} changes in {ElapsedMilliseconds}ms",
            handlers.Count(), actions.Count,
            actions.CountChanges(),
            sw.ElapsedMilliseconds);

        callbacks?.Update?.Invoke($"Processed {actions.Count} items in {sw.ElapsedMilliseconds}ms", 1, 1);

        return actions;
    }

    #endregion

    #region Importing
    static SemaphoreSlim _importSemaphoreLock = new SemaphoreSlim(1, 1);

    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> StartupImportAsync(string[] folders, bool force, SyncHandlerOptions handlerOptions, uSyncCallbacks? callbacks = null)
    {
        handlerOptions ??= new SyncHandlerOptions();
        handlerOptions.Action = HandlerActions.Import;
        var handlers = _handlerFactory.GetValidHandlers(handlerOptions);
        return await ImportAsync(folders, force, handlers, handlerOptions, callbacks);
    }

    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> ImportAsync(string[] folders, bool force, IEnumerable<HandlerConfigPair> handlers, SyncHandlerOptions handlerOptions, uSyncCallbacks? callbacks)
    {
        // if its blank, we just throw it back empty. 
        if (handlers == null || !handlers.Any()) return [];

        var sw = Stopwatch.StartNew();

        using (var pause = _mutexService.ImportPause(true))
        {

            // pre import event
            await _mutexService.FireBulkStartingAsync(new uSyncImportStartingNotification());

            var actions = new List<uSyncAction>();

            var summary = new SyncProgressSummary(handlers.Select(x => x.Handler), "Importing", handlers.Count() + 1);
            summary.Handlers.Add(new SyncHandlerSummary()
            {
                Icon = "icon-defrag",
                Name = "Post Import",
                Status = HandlerStatus.Pending
            });

            var importOptions = new uSyncImportOptions
            {
                Flags = force ? SerializerFlags.Force : SerializerFlags.None,
                Callbacks = callbacks,
                Folders = folders,
                HandlerSet = handlerOptions.Set,
                UserId = handlerOptions.UserId,
            };

            foreach (var configuredHandler in handlers)
            {
                var handler = configuredHandler.Handler;
                var handlerSettings = configuredHandler.Settings;

                summary.Increment();

                summary.UpdateHandler(
                    handler.Name, HandlerStatus.Processing, $"Importing {handler.Name}", 0);

                callbacks?.Callback?.Invoke(summary);

                var handlerActions = await this.ImportHandlerAsync(handler.Alias, importOptions);

                actions.AddRange(handlerActions);

                summary.UpdateHandler(handler.Name, HandlerStatus.Complete,
                    handlerActions.CountChanges(),
                    handlerActions.ContainsErrors());

            }

            // fire complete
            await _mutexService.FireBulkCompleteAsync(new uSyncImportCompletedNotification(actions));

            _logger.LogInformation("uSync Import: {handlerCount} handlers, processed {itemCount} items, {changeCount} changes in {ElapsedMilliseconds}ms",
                handlers.Count(),
                actions.Count,
                actions.CountChanges(),
            sw.ElapsedMilliseconds);

            callbacks?.Update?.Invoke($"Processed {actions.Count} items in {sw.ElapsedMilliseconds}ms", 1, 1);

            return actions;
        }

    }

    private static async Task<List<uSyncAction>> PerformPostImportAsync(IEnumerable<HandlerConfigPair> handlers, IEnumerable<uSyncAction> actions)
    {
        var postImportActions = actions.Where(x => x.Success && x.Change > Core.ChangeType.NoChange && x.RequiresPostProcessing).ToList();
        if (postImportActions.Count == 0) return [];

        var results = new List<uSyncAction>();

        foreach (var handlerPair in handlers)
        {
            if (handlerPair.Handler is ISyncPostImportHandler postImportHandler)
            {
                var handlerActions = postImportActions.Where(x => x.ItemType == handlerPair.Handler.ItemType);
                if (handlerActions.Any() == false) continue;

                results.AddRange(await postImportHandler.ProcessPostImportAsync(handlerActions, handlerPair.Settings));
            }
        }

        return results;
    }

    /// <inheritdoc/>>
    public async Task<uSyncAction> ImportSingleActionAsync(uSyncAction action)
    {
        if (action.HandlerAlias is null || action.FileName is null) return new();

        var folders = _uSyncConfig.Settings.Folders;
        var handlerConfig = _handlerFactory.GetValidHandler(action.HandlerAlias);
        if (handlerConfig is null) return new();

        return (await handlerConfig.Handler.ImportAsync(action.FileName, handlerConfig.Settings, true)).FirstOrDefault();
    }

    #endregion

    #region Exporting 

    /// <inheritdoc/>>
    public bool CleanExportFolder(string folder)
    {
        try
        {
            if (_syncFileService.DirectoryExists(folder))
                _syncFileService.CleanFolder(folder);
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Failed to delete uSync folder (may be in use)", ex);
        }

        return true;
    }


    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> StartupExportAsync(string folder, SyncHandlerOptions handlerOptions, uSyncCallbacks? callbacks = null)
    {
        handlerOptions ??= new SyncHandlerOptions();
        handlerOptions.Action = HandlerActions.Export;

        var handlers = _handlerFactory.GetValidHandlers(handlerOptions);

        await WriteVersionFileAsync(folder);

        return await ExportAsync(folder, handlers, callbacks);
    }


    /// <inheritdoc/>>
    public async Task<bool> CheckVersionFileAsync(string[] folders)
    {
        foreach (var folder in folders.Reverse())
        {
            if (await CheckVersionFileAsync(folder))
                return true;
        }

        return false;
    }

    /// <inheritdoc/>>
    public async Task<bool> CheckVersionFileAsync(string folder)
    {
        var versionFile = Path.Combine(_syncFileService.GetAbsPath(folder), $"usync.{_uSyncConfig.Settings.DefaultExtension}");

        if (!_syncFileService.FileExists(versionFile))
        {
            return false;
        }
        else
        {
            try
            {
                var node = await _syncFileService.LoadXElementAsync(versionFile);
                var format = node.Attribute("format").ValueOrDefault("");
                if (!format.InvariantEquals(Core.uSyncConstants.FormatVersion))
                {
                    var expectedVersion = SemVersion.Parse(Core.uSyncConstants.FormatVersion);
                    if (SemVersion.TryParse(format, out SemVersion? current) && current is not null)
                    {
                        if (current.CompareTo(expectedVersion) >= 0) return true;
                    }

                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    private async Task WriteVersionFileAsync(string folder)
    {
        try
        {
            var versionFile = Path.Combine(_syncFileService.GetAbsPath(folder), $"usync.{_uSyncConfig.Settings.DefaultExtension}");
            var versionNode = new XElement("uSync",
                new XAttribute("version", typeof(uSync).Assembly.GetName()?.Version?.ToString() ?? "15.0.0"),
                new XAttribute("format", Core.uSyncConstants.FormatVersion));
            // remove date, we don't really care, and it causes unnecessary git changes.

            _syncFileService.CreateFoldersForFile(versionFile);
            await _syncFileService.SaveXElementAsync(versionNode, versionFile);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Issue saving the usync.config file in the root of {folder}", folder);
        }
    }

    /// <inheritdoc/>>
    [Obsolete("Will be removed in v15")]
    public IEnumerable<uSyncAction> Export(string folder, IEnumerable<string> handlerAliases, uSyncCallbacks? callbacks)
    {
        var handlers = _handlerFactory.GetDefaultHandlers(handlerAliases);
        return Export(folder, handlers, callbacks);
    }

    /// <inheritdoc/>>
    [Obsolete("Will be removed in v15")]
    public IEnumerable<uSyncAction> Export(string folder, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks? callbacks)
        => ExportAsync(folder, handlers, callbacks).Result;

    /// <inheritdoc/>>
    public async Task<IEnumerable<uSyncAction>> ExportAsync(string folder, IEnumerable<HandlerConfigPair> handlers, uSyncCallbacks? callbacks)
    {
        var sw = Stopwatch.StartNew();

        await _mutexService.FireBulkStartingAsync(new uSyncExportStartingNotification());

        var actions = new List<uSyncAction>();
        var summary = new SyncProgressSummary(handlers.Select(x => x.Handler), "Exporting", handlers.Count());

        foreach (var configuredHandler in handlers)
        {
            var handler = configuredHandler.Handler;

            summary.Increment();
            summary.UpdateHandler(
                handler.Name, HandlerStatus.Processing, $"Exporting {handler.Name}", 0);

            callbacks?.Callback?.Invoke(summary);

            var handlerActions = await handler.ExportAllAsync([$"{folder}/{handler.DefaultFolder}"], configuredHandler.Settings, callbacks?.Update);

            actions.AddRange(handlerActions);

            summary.UpdateHandler(handler.Name, HandlerStatus.Complete,
                handlerActions.CountChanges(),
                handlerActions.ContainsErrors());
        }


        summary.UpdateMessage("Export Completed");
        callbacks?.Callback?.Invoke(summary);

        await _mutexService.FireBulkCompleteAsync(new uSyncExportCompletedNotification(actions));

        sw.Stop();

        _logger.LogInformation("uSync Export: {handlerCount} handlers, processed {itemCount} items, {changeCount} changes in {ElapsedMilliseconds}ms",
            handlers.Count(), actions.Count,
            actions.CountChanges(),
            sw.ElapsedMilliseconds);

        callbacks?.Update?.Invoke($"Processed {actions.Count} items in {sw.ElapsedMilliseconds}ms", 1, 1);

        return actions;
    }

    #endregion

    /// <summary>
    ///  Do an import triggered by an event.
    /// </summary>
    /// <param name="e"></param>
    private void USyncTriggers_DoImport(uSyncTriggerArgs e)
    {
        if (e.EntityTypes != null && !string.IsNullOrWhiteSpace(e.Folder))
        {
            _logger.LogInformation("Import Triggered by downlevel change {folder}", e.Folder);

            var handlers = _handlerFactory
                .GetValidHandlersByEntityType(e.EntityTypes, e.HandlerOptions);

            var syncHandlerOptions = new SyncHandlerOptions
            {
                UserId = -1,
                Set = _uSyncConfig.Settings.DefaultSet,
                Action = HandlerActions.Import,
            };

            if (handlers.Any())
                this.ImportAsync([e.Folder], false, handlers, syncHandlerOptions, null).Wait();
        }
    }

    /// <summary>
    ///  do an export triggered by events. 
    /// </summary>
    /// <param name="e"></param>
    private void USyncTriggers_DoExport(uSyncTriggerArgs e)
    {
        if (e.EntityTypes != null && !string.IsNullOrWhiteSpace(e.Folder))
        {
            _logger.LogInformation("Export Triggered by downlevel change {folder}", e.Folder);

            var handlers = _handlerFactory
                .GetValidHandlersByEntityType(e.EntityTypes, e.HandlerOptions);

            if (handlers.Any()) this.ExportAsync(e.Folder, handlers, null).Wait();
        }
    }
}
