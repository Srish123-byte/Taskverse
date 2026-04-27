import { Injectable } from '@angular/core';

@Injectable()
export class LocationStrategyService {
  private static shouldSkip = false;
  private static blackListed: string[] = [];

  init(): void {
    window.onpopstate = this.overridingPopState;
  }

  overridingPopState(event: any): void {
    const isBlackListed = (navigatingTo: string): boolean => {
      navigatingTo = navigatingTo.split('?')[0];
      const pathFound = LocationStrategyService.blackListed.find(route => navigatingTo.endsWith(route));
      return pathFound !== undefined && pathFound.length > 0;
    };

    if (!LocationStrategyService.shouldSkip && isBlackListed(event.currentTarget.location.pathname)) {
      window.history.forward();
      LocationStrategyService.shouldSkip = isBlackListed(window.location.pathname);
    } else {
      LocationStrategyService.shouldSkip = false;
    }
  }
}
