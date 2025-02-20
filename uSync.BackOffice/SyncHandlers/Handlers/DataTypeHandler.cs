﻿using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

using uSync.BackOffice.Configuration;
using uSync.BackOffice.Models;
using uSync.BackOffice.Services;
using uSync.BackOffice.SyncHandlers.Interfaces;
using uSync.BackOffice.SyncHandlers.Models;
using uSync.Core;
using uSync.Core.Serialization;

using static Umbraco.Cms.Core.Constants;

namespace uSync.BackOffice.SyncHandlers.Handlers;

/// <summary>
///  Handler to manage DataTypes via uSync
/// </summary>
[SyncHandler(uSyncConstants.Handlers.DataTypeHandler, "Datatypes", "DataTypes", uSyncConstants.Priorites.DataTypes,
    Icon = "icon-autofill", IsTwoPass = true, EntityType = UdiEntityType.DataType)]
public class DataTypeHandler : SyncHandlerContainerBase<IDataType>, ISyncHandler, ISyncPostImportHandler,
    INotificationAsyncHandler<SavedNotification<IDataType>>,
    INotificationAsyncHandler<MovedNotification<IDataType>>,
    INotificationAsyncHandler<DeletedNotification<IDataType>>,
    INotificationAsyncHandler<EntityContainerSavedNotification>,
    INotificationAsyncHandler<EntityContainerRenamedNotification>,
    INotificationAsyncHandler<SavingNotification<IDataType>>,
    INotificationAsyncHandler<MovingNotification<IDataType>>,
    INotificationAsyncHandler<DeletingNotification<IDataType>>
{

    private readonly IDataTypeService dataTypeService;
    private readonly IDataTypeContainerService _dataTypeContainerService;

    /// <summary>
    /// Constructor called via DI
    /// </summary>
    public DataTypeHandler(
        ILogger<DataTypeHandler> logger,
        IEntityService entityService,
        IDataTypeService dataTypeService,
        AppCaches appCaches,
        IShortStringHelper shortStringHelper,
        ISyncFileService syncFileService,
        ISyncEventService mutexService,
        ISyncConfigService uSyncConfig,
        ISyncItemFactory syncItemFactory,
        IDataTypeContainerService dataTypeContainerService)
        : base(logger, entityService, appCaches, shortStringHelper, syncFileService, mutexService, uSyncConfig, syncItemFactory)
    {
        this.dataTypeService = dataTypeService;
        _dataTypeContainerService = dataTypeContainerService;
    }

    /// <summary>
    /// Process all DataType actions at the end of the import process
    /// </summary>
    /// <remarks>
    /// Datatypes have to exist early on so DocumentTypes can reference them, but
    /// some doctypes reference content or document types, so we re-process them
    /// at the end of the import process to ensure those settings can be made too.
    /// 
    /// HOWEVER: The above isn't a problem Umbraco 10+ - the references can be set
    /// before the actual doctypes exist, so we can do that in one pass.
    /// 
    /// HOWEVER: If we move deletes to the end , we still need to process them. 
    /// but deletes are always 'change' = 'Hidden', so we only process hidden changes
    /// </remarks>
    public override async Task<IEnumerable<uSyncAction>> ProcessPostImportAsync(IEnumerable<uSyncAction> actions, HandlerSettings config)
    {
        if (actions == null || !actions.Any()) return [];

        var results = new List<uSyncAction>();
        var options = new uSyncImportOptions { Flags = SerializerFlags.LastPass };

        // we only do deletes here. 
        foreach (var action in actions.Where(x => x.Change == ChangeType.Hidden))
        {
            if (action.FileName is null) continue;
            results.AddRange(
                await ImportAsync(action.FileName, config, options));
        }

        results.AddRange(await CleanFoldersAsync(Guid.Empty));

        return results;
    }

    /// <summary>
    ///  Fetch a DataType Container from the DataTypeService
    /// </summary>
    /// <summary>
    ///  Fetch a DataType Container from the DataTypeService
    /// </summary>
    protected override async Task<IEntity?> GetContainerAsync(Guid key)
        => await _dataTypeContainerService.GetAsync(key);

    /// <summary>
    ///  Delete a DataType Container from the DataTypeService
    /// </summary>
    protected override async Task DeleteFolderAsync(Guid key)
        => await _dataTypeContainerService.DeleteAsync(key, Constants.Security.SuperUserKey);

    /// <summary>
    ///  Get the filename to use for a DataType when we save it
    /// </summary>
    protected override string GetItemFileName(IDataType item)
        => GetItemAlias(item).ToSafeAlias(shortStringHelper);
}
