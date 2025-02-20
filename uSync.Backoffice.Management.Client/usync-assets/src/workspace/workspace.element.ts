import { UmbTextStyles } from '@umbraco-cms/backoffice/style';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import {
	LitElement,
	css,
	customElement,
	html,
	state,
} from '@umbraco-cms/backoffice/external/lit';

import { uSyncWorkspaceContext, uSyncConstants } from '@jumoo/uSync';

import './views/default/default.element.js';

@customElement('usync-workspace-root')
export class uSyncWorkspaceRootElement extends UmbElementMixin(LitElement) {
	#workspaceContext: uSyncWorkspaceContext;

	@state()
	version: string = uSyncConstants.version;

	constructor() {
		super();

		this.#workspaceContext = new uSyncWorkspaceContext(this);

		this.observe(this.#workspaceContext.completed, (_completed) => {
			// console.log('completed', _completed);
		});
	}

	async connectedCallback() {
		super.connectedCallback();
		const addons = await this.#workspaceContext.getAddons();
		this.version = `v${addons?.version ?? uSyncConstants.version}`;
	}

	render() {
		return html`
			<umb-workspace-editor .enforceNoFooter=${true}>
				<div slot="header" class="header">
					<div>
						<strong><umb-localize key="uSync_name"></umb-localize></strong><br /><em
							>${this.version}</em
						>
					</div>
				</div>
			</umb-workspace-editor>
		`;
	}

	static styles = [
		UmbTextStyles,
		css`
			umb-workspace-editor > div.header {
				display: flex;
				align-items: center;
				align-content: center;
			}
		`,
	];
}

export default uSyncWorkspaceRootElement;

declare global {
	interface HTMLElementTagNameMap {
		'usync-workspace-root': uSyncWorkspaceRootElement;
	}
}
