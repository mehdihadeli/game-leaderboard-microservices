using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Models;

namespace LeaderBoard.WriteThrough.Providers;

public class PostgresWriteProviderDatabase : IWriteProviderDatabase
{
    private readonly LeaderBoardDBContext _leaderBoardDbContext;

    public PostgresWriteProviderDatabase(LeaderBoardDBContext leaderBoardDbContext)
    {
        _leaderBoardDbContext = leaderBoardDbContext;
    }

    public async Task AddPlayerScore(
        PlayerScore playerScore,
        CancellationToken cancellationToken = default
    )
    {
        await _leaderBoardDbContext.PlayerScores.AddAsync(playerScore, cancellationToken);
        await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateScore(
        string leaderBoardName,
        string playerId,
        double score,
        long? rank,
        CancellationToken cancellationToken = default
    )
    {
        var existPlayerScore = _leaderBoardDbContext.PlayerScores.SingleOrDefault(
            x => x.PlayerId == playerId && x.LeaderBoardName == leaderBoardName
        );
        if (existPlayerScore is { })
        {
            existPlayerScore.Rank = rank;
            existPlayerScore.Score = score;
            await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
