import typescript from 'rollup-plugin-typescript2';

export default {
    input: 'src/index.ts',
    output: {
        dir: 'dist/esm',
        format: 'esm',
        preserveModules: true,
        preserveModulesRoot: 'src',
        entryFileNames: '[name].js',
        sourcemap: true
    },
    plugins: [
        typescript({
            tsconfig: './tsconfig.esm.json',
            useTsconfigDeclarationDir: true
        })
    ]
};