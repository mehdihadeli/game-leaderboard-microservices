using Microsoft.AspNetCore.Authentication;

namespace LeaderBoard.GameEventsSource.Accounts.Logout;

public static class LogoutEndpoint
{
    internal static RouteHandlerBuilder MapLogoutEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/logout", Handler)
            .WithTags(nameof(Accounts))
            .WithName("Logout");

        async Task<IResult> Handler([AsParameters] LogoutRequestParameters requestParameters)
        {
            await requestParameters.HttpContext.SignOutAsync();
            return TypedResults.Ok();
        }
    }

    internal record LogoutRequestParameters(
        HttpContext HttpContext,
        CancellationToken CancellationToken
    );
}
