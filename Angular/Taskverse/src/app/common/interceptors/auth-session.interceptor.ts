import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { switchMap, take } from 'rxjs/operators';
import { RoleType } from '../enums/role-type.enum';
import { AuthSessionService } from '../services/session/auth-session.service';
import { Session } from '../services/session/session.service';

@Injectable()
export class AuthSessionInterceptor implements HttpInterceptor {
  private static readonly bypassHeader = 'X-Skip-Auth-Refresh';

  constructor(
    private readonly authSessionService: AuthSessionService,
    private readonly session: Session
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const shouldBypassRefresh = req.headers.has(AuthSessionInterceptor.bypassHeader);
    const sanitizedRequest = shouldBypassRefresh
      ? req.clone({ headers: req.headers.delete(AuthSessionInterceptor.bypassHeader) })
      : req;

    if (shouldBypassRefresh || !this.session.isLoggedIn()) {
      return next.handle(this.attachLatestHeaders(sanitizedRequest));
    }

    return this.authSessionService.ensureActiveAccessToken().pipe(
      take(1),
      switchMap(() => next.handle(this.attachLatestHeaders(sanitizedRequest)))
    );
  }

  private attachLatestHeaders(req: HttpRequest<any>): HttpRequest<any> {
    let headers = req.headers;
    const token = this.session.jwtToken;
    const userId = this.session.userId;
    const role = this.session.role;
    const collegeId = this.session.collegeId;

    if (token) {
      headers = headers.set('Authorization', `Bearer ${token}`);
    }

    if (userId) {
      headers = headers.set('UserId', userId);
    }

    if (role) {
      headers = headers.set('UserRole', role);
    }

    const shouldSendCollegeId =
      role === RoleType.CollegeAdmin ||
      role === RoleType.Trainer ||
      role === RoleType.Student;

    if (shouldSendCollegeId && collegeId) {
      headers = headers.set('CollegeId', collegeId);
    }

    return req.clone({ headers });
  }
}
