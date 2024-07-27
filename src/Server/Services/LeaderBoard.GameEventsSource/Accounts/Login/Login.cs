using Ardalis.GuardClauses;
using FluentValidation;
using LeaderBoard.GameEventsSource.Players.Models;
using LeaderBoard.GameEventsSource.Shared;
using LeaderBoard.GameEventsSource.Shared.Services;
using LeaderBoard.SharedKernel.Core.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NotFoundException = LeaderBoard.SharedKernel.Core.Exceptions.NotFoundException;

namespace LeaderBoard.GameEventsSource.Accounts.Login;

public record Login(string UserNameOrId, string Password) : IRequest<LoginResult>;

internal class LoginValidator : AbstractValidator<Login>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserNameOrId).NotEmpty().WithMessage("UserNameOrId cannot be empty");
        RuleFor(x => x.Password).NotEmpty().WithMessage("password cannot be empty");
    }
}

internal class LoginHandler : IRequestHandler<Login, LoginResult>
{
    private readonly ILogger<LoginHandler> _logger;
    private readonly ITokenService _tokenService;
    private readonly SignInManager<Player> _signInManager;
    private readonly UserManager<Player> _userManager;

    public LoginHandler(
        UserManager<Player> userManager,
        SignInManager<Player> signInManager,
        ILogger<LoginHandler> logger,
        ITokenService tokenService
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _tokenService = tokenService;
    }

    public async Task<LoginResult> Handle(Login request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(Login));

        var identityUser =
            await _userManager.FindByNameAsync(request.UserNameOrId)
            ?? await _userManager.FindByIdAsync(request.UserNameOrId);

        if (identityUser == null)
            throw new NotFoundException("User not found");

        // instead of PasswordSignInAsync, we use CheckPasswordSignInAsync because we don't want set cookie, instead we use JWT
        var signinResult = await _signInManager.CheckPasswordSignInAsync(identityUser, request.Password, false);

        if (signinResult.IsNotAllowed)
        {
            if (!await _userManager.IsEmailConfirmedAsync(identityUser))
                throw new CustomException(
                    $"Email {identityUser.Email} not confirmed.",
                    statusCode: StatusCodes.Status400BadRequest
                );

            if (!await _userManager.IsPhoneNumberConfirmedAsync(identityUser))
                throw new CustomException(
                    $"Phone Number {identityUser.PhoneNumber} not confirmed.",
                    statusCode: StatusCodes.Status400BadRequest
                );
        }
        else if (signinResult.IsLockedOut)
        {
            throw new CustomException(
                $"The account {identityUser.Id.ToString()} is locked.",
                statusCode: StatusCodes.Status400BadRequest
            );
        }
        else if (!signinResult.Succeeded)
        {
            throw new CustomException("UserName or Password is invalid.", statusCode: StatusCodes.Status400BadRequest);
        }

        string token = await _tokenService.GetJwtTokenAsync(identityUser);

        return new LoginResult(token, identityUser.UserName!);
    }
}

public record LoginResult(string Token, string UserName);
