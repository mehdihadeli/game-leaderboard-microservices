namespace LeaderBoard.SharedKernel.EventStoreDB;

// https://developers.eventstore.com/clients/dotnet/21.2/#connect-to-eventstoredb
// https://developers.eventstore.com/clients/http-api/v5
// https://developers.eventstore.com/clients/grpc/
// https://developers.eventstore.com/server/v20.10/networking.html#http-configuration
public class EventStoreDBOptions
{
    public bool UseInternalCheckpointing { get; set; } = true;
    public string Host { get; set; } = default!;

    // HTTP is the primary protocol for EventStoreDB. It is used in gRPC communication and HTTP APIs (management, gossip and diagnostics).
    public int HttpPort { get; set; } = 2113;
    public int TcpPort { get; set; } = 1113;

    // https://developers.eventstore.com/server/v20.10/networking.html#http-configuration
    // https://developers.eventstore.com/clients/grpc/#creating-a-client
    public string GrpcConnectionString => $"esdb://{Host}:{HttpPort}?tls=false";

    // https://developers.eventstore.com/clients/dotnet/21.2/#connect-to-eventstoredb
    // https://developers.eventstore.com/server/v20.10/networking.html#external
    public string TcpConnectionString => $"tcp://{Host}:{TcpPort}?tls=false";
}
