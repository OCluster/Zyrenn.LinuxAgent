using Npgsql;
using Zyrenn.DatabaseQHub.QueryHub.PostgreSQL;
using Zyrenn.LinuxAgent.Helpers;
using Zyrenn.LinuxAgent.Models.Common;
using Zyrenn.LinuxAgent.Models.Common.Config;
using Zyrenn.LinuxAgent.Models.Databases;

namespace Zyrenn.LinuxAgent.Services.Databases;

public class DatabaseService(ILogger<DatabaseService> logger) : IDatabaseService
{
    #region Methods region

    #region Public methods region

    /// <summary>
    /// Retrieves a list of databases along with their detailed data,
    /// getting it from multiple database configurations.
    /// </summary>
    public async ValueTask<DatabaseList> GetDatabaseListAsync(CancellationToken ct)
    {
        var dbDataList = new DatabaseList();

        if (ConfigDataHelper.DbConfigs == null || !ConfigDataHelper.DbConfigs.Any())
        {
            logger.LogWarning("Database configuration is missing or empty.");
            return dbDataList;
        }

        foreach (var dbConfig in ConfigDataHelper.DbConfigs)
        {
            try
            {
                var dbDetail = await CollectDatabaseDetailAsync(dbConfig.Type, dbConfig.Connection, ct);
                dbDataList.Databases.Add(dbDetail);
            }
            catch (PostgresException ex) when (ex.SqlState == "57014")
            {
                logger.LogInformation("Query canceled while collecting data for database [{Connection}].",
                    dbConfig.Connection);
                throw;
            }
            catch (NpgsqlException ex) when (ex.InnerException is TimeoutException)
            {
                logger.LogWarning("Timeout occurred while collecting data for database [{Connection}].",
                    dbConfig.Connection);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while collecting database detail for [{Connection}].",
                    dbConfig.Connection);
                throw;
            }
        }

        return dbDataList;
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