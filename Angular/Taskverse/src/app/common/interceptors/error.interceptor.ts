import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { ConstantsService } from '../services/utilities/constants.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  private static readonly bypassHeader = 'X-Skip-Global-Error-Redirect';

  constructor(private readonly router: Router) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const shouldBypassRedirect = req.headers.has(ErrorInterceptor.bypassHeader);
    const sanitizedRequest = shouldBypassRedirect
      ? req.clone({ headers: req.headers.delete(ErrorInterceptor.bypassHeader) })
      : req;

    return next.handle(sanitizedRequest).pipe(
      catchError((error: HttpErrorResponse) => {
        if (!shouldBypassRedirect && ConstantsService.errorCodes.includes(error.status)) {
          this.router.navigate(['/error']);
        }
        return throwError(() => error);
      })
    );
  }
}
