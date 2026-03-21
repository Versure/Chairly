import nx from '@nx/eslint-plugin';
import playwright from 'eslint-plugin-playwright';
import simpleImportSort from 'eslint-plugin-simple-import-sort';
import sonarjs from 'eslint-plugin-sonarjs';

export default [
  // ─── Ignores ───────────────────────────────────────────────
  {
    ignores: ['**/dist', '**/test-results', '**/playwright-report'],
  },

  // ─── Nx (base only, no type-aware rules) ──────────────────
  ...nx.configs['flat/base'],
  ...nx.configs['flat/javascript'],

  // ─── Playwright ───────────────────────────────────────────
  playwright.configs['flat/recommended'],

  // ─── TypeScript basics ────────────────────────────────────
  {
    files: ['**/*.ts', '**/*.js'],
    rules: {
      eqeqeq: ['error', 'always'],
      'no-console': 'off',
    },
  },

  // ─── Import sorting ──────────────────────────────────────
  {
    files: ['**/*.ts'],
    plugins: { 'simple-import-sort': simpleImportSort },
    rules: {
      'simple-import-sort/imports': 'error',
      'simple-import-sort/exports': 'error',
    },
  },

  // ─── SonarJS ─────────────────────────────────────────────
  {
    files: ['**/*.ts', '**/*.js'],
    ...sonarjs.configs.recommended,
  },
];
