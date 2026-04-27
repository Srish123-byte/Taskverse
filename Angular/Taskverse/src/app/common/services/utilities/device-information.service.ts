import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DeviceInformationService {
  constructor() {}

  isMobile(): boolean {
    return /Mobi|Android/i.test(navigator.userAgent);
  }

  isTablet(): boolean {
    return /Tablet|iPad/i.test(navigator.userAgent);
  }

  isDesktop(): boolean {
    return !this.isMobile() && !this.isTablet();
  }
}
