using Microsoft.AspNetCore.Http;

namespace LeaderBoard.SharedKernel.Core.Exceptions;

public class InternalServerException : CustomException
{
	public InternalServerException(string message, Exception? innerException = null)
		: base(message, StatusCodes.Status500InternalServerError, innerException) { }
}
