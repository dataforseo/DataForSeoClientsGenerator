import typescript from 'rollup-plugin-typescript2';

export default {
    input: 'src/index.ts',
    output: {
        dir: 'dist/cjs',
        format: 'cjs',
        preserveModules: true,
        preserveModulesRoot: 'src',
        entryFileNames: '[name].js',
        exports: 'named',
        sourcemap: true
    },
    plugins: [
        typescript({
            tsconfig: './tsconfig.cjs.json',
            useTsconfigDeclarationDir: true
        })
    ]
};