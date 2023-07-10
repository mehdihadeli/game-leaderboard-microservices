namespace LeaderBoard.Dtos;

public record PlayerScoreDetailDto(
	string? Country = null,
	string? FirstName = null,
	string? LastName = null);