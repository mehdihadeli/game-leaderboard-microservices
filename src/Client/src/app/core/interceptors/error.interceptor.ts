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
        const status = err?.status;

        if ([401, 403].includes(err.status)) {
          // auto logout if 401 Unauthorized or 403 Forbidden response returned from api
          this.authenticationService.logout();
        }

        const error = err.error;
        let message = err.message;

        if (typeof error === 'object') {
          const problem = err as ProblemDetails;
          // Check if error response body is of type ProblemDetails
          if (problem.detail && problem.status) {
            // handle ProblemDetails error
            err.error.detail || err.error.exception.details;
            message = problem.detail ?? problem.title ?? 'Unexpected error';
          } else {
            const keys = Object.keys(error);
            if (keys.some((item) => item === 'message')) {
              message = error.message;
            }
          }
        } else if (typeof error === 'string') {
          message = error;
        }

        this.toastr.error(message);

        return throwError({ message, status });
      })
    );
  }
}
