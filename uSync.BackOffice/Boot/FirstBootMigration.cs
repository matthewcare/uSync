﻿using Microsoft.Extensions.Logging;

using System;
using System.Diagnostics;

using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Migrations;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.BackOffice.Boot;

/// <summary>
/// Migration plan to add FirstBoot feature
/// </summary>
public class FirstBootMigrationPlan : MigrationPlan
{
    /// <inheritdoc/>
    public FirstBootMigrationPlan()
        : base("uSync_FirstBoot")
    {
        From(string.Empty)
                .To<FirstBootMigration>("FirstBoot-Migration")
                .To<LogViewerMigration>("LogViewer-Migration");
    }
}

/// <summary>
/// First boot Feature migration
/// </summary>
public class FirstBootMigration : MigrationBase
{
    private readonly IUmbracoContextFactory _umbracoContextFactory;
    private readonly ISyncConfigService _uSyncConfig;
    private readonly ISyncService _uSyncService;
    private readonly ILogger<FirstBootMigration> _logger;

    /// <inheritdoc/>
    public FirstBootMigration(
        IMigrationContext context,
        IUmbracoContextFactory umbracoContextFactory,
        ISyncConfigService uSyncConfig,
        ISyncService uSyncService,
        ILogger<FirstBootMigration> logger) : base(context)
    {
        _umbracoContextFactory = umbracoContextFactory;
        _uSyncConfig = uSyncConfig;
        _uSyncService = uSyncService;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override void Migrate()
    {
        // TODO: doesn't work in the betas. might need a new migration to add it.
        // return;

        // first boot migration. 
        try
        {

            if (!_uSyncConfig.Settings.ImportOnFirstBoot)
                return;

            var sw = Stopwatch.StartNew();
            var changes = 0;

            _logger.LogInformation("Import on First-boot Set - will import {group} handler groups",
                _uSyncConfig.Settings.FirstBootGroup);

            // if config service is set to import on first boot then this 
            // will let uSync do a first boot import 

            // not sure about context on migrations so will need to test
            // or maybe we fire something into a notification (or use a static)

            using (var reference = _umbracoContextFactory.EnsureUmbracoContext())
            {
                var results = _uSyncService.StartupImportAsync(_uSyncConfig.GetFolders(), false, new SyncHandlerOptions
                {
                    Group = _uSyncConfig.Settings.FirstBootGroup
                }).Result;

                changes = results.CountChanges();
            };

            sw.Stop();
            _logger.LogInformation("uSync First boot complete {changes} changes in ({time}ms)",
                changes, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "uSync First boot failed {message}", ex.Message);
            throw;
        }
    }
}
