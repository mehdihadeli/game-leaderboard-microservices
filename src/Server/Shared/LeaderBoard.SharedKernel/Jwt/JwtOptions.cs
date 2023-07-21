namespace LeaderBoard.SharedKernel.Jwt;

public class JwtOptions
{
    public string Issuer { get; set; } = default!;

    public string Key { get; set; } = default!;

    public int TokenLifeTimeInMinute { get; set; }
}
