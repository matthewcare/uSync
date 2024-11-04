const modal: UmbExtensionManifest = {
	type: 'modal',
	alias: 'usync.import.modal',
	name: 'uSync import modal',
	js: () => import('./import-modal.element.js'),
};

export const manifests = [modal];
