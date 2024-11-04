import { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import { UmbDataSourceResponse } from '@umbraco-cms/backoffice/repository';
import { tryExecuteAndNotify } from '@umbraco-cms/backoffice/resources';
import {
	ActionsService,
	PerformActionRequest,
	PerformActionResponse,
	SyncActionGroup,
} from '@jumoo/uSync';

export interface SyncActionDataSource {
	getActions(): Promise<UmbDataSourceResponse<unknown>>;
	performAction(
		request: PerformActionRequest,
	): Promise<UmbDataSourceResponse<PerformActionResponse>>;
}

export class uSyncActionDataSource implements SyncActionDataSource {
	#host: UmbControllerHost;

	constructor(host: UmbControllerHost) {
		this.#host = host;
	}

	async getActions(): Promise<UmbDataSourceResponse<Array<SyncActionGroup>>> {
		return await tryExecuteAndNotify(this.#host, ActionsService.getActions());
	}

	async performAction(
		request: PerformActionRequest,
	): Promise<UmbDataSourceResponse<PerformActionResponse>> {
		return await tryExecuteAndNotify(
			this.#host,
			ActionsService.performAction({
				requestBody: request,
			}),
		);
	}

	async downloadFile(requestId: string) {
		return await tryExecuteAndNotify(
			this.#host,
			ActionsService.download({
				requestId: requestId,
			}),
		);
	}

	async processUpload(fileId: string) {
		return await tryExecuteAndNotify(
			this.#host,
			ActionsService.processUpload({
				tempKey: fileId,
			}),
		);
	}
}
