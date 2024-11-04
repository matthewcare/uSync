import {
	css,
	customElement,
	html,
	state,
	when,
} from '@umbraco-cms/backoffice/external/lit';
import { UmbModalBaseElement } from '@umbraco-cms/backoffice/modal';
import { UploadImportResult } from '../../api';

@customElement('usync-import-dialog')
export class uSyncImportModalDialog extends UmbModalBaseElement<any, any> {
	@state()
	result: UploadImportResult | undefined;

	#onClose() {
		this.modalContext?.reject();
	}

	#onImport() {
		this.value = this.result?.success;
		this.modalContext?.submit();
	}

	#onUploaded(e: CustomEvent<UploadImportResult>) {
		this.result = e.detail;
	}

	render() {
		return html`
			<umb-body-layout .headline=${this.localize.term('uSync_importHeader')}>
				${this.renderForm()} ${this.renderResult()}
			</umb-body-layout>
		`;
	}

	renderForm() {
		if (this.result !== undefined) return;

		return html` ${this.localize.term('uSync_uploadIntro')}
			<usync-file-upload @uploaded=${this.#onUploaded}></usync-file-upload>
			<div slot="actions">
				<uui-button
					id="cancel"
					.label=${this.localize.term('general_close')}
					@click="${this.#onClose}"></uui-button>
			</div>`;
	}

	renderResult() {
		if (this.result == undefined) return;

		return html`${when(
				this.result.success,
				() => html`${this.localize.term('uSync_uploadSuccess')}`,
				() => html`${this.localize.term('uSync_uploadError')} ${this.result?.errors}`,
			)}
			<div slot="actions">
				<uui-button id="continue" label="Import" @click="${this.#onImport}"></uui-button>
			</div>`;
	}

	static styles = css`
		umb-body-layout {
			max-width: 350px;
		}

		usync-file-upload {
			padding: 10px 0;
		}
	`;
}

export default uSyncImportModalDialog;
