// @ts-check
import withNuxt from './.nuxt/eslint.config.mjs'

export default withNuxt(
  // Your custom configs here
  {
    rules: {
      '@stylistic/arrow-parens': 'off',
      'vue/singleline-html-element-content-newline': 'off',
      '@stylistic/member-delimiter-style': 'off',
      'vue/max-attributes-per-line': [
        'warn',
        {
          singleline: {
            max: 5
          }
        }
      ]
    }
  }
)
