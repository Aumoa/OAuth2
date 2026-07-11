import { createApp } from 'vue'
import { createPinia } from 'pinia'
import 'material-symbols/outlined.css'
import './style.css'
import './core/style.css'
import App from './App.vue'
import { i18n } from './i18n'
import { router } from './router'

createApp(App)
  .use(createPinia())
  .use(router)
  .use(i18n)
  .mount('#app')
