import { SyncLegacyFilesCondition } from '@jumoo/uSync';
import { uSyncConstants } from '@jumoo/uSync';

export const manifests: UmbExtensionManifest[] = [
	{
		type: 'condition',
		alias: uSyncConstants.conditions.legacy,
		name: 'uSync Legacy Files Condition',
		api: SyncLegacyFilesCondition,
	},
];
