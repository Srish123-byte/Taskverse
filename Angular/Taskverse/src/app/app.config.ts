import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../environments/environment';

export function loadConfigurationSettings(appConfig: AppConfig) {
  return appConfig.load();
}

@Injectable()
export class AppConfig {
  private configurationFilename = environment.configFileName;
  private config: any | null = null;
  private configLoaded = false;

  constructor(private readonly http: HttpClient) {}

  get api_url(): string {
    return this.config?.['api_url'] || '';
  }

  get cspMetaTag(): string {
    return this.config?.['csp_meta_tag'] || '';
  }

  get localCspMetaTag(): string {
    return this.config?.['local_csp_meta_tag'] || '';
  }

  get supportPhone(): string {
    return this.config?.['support_phone'] || '';
  }

  get supportEmail(): string {
    return this.config?.['support_email'] || '';
  }

  get isConfigLoaded(): boolean {
    return this.configLoaded;
  }

  public load(): Promise<any> {
    return new Promise((resolve, reject) => {
      this.http
        .get(this.configurationFilename)
        .subscribe({
          next: (responseData: any) => {
            this.config = responseData;
            this.configLoaded = true;
            resolve(true);
          },
          error: (error: HttpErrorResponse) => {
            this.configLoaded = false;
            reject(error);
          }
        });
    });
  }
}
