import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '@environments/environment';
import { UserProfileResponse } from '../dtos/user-profile';
import { LoginRequest } from '../dtos/login-request';
import { LoginResponse } from '../dtos/login-response';

@Injectable({ providedIn: 'root' })
export class AuthenticationService {
  private loginResponseSubject: BehaviorSubject<LoginResponse | null>;
  public loginResponse: Observable<LoginResponse | null>;
  private accountsBaseAddress: string;

  constructor(private router: Router, private http: HttpClient) {
    this.loginResponseSubject = new BehaviorSubject(
      JSON.parse(localStorage.getItem('loginResponse')!)
    );
    this.loginResponse = this.loginResponseSubject.asObservable();

    this.accountsBaseAddress = `${environment.apiUrl}/accounts`;
  }

  public get tokenValue(): string | undefined {
    return this.loginResponseSubject.value?.token;
  }

  login(loginRequest: LoginRequest): Observable<LoginResponse> {
    let res = this.http
      .post<LoginResponse>(`${this.accountsBaseAddress}/login`, loginRequest)
      .pipe(
        map((loginResponse: LoginResponse) => {
          // store user details and jwt token in local storage to keep user logged in between page refreshes
          localStorage.setItem('loginResponse', JSON.stringify(loginResponse));
          this.loginResponseSubject.next(loginResponse);

          return loginResponse;
        })
      );

    return res;
  }

  getUserProfile(): Observable<UserProfileResponse> {
    return this.http.get<UserProfileResponse>(
      `${this.accountsBaseAddress}/profile`
    );
  }

  logout(): Observable<void> {
    // remove user from local storage to log user out
    localStorage.removeItem('loginResponse');
    this.loginResponseSubject.next(null);

    let observable = this.http.post<void>(
      `${this.accountsBaseAddress}/logout`,
      null
    );

    this.router.navigate(['accounts/login']);

    return observable;
  }
}
