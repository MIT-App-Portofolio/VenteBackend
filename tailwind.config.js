// tailwind.config.js


module.exports = {
  content: [
      "/Pages/*.{html,cshtml}", 
    "./Pages/**/*.{html,cshtml}",
    "./wwwroot/js/**/*.{js,jsx}"
  ],
  theme: {
    extend: {
      colors: {
        primary: "#ffffff", // strong white for primary text/elements
        secondary: "#f1f5f9", // light gray for subtle highlights
        accent: "#d1d5db", // light gray accents
        neutral: "#000000", // pure black background
        "base-100": "#0a0a0a", // slightly lighter black for base
      },
      fontFamily: {
        sans: [
          '"Inter"',
          'system-ui',
          '-apple-system',
          'BlinkMacSystemFont',
          '"Segoe UI"',
          'Roboto',
          '"Helvetica Neue"',
          'Arial',
          '"Noto Sans"',
          'sans-serif',
          '"Apple Color Emoji"',
          '"Segoe UI Emoji"',
          '"Segoe UI Symbol"',
          '"Noto Color Emoji"',
        ],
      },
      boxshadow: {
        modern: "0px 4px 10px rgba(255, 255, 255, 0.1)", // soft white shadow for depth
      },
    },
  },
    plugins: [ require('daisyui') ],
  daisyui: {
    themes: [
      {
        modern: {
          primary: "#ffffff",
          secondary: "#f1f5f9",
          accent: "#d1d5db",
          neutral: "#000000",
          "base-100": "#0a0a0a",
          "base-200": "#1c1c1c", // darker shades for secondary background
          "base-300": "#2d2d2d", // even darker for cards or containers
          info: "#e5e7eb", // light gray info color
          success: "#d1d5db", // light success color
          warning: "#f59e0b", // yellow warning
          error: "#f87171", // light red for error
        },
      },
    ],
    darktheme: "modern", // default to dark theme
  }
}