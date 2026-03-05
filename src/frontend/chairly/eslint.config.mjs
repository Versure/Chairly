import nx from '@nx/eslint-plugin';
import angular from 'angular-eslint';
import sheriff from '@softarc/eslint-plugin-sheriff';
import ngrx from '@ngrx/eslint-plugin/v9';
import rxjs from '@smarttools/eslint-plugin-rxjs';
import rxjsAngular from 'eslint-plugin-rxjs-angular-x';
import vitest from '@vitest/eslint-plugin';
import playwright from 'eslint-plugin-playwright';
import simpleImportSort from 'eslint-plugin-simple-import-sort';
import unusedImports from 'eslint-plugin-unused-imports';
import sonarjs from 'eslint-plugin-sonarjs';

export default [
  // ─── Ignores ───────────────────────────────────────────────
  {
    ignores: ['**/dist', '**/vite.config.*.timestamp*', '**/vitest.config.*.timestamp*'],
  },

  // ─── Nx ────────────────────────────────────────────────────
  ...nx.configs['flat/base'],
  ...nx.configs['flat/typescript'],
  ...nx.configs['flat/javascript'],
  {
    files: ['**/*.ts', '**/*.tsx', '**/*.js', '**/*.jsx'],
    rules: {
      '@nx/enforce-module-boundaries': [
        'error',
        {
          enforceBuildableLibDependency: true,
          allow: ['^.*/eslint(\\.base)?\\.config\\.[cm]?[jt]s$'],
          depConstraints: [{ sourceTag: '*', onlyDependOnLibsWithTags: ['*'] }],
        },
      ],
    },
  },

  // ─── Sheriff (module boundaries) ──────────────────────────
  sheriff.configs.all,

  // ─── Angular: TS rules ────────────────────────────────────
  ...angular.configs.tsRecommended.map((config) => ({ ...config, files: ['**/*.ts'] })),
  {
    files: ['**/*.ts'],
    rules: {
      // Selectors
      '@angular-eslint/component-selector': [
        'error',
        { type: 'element', prefix: 'chairly', style: 'kebab-case' },
      ],
      '@angular-eslint/directive-selector': [
        'error',
        { type: 'attribute', prefix: 'chairly', style: 'camelCase' },
      ],
      '@angular-eslint/component-class-suffix': 'error',
      '@angular-eslint/directive-class-suffix': 'error',

      // Templates must be in separate .html files (templateUrl:)
      '@angular-eslint/component-max-inline-declarations': ['error', { template: 0 }],

      // Modern Angular (signals)
      '@angular-eslint/prefer-signals': 'error',
      '@angular-eslint/prefer-signal-model': 'error',
      '@angular-eslint/prefer-output-emitter-ref': 'error',
      '@angular-eslint/prefer-on-push-component-change-detection': 'error',

      // Code quality
      '@angular-eslint/no-async-lifecycle-method': 'error',
      '@angular-eslint/no-pipe-impure': 'error',
      '@angular-eslint/use-injectable-provided-in': 'error',
      '@angular-eslint/use-lifecycle-interface': 'error',
      '@angular-eslint/no-empty-lifecycle-method': 'error',
      '@angular-eslint/prefer-standalone': 'error',
    },
  },

  // ─── Angular: Template rules ──────────────────────────────
  ...angular.configs.templateRecommended.map((config) => ({
    ...config,
    files: ['**/*.html'],
  })),
  ...angular.configs.templateAccessibility.map((config) => ({
    ...config,
    files: ['**/*.html'],
  })),
  {
    files: ['**/*.html'],
    rules: {
      // Angular 17+ signals must be called in templates (title(), mySignal()).
    // The no-call-expression rule does not support signal detection, so it is
    // disabled. The CLAUDE.md "no function calls in templates" convention still
    // applies in code review — signals and pipes are the approved alternatives.
    '@angular-eslint/template/no-call-expression': 'off',
      '@angular-eslint/template/prefer-self-closing-tags': 'error',
      '@angular-eslint/template/button-has-type': 'error',
      '@angular-eslint/template/no-any': 'error',
      // allowBindToStyle: true — needed for dynamic [style.background-color] binding in color picker
      '@angular-eslint/template/no-inline-styles': ['error', { allowBindToStyle: true }],
    },
  },

  // ─── Angular: Inline template processing ──────────────────
  {
    files: ['**/*.ts'],
    processor: angular.processInlineTemplates,
  },

  // ─── NgRx SignalStore ─────────────────────────────────────
  ...ngrx.configs.signals,

  // ─── TypeScript strict rules ──────────────────────────────
  {
    files: ['**/*.ts'],
    rules: {
      // Strict equality
      eqeqeq: ['error', 'always'],

      // No console
      'no-console': 'error',

      // File length
      'max-lines': ['warn', { max: 300, skipBlankLines: true, skipComments: true }],

      // Explicit return types
      '@typescript-eslint/explicit-function-return-type': [
        'error',
        {
          allowExpressions: true,
          allowTypedFunctionExpressions: true,
          allowHigherOrderFunctions: true,
          allowDirectConstAssertionInArrowFunctions: true,
        },
      ],

      // No any
      '@typescript-eslint/no-explicit-any': 'error',

      // Unused variables
      'no-unused-vars': 'off',
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
    },
  },

  // ─── RxJS ─────────────────────────────────────────────────
  {
    files: ['**/*.ts'],
    plugins: rxjs.configs.recommended.plugins,
    languageOptions: {
      parserOptions: {
        projectService: true,
      },
    },
    rules: rxjs.configs.recommended.rules,
  },

  // ─── RxJS Angular ─────────────────────────────────────────
  {
    files: ['**/*.ts'],
    languageOptions: {
      parserOptions: {
        projectService: true,
      },
    },
    plugins: { 'rxjs-angular-x': rxjsAngular },
    rules: {
      'rxjs-angular-x/prefer-takeuntil': [
        'error',
        {
          alias: ['takeUntilDestroyed'],
          checkDecorators: ['Component', 'Directive'],
          checkDestroy: true,
        },
      ],
      'rxjs-angular-x/prefer-async-pipe': 'warn',
    },
  },

  // ─── Import sorting ───────────────────────────────────────
  {
    files: ['**/*.ts'],
    plugins: { 'simple-import-sort': simpleImportSort },
    rules: {
      'simple-import-sort/imports': [
        'error',
        {
          groups: [
            // Angular imports
            ['^@angular/'],
            // Nx and third-party
            ['^@ngrx/', '^@nx/', '^rxjs', '^(?!\\.)'],
            // Project aliases
            ['^@org/'],
            // Relative imports
            ['^\\.'],
          ],
        },
      ],
      'simple-import-sort/exports': 'error',
    },
  },

  // ─── Unused imports ───────────────────────────────────────
  {
    files: ['**/*.ts'],
    plugins: { 'unused-imports': unusedImports },
    rules: {
      'unused-imports/no-unused-imports': 'error',
    },
  },

  // ─── SonarJS ──────────────────────────────────────────────
  {
    files: ['**/*.ts', '**/*.js', '**/*.mjs'],
    ...sonarjs.configs.recommended,
  },

  // ─── Vitest (unit test files) ─────────────────────────────
  {
    files: ['**/*.spec.ts', '**/*.test.ts'],
    plugins: { vitest },
    rules: {
      ...vitest.configs.recommended.rules,
      // Relax rules that conflict in test files
      'no-console': 'off',
      '@typescript-eslint/explicit-function-return-type': 'off',
      'max-lines': 'off',
    },
  },

  // ─── Playwright (e2e test files) ──────────────────────────
  {
    files: ['**/e2e/**/*.ts', '**/*-e2e/**/*.ts', '**/chairly-e2e/**/*.ts'],
    ...playwright.configs['flat/recommended'],
    rules: {
      ...playwright.configs['flat/recommended'].rules,
      // Relax rules that conflict in e2e files
      'no-console': 'off',
      '@typescript-eslint/explicit-function-return-type': 'off',
      'max-lines': 'off',
    },
  },
];
