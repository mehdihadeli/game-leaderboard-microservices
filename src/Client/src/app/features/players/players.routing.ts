import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { RegistrationComponent } from './registration/registration.component';
import { PlayerScoreComponent } from './player-score/player-score.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'player-score',
    pathMatch: 'full',
  },
  {
    path: 'player-score',
    component: PlayerScoreComponent,
  },
  {
    path: 'register',
    component: RegistrationComponent,
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PlayersRoutingModule {}
