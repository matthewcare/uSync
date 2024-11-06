import { defineConfig } from 'vite';

export default defineConfig({
	build: {
		lib: {
			entry: 'src/index.ts', // your web component source file
			formats: ['es'],
			fileName: 'uSync',
		},
		outDir: '../wwwroot/App_Plugins/uSync',
		emptyOutDir: true,
		sourcemap: true,
		rollupOptions: {
			external: [/^@umbraco/],
			onwarn: () => {},
		},
	},
	base: '/App_Plugins/uSync/',
	mode: 'production',
	plugins: [
		// viteStaticCopy({
		// 	targets: [
		// 		{
		// 			src: 'src/icons/svg/*.js',
		// 			dest: 'icons',
		// 		},
		// 	],
		// }),
	],
});
