/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'Segoe UI', 'Roboto', 'sans-serif'],
      },
      colors: {
        flit: {
          primary: '#3B82F6',
          'primary-hover': '#2563EB',
          'primary-dark': '#1E40AF',
          accent: '#2DD4BF',
          'accent-dark': '#14B8A6',
          heading: '#1E3A5F',
          muted: '#64748B',
          border: '#E2E8F0',
          canvas: '#F8FAFC',
          'canvas-dark': '#0F172A',
          'surface-dark': '#1E293B',
          'heading-dark': '#F1F5F9',
          'muted-dark': '#CBD5E1',
          'border-dark': '#334155',
        },
      },
      boxShadow: {
        flit: '0 1px 3px 0 rgb(30 58 95 / 0.08), 0 1px 2px -1px rgb(30 58 95 / 0.06)',
        'flit-md': '0 4px 12px -2px rgb(30 58 95 / 0.12)',
        'flit-dark': '0 1px 3px 0 rgb(0 0 0 / 0.35), 0 1px 2px -1px rgb(0 0 0 / 0.25)',
        'flit-dark-md': '0 8px 24px -4px rgb(0 0 0 / 0.45)',
      },
    },
  },
  plugins: [],
};
