import { NgModule } from '@angular/core';
import { AccountsRoutingModule } from './accounts.routing';
import { LoginComponent } from './login/login.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@NgModule({
  declarations: [LoginComponent],
  imports: [CommonModule, ReactiveFormsModule, AccountsRoutingModule],
  exports: [],
  providers: [],
})
export class AccountsModule {}
