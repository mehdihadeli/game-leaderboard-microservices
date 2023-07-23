import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { PlayerScoreComponent } from './player-score/player-score.component';
import { AuthGuard } from '@app/core/guards';
import { CreatePlayerComponent } from './create-player/create-player.component';
import { NoAuthGuard } from '@app/core/guards/no-auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'player-score',
    pathMatch: 'full',
  },
  {
    path: 'player-score',
    component: PlayerScoreComponent,
    canActivate: [AuthGuard],
  },
  {
    path: 'create',
    component: CreatePlayerComponent,
    canActivate: [NoAuthGuard],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PlayersRoutingModule {}
