using Microsoft.AspNetCore.Http;

namespace LeaderBoard.SharedKernel.Core.Exceptions;

public class BadRequestException : CustomException
{
    public BadRequestException(string message, Exception? innerException = null)
        : base(message, StatusCodes.Status404NotFound, innerException) { }
}
