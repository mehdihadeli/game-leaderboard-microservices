namespace LeaderBoard.SharedKernel.Web.ProblemDetail;

public interface IProblemDetailMapper
{
    int GetMappedStatusCodes(Exception exception);
}
