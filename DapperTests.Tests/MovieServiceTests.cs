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
    public async Task CreateManyAndGetAll()
    {
        // Arrange
        var firstMovie = new Movie(100, "firstMovie");
        var secondMovie = new Movie(1000, "secondMovie");

        // Act
        await _movieService.CreateAsync(firstMovie);
        await _movieService.CreateAsync(secondMovie);
        var insertedMovies = await _movieService.GetAsync();

        // Assert
        insertedMovies.Should().Contain(new Movie[]
        {
            firstMovie with { Id = 1 },
            secondMovie with { Id = 2 },
        });
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

    [Fact]
    public async Task CreateUpdateAndGet()
    {
        // Arrange
        var movie = new Movie(5, "Movie");
        var movieUpdate = new Movie(1, "UpdatedName");

        // Act
        await _movieService.CreateAsync(movie);
        await _movieService.UpdateAsync(movieUpdate);
        var updatedMovie = await _movieService.GetByIdAsync(1);

        // Assert
        updatedMovie.Should().Be(movieUpdate);
    }

    [Fact]
    public async Task GetById_ReturnsNull_WhenMovieDoesNotExist()
    {
        // Arrange
        var id = 1;

        // Act
        var result = await _movieService.GetByIdAsync(id);

        // Assert
        result.Should().BeNull();
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