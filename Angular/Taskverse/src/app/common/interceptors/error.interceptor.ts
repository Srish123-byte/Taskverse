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
  constructor(private readonly router: Router) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (ConstantsService.errorCodes.includes(error.status)) {
          this.router.navigate(['/error']);
        }
        return throwError(() => error);
      })
    );
  }
}
