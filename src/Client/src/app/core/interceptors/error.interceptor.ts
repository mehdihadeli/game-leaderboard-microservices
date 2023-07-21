import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthenticationService } from '../services';
import { ProblemDetails } from '../models/problem-details';
import { ToastrService } from 'ngx-toastr';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(
    private authenticationService: AuthenticationService,
    private toastr: ToastrService
  ) {}

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      catchError((err) => {
        if ([401, 403].includes(err.status)) {
          // auto logout if 401 Unauthorized or 403 Forbidden response returned from api
          this.authenticationService.logout();
        }

        // Check if error response body is of type ProblemDetails
        if (err.error && typeof err.error === 'object' && 'type' in err.error) {
          const problem = err.error as ProblemDetails;

          if (problem.detail && problem.status) {
            // handle ProblemDetails error
            this.toastr.error(
              problem.detail ?? problem.title ?? 'Unexpected error'
            );
          } else {
            if (err?.message) {
              this.toastr.error(err.message);
            }
          }
        } else {
          // handle other errors
          this.toastr.error('Unexpected error', err.message);
        }

        const error = err.error.detail || err.error.exception.details;
        return throwError(error);
      })
    );
  }
}
