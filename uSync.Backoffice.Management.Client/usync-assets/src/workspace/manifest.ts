import { uSyncConstants, SyncLegacyFilesConditionConfig } from '@jumoo/uSync';

import { manifests as dialogManifests } from './dialogs/manifest.js';

import './components/index.js';
import './dialogs/index.js';

const workspaceAlias = uSyncConstants.workspace.alias;

const workspace: UmbExtensionManifest = {
	type: 'workspace',
	alias: workspaceAlias,
	name: 'uSync core workspace',
	js: () => import('./workspace.element.js'),
	meta: {
		entityType: uSyncConstants.workspace.rootElement,
	},
};

const context: UmbExtensionManifest = {
	type: 'workspaceContext',
	alias: uSyncConstants.workspace.contextAlias,
	name: 'uSync workspace context',
	js: () => import('./workspace.context.js'),
	conditions: [
		{
			alias: 'Umb.Condition.WorkspaceAlias',
			match: workspaceAlias,
		},
	],
};

const workspaceViews: Array<UmbExtensionManifest> = [
	{
		type: 'workspaceView',
		alias: uSyncConstants.workspace.defaultView.alias,
		name: 'uSync workspace default view',
		js: () => import('./views/default/default.element.js'),
		weight: 300,
		meta: {
			label: 'Default',
			pathname: 'default',
			icon: 'usync-logo',
		},
		conditions: [
			{
				alias: 'Umb.Condition.WorkspaceAlias',
				match: workspaceAlias,
			},
		],
	},
	{
		type: 'workspaceView',
		alias: uSyncConstants.workspace.settingView.alias,
		name: 'uSync workspace settings view',
		js: () => import('./views/settings/settings.element.js'),
		weight: 200,
		meta: {
			label: 'Settings',
			pathname: 'settings',
			icon: 'icon-settings',
		},
		conditions: [
			{
				alias: 'Umb.Condition.WorkspaceAlias',
				match: workspaceAlias,
			},
		],
	},
	{
		type: 'workspaceView',
		alias: uSyncConstants.workspace.addOnView.alias,
		name: 'uSync addons',
		js: () => import('./views/addons/addons.element.js'),
		weight: 100,
		meta: {
			label: 'AddOns',
			pathname: 'addons',
			icon: 'icon-box',
		},
		conditions: [
			{
				alias: 'Umb.Condition.WorkspaceAlias',
				match: workspaceAlias,
			},
		],
	},
	{
		type: 'workspaceView',
		alias: uSyncConstants.workspace.legacyView.alais,
		name: 'uSync legacy',
		js: () => import('./views/legacy/legacy.element.js'),
		weight: 150,
		meta: {
			label: 'Legacy',
			pathname: 'legacy',
			icon: 'icon-dock-connector color-red',
		},
		conditions: [
			{
				alias: 'Umb.Condition.WorkspaceAlias',
				match: workspaceAlias,
			},
			{
				alias: uSyncConstants.conditions.legacy,
				hasLegacyFiles: true,
			} as SyncLegacyFilesConditionConfig,
		],
	},
];

const workspaceActions: Array<UmbExtensionManifest> = [];

export const manifests = [
	context,
	workspace,
	...workspaceViews,
	...workspaceActions,
	...dialogManifests,
];
