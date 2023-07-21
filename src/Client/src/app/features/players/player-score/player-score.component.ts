import { Component, OnInit } from '@angular/core';
import { SignalRService } from '@app/core/services/signalr.service';

@Component({
  selector: 'app-player-score',
  templateUrl: './player-score.component.html',
  styleUrls: ['./player-score.component.scss'],
})
export class PlayerScoreComponent implements OnInit {
  hubHelloMessage?: string;

  constructor(private signalrService: SignalRService) {}

  ngOnInit(): void {
    // receive message from signalr server
    this.signalrService.hubHelloMessage.subscribe((hubHelloMessage: string) => {
      this.hubHelloMessage = hubHelloMessage;
    });
  }

  helloServerWithConnection(): void {
    // send message to signalr server
    this.signalrService.connection
      .invoke('HelloServerWithConnection', this.signalrService.connectionId)
      .catch((error: any) => {
        console.log(`HelloServerWithConnection() error: ${error}`);
        alert('HelloServerWithConnection() error!, see console for details.');
      });
  }

  helloServer(): void {
    // send message to signalr server
    this.signalrService.connection.invoke('HelloServer').catch((error: any) => {
      console.log(`HelloServer() error: ${error}`);
      alert('HelloServer() error!, see console for details.');
    });
  }
}
