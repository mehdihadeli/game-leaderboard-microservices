using Microsoft.AspNetCore.Http;

namespace LeaderBoard.SharedKernel.Core.Exceptions;

public class ValidationException : CustomException
{
    public ValidationException(string message, Exception? innerException = null, params string[] errors)
        : base(message, StatusCodes.Status400BadRequest, innerException, errors) { }
}
