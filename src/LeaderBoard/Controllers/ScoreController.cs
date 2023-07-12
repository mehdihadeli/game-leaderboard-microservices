using LeaderBoard.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeaderBoard.Controllers;

[ApiController]
[Route("[controller]")]
public class ScoreController : ControllerBase
{
    private readonly IPlayerScoreService _playerScoreService;

    public ScoreController(IPlayerScoreService playerScoreService)
    {
        _playerScoreService = playerScoreService;
    }

    [HttpPost("global-board/scores/players/{playerId}")]
    public IActionResult AddOrUpdateScore(string playerId, [FromBody] double score)
    {
        var res = _playerScoreService.UpdateScore(
            Constants.GlobalLeaderBoard,
            playerId,
            score
        );
        return Ok(new { success = res });
    }

    [HttpGet("global-board/scores/range")]
    public async Task<IActionResult> GetTop10ScoresAndRanks(
        [FromQuery] int from = 0,
        [FromQuery] int to = 9,
        [FromQuery] bool isDesc = true
    )
    {
        return Ok(
            await _playerScoreService.GetRangeScoresAndRanks(
                Constants.GlobalLeaderBoard,
                from,
                to,
                isDesc
            )
        );
    }

    [HttpGet("global-board/scores/players/{playerId}")]
    public async Task<IActionResult> GetGlobalScoreAdnRank(string playerId)
    {
        return Ok(
            await _playerScoreService.GetGlobalScoreAndRank(
                Constants.GlobalLeaderBoard,
                playerId,
                true
            )
        );
    }
}
