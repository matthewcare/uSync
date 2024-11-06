import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
	client: 'legacy/fetch',
	input: 'http://localhost:53015/umbraco/swagger/uSync/swagger.json',
	output: {
		format: 'prettier',
		path: 'src/api',
	},
	plugins: [
		{
			name: '@hey-api/schemas',
			type: 'json',
		},
		{
			name: '@hey-api/types',
			enums: 'javascript',
		},
		{
			name: '@hey-api/services',
			asClass: true,
		},
	],
});
