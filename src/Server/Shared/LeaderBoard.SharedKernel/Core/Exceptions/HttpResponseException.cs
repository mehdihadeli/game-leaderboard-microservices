namespace LeaderBoard.SharedKernel.Core.Exceptions;

// https://stackoverflow.com/questions/21097730/usage-of-ensuresuccessstatuscode-and-handling-of-httprequestexception-it-throws
public class HttpResponseException : CustomException
{
    public string? Messages { get; }

    public IReadOnlyDictionary<string, IEnumerable<string>>? Headers { get; }

    public HttpResponseException(
        int statusCode,
        string messages,
        IReadOnlyDictionary<string, IEnumerable<string>>? headers = null,
        Exception? innerException = null
    )
        : base(messages, statusCode, innerException)
    {
        StatusCode = statusCode;
        Messages = messages;
        Headers = headers;
    }

    public override string ToString()
    {
        return $"HTTP Response: \n\n{Messages}\n\n{base.ToString()}";
    }
}
