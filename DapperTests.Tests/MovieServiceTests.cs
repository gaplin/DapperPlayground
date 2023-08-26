using Dapper;
using DapperTests.API.ConnectionFactories;
using DapperTests.API.Movies;
using Testcontainers.MsSql;

namespace DapperTests.Tests;

public sealed class MovieServiceTests : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();
    private SqlConnectionFactory _connectionFactory = default!;
    private MovieService _movieService = default!;


    [Fact]
    public async Task CreateAndGet()
    {
        // Arrange
        var movie = new Movie(3, "Name");

        // Act
        await _movieService.CreateAsync(movie);
        var insertedMovie = await _movieService.GetByIdAsync(1); // 1 -> auto increment

        // Assert
        insertedMovie.Should().NotBeNull();
        insertedMovie!.Name.Should().Be("Name"); ;
    }

    [Fact]
    public async Task CreateAndDelete()
    {
        // Arrange
        var movie = new Movie(3, "Name");

        // Act
        await _movieService.CreateAsync(movie);
        await _movieService.DeleteAsync(1);
        var insertedMovie = await _movieService.GetByIdAsync(1); // 1 -> auto increment

        // Assert
        insertedMovie.Should().BeNull();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        _connectionFactory = new(_dbContainer.GetConnectionString());

        await using var connection = _connectionFactory.Create();

        await connection.ExecuteAsync(
            """
            CREATE TABLE [dbo].[Movies] (
            [Id]   INT          IDENTITY (1, 1) NOT NULL,
            [Name] VARCHAR (50) NOT NULL,
            CONSTRAINT [PK_Movies] PRIMARY KEY CLUSTERED ([Id] ASC)
            )
            """
            );

        _movieService = new MovieService(_connectionFactory);
    }
}