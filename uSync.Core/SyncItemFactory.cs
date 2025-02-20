﻿using System.Xml.Linq;

using uSync.Core.Cache;
using uSync.Core.Dependency;
using uSync.Core.Models;
using uSync.Core.Serialization;
using uSync.Core.Tracking;

namespace uSync.Core;

public class SyncItemFactory : ISyncItemFactory
{
    private readonly SyncTrackerCollection syncTrackers;
    private readonly SyncDependencyCollection syncCheckers;

    private readonly SyncEntityCache entityCache;

    private readonly SyncSerializerCollection syncSerializers;


    public SyncItemFactory(
        SyncEntityCache entityCache,
        SyncSerializerCollection syncSerializers,
        SyncTrackerCollection syncTrackers,
        SyncDependencyCollection syncCheckers)
    {
        this.syncSerializers = syncSerializers;
        this.syncTrackers = syncTrackers;
        this.syncCheckers = syncCheckers;
        this.entityCache = entityCache;
    }

    public SyncEntityCache EntityCache => entityCache;

    public IEnumerable<ISyncSerializer<TObject>> GetSerializers<TObject>()
        => syncSerializers.GetSerializers<TObject>();

    public ISyncSerializer<TObject>? GetSerializer<TObject>(string name)
        => syncSerializers.GetSerializer<TObject>(name);


    public IEnumerable<ISyncTracker<TObject>> GetTrackers<TObject>()
        => syncTrackers.GetTrackers<TObject>();

    public async Task<IEnumerable<uSyncChange>> GetChangesAsync<TObject>(XElement node, SyncSerializerOptions options)
        => await syncTrackers.GetChangesAsync<TObject>(node, options);

    public async Task<IEnumerable<uSyncChange>> GetChangesAsync<TObject>(XElement node, XElement currentNode, SyncSerializerOptions options)
    {
        if (currentNode == null)
            return await syncTrackers.GetChangesAsync<TObject>(node, options);
        else
            return await syncTrackers.GetChangesAsync<TObject>(node, currentNode, options);
    }

    public IEnumerable<ISyncDependencyChecker<TObject>> GetCheckers<TObject>()
        => syncCheckers.GetCheckers<TObject>();

    public async Task<IEnumerable<uSyncDependency>> GetDependenciesAsync<TObject>(TObject item, DependencyFlags flags)
    {
        var dependencies = new List<uSyncDependency>();
        foreach (var checker in syncCheckers.GetCheckers<TObject>())
        {
            if (checker is null) continue;
            dependencies.AddRange(await checker.GetDependenciesAsync(item, flags) ?? []);
        }
        return dependencies;
    }

}
