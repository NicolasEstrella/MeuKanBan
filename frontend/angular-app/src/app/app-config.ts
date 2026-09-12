import { InjectionToken, inject, provideAppInitializer } from '@angular/core';

/**
 * Configuracao de runtime do frontend, carregada de assets/config.json antes
 * do bootstrap. Nenhum valor de ambiente (URL da API, credenciais) e embutido
 * em build time: o container gera config.json a partir de config.json.template
 * via envsubst com a variavel API_URL. Em desenvolvimento nativo (ng serve),
 * o config.json versionado carrega o default local.
 */
export interface AppConfig {
  apiUrl: string;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

export function provideAppConfig() {
  return [
    { provide: APP_CONFIG, useValue: { apiUrl: '' } satisfies AppConfig },
    provideAppInitializer(async () => {
      const config = inject(APP_CONFIG);
      const response = await fetch('assets/config.json');
      if (!response.ok) {
        throw new Error(
          `Falha ao carregar assets/config.json (status ${response.status}). ` +
            'A configuracao de runtime e obrigatoria para inicializar a aplicacao.'
        );
      }
      const loaded = (await response.json()) as AppConfig;
      if (!loaded.apiUrl) {
        throw new Error('assets/config.json nao define "apiUrl".');
      }
      config.apiUrl = loaded.apiUrl;
    }),
  ];
}
