import js from '@eslint/js';
import reactHooks from 'eslint-plugin-react-hooks';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  { ignores: ['dist', 'node_modules'] },
  js.configs.recommended,
  ...tseslint.configs.strict,
  {
    plugins: { 'react-hooks': reactHooks },
    rules: {
      ...reactHooks.configs.recommended.rules,
      // Never leave bare TODOs; use PROD:, VERIFY: or DEMO: markers.
      'no-warning-comments': ['error', { terms: ['todo', 'fixme'], location: 'anywhere' }],
      // Data access lives in src/api and hooks, never inside components.
      'no-restricted-globals': ['error', 'fetch'],
    },
  },
  { files: ['src/api/**'], rules: { 'no-restricted-globals': 'off' } },
);
