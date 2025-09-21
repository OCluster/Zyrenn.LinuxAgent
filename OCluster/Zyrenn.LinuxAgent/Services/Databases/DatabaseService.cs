using Npgsql;
using Zyrenn.DatabaseQHub.QueryHub.PostgreSQL;
using Zyrenn.LinuxAgent.Models.Common;
using Zyrenn.LinuxAgent.Models.Databases;

namespace Zyrenn.LinuxAgent.Services.Databases;

public class DatabaseService : IDatabaseService
{
    #region Private fields region

    private readonly List<DatabaseConfig>? _dbConfigs;
    private readonly ILogger<DatabaseService> _logger;

    #endregion

    #region Constrcutors region

    public DatabaseService(IConfiguration config, ILogger<DatabaseService> logger)
    {
        _logger = logger;
        _dbConfigs = config.GetSection("DatabaseConnections").Get<List<DatabaseConfig>>();
    }

    #endregion

    #region Methods region

    #region Public methods region

    /// <summary>
    /// Retrieves a list of databases along with their detailed metrics, consolidating data from multiple database configurations.
    /// </summary> todo consider database versioning
    public async ValueTask<DatabaseList> GetDatabaseListAsync(CancellationToken ct)
    {
        var metrics = new DatabaseList();

        if (_dbConfigs != null)
        {
            foreach (var dbConfig in _dbConfigs)
            {
                try
                {
                    var dbMetrics = await CollectDatabaseDetailAsync(dbConfig.Type, dbConfig.Connection, ct);
                    metrics.Databases.Add(dbMetrics);
                }
                catch (PostgresException ex) when (ex.SqlState == "57014")
                {
                }
                catch (NpgsqlException ex) when (ex.InnerException is TimeoutException)
                {
                } 
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to collect database metrics for {dbConfig.Connection}");
                }
            }
        } //todo may be handle the null case, so if the worker is enabled and the config is not present, it will not collect metrics

        return metrics;
    }


    public async ValueTask<DatabaseDetail> GetPostgresDetailAsync(
        string connectionString,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var cmd = new NpgsqlCommand(cmdText: PostgreSqlQueryHandler.GetDbMetadata(), conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        await reader.ReadAsync(ct);

        return new DatabaseDetail
        {
            Name = reader.GetString(reader.GetOrdinal("name")),
            Ip = reader.GetString(reader.GetOrdinal("ip")),
            Size = reader.GetInt64(reader.GetOrdinal("Size")),
            IndexCount = reader.GetInt32(reader.GetOrdinal("indexes")),
            FunctionCount = reader.GetInt32(reader.GetOrdinal("functions")),
            TriggerCount = reader.GetInt32(reader.GetOrdinal("triggers")),
            ViewCount = reader.GetInt32(reader.GetOrdinal("views")),
            MaterializedViewCount = reader.GetInt32(reader.GetOrdinal("materialized_views")),
            UserCount = reader.GetInt32(reader.GetOrdinal("users")),
            RoleCount = reader.GetInt32(reader.GetOrdinal("roles")),
            ExtensionCount = reader.GetInt32(reader.GetOrdinal("extensions")),
            ProcedureCount = reader.GetInt32(reader.GetOrdinal("procedures")),
            Status = reader.GetString(reader.GetOrdinal("status")),
            ActiveConnectionCount = reader.GetInt32(reader.GetOrdinal("active_connections")),
            DatabaseType = Enum.GetName(DatabaseType.Postgres)!,
        };
    }

    #endregion

    #region Private methods region

    /// <summary>
    /// Collects detailed metrics about a specific database based on its type and connection string.
    /// </summary>
    /// <param name="dbType">The type of the database (e.g., Postgres).</param>
    /// <param name="connectionString">The connection string used to connect to the database.</param>
    private async ValueTask<DatabaseDetail> CollectDatabaseDetailAsync(
        DatabaseType dbType,
        string connectionString,
        CancellationToken ct)
    {
        return dbType switch
        {
            DatabaseType.Postgres => await GetPostgresDetailAsync(connectionString, ct),
            //DatabaseType.MySql => await CollectMySqlMetricsAsync(connectionString, ct),
            _ => throw new NotSupportedException($"Database type {dbType} not supported")
        };
    }

    #endregion

    #endregion
}