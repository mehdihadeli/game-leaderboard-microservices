import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { AuthenticationService } from '../services';

//https://techincent.com/angular-jwt-auth/

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  constructor(private authenticationService: AuthenticationService) {}

  intercept(
    request: HttpRequest<any>,
    next: HttpHandler
  ): Observable<HttpEvent<any>> {
    // add auth header with jwt if user is logged in and request is to the api url
    const token = this.authenticationService.tokenValue;
    const isApiUrl = request.url.startsWith(environment.apiUrl);
    const isSignalRUrl = request.url.startsWith(environment.signalrHubUrl);

    // Checking access_token exists(mean user logged in) or not
    if (token && (isApiUrl || isSignalRUrl)) {
      if (this.authenticationService.isAuthTokenValid(token)) {
        request = request.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`,
          },
        });
      }
    }

    return next.handle(request);
  }
}
