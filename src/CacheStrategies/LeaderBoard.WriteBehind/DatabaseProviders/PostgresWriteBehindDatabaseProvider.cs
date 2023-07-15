using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.WriteBehind.DatabaseProviders;

public class PostgresWriteBehindDatabaseProvider : IWriteBehindDatabaseProvider
{
    private readonly LeaderBoardDbContext _leaderBoardDbContext;
    private readonly ILogger<PostgresWriteBehindDatabaseProvider> _logger;

    public PostgresWriteBehindDatabaseProvider(
        LeaderBoardDbContext leaderBoardDbContext,
        ILogger<PostgresWriteBehindDatabaseProvider> logger
    )
    {
        _leaderBoardDbContext = leaderBoardDbContext;
        _logger = logger;
    }

    public async Task UpdatePlayerScore(
        string leaderBoardName,
        string playerId,
        double score,
        long? rank,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var existPlayerScore = await _leaderBoardDbContext.PlayerScores.SingleOrDefaultAsync(
                x => x.PlayerId == playerId && x.LeaderBoardName == leaderBoardName,
                cancellationToken: cancellationToken
            );
            if (existPlayerScore is not null)
            {
                existPlayerScore.Score = score;
                existPlayerScore.Rank = rank;
            }

            await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in UpdatePlayerScore: {Message}", e.Message);
        }
    }

    public async Task AddPlayerScore(PlayerScore playerScore, CancellationToken cancellationToken)
    {
        try
        {
            await _leaderBoardDbContext.PlayerScores.AddAsync(playerScore, cancellationToken);
            await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError("Error in AddPlayerScore: {Message}", e.Message);
        }
    }
}
