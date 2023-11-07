using Dapper;
using DapperPlayground.API.ConnectionFactories;
using DapperPlayground.API.Enums;
using Microsoft.Data.SqlClient;
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
            INSERT INTO Movies (Name)
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
            var watch = Stopwatch.StartNew();
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

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            await transaction.CommitAsync();
            Console.WriteLine(watch.ElapsedMilliseconds);
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
            INSERT INTO [Movies] (Name)
            VALUES
            (@Name)
            """;

        await connection.ExecuteAsync(sql, movies, transaction: transaction);
    }

    private static async Task BulkInsertAsync(SqlConnection connection, IReadOnlyList<Movie> movies, SqlTransaction transaction)
    {
        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction);
        bulkCopy.DestinationTableName = "[Movies]";
        bulkCopy.ColumnMappings.Add("Name", "Name");

        using var table = new DataTable();
        table.Columns.Add("Name", typeof(string));
        foreach (var movie in movies)
        {
            table.Rows.Add(movie.Name);
        }

        await bulkCopy.WriteToServerAsync(table);
    }

    private static async Task FastInsertAsync(int count, int batchSize, SqlConnection connection, IReadOnlyList<Movie> movies, SqlTransaction transaction)
    {
        const string baseSql =
                """
                INSERT INTO [Movies] (Name)
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
            DELETE Movies
            WHERE Id = @Id
            """;

        _ = await connection.ExecuteAsync(sql, new { id });
    }

    public async Task DeleteAllAsync()
    {
        await using var connection = _connectionFactory.Create();

        const string sql = "DELETE FROM Movies";

        _ = await connection.ExecuteAsync(sql);
    }

    public async Task<List<Movie>> GetAsync()
    {
        await using var connection = _connectionFactory.Create();

        const string sql =
            """
            SELECT Id, Name
            FROM Movies
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
            FROM Movies
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
            UPDATE Movies
            SET
                Name = @Name
            WHERE Id = @Id
            """;

        _ = await connection.ExecuteAsync(sql, movie);
    }

    public async Task ResetIdentityAsync()
    {
        await using var connection = _connectionFactory.Create();

        await connection.ExecuteAsync("DBCC CHECKIDENT ('[Movies]', RESEED, 0);");
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