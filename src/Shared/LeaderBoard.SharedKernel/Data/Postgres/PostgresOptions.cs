namespace LeaderBoard.SharedKernel.Data.Postgres;

public class PostgresOptions
{
    public string ConnectionString { get; set; } = default!;
    public string? MigrationAssembly { get; set; } = null!;
    public bool UseInMemory { get; set; }
}
