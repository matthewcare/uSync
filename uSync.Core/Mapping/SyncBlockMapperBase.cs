using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Services;

using uSync.Core.Dependency;
using uSync.Core.Extensions;
using uSync.Core.Mapping.Mappers;

namespace uSync.Core.Mapping;

public abstract class SyncBlockMapperBase<TBlockValue> : SyncValueMapperBase
    where TBlockValue : BlockValue
{
    private readonly IContentTypeService _contentTypeService;
    private readonly Lazy<SyncValueMapperCollection> _mapperCollection;
    private readonly ILogger<BlockGridMapper> _logger;

    public SyncBlockMapperBase(
        IEntityService entityService,
        IContentTypeService contentTypeService,
        Lazy<SyncValueMapperCollection> mapperCollection,
        ILogger<BlockGridMapper> logger)
        : base(entityService)
    {
        _contentTypeService = contentTypeService;
        _mapperCollection = mapperCollection;
        _logger = logger;
    }

    public override async Task<string?> GetImportValueAsync(string value, string editorAlias)
        => await ProcessBlockValuesAsync(value, GetImportProperty);

    public override async Task<string?> GetExportValueAsync(object value, string editorAlias)
        => await ProcessBlockValuesAsync(value?.ToString() ?? string.Empty, GetExportProperty);


    private async Task<object?> GetImportProperty(object? value, string propertyEditorAlias)
    {
        if (_mapperCollection.Value is null) return value;
        return await _mapperCollection.Value.GetImportValueAsync(value?.ToString() ?? string.Empty, propertyEditorAlias);
    }

    private async Task<object?> GetExportProperty(object? value, string propertyEditorAlias)
    {
        if (_mapperCollection.Value is null) return value;
        return await _mapperCollection.Value.GetExportValueAsync(value?.ToString() ?? string.Empty, propertyEditorAlias);
    }

    private async Task<string?> ProcessBlockValuesAsync(string value, Func<object?, string, Task<object?>> GetValueMethod)
    {
        var blockValue = GetBlockValue(value);
        if (blockValue == null) return value;

        foreach (var contentItem in blockValue.ContentData)
        {
            await ProcessBlockData(contentItem, GetValueMethod);
        }

        foreach (var settingsItem in blockValue.SettingsData)
        {
            await ProcessBlockData(settingsItem, GetValueMethod);
        }

        if (blockValue.Expose.Count == 0)
        {
            // migration from v14 to v15+ block values.
            blockValue.Expose = blockValue.ContentData
                .Select(x => new BlockItemVariation(x.Key, null, null))
                .ToList();
        }

        return blockValue.SerializeJsonString(true);
    }

    private async Task ProcessBlockData(BlockItemData? blockItem, Func<object?, string, Task<object?>> GetValueMethod)
    {
        if (blockItem == null) return;

        var contentType = await GetContentType(blockItem.ContentTypeKey);
        if (contentType is null) return;

        foreach (var value in blockItem.Values)
        {
            var property = contentType.CompositionPropertyTypes.FirstOrDefault(x => x.Alias == value.Alias);
            if (property == null) continue;

            var mappedValue = await GetValueMethod(value.Value, property.PropertyEditorAlias);
            if (mappedValue != null)
                value.Value = mappedValue;
        }
    }

    private async Task<IContentType?> GetContentType(Guid contentTypeKey)
        => await _contentTypeService.GetAsync(contentTypeKey);

    private TBlockValue? GetBlockValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            return value.DeserializeJson<TBlockValue>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting block value");
            return null;
        }
    }

    public override async Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(object value, string editorAlias, DependencyFlags flags)
    {
        var blockValue = GetBlockValue(value?.ToString() ?? string.Empty);
        if (blockValue == null) return [];

        var dependencies = new List<uSyncDependency>();

        List<BlockItemData> blocks = [
            ..blockValue.ContentData,
            ..blockValue.SettingsData
        ];

        foreach (var block in blocks)
        {
            dependencies.AddRange(await GetBlockDependencies(block, flags));
        }

        return dependencies;
    }

    private async Task<IEnumerable<uSyncDependency>> GetBlockDependencies(BlockItemData block, DependencyFlags flags)
    {
        var dependencies = new List<uSyncDependency>();
        var contentType = await GetContentType(block.ContentTypeKey);
        if (contentType is null) return dependencies;
        foreach (var value in block.Values)
        {
            var property = contentType.CompositionPropertyTypes.FirstOrDefault(x => x.Alias == value.Alias);
            if (property == null) continue;
            dependencies.AddRange(await _mapperCollection.Value.GetDependenciesAsync(value, property.PropertyEditorAlias, flags));
        }
        return dependencies;
    }
}
