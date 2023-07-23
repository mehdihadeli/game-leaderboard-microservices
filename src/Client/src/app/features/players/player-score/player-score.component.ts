import { Component, OnDestroy, OnInit } from '@angular/core';
import { PlayerScoreWithNeighborsDto } from '@app/core/dtos/player-score-dto';
import { SignalRService } from '@app/core/services/signalr.service';

//https://referbruv.com/blog/how-to-use-signalr-with-asp-net-core-angular/

@Component({
  selector: 'app-player-score',
  templateUrl: './player-score.component.html',
  styleUrls: ['./player-score.component.scss'],
})
export class PlayerScoreComponent implements OnInit, OnDestroy {
  hubHelloMessage?: string;
  playerScoreWithNeighborsDto?: PlayerScoreWithNeighborsDto;

  constructor(public signalrService: SignalRService) {}

  ngOnInit(): void {
    this.signalrService.listenToHelloClient();
    this.signalrService.listenToInitialPlayerScoreForClient();
    this.signalrService.listenToUpdatePlayerScoreForClient();

    // receive message from signalr server
    this.signalrService.hubHelloMessageSubject.subscribe(
      (hubHelloMessage: string) => {
        this.hubHelloMessage = hubHelloMessage;
      }
    );

    this.signalrService.hubUpdatePlayerScoreSubject.subscribe(
      (message: PlayerScoreWithNeighborsDto) => {
        this.playerScoreWithNeighborsDto = message;
      }
    );

    this.signalrService.hubInitialPlayerScoreSubject.subscribe(
      (message: PlayerScoreWithNeighborsDto) => {
        this.playerScoreWithNeighborsDto = message;
      }
    );

    // call hub service when connection is ready
    this.signalrService.initiateSignalrConnection().then(() => {
      this.signalrService.getCurrentPlayerScoreFromServer();
    });
  }

  ngOnDestroy(): void {}
}
