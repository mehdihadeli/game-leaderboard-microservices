using Microsoft.AspNetCore.Http;

namespace LeaderBoard.SharedKernel.Core.Exceptions;

public class AppException : CustomException
{
    public AppException(
        string message,
        int statusCode = StatusCodes.Status400BadRequest,
        Exception? innerException = null
    )
        : base(message, statusCode, innerException) { }
}
