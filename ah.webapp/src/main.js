import '@fontsource/ibm-plex-sans/400.css';
import '@fontsource/ibm-plex-sans/500.css';
import '@fontsource/ibm-plex-sans/600.css';
import '@fontsource/ibm-plex-mono/400.css';
import '@fontsource/ibm-plex-mono/500.css';
import './main.css';

import { createPinia } from 'pinia';
import { createApp } from 'vue';
import App from './App.vue';
import { i18n, setLocale } from '@/i18n';
import router from '@/router';
import { currentTheme, setTheme } from '@/theme';

setTheme(currentTheme.value);
setLocale(i18n.global.locale.value);

const app = createApp(App);

app.config.errorHandler = (error, _instance, info) => {
    console.error(`[AstraHive] Unhandled error (${info})`, error);
};

app.use(createPinia());
app.use(i18n);
app.use(router);

app.mount('#app');
