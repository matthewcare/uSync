import type { CancelablePromise } from './core/CancelablePromise';
import { OpenAPI } from './core/OpenAPI';
import { request as __request } from './core/request';
import type { ActionsData, MigrationsData, SettingsData } from './models';

export class ActionsResource {

	/**
	 * @returns unknown Success
	 * @throws ApiError
	 */
	public static getActions(): CancelablePromise<ActionsData['responses']['GetActions']> {
		
		return __request(OpenAPI, {
			method: 'GET',
			url: '/umbraco/usync/api/v1/Actions',
		});
	}

	/**
	 * @returns unknown Success
	 * @throws ApiError
	 */
	public static performAction(data: ActionsData['payloads']['PerformAction'] = {}): CancelablePromise<ActionsData['responses']['PerformAction']> {
		const {
                    
                    requestBody
                } = data;
		return __request(OpenAPI, {
			method: 'POST',
			url: '/umbraco/usync/api/v1/Perform',
			body: requestBody,
			mediaType: 'application/json',
		});
	}

}

export class MigrationsResource {

	/**
	 * @returns unknown Success
	 * @throws ApiError
	 */
	public static checkLegacy(): CancelablePromise<MigrationsData['responses']['CheckLegacy']> {
		
		return __request(OpenAPI, {
			method: 'GET',
			url: '/umbraco/usync/api/v1/CheckLegacy',
		});
	}

}

export class SettingsResource {

	/**
	 * @returns unknown Success
	 * @throws ApiError
	 */
	public static getAddOns(): CancelablePromise<SettingsData['responses']['GetAddOns']> {
		
		return __request(OpenAPI, {
			method: 'GET',
			url: '/umbraco/usync/api/v1/AddOns',
		});
	}

	/**
	 * @returns unknown Success
	 * @throws ApiError
	 */
	public static getAddonSplash(): CancelablePromise<SettingsData['responses']['GetAddonSplash']> {
		
		return __request(OpenAPI, {
			method: 'GET',
			url: '/umbraco/usync/api/v1/AddOnSplash',
		});
	}

	/**
	 * @returns unknown Success
	 * @throws ApiError
	 */
	public static getHandlerSetSettings(data: SettingsData['payloads']['GetHandlerSetSettings'] = {}): CancelablePromise<SettingsData['responses']['GetHandlerSetSettings']> {
		const {
                    
                    id
                } = data;
		return __request(OpenAPI, {
			method: 'GET',
			url: '/umbraco/usync/api/v1/HandlerSettings',
			query: {
				id
			},
		});
	}

	/**
	 * @returns unknown Success
	 * @throws ApiError
	 */
	public static getSettings(): CancelablePromise<SettingsData['responses']['GetSettings']> {
		
		return __request(OpenAPI, {
			method: 'GET',
			url: '/umbraco/usync/api/v1/Settings',
		});
	}

}