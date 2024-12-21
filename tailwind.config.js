// tailwind.config.js
module.exports = {
  content: [
    "./Pages/**/*.{html,cshtml}",
    "./Views/**/*.{html,cshtml}",
    "./wwwroot/js/**/*.{js,jsx}"
  ],
  theme: {
    extend: {},
  },
  plugins: [
    require('daisyui'),
  ],
}