using LeaderBoard.SharedKernel.Core.Exceptions;
using Microsoft.AspNetCore.Http;

namespace LeaderBoard.SharedKernel.Web.ProblemDetail;

internal sealed class DefaultProblemDetailMapper : IProblemDetailMapper
{
    public int GetMappedStatusCodes(Exception exception)
    {
        return exception switch
        {
            ConflictException conflictException => conflictException.StatusCode,
            ValidationException validationException => validationException.StatusCode,
            ArgumentException _ => StatusCodes.Status400BadRequest,
            BadRequestException badRequestException => badRequestException.StatusCode,
            NotFoundException notFoundException => notFoundException.StatusCode,
            HttpResponseException httpResponseException => httpResponseException.StatusCode,
            HttpRequestException httpRequestException => (int)httpRequestException.StatusCode,
            AppException appException => appException.StatusCode,
            CustomException customException => customException.StatusCode,
            _ => 0
        };
    }
}
