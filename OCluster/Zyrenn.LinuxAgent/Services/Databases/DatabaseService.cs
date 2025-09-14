using Npgsql;
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
        _dbConfigs = config.GetSection("DatabaseConnections")
            .Get<List<DatabaseConfig>>();
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
                } //todo return Offline if the db is offline, focus on TimeOu
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to collect database metrics for {dbConfig.Connection}");
                }
            }
        }

        return metrics;
    }

    
    public async ValueTask<DatabaseDetail> GetPostgresDetailAsync(
        string connectionString,
        CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);
        
        //todo move queries to a nuget package.
        var cmd = new NpgsqlCommand(
            @"SELECT
    current_database() AS name,
    inet_server_addr()::varchar AS ip,
    pg_database_size(current_database()) AS size,
    -- Indexes
    (
        SELECT COUNT(*)
        FROM pg_class
        WHERE relkind = 'i'
          AND relnamespace IN (
            SELECT oid FROM pg_namespace
            WHERE nspname NOT LIKE 'pg_%' AND nspname != 'information_schema'
        )
    ) AS indexes,

    -- Functions
    (
        SELECT COUNT(*)
        FROM pg_proc
        WHERE pronamespace IN (
            SELECT oid FROM pg_namespace
            WHERE nspname NOT LIKE 'pg_%' AND nspname != 'information_schema'
        )
    ) AS functions,

    -- Triggers
    (
        SELECT COUNT(*)
        FROM pg_trigger
        WHERE NOT tgisinternal
    ) AS triggers,

    -- Views
    (
        SELECT COUNT(*)
        FROM pg_views
        WHERE schemaname NOT LIKE 'pg_%' AND schemaname != 'information_schema'
    ) AS views,

    -- Materialized Views
    (
        SELECT COUNT(*)
        FROM pg_matviews
        WHERE schemaname NOT LIKE 'pg_%' AND schemaname != 'information_schema'
    ) AS materialized_views,

    -- Users
    (
        SELECT COUNT(*)
        FROM pg_user
    ) AS users,

    -- Roles
    (
        SELECT COUNT(*)
        FROM pg_roles
    ) AS roles,

    -- Extensions
    (
        SELECT COUNT(*)
        FROM pg_extension
    ) AS extensions,

    -- Procedures
    (SELECT COUNT(*)
     FROM pg_proc
     WHERE prokind = 'p')           AS procedures,
    (SELECT CASE
                WHEN EXISTS (SELECT 1 FROM pg_stat_activity WHERE datname = current_database()) THEN 'Online'
                ELSE 'Offline' END) AS status,
    (SELECT COUNT(*)
     FROM pg_stat_activity
     WHERE datname = current_database()
       and state = 'active')        as active_connections;", conn);

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