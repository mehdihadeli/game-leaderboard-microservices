import { NgModule } from '@angular/core';
import { PlayersRoutingModule } from './players.routing';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { PlayerScoreComponent } from './player-score/player-score.component';
import { CreatePlayerComponent } from './create-player/create-player.component';
import { RouterModule } from '@angular/router';

@NgModule({
  declarations: [PlayerScoreComponent, CreatePlayerComponent],
  imports: [
    RouterModule,
    FormsModule,
    CommonModule,
    ReactiveFormsModule,
    PlayersRoutingModule,
  ],
  exports: [],
  providers: [],
})
export class PlayersModule {}
