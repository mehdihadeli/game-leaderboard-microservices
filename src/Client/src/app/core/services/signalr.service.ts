import { Injectable } from '@angular/core';
import { BehaviorSubject, from } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from 'src/environments/environment';
import { AuthenticationService } from './authentication.service';

//https://mfcallahan.blog/2020/11/05/how-to-implement-signalr-in-a-net-core-angular-web-application/
//https://code-maze.com/netcore-signalr-angular-realtime-charts/

@Injectable({
  providedIn: 'root',
})
export class SignalRService {
  connection!: signalR.HubConnection;
  hubHelloMessage: BehaviorSubject<string>;
  progressPercentage: BehaviorSubject<number>;
  progressMessage: BehaviorSubject<string>;
  public connectionId: string = '';

  constructor(private authenticationService: AuthenticationService) {
    this.hubHelloMessage = new BehaviorSubject<string>('');
    this.progressPercentage = new BehaviorSubject<number>(0);
    this.progressMessage = new BehaviorSubject<string>('');
  }

  // Establish a connection to the SignalR server hub
  public initiateSignalrConnection(): Promise<void> {
    this.connection = new signalR.HubConnectionBuilder()
      //https://stackoverflow.com/questions/55839073/signalr-connection-with-accesstokenfactory-on-js-client-doesnt-connect-with-con
      //https://learn.microsoft.com/en-us/aspnet/core/signalr/authn-and-authz?view=aspnetcore-7.0#bearer-token-authentication
      .withUrl(environment.signalrHubUrl, {
        accessTokenFactory: async () => {
          let token = this.authenticationService.tokenValue;

          return token as string;
        },
      })
      .build();

    this.setSignalRClientMethods();

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

    //https://stackoverflow.com/questions/39319279/convert-promise-to-observable
    const connectionObservable = from(connectionPromise);
    connectionObservable.subscribe();

    return connectionPromise;
  }

  private getConnectionId = () => {
    this.connection.invoke('getConnectionId').then((data) => {
      console.log(data);
      this.connectionId = data;
    });
  };

  // This method will implement the methods defined in the IPlayerScoreClient interface in the LeaderBoard.SignalR.Hubs .NET solution
  // This will setup the client side methods that the server can call.
  private setSignalRClientMethods(): void {
    this.connection.on('HelloClient', (message: string) => {
      this.hubHelloMessage.next(message);
    });

    this.connection.on('GetMessage', async () => {
      let promise = new Promise((resolve, reject) => {
        resolve('hello world!');
      });
      return promise;
    });

    this.connection.on('UpdateProgressBar', (percentage: number) => {
      this.progressPercentage.next(percentage);
    });

    this.connection.on('DisplayProgressMessage', (message: string) => {
      this.progressMessage.next(message);
    });
  }
}
