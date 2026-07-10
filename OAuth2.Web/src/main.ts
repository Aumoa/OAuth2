import { createApp } from 'vue'
import 'material-symbols/outlined.css'
import './style.css'
import './core/style.css'
import App from './App.vue'
import { i18n } from './i18n'

createApp(App)
  .use(i18n)
  .mount('#app')
