using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LeaderBoard.GameEventsSource.Accounts.GettingProfile;

public static class GetProfileEndpoint
{
    internal static RouteHandlerBuilder MapGetProfileEndpoint(
        this IEndpointRouteBuilder routeBuilder
    )
    {
        return routeBuilder
            .MapGet("profile", Handle)
            .WithTags(nameof(Accounts))
            .WithName(nameof(GetProfile));

        static async Task<
            Results<Ok<GetProfileResponse>, ValidationProblem, ProblemHttpResult>
        > Handle([AsParameters] GetProfileRequestParameter requestParameters)
        {
            var (mediator, httpContext, ct) = requestParameters;

            string? userId = httpContext.User.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
                ?.Value;

            var res = await mediator.Send(new GetProfile(userId), ct);
            if (res == null)
            {
                return TypedResults.Problem(
                    "Profile not found",
                    statusCode: StatusCodes.Status404NotFound
                );
            }

            var response = new GetProfileResponse(
                res.Id,
                res.FirstName,
                res.LastName,
                res.Country,
                res.Email,
                res.UserName
            );

            return TypedResults.Ok(response);
        }
    }
}

internal record GetProfileRequestParameter(
    IMediator Mediator,
    HttpContext HttpContext,
    CancellationToken CancellationToken
);

record GetProfileResponse(
    string Id,
    string FirstName,
    string LastName,
    string Country,
    string? Email,
    string? UserName
);
