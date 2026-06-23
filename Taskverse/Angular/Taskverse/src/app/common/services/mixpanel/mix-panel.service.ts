import { Injectable } from '@angular/core';
import { MixpanelEvents } from './mixpanel-events';

@Injectable({
  providedIn: 'root'
})
export class MixPanelService {
  private pageLoadTime = Date.now();

  track(eventName: MixpanelEvents, properties: object = {}): void {
    // Mixpanel tracking placeholder
    console.log('Track event:', eventName, properties);
  }

  createBaseProperties(stepDuration: number): any {
    return { step_duration_ms: stepDuration };
  }

  resetPageLoadTime(): void {
    this.pageLoadTime = Date.now();
  }
}
