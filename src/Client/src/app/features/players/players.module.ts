import { NgModule } from '@angular/core';
import { PlayersRoutingModule } from './players.routing';
import { CommonModule } from '@angular/common';
import { RegistrationComponent } from './registration/registration.component';
import { ReactiveFormsModule } from '@angular/forms';
import { PlayerScoreComponent } from './player-score/player-score.component';

@NgModule({
  declarations: [RegistrationComponent, PlayerScoreComponent],
  imports: [CommonModule, ReactiveFormsModule, PlayersRoutingModule],
  exports: [],
  providers: [],
})
export class PlayersModule {}
