import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject, from } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from 'src/environments/environment';
import { AuthenticationService } from './authentication.service';
import { PlayerScoreWithNeighborsDto } from '../dtos/player-score-dto';

//https://mfcallahan.blog/2020/11/05/how-to-implement-signalr-in-a-net-core-angular-web-application/
//https://code-maze.com/netcore-signalr-angular-realtime-charts/
//https://referbruv.com/blog/how-to-use-signalr-with-asp-net-core-angular/
//https://code-maze.com/how-to-send-client-specific-messages-using-signalr/
//https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client?view=aspnetcore-7.0&tabs=visual-studio

@Injectable({
  providedIn: 'root',
})
export class SignalRService {
  connection!: signalR.HubConnection;
  hubHelloMessageSubject: BehaviorSubject<string>;
  hubUpdatePlayerScoreSubject: Subject<PlayerScoreWithNeighborsDto>;
  hubInitialPlayerScoreSubject: Subject<PlayerScoreWithNeighborsDto>;
  public connectionId: string = '';

  constructor(private authenticationService: AuthenticationService) {
    this.hubHelloMessageSubject = new BehaviorSubject<string>('');
    this.hubUpdatePlayerScoreSubject =
      new Subject<PlayerScoreWithNeighborsDto>();
    this.hubInitialPlayerScoreSubject =
      new Subject<PlayerScoreWithNeighborsDto>();

    this.connection = new signalR.HubConnectionBuilder()
      //https://stackoverflow.com/questions/55839073/signalr-connection-with-accesstokenfactory-on-js-client-doesnt-connect-with-con
      //https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-7.0#bearer-token-authentication
      .withUrl(environment.signalrHubUrl, {
        accessTokenFactory: async () => {
          let token = this.authenticationService.tokenValue;
          return token as string;
        },
      })
      //.withAutomaticReconnect()
      .build();

    // manually reconnecting
    this.connection.onclose(async () => {
      await this.initiateSignalrConnection();
    });
  }

  // Establish a connection to the SignalR server hub
  public async initiateSignalrConnection(): Promise<void> {
    let connectionPromise = this.connection
      .start()
      .then(() => this.getConnectionId())
      .then(() => {
        console.log(
          `SignalR connection success! connectionId: ${this.connection.connectionId}`
        );
      })
      .catch((error: any) => {
        console.log(`SignalR connection error: ${error}`);
      });

    return await connectionPromise;
  }

  public getConnectionId = () => {
    this.connection.invoke('getConnectionId').then((data) => {
      console.log(data);
      this.connectionId = data;
    });
  };

  // This method will implement the methods defined in the IPlayerScoreClient interface in the LeaderBoard.SignalR.Hubs .NET solution
  // This will setup the client side methods that the server can call.
  public listenToHelloClient(): void {
    this.connection.on('HelloClient', (message: string) => {
      this.hubHelloMessageSubject.next(message);
    });
  }

  public listenToUpdatePlayerScoreForClient(): void {
    // called from signalr server
    this.connection.on(
      'UpdatePlayerScoreForClient',
      (message: PlayerScoreWithNeighborsDto) => {
        console.log(message);
        this.hubUpdatePlayerScoreSubject.next(message);
      }
    );
  }

  public listenToInitialPlayerScoreForClient(): void {
    // called from signalr server
    this.connection.on(
      'InitialPlayerScoreForClient',
      (message: PlayerScoreWithNeighborsDto) => {
        console.log(message);
        this.hubInitialPlayerScoreSubject.next(message);
      }
    );
  }

  public getCurrentPlayerScoreFromServer(): void {
    // send message to signalr server
    // invoke method wait for return result, but send method doesn't wait for return value
    this.connection
      .invoke('GetCurrentPlayerScoreFromServer')
      .catch((error: any) => {
        console.log(`GetCurrentPlayerScoreFromServer() error: ${error}`);
        alert(
          'GetCurrentPlayerScoreFromServer() error!, see console for details.'
        );
      });
  }

  public helloWithConnectionFromServer(): void {
    // send message to signalr server
    this.connection
      .invoke('HelloWithConnectionFromServer', this.connectionId)
      .catch((error: any) => {
        console.log(`HelloWithConnectionFromServer() error: ${error}`);
        alert(
          'HelloWithConnectionFromServer() error!, see console for details.'
        );
      });
  }

  public helloFromServer(): void {
    // send message to signalr server
    this.connection.invoke('HelloFromServer').catch((error: any) => {
      console.log(`HelloFromServer() error: ${error}`);
      alert('HelloFromServer() error!, see console for details.');
    });
  }
}
