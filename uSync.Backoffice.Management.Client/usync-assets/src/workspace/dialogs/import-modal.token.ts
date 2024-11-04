import { UmbModalToken } from '@umbraco-cms/backoffice/modal';

export interface uSyncImportModalData {}
export interface uSyncImportModalValue {}

export const USYNC_IMPORT_MODAL = new UmbModalToken<
	uSyncImportModalData,
	uSyncImportModalValue
>('usync.import.modal', {
	modal: {
		type: 'dialog',
	},
});
