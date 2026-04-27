import { Inject, Injectable } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { Renderer2, RendererFactory2 } from '@angular/core';
import { AppConfig } from '../../app.config';

declare global {
  interface Window {
    dataLayer: any[];
  }
}

@Injectable({
  providedIn: 'root'
})
export class GtmService {
  private renderer: Renderer2;

  constructor(
    private readonly appConfig: AppConfig,
    private readonly rendererFactory: RendererFactory2,
    @Inject(DOCUMENT) private readonly document: Document
  ) {
    this.renderer = this.rendererFactory.createRenderer(null, null);
    window.dataLayer = window.dataLayer || [];
  }

  public loadGtmScript(): void {
    const gtmEnabled = this.appConfig.gtmEnabled;
    const gtmId = this.appConfig.gtmId;

    if (!gtmEnabled) {
      return;
    }

    if (!gtmId) {
      console.warn('GTM is enabled, but no GTM ID was provided. Skipping script injection.');
      return;
    }

    const script = this.renderer.createElement('script');
    script.text = `(function(w,d,s,l,i){w[l]=w[l]||[];w[l].push({'gtm.start':
      new Date().getTime(),event:'gtm.js'});var f=d.getElementsByTagName(s)[0],
      j=d.createElement(s),dl=l!='dataLayer'?'&l='+l:'';j.async=true;j.src=
      'https://www.googletagmanager.com/gtm.js?id='+i+dl;f.parentNode.insertBefore(j,f);
      })(window,document,'script','dataLayer','${gtmId}');`;
    this.renderer.appendChild(this.document.head, script);

    const noscript = this.renderer.createElement('noscript');
    const iframe = this.renderer.createElement('iframe');
    this.renderer.setAttribute(iframe, 'src', `https://www.googletagmanager.com/ns.html?id=${gtmId}`);
    this.renderer.setAttribute(iframe, 'height', '0');
    this.renderer.setAttribute(iframe, 'width', '0');
    this.renderer.setStyle(iframe, 'display', 'none');
    this.renderer.setStyle(iframe, 'visibility', 'hidden');
    this.renderer.appendChild(noscript, iframe);
    this.renderer.insertBefore(this.document.body, noscript, this.document.body.firstChild);
  }

  public pushEvent(event: object): void {
    if (this.appConfig.gtmEnabled) {
      window.dataLayer.push(event);
    }
  }
}
