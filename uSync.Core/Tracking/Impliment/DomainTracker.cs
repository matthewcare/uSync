﻿using Umbraco.Cms.Core.Models;

using uSync.Core.Serialization;

namespace uSync.Core.Tracking.Impliment;

public class DomainTracker : SyncXmlTrackAndMerger<IDomain>, ISyncTracker<IDomain>
{
    public DomainTracker(SyncSerializerCollection serializers)
        : base(serializers)
    {
    }

    public override List<TrackingItem> TrackingItems =>
    [
        TrackingItem.Single("Domain > Wildcard", "/Info/IsWildcard"),
        TrackingItem.Single("Domain > Language", "/Info/Language"),
        TrackingItem.Single("Domain > Root", "/Info/Root")
    ];
}
