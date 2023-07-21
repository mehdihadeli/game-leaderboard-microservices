import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { UserProfileResponse } from '@app/core/dtos/user-profile';
import { AuthenticationService } from '@app/core/services';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styles: [],
})
export class HomeComponent implements OnInit {
  userDetails?: UserProfileResponse;
  loading: boolean = false;

  constructor(private router: Router, private service: AuthenticationService) {}

  ngOnInit() {
    this.loading = true;
    this.service.getUserProfile().subscribe(
      (res) => {
        this.userDetails = res;
        this.loading = false;
      },
      (err) => {
        console.log(err);
      }
    );
  }

  onLogout() {
    localStorage.removeItem('token');
    this.router.navigate(['/user/login']);
  }
}
