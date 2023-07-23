import { NgModule } from '@angular/core';
import { AccountsRoutingModule } from './accounts.routing';
import { LoginComponent } from './login/login.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@NgModule({
  declarations: [LoginComponent],
  imports: [
    RouterModule,
    FormsModule,
    CommonModule,
    ReactiveFormsModule,
    AccountsRoutingModule,
  ],
  exports: [],
  providers: [],
})
export class AccountsModule {}
