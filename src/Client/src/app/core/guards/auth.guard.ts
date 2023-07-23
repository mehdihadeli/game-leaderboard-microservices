import { Injectable } from '@angular/core';
import {
  Router,
  CanActivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { AuthenticationService } from '../services';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(
    private router: Router,
    private authenticationService: AuthenticationService
  ) {}
  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ):
    | boolean
    | UrlTree
    | Observable<boolean | UrlTree>
    | Promise<boolean | UrlTree> {
    // Stage 1: check user authentication
    if (!this.authenticationService.isAuthenticated) {
      this.authenticationService.logout();

      // not logged in so redirect to login page with the return url
      this.router.navigate(['accounts/login'], {
        queryParams: { returnUrl: state.url },
      });

      return false;
    }
    const validRoles = route.data['authorities'] || [];
    const token = this.authenticationService.tokenValue;
    if (token) {
      return true;
    }
    // // Stage 2: Check user role
    // // Condition for multiple role
    // // (!validRoles.some((r: string) => userData?.userInfo?.role.include(r)))
    // if (!validRoles.some((r: string) => r === userData?.userInfo?.role)) {
    //   // this.router.navigate(['/error/403']); // Best place to send user
    //   this.router.navigate(['/']); // For this example case
    //   return false;
    // }
    return false;
  }
}
