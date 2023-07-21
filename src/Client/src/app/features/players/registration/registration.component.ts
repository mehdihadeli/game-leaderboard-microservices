import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { first } from 'rxjs/operators';
import { AuthenticationService, PlayerService } from '@app/core/services';
import { CreatePlayerRequest } from '@app/core/dtos/create-player-request';
import { ToastrService } from 'ngx-toastr';

@Component({ templateUrl: 'registration.component.html' })
export class RegistrationComponent implements OnInit {
  registerForm!: FormGroup;
  loading = false;
  submitted = false;

  constructor(
    private formBuilder: FormBuilder,
    private router: Router,
    private authenticationService: AuthenticationService,
    private playerService: PlayerService,
    private toastr: ToastrService
  ) {
    // redirect to home if already logged in
    if (this.authenticationService.tokenValue) {
      this.router.navigate(['/']);
    }
  }

  ngOnInit() {
    this.registerForm = this.formBuilder.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      userName: ['', Validators.required],
      email: ['', Validators.required],
      country: ['', Validators.required],
      password: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  // convenience getter for easy access to form fields
  get f() {
    return this.registerForm.controls;
  }

  onSubmit() {
    this.submitted = true;

    // stop here if form is invalid
    if (this.registerForm.invalid) {
      return;
    }

    const country = this.registerForm.get('country')?.value;
    const email = this.registerForm.get('email')?.value;
    const userName = this.registerForm.get('userName')?.value;
    const firstName = this.registerForm.get('firstName')?.value;
    const lastName = this.registerForm.get('lastName')?.value;
    const password = this.registerForm.get('password')?.value;

    let createPlayerRequest: CreatePlayerRequest = {
      country: country,
      email: email,
      firstName: firstName,
      lastName: lastName,
      password: password,
      userName: userName,
    };

    this.loading = true;
    this.playerService
      .register(createPlayerRequest)
      .pipe(first())
      .subscribe(
        (data) => {
          this.toastr.success('New user created!', 'Registration successful.');
          this.router.navigate(['/login']);
        },
        (error) => {
          this.toastr.error(error, 'Registration failed.');
          this.loading = false;
        }
      );
  }
}
