﻿using Umbraco.Cms.Core;

namespace uSync.Core.DataTypes.DataTypeSerializers;

internal class TagMigratingConfigSerializer : ConfigurationSerializerBase, IConfigurationSerializer
{
    public string Name => nameof(TagMigratingConfigSerializer);

    public string[] Editors => [Constants.PropertyEditors.Aliases.Tags];

    public override IDictionary<string, object> GetConfigurationImport(IDictionary<string, object> configuration)
    {
        if (configuration.TryGetValue("StorageType", out var storageType) is false
            || storageType == null)
        {
            return configuration;
        }

        if (configuration.ContainsKey("delimiter"))
            configuration.Remove("delimiter");

        if (storageType is int storageInt is false) return configuration;

        var typeString = storageInt == 0 ? "Csv" : "Json";
        // if storage type is a number.
        configuration.Remove("StorageType");

        configuration.Add("storageType", typeString);

        return configuration;


    }

}
