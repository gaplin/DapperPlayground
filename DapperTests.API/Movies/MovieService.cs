using Dapper;
using DapperTests.API.ConnectionFactories;

namespace DapperTests.API.Movies;

public sealed class MovieService : IMovieService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MovieService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task CreateAsync(Movie movie)
    {
        await using var connection = _connectionFactory.Create();

        const string sql =
            """
            INSERT INTO Movies (Name)
            VALUES (@Name)
            """;

        _ = await connection.ExecuteAsync(sql, movie);
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
}