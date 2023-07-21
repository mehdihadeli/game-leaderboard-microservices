using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LeaderBoard.GameEventsSource.Accounts.Login;

public static class LoginEndpoint
{
    internal static RouteHandlerBuilder MapLoginUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/login", Handler)
            .WithTags(nameof(Accounts))
            .WithName(nameof(Login));

        async Task<Results<Ok<LoginResponse>, ValidationProblem>> Handler(
            [AsParameters] LoginRequestParameters requestParameters
        )
        {
            var (req, mediator, ct) = requestParameters;

            var command = new Login(req.UserNameOrId, req.Password);

            var result = await mediator.Send(command, ct);

            return TypedResults.Ok(new LoginResponse(result.Token, result.UserName));
        }
    }
}

internal record LoginRequestParameters(
    LoginRequest Request,
    IMediator Mediator,
    CancellationToken CancellationToken
);

public record LoginRequest(string UserNameOrId, string Password);

public record LoginResponse(string Token, string UserName);
