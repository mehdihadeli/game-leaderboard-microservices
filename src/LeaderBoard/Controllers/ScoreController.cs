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
        var res = _playerScoreService.AddOrUpdateScore(Constants.GlobalLeaderBoard, playerId, score);
        return Ok(new { success = res });
    }

    [HttpGet("global-board/scores/top10-scores")]
    public async Task<IActionResult> GetTop10ScoresAndRanks()
    {
        return Ok(await _playerScoreService.GetScoresAndRanks(Constants.GlobalLeaderBoard, 0, 9, true));
    }

    [HttpGet("global-board/scores/players/{playerId}")]
    public async Task<IActionResult> GetScoreAdnRank(string playerId)
    {
        return Ok(await _playerScoreService.GetScoreAndRank(Constants.GlobalLeaderBoard, playerId, true));
    }
}
