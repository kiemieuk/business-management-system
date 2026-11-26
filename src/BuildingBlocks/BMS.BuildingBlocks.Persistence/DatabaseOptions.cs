namespace BMS.BuildingBlocks.Persistence;

public enum DatabaseProvider
{
    SqlServer,
    Sqlite,
}

/// <summary>
/// Bound from the <c>Database</c> configuration section of every microservice.
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>
    /// Database engine. <c>SqlServer</c> (default, uses EF Core migrations) or <c>Sqlite</c> (zero-setup local development / tests).
    /// </summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;

    public string ConnectionString { get; set; } = string.Empty;
}
