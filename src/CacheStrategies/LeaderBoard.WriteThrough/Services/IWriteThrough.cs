using LeaderBoard.WriteThrough.Dtos;

namespace LeaderBoard.WriteThrough.Services;

public interface IWriteThrough
{
	Task<bool> AddPlayerScore(
		PlayerScoreDto playerScoreDto,
		CancellationToken cancellationToken = default
	);

	Task<bool> UpdateScore(
		string leaderBoardName,
		string playerId,
		double value,
		CancellationToken cancellationToken = default
	);
}