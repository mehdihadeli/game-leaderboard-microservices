import { NgModule } from '@angular/core';
import { HomeRoutingModule } from './home.routing';
import { HomeComponent } from './home.component';
import { CommonModule } from '@angular/common';

@NgModule({
  declarations: [HomeComponent],
  imports: [CommonModule, HomeRoutingModule],
  exports: [],
  providers: [],
})
export class HomeModule {}
