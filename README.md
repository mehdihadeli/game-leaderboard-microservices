# Game LeaderBoard Microservices

This project is an implementation of an imaginary [Game LeaderBoard](https://en.wikipedia.org/wiki/Ladder_tournament) application, based on Microservices Architecture, [Event Driven Architecture](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven), [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/), [Event Sourcing](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing) with EventStoreDB, [Redis SortedSet](https://redis.io/docs/data-types/sorted-sets/), [Redis Pub/Sub](https://redis.io/docs/interact/pubsub/), SignalR and .Net 8.

This application capable of handling online calculation of player ranks with using Redis `SortedSet` so it is very fast and capable for handling 1 million request per second.

![](./assets/leaderboard.gif)

## Prerequisites

## Features

- ✅ Using `Vertical Slice Architecture` as a high level architecture
- ✅ Using `Event Driven Architecture` and asynchronous communications on top of RabbitMQ Message Broker and MassTransit
- ✅ Using `Outbox Pattern` for all microservices for [Guaranteed Delivery](https://www.enterpriseintegrationpatterns.com/GuaranteedMessaging.html) or [At-least-once Delivery](https://www.cloudcomputingpatterns.org/at_least_once_delivery/) And Using [Inbox Pattern](https://learn.microsoft.com/en-us/azure/service-bus-messaging/duplicate-detection) for handling [Idempotency](https://www.cloudcomputingpatterns.org/idempotent_processor/) in receiver side and [Exactly-once Delivery](https://www.cloudcomputingpatterns.org/exactly_once_delivery/)
- ✅ Using `CQRS Pattern` on top of `MediatR` library
- ✅ Using `Minimal APIs` for handling requests
- ✅ Using [Redis SortedSet](https://redis.io/docs/data-types/sorted-sets) for calculating player ranks
- ✅ Using [Redis Pub/Sub](https://redis.io/docs/interact/pubsub/) for some of asynchronous communications
- ✅ Using Event Sourcing and [EventStoreDB](https://www.eventstore.com/eventstoredb) as our primary database
- ✅ Using [Postgres](https://www.npgsql.org/efcore/) and Redis as secondary database on top of EventStore [Projections](https://web.archive.org/web/20230128040244/https://zimarev.com/blog/event-sourcing/projections/)
- ✅ Supporting different type of caching strategy like `Read-Through`, `Write-Through`, `Write-Behind`, `Read and Write Cache Aside` on top of `redis` for handling millions of request per second

## Libraries

- ✔️ **[`.NET 8`](https://dotnet.microsoft.com/download)** - .NET Framework and .NET Core, including ASP.NET and ASP.NET Core
- ✔️ **[`StackExchange.Redis`](https://github.com/StackExchange/StackExchange.Redis)** - General purpose redis client
- ✔️ **[`MassTransit`](https://github.com/MassTransit/MassTransit)** - Distributed Application Framework for .NET
- ✔️ **[`EventStore-Client-Dotnet`](https://github.com/EventStore/EventStore-Client-Dotnet)** - Dotnet Client SDK for the Event Store gRPC Client API written in C#
- ✔️ **[`Npgsql Entity Framework Core Provider`](https://www.npgsql.org/efcore/)** - Npgsql has an Entity Framework (EF) Core provider. It behaves like other EF Core providers (e.g. SQL Server), so the general EF Core docs apply here as well
- ✔️ **[`FluentValidation`](https://github.com/FluentValidation/FluentValidation)** - Popular .NET validation library for building strongly-typed validation rules
- ✔️ **[`Swagger & Swagger UI`](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)** - Swagger tools for documenting API's built on ASP.NET Core
- ✔️ **[`Serilog`](https://github.com/serilog/serilog)** - Simple .NET logging with fully-structured events
- ✔️ **[`Polly`](https://github.com/App-vNext/Polly)** - Polly is a .NET resilience and transient-fault-handling library that allows developers to express policies such as Retry, Circuit Breaker, Timeout, Bulkhead Isolation, and Fallback in a fluent and thread-safe manner
- ✔️ **[`Scrutor`](https://github.com/khellang/Scrutor)** - Assembly scanning and decoration extensions for Microsoft.Extensions.DependencyInjection
- ✔️ **[`Newtonsoft.Json`](https://github.com/JamesNK/Newtonsoft.Json)** - Json.NET is a popular high-performance JSON framework for .NET
- ✔️ **[`AspNetCore.Diagnostics.HealthChecks`](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks)** - Enterprise HealthChecks for ASP.NET Core Diagnostics Package
  NET Compiler Platform
- ✔️ **[`AutoMapper`](https://github.com/AutoMapper/AutoMapper)** - Convention-based object-object mapper in .NET.

## Architecture

For implementing this application we can use different type of caching strategy and we can config our caching strategy in [appsettings.json](src/Server/Services/LeaderBoard.GameEventsProcessor/appsettings.json) file of our [GameEventsProcessor](src/Server/Services/LeaderBoard.GameEventsProcessor/) service and run our [caching strategy workers](src/Server/CacheStrategies/) separately (like WriteThrough, WriteBehind and ReadThrough), if we don't want to use our built-in `Write-aside caching` and ` Read-aside caching` caching strategy.

For decreasing calculation and response time for real-time rank calculation with millions of request and changes per second we need to use a high performant approach to handling this issue, redis has very handy feature of [SortedSet](https://redis.io/docs/data-types/sorted-sets/) and when store a member with specific score, based on score sorted re-arrange affected members with new rank for each member in the SortedSet. With sorted sets it is trivial to return a list of player sorted by their scores because actually they are already sorted and ranked.

Every time we add an element Redis performs an maximum `O(log(N))` operations, where n is the number of members, to re-sort and re-rank affected elements based on new element score. after that when we ask for sorted elements Redis does not have to do any work at all, it's `already all sorted` and drastically decrease our reading times and reading hits (It performs a binary search-like operation to locate the element efficiently, resulting in a time complexity of O(log N)).

- Getting the score of an element: O(1)
- Retrieving an element by its rank: O(log N)

Also for ensuring about losing our data and events in our redis cache because it is on the ram, we need to have a primary database and because we want to keep track of all of our events over time we use EventStoreDB as our primary storage and based on caching-strategies on the `write` and `read` level we update our primary database and secondary redis database and postgres database (using EventStore projections for updating secondary databases).

### Write-Aside Caching & Read-Aside Caching

![](./assets/write-read-cache-aside.png)

Here we used `Cache-Aside` strategy for both read and write.

The flow of our application for showing leader board to users is according these steps:

1. Suppose we have a online game and our users can play the game through mobile or web browser. After getting some points in the game our `mobile app` or `web app` will send a `AddOrUpdate` command to its corresponding endpoint in `GameEventSource` service through our `traefik ingress`, load balancer and reverse proxy.
2. Our traefik will route `AddOrUpdate` request to `GameEventSource` service endpoint.
3. AddOrUpdate endpoint `GameEventSource` service publishes `GameEventChanged` to the broker.
4. `GameEventChangedConsumer` which is subscribed on `GameEventChanged` event in `GameEventProcessor` service, will get `GameEventChanged` event from the broker.
5. our `GameEventChangedConsumer` will call `AddOrUpdatePlayerScore` command and inner `AddOrUpdatePlayerScoreHandler` handler we store events on the EventStoreDB for keep track of all events over the time.
6. After storing events on EventStoreDB our `Postgres Projection (EFCorePlayerScoreReadModelProjection)` and `Redis Projection (RedisPlayerScoreReadModelProjection)` will be triggered.Then these projections will materialize the input data into their respective read data models and store them on Redis and Postgres.
7. Our `RedisPlayerScoreReadModelProjection` projection will publish a `RedisScoreChangedMessage` message through Redis `Pub/Sub`
8. Our `GameEventProcessor` service, which is subscribed on `RedisScoreChangedMessage` Redis message, will get message by its predefined `Redis subscriber` on `RedisScoreChangedMessage` message.
9. Our `Redis Subscriber` on `RedisScoreChangedMessage` message will publish `PlayersRankAffected` message to the broker.
10. Our SignalR service which is subscribed on `PlayersRankAffected` message through `PlayersRankAffectedConsumer` consumer, will get the message and calls `UpdatePlayersScoreForClient` on our `IHubService`.
11. Our `UpdatePlayersScoreForClient` on `IHubService` of SignalR service, will get all affected players based on our `ScoreChanged` event through a REST call to `GameEventProcessor` service.
12. Our `GameEventProcessor` service and `GetPlayerGroupGlobalScoresAndRanks` endpoint will get all related players score with `GetGlobalScoreAndRank` query. This query at-first tries to get rank and score form redis sorted set and if not exists it will uses `Read-Aside Caching` and will read data from primary database and will update our redis database.
13. If the data not existed on the redis we check our primary database which is postgres in this example.
14. After getting data from postgres we update our Redis SortedSet and HashSet data.
15. We send fetched score via `HubService` of our SignalR service in a real time to connected affected players.

### Write-Through & Read-Through

TODO

### Write-Behind & Read-Through

TODO

## Application Structure

In this project I used [vertical slice architecture](https://jimmybogard.com/vertical-slice-architecture/) or [Restructuring to a Vertical Slice Architecture](https://codeopinion.com/restructuring-to-a-vertical-slice-architecture/) also I used [feature folder structure](http://www.kamilgrzybek.com/design/feature-folders/) in this project.

- We treat each request as a distinct use case or slice, encapsulating and grouping all concerns from front-end to back.
- When We adding or changing a feature in an application in n-tire architecture, we are typically touching many different "layers" in an application. we are changing the user interface, adding fields to models, modifying validation, and so on. Instead of coupling across a layer, we couple vertically along a slice and each change affects only one slice.
- We `Minimize coupling` `between slices`, and `maximize coupling` `in a slice`.
- With this approach, each of our vertical slices can decide for itself how to best fulfill the request. New features only add code, we're not changing shared code and worrying about side effects. For implementing vertical slice architecture using cqrs pattern is a good match.

![](./assets/vertical-slice-architecture.jpg)

Also here I used [CQRS](https://www.eventecommerce.com/cqrs-pattern) for decompose my features to very small parts that makes our application:

- maximize performance, scalability and simplicity.
- adding new feature to this mechanism is very easy without any breaking change in other part of our codes. New features only add code, we're not changing shared code and worrying about side effects.
- easy to maintain and any changes only affect on one command or query (or a slice) and avoid any breaking changes on other parts
- it gives us better separation of concerns and cross cutting concern (with help of MediatR behavior pipelines) in our code instead of a big service class for doing a lot of things.

With using [CQRS](https://event-driven.io/en/cqrs_facts_and_myths_explained/), our code will be more aligned with [SOLID principles](https://en.wikipedia.org/wiki/SOLID), especially with:

- [Single Responsibility](https://en.wikipedia.org/wiki/Single-responsibility_principle) rule - because logic responsible for a given operation is enclosed in its own type.
- [Open-Closed](https://en.wikipedia.org/wiki/Open%E2%80%93closed_principle) rule - because to add new operation you don’t need to edit any of the existing types, instead you need to add a new file with a new type representing that operation.

Here instead of some [Technical Splitting](http://www.kamilgrzybek.com/design/feature-folders/) for example a folder or layer for our `services`, `controllers` and `data models` which increase dependencies between our technical splitting and also jump between layers or folders, We cut each business functionality into some vertical slices, and inner each of these slices we have [Technical Folders Structure](http://www.kamilgrzybek.com/design/feature-folders/) specific to that feature (command, handlers, infrastructure, repository, controllers, data models, ...).

## Prerequisites

1. Install git - [https://git-scm.com/downloads](https://git-scm.com/downloads).
2. Install .NET Core 8.0 - [https://dotnet.microsoft.com/en-us/download/dotnet/8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
3. Install Visual Studio, Rider or VSCode.
4. Install docker - [https://docs.docker.com/docker-for-windows/install/](https://docs.docker.com/docker-for-windows/install/).
5. Make sure that you have ~10GB disk space.
6. Clone Project [https://github.com/mehdihadeli/leaderboard](https://github.com/mehdihadeli/leaderboard), make sure that's compiling
7. Run the [docker-compose.infrastructure.yaml](./docker-compose.infrastructure.yaml) file, for running prerequisites infrastructures with `docker-compose -f ./docker-compose.infrastructure.yaml up -d` command.
8. Open [leaderboard.sln](./leaderboard.sln) solution.

## How To Run FrontEnd

For implementing our frontend we used Angular and for real-time communication with SignalR Hub we used [@microsoft/signalr](https://www.npmjs.com/package/@microsoft/signalr) library.

For running our front-end App:

1. go to `cd src/Client` folder and open it in VSCode:

```bash
cd src/Client
code .
```

2. Install node modules:

```bash
npm install
```

3. Run front end:

```bash
npm start
```

## How To Run Backend

For running our backend we can use different caching strategies:

### Running With Cache Aside Strategies

First of all we should turn-on both write and read cache aside strategies in `GameEventProcessor` service and [appsettings.json](src/Server/Services/LeaderBoard.GameEventsProcessor/appsettings.json) file with setting `UseReadCacheAside` and `UseWriteCacheAside` to `true`:

```json
  "LeaderBoardOptions": {
    "UseReadCacheAside": true,
    "UseWriteCacheAside": true,
    "UseReadThrough": false,
    "UseWriteBehind": false,
    "UseWriteThrough": false,
    "CleanupRedisOnStart": true,
    "UseCacheWarmUp": true,
    "SeedInitialData": true
  },
```

Now we should run our needed services:

```bash
dotnet run --project src/Server/Services/LeaderBoard.GameEventsSource
dotnet run --project src/Server/Services/LeaderBoard.GameEventsProcessor
dotnet run --project src/Server/Services/LeaderBoard.SignalR
```

Now our `GameEventSource` service is available on [`http://localhost:3500`](http://localhost:3500), and `GameEventsProcessor` service is available on [`http://localhost:5000`](http://localhost:5000) and our SignalR is available on [`http://localhost:7200`](http://localhost:7200).

### Running With Write Behind And ReadThrough Strategies

First of all we should turn-on both write-behind and read-through strategies in `GameEventProcessor` service and [appsettings.json](src/Server/Services/LeaderBoard.GameEventsProcessor/appsettings.json) file with setting `UseWriteBehind` and `UseReadThrough` to `true`:

```json
  "LeaderBoardOptions": {
    "UseReadCacheAside": false,
    "UseWriteCacheAside": false,
    "UseReadThrough": true,
    "UseWriteBehind": true,
    "UseWriteThrough": false,
    "CleanupRedisOnStart": true,
    "UseCacheWarmUp": true,
    "SeedInitialData": true
  },
```

Now we should run our needed services:

```bash
dotnet run --project src/Server/CacheStrategies/LeaderBoard.ReadThrough
dotnet run --project src/Server/CacheStrategies/LeaderBoard.WriteBehind
dotnet run --project src/Server/Services/LeaderBoard.GameEventsSource
dotnet run --project src/Server/Services/LeaderBoard.GameEventsProcessor
dotnet run --project src/Server/Services/LeaderBoard.SignalR
```

Now our `GameEventSource` service is available on [`http://localhost:3500`](http://localhost:3500), and `GameEventsProcessor` service is available on [`http://localhost:5000`](http://localhost:5000) and our SignalR is available on [`http://localhost:7200`](http://localhost:7200).

## Contribution

The application is in development status. You are feel free to submit pull request or create the issue.

## License

The project is under [MIT license](https://github.com/mehdihadeli/game-leaderboard-microservices/blob/main/LICENSE).

## References

### Articles

- [3 crucial caching choices: Where, when, and how](https://www.gomomento.com/blog/3-crucial-caching-choices-where-when-and-how)
- [6 common caching design patterns to execute your caching strategy](https://www.gomomento.com/blog/6-common-caching-design-patterns-to-execute-your-caching-strategy)
- [Caching Strategies and How to Choose the Right One](https://codeahoy.com/2017/08/11/caching-strategies-and-how-to-choose-the-right-one/)
