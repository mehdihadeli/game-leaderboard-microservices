import { NgModule } from '@angular/core';
import { HomeRoutingModule } from './home.routing';
import { HomeComponent } from './home.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

@NgModule({
  declarations: [HomeComponent],
  imports: [RouterModule, FormsModule, CommonModule, HomeRoutingModule],
  exports: [],
  providers: [],
})
export class HomeModule {}
