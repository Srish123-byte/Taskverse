import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

export interface ProctoringDeviceDetails {
  browserName: string;
  browserVersion: string;
  operatingSystem: string;
  deviceType: string;
  userAgent: string;
  ipAddress: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class DeviceInformationService {
  constructor(private readonly http: HttpClient) {}

  isMobile(): boolean {
    return /Mobi|Android/i.test(navigator.userAgent);
  }

  isTablet(): boolean {
    return /Tablet|iPad/i.test(navigator.userAgent);
  }

  isDesktop(): boolean {
    return !this.isMobile() && !this.isTablet();
  }

  getProctoringDeviceDetails(): Observable<ProctoringDeviceDetails> {
    const userAgent = navigator.userAgent ?? '';
    const { browserName, browserVersion } = this.resolveBrowser(userAgent);

    return this.resolveIpAddress().pipe(
      map(ipAddress => ({
        browserName,
        browserVersion,
        operatingSystem: this.resolveOperatingSystem(userAgent),
        deviceType: this.resolveDeviceType(),
        userAgent,
        ipAddress
      }))
    );
  }

  getVisibilityState(): 'Visible' | 'Hidden' | 'Unknown' {
    if (typeof document === 'undefined') {
      return 'Unknown';
    }

    if (document.visibilityState === 'hidden') {
      return 'Hidden';
    }

    if (document.visibilityState === 'visible') {
      return 'Visible';
    }

    return 'Unknown';
  }

  isFullscreenActive(): boolean {
    if (typeof document === 'undefined') {
      return false;
    }

    return !!document.fullscreenElement;
  }

  getNetworkStatus(): 'Online' | 'Offline' | 'Unknown' {
    if (typeof navigator === 'undefined' || typeof navigator.onLine !== 'boolean') {
      return 'Unknown';
    }

    return navigator.onLine ? 'Online' : 'Offline';
  }

  private resolveIpAddress(): Observable<string | null> {
    return this.http
      .get<{ ip?: string }>('https://api64.ipify.org?format=json')
      .pipe(
        map(response => response.ip?.trim() || null),
        catchError(() => of(null))
      );
  }

  private resolveDeviceType(): string {
    if (this.isTablet()) {
      return 'Tablet';
    }

    if (this.isMobile()) {
      return 'Mobile';
    }

    return 'Desktop';
  }

  private resolveOperatingSystem(userAgent: string): string {
    if (/Windows NT/i.test(userAgent)) {
      return 'Windows';
    }

    if (/Android/i.test(userAgent)) {
      return 'Android';
    }

    if (/(iPhone|iPad|iPod)/i.test(userAgent)) {
      return 'iOS';
    }

    if (/Mac OS X|Macintosh/i.test(userAgent)) {
      return 'macOS';
    }

    if (/Linux/i.test(userAgent)) {
      return 'Linux';
    }

    return 'Unknown';
  }

  private resolveBrowser(userAgent: string): { browserName: string; browserVersion: string } {
    const browserMatchers: { name: string; pattern: RegExp }[] = [
      { name: 'Edge', pattern: /Edg\/([0-9.]+)/i },
      { name: 'Opera', pattern: /OPR\/([0-9.]+)/i },
      { name: 'Chrome', pattern: /Chrome\/([0-9.]+)/i },
      { name: 'Firefox', pattern: /Firefox\/([0-9.]+)/i },
      { name: 'Safari', pattern: /Version\/([0-9.]+).*Safari/i }
    ];

    for (const matcher of browserMatchers) {
      const match = userAgent.match(matcher.pattern);
      if (match) {
        return {
          browserName: matcher.name,
          browserVersion: match[1] ?? 'Unknown'
        };
      }
    }

    return {
      browserName: 'Unknown',
      browserVersion: 'Unknown'
    };
  }
}
