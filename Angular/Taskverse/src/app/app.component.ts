import { Component, OnInit, Renderer2, Inject } from '@angular/core';
import { Router } from '@angular/router';
import { AppConfig } from './app.config';
import { Meta } from '@angular/platform-browser';
import { DOCUMENT } from '@angular/common';
import { LocationStrategyService } from './common/services/utilities/location-strategy.service';
import { SessionActivityService } from './common/services/session/session-activity.service';

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
    private readonly locationStrategyService: LocationStrategyService,
    private readonly sessionActivityService: SessionActivityService,
    private readonly router: Router
  ) {}

  get isLoginPage(): boolean {
    return this.router.url === '/login' || this.router.url === '/';
  }

  ngOnInit(): void {
    this.locationStrategyService.init();
    this.sessionActivityService.init();
    this.addCspMetaTag();
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
