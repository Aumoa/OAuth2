import { createApp } from 'vue';
import { createPinia } from 'pinia';
import 'material-symbols/outlined.css';
import './style.css';
import './core/style.css';
import App from './App.vue';
import { i18n } from './i18n';
import { router } from './router';

function redirectAuthorizationResponse(): boolean {
  const currentUri = new URL(window.location.href);
  const code = currentUri.searchParams.get('code');
  const state = currentUri.searchParams.get('state');
  if (currentUri.pathname !== '/' || !code || !state) {
    return false;
  }

  const callbackUri = new URL('/api/v1/auth/redirect', window.location.origin);
  callbackUri.searchParams.set('code', code);
  callbackUri.searchParams.set('state', state);
  window.location.replace(callbackUri.href);
  return true;
}

if (!redirectAuthorizationResponse()) {
  createApp(App)
    .use(createPinia())
    .use(router)
    .use(i18n)
    .mount('#app');
}
