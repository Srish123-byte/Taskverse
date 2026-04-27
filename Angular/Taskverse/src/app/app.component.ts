import { Component, OnInit, Renderer2, Inject } from '@angular/core';
import { AppConfig } from './app.config';
import { Meta } from '@angular/platform-browser';
import { DOCUMENT } from '@angular/common';
import { GtmService } from './common/services/gtm.service';
import { LocationStrategyService } from './common/services/utilities/location-strategy.service';

@Component({
  selector: 'app-root',
  standalone: false,
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'Taskverse';

  constructor(
    private readonly appConfig: AppConfig,
    private readonly meta: Meta,
    private readonly renderer: Renderer2,
    @Inject(DOCUMENT) private readonly document: Document,
    private readonly gtmService: GtmService,
    private readonly locationStrategyService: LocationStrategyService
  ) {}

  ngOnInit(): void {
    this.locationStrategyService.init();
    this.addCspMetaTag();
    this.gtmService.loadGtmScript();
  }

  addCspMetaTag(): void {
    const cspContent = this.appConfig.api_url.includes('localhost')
      ? this.appConfig.localCspMetaTag
      : this.appConfig.cspMetaTag;

    if (!cspContent) {
      return;
    }

    const metaTag = this.renderer.createElement('meta');
    this.renderer.setAttribute(metaTag, 'http-equiv', 'Content-Security-Policy');
    this.renderer.setAttribute(metaTag, 'content', cspContent);
    this.renderer.appendChild(this.document.head, metaTag);
  }
}
