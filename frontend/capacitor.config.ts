import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'br.com.radianciasistemas.ks.app',
  appName: 'Radiância KS',
  webDir: 'dist/frontend/browser',
  server: {
    androidScheme: 'http',
    cleartext: true,
  },
};

export default config;
