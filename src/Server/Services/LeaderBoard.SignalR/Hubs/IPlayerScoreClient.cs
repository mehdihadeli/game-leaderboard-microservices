namespace LeaderBoard.SignalR.Hubs;

// We also have communication the other way i.e. server-to-client. For this, we create interface representing the client and the things it will listen for.
// https://learn.microsoft.com/en-us/aspnet/core/signalr/hubcontext
public interface IPlayerScoreClient
{
    Task HelloClient(string message);
}
