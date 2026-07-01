import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClientService } from '../http/http-client.service';

export interface SendReportEmailRequest {
  recipients: string[];
  fileName: string;
  fileContentBase64: string;
  subject?: string;
  body?: string;
}

@Injectable({ providedIn: 'root' })
export class ReportEmailService {
  private readonly url = 'reports/send-email';

  constructor(private readonly http: HttpClientService) {}

  sendEmail(request: SendReportEmailRequest): Observable<void> {
    return this.http.post<void>(this.url, request);
  }

  parseRecipients(raw: string): string[] {
    return raw
      .split(/[\s,;]+/)
      .map(e => e.trim())
      .filter(e => e.includes('@'));
  }
}
