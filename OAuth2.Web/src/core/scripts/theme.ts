import { ref } from 'vue';

type Theme = 'light' | 'dark' | 'system';

const setTheme = (newTheme: Theme) => {
  theme.value = newTheme;
  localStorage.setItem('preference-theme', newTheme);
  document.documentElement.dataset.theme = newTheme === 'system' ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light') : newTheme;
}

const theme = ref<Theme>(localStorage.getItem('preference-theme') as Theme | null ?? 'system');

export { setTheme, theme };
