import js from '@eslint/js';
import tseslint from 'typescript-eslint';

export default tseslint.config(
  { ignores: ['dist', 'node_modules', 'public/*.js'] },
  js.configs.recommended,
  ...tseslint.configs.strict,
  {
    rules: {
      // Never leave bare TODOs; use PROD:, VERIFY: or DEMO: markers.
      'no-warning-comments': ['error', { terms: ['todo', 'fixme'], location: 'anywhere' }],
    },
  },
);
