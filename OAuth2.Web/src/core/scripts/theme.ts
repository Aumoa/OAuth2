import { ref } from 'vue';

type Theme = 'light' | 'dark' | 'system';
let transitionTimer: number | undefined;

const themeTransitionDuration = 500;
const themeTransitionClassName = 'theme-transitioning';

function resolveTheme(theme: Theme): 'light' | 'dark' {
  if (theme !== 'system') {
    return theme;
  }

  return window.matchMedia('(prefers-color-scheme: dark)').matches
    ? 'dark'
    : 'light';
}

const setTheme = (newTheme: Theme) => {
  const root = document.documentElement;
  root.classList.add(themeTransitionClassName);

  if (transitionTimer !== undefined) {
    window.clearTimeout(transitionTimer);
  }

  theme.value = newTheme;
  localStorage.setItem('preference-theme', newTheme);
  document.documentElement.dataset.theme = resolveTheme(newTheme);

  transitionTimer = window.setTimeout(() => {
    root.classList.remove(themeTransitionClassName);
    transitionTimer = undefined;
  }, themeTransitionDuration);
}

const theme = ref<Theme>(localStorage.getItem('preference-theme') as Theme | null ?? 'system');

export { setTheme, theme };
