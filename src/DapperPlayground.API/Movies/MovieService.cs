using Dapper;
using DapperPlayground.API.ConnectionFactories;
using DapperPlayground.API.Enums;
using DapperPlayground.API.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace DapperPlayground.API.Movies;

public sealed class MovieService : IMovieService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public MovieService(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> CreateAsync(Movie movie)
    {
        await using var connection = _connectionFactory.Create();

        const string sql =
            """
            INSERT INTO [dbo].[Movies] (Name)
            OUTPUT inserted.Id
            VALUES (@Name)
            """;

        var result = await connection.ExecuteScalarAsync<int>(sql, movie);

        return result;
    }

    public async Task CreateManyAsync(int count, CreateManyType type = CreateManyType.Normal)
    {
        var movies = MoviesToInsert(count);
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            var start = Stopwatch.GetTimestamp();
            switch (type)
            {
                case CreateManyType.Normal:
                    await NormalInsertAsync(connection, movies, transaction);
                    break;

                case CreateManyType.Faster:
                    await FastInsertAsync(count, 10, connection, movies, transaction);
                    break;

                case CreateManyType.Bulk:
                    await BulkInsertAsync(connection, movies, transaction);
                    break;

                case CreateManyType.Tvp:
                    await TvpDataTableInsertAsync(connection, movies, transaction);
                    break;

                case CreateManyType.TvpSqlDataRecord:
                    await TvpSqlDataRecordInsertAsync(connection, movies, transaction);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            await transaction.CommitAsync();
            var elapsed = Stopwatch.GetElapsedTime(start);
            Console.WriteLine(elapsed);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task NormalInsertAsync(SqlConnection connection, IReadOnlyList<Movie> movies, SqlTransaction transaction)
    {
        const string sql =
            """
            INSERT INTO [dbo].[Movies] (Name)
            VALUES
            (@Name)
            """;

        await connection.ExecuteAsync(sql, movies, transaction: transaction);
    }

    private static async Task BulkInsertAsync(SqlConnection connection, IReadOnlyList<Movie> movies, SqlTransaction transaction)
    {
        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);
        bulkCopy.DestinationTableName = "[dbo].[Movies]";
        bulkCopy.ColumnMappings.Add("Name", "Name");

        using var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        foreach (var movie in movies)
        {
            table.Rows.Add(movie.Name);
        }

        await bulkCopy.WriteToServerAsync(table);
    }

    private static async Task TvpDataTableInsertAsync(SqlConnection connection, IReadOnlyList<Movie> movies, SqlTransaction transaction)
    {
        using var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        foreach (var movie in movies)
        {
            table.Rows.Add(movie.Name);
        }

        const string sql =
            """
            INSERT INTO [dbo].[Movies]
            (Name)
            SELECT Name
            FROM @Tvp
            """;

        await connection.ExecuteAsync(sql, new { Tvp = table.AsTableValuedParameter("[dbo].[TVP_Movies_Insert]") }, transaction: transaction);
    }

    private static async Task TvpSqlDataRecordInsertAsync(SqlConnection connection, IReadOnlyList<Movie> movies, SqlTransaction transaction)
    {
        SqlMetaData[] meta =
        [
            new("Name", SqlDbType.VarChar, maxLength: 50)
        ];
        SqlDataRecord[] records = new SqlDataRecord[movies.Count];
        for (int i = 0; i < movies.Count; ++i)
        {
            var record = new SqlDataRecord(meta);
            record.SetString(ordinal: 0, movies[i].Name);
            records[i] = record;
        }

        const string sql =
            """
            INSERT INTO [dbo].[Movies]
            (Name)
            SELECT Name
            FROM @Tvp
            """;

        await connection.ExecuteAsync(sql, new { Tvp = records.AsTableValuedParameter("[dbo].[TVP_Movies_Insert]") }, transaction: transaction);
    }

    private static async Task FastInsertAsync(int count, int batchSize, SqlConnection connection, IReadOnlyList<Movie> movies, SqlTransaction transaction)
    {
        const string baseSql =
                """
                INSERT INTO [dbo].[Movies] (Name)
                VALUES

                """;
        var builder = new StringBuilder(baseSql);
        var firstBatch = count % batchSize;
        var parameters = new DynamicParameters();
        if (firstBatch > 0)
        {
            for (int i = 0; i < firstBatch; i++)
            {
                var parameterName = $"@{i}";
                builder.Append($"({parameterName}),");
                parameters.Add(parameterName, movies[i].Name);
            }
            builder.Length--;
            await connection.ExecuteAsync(builder.ToString(), parameters, transaction: transaction);
            builder.Append(',');
        }

        if (firstBatch == count)
        {
            return;
        }
        for (int i = firstBatch; i < batchSize; ++i)
        {
            builder.Append($"(@{i}),");
        }
        builder.Length--;
        var sql = builder.ToString();
        for (int i = firstBatch; i < count; i += batchSize)
        {
            parameters = new DynamicParameters();
            for (int j = 0; j < batchSize; j++)
            {
                parameters.Add($"@{j}", movies[i + j].Name);
            }
            _ = await connection.ExecuteAsync(sql, parameters, transaction: transaction);
        }
    }

    public async Task DeleteAsync(int id)
    {
        await using var connection = _connectionFactory.Create();

        const string sql =
            """
            DELETE [dbo].[Movies]
            WHERE Id = @Id
            """;

        _ = await connection.ExecuteAsync(sql, new { id });
    }

    public async Task DeleteManyAsync(DeleteManyRequest request)
    {
        var ids = Enumerable.Range(request.StartIdx, request.Count).ToList();
        ids.Shuffle();
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            var stopWatch = Stopwatch.StartNew();
            switch (request.DeleteManyType)
            {
                case DeleteManyType.In:
                    await DeleteManyInAsync(ids, connection, transaction);
                    break;

                case DeleteManyType.Tvp:
                    await DeleteManyTvpAsync(ids, connection, transaction);
                    break;

                case DeleteManyType.Bulk:
                    await DeleteManyBulkAsync(ids, connection, transaction);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(request.DeleteManyType));
            }
            await transaction.CommitAsync();
            Console.WriteLine(stopWatch.ElapsedMilliseconds);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task DeleteManyInAsync(IEnumerable<int> ids, SqlConnection connection, SqlTransaction transaction)
    {
        const string sql =
            """
            DELETE FROM [dbo].[Movies]
            WHERE
                Id in @Ids
            """;
        await connection.ExecuteAsync(sql, new { ids }, transaction: transaction);
    }

    private static async Task DeleteManyTvpAsync(IEnumerable<int> ids, SqlConnection connection, SqlTransaction transaction)
    {
        const string sql =
            """
            DELETE M
            FROM [dbo].[Movies] M
            JOIN @Ids ids ON ids.Id = M.Id
            """;
        using var dt = new DataTable();
        dt.Columns.Add("Id", typeof(int));
        foreach (var id in ids)
        {
            dt.Rows.Add(id);
        }

        await connection.ExecuteAsync(sql, new { Ids = dt.AsTableValuedParameter("TVP_Ids") }, transaction: transaction);
    }

    private static async Task DeleteManyBulkAsync(IEnumerable<int> ids, SqlConnection connection, SqlTransaction transaction)
    {
        const string createTempIdsTableSql =
            """
            CREATE TABLE #Ids(
                Id INT NOT NULL PRIMARY KEY
            )
            """;
        await connection.ExecuteAsync(createTempIdsTableSql, transaction: transaction);
        using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
        {
            bulkCopy.DestinationTableName = "#Ids";
            using var dt = new DataTable();
            dt.Columns.Add("Id", typeof(int));
            foreach (var id in ids)
            {
                dt.Rows.Add(id);
            }
            await bulkCopy.WriteToServerAsync(dt);
        }

        const string sql =
            """
            DELETE M
            FROM [dbo].[Movies] M
            JOIN #Ids ids ON ids.Id = M.Id
            """;

        await connection.ExecuteAsync(sql, transaction: transaction);
        await connection.ExecuteAsync("Drop table #Ids", transaction: transaction);
    }

    public async Task DeleteAllAsync()
    {
        await using var connection = _connectionFactory.Create();

        const string sql = "DELETE FROM [dbo].[Movies]";

        _ = await connection.ExecuteAsync(sql);
    }

    public async Task<List<Movie>> GetAsync()
    {
        await using var connection = _connectionFactory.Create();

        const string sql =
            """
            SELECT Id, Name
            FROM [dbo].[Movies]
            """;

        var movies = await connection.QueryAsync<Movie>(sql);
        return movies.AsList();
    }

    public async Task<Movie?> GetByIdAsync(int id)
    {
        await using var connection = _connectionFactory.Create();

        const string sql =
            """
            SELECT Id, Name
            FROM [dbo].[Movies]
            WHERE Id = @Id
            """;

        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(sql, new { id });
        return movie;
    }

    public async Task UpdateAsync(Movie movie)
    {
        await using var connection = _connectionFactory.Create();

        const string sql =
            """
            UPDATE [dbo].[Movies]
            SET
                Name = @Name
            WHERE Id = @Id
            """;

        _ = await connection.ExecuteAsync(sql, movie);
    }

    public async Task ResetIdentityAsync()
    {
        await using var connection = _connectionFactory.Create();

        await connection.ExecuteAsync("DBCC CHECKIDENT ('[dbo].[Movies]', RESEED, 0);");
    }

    private static IReadOnlyList<Movie> MoviesToInsert(int count)
    {
        var list = new List<Movie>(count);
        for (int i = 0; i < count; ++i)
        {
            list.Add(new Movie(0, Guid.NewGuid().ToString()));
        }

        return list;
    }
}