# LeaderBoard

This project is an implementation of a imaginary [Game LeaderBoard](https://en.wikipedia.org/wiki/Ladder_tournament) application, based on [Event Driven Architecture](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven), [Vertical Slice Architecture](https://jimmybogard.com/vertical-slice-architecture/), [Event Sourcing](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing) with EventStoreDB, [Redis SortedSet](https://redis.io/docs/data-types/sorted-sets/), [Redis Pub/Sub](https://redis.io/docs/interact/pubsub/), SignalR and .Net 8.

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

## Architecture

For implementing this application we can use different type of caching strategy and we can config our caching strategy in [appsettings.json](src/Server/Services/LeaderBoard.GameEventsProcessor/appsettings.json) file of our [GameEventsProcessor](src/Server/Services/LeaderBoard.GameEventsProcessor/) service and run our [caching strategy workers](src/Server/CacheStrategies/) separately (like WriteThrough, WriteBehind and ReadThrough) if we don't want to use our built-in `Write-aside caching` and ` Read-aside caching` caching strategy.

For decreasing calculation and response time for real-time rank calculation with millions of request and changes per second we need to use a high performant approach to handling this issue, redis has very handy feature of [SortedSet](https://redis.io/docs/data-types/sorted-sets/) and when store a member with specific score, based on score sorted re-arrange affected members with new rank for each member in the SortedSet. With sorted sets it is trivial to return a list of player sorted by their scores because actually they are already sorted and ranked.

Every time we add an element Redis performs an maximum `O(log(N))` operations, where n is the number of members, to re-sort and re-rank affected elements based on new element score. after that when we ask for sorted elements Redis does not have to do any work at all, it's `already all sorted` and drastically decrease our reading times and reading hits (It performs a binary search-like operation to locate the element efficiently, resulting in a time complexity of O(log N)).

- Getting the score of an element: O(1)
- Retrieving an element by its rank: O(log N)

Also for ensuring about losing our data and events in our redis cache because it is on the ram, we need to have a primary database and because we want to keep track of all of our events over time we use EventStoreDB as our primary storage and based on caching-strategies on the `write` and `read` level we update our primary database and secondary redis database and postgres database (using EventStore projections for updating secondary databases).

### Write-Aside Caching & Read-Aside Caching

### Write-Through & Read-Through

### Write-Behind & Read-Through

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

## References

### Articles

- [3 crucial caching choices: Where, when, and how](https://www.gomomento.com/blog/3-crucial-caching-choices-where-when-and-how)
- [6 common caching design patterns to execute your caching strategy](https://www.gomomento.com/blog/6-common-caching-design-patterns-to-execute-your-caching-strategy)
- [Caching Strategies and How to Choose the Right One](https://codeahoy.com/2017/08/11/caching-strategies-and-how-to-choose-the-right-one/)
