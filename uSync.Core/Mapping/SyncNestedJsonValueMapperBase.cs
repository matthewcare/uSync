﻿using System.Text.Json.Nodes;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

using uSync.Core.Extensions;

namespace uSync.Core.Mapping;

/// <summary>
///  Handle nested values that are in JSON and follow certain nested content like patterns.
/// </summary>
[Obsolete("Nested Content has been removed from umbraco")]
public abstract class SyncNestedJsonValueMapperBase : SyncNestedValueMapperBase
{
    protected SyncNestedJsonValueMapperBase(IEntityService entityService,
        Lazy<SyncValueMapperCollection> mapperCollection,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService)
        : base(entityService, mapperCollection, contentTypeService, dataTypeService)
    {
    }

    /// <summary>
    ///  Get Import value string 
    /// </summary>
    /// <remarks>
    ///  gets the string value, null checks it, and returns a JToken element 
    ///  to the GetImportProperties method.
    /// </remarks>
    public override async Task<string?> GetImportValueAsync(string value, string editorAlias)
    {
        if (value.IsObjectNullOrEmptyString()) return null;
        if (value.TryConvertToJsonNode(out var jsonValue) is false) return null;
        if (jsonValue == null) return value.ToString();

        return await ProcessValuesAsync(jsonValue.AsObject(), editorAlias, GetImportPropertiesAsync);
    }

    /// <summary>
    ///  Get Export string 
    /// </summary>
    /// <remarks>
    ///  get the current value, checks it and returns a JTOKEN element 
    ///  to the GetExportProperties method.
    /// </remarks>
    public override async Task<string?> GetExportValueAsync(object value, string editorAlias)
    {
        if (value.IsObjectNullOrEmptyString()) return null;
        if (value.TryConvertToJsonNode(out var jsonValue) is false) return null;
        if (jsonValue == null) return value.ToString();

        return await ProcessValuesAsync(jsonValue.AsObject(), editorAlias, GetExportPropertiesAsync);
    }

    protected abstract Task<string?> ProcessValuesAsync(JsonObject jsonValue, string editorAlias,
        Func<JsonObject, IContentType, Task<JsonObject>> GetPropertiesMethod);
}
