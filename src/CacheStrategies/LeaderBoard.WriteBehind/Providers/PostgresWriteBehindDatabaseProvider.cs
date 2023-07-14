using AutoMapper;
using LeaderBoard.SharedKernel.Application.Data.EFContext;
using LeaderBoard.SharedKernel.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoard.WriteBehind.Providers;

public class PostgresWriteBehindDatabaseProvider : IWriteBehindDatabaseProvider
{
    private readonly LeaderBoardDBContext _leaderBoardDbContext;
    private readonly IMapper _mapper;

    public PostgresWriteBehindDatabaseProvider(
        LeaderBoardDBContext leaderBoardDbContext,
        IMapper mapper
    )
    {
        _leaderBoardDbContext = leaderBoardDbContext;
        _mapper = mapper;
    }

    public async Task UpdatePlayerScore(
        string leaderBoardName,
        string playerId,
        double score,
        long? rank,
        CancellationToken cancellationToken
    )
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

    public async Task AddPlayerScore(PlayerScore playerScore, CancellationToken cancellationToken)
    {
        await _leaderBoardDbContext.PlayerScores.AddAsync(playerScore, cancellationToken);
        await _leaderBoardDbContext.SaveChangesAsync(cancellationToken);
    }
}
