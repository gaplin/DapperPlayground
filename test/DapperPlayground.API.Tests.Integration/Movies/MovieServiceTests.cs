using Dapper;
using DapperPlayground.API.ConnectionFactories;
using DapperPlayground.API.Movies;
using Microsoft.Extensions.DependencyInjection;

namespace DapperPlayground.API.Tests.Integration.Movies;

public sealed class MovieServiceTests : IClassFixture<MovieTestsFixture>, IAsyncLifetime
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMovieService _movieService;

    public MovieServiceTests(MovieTestsFixture testsFixture)
    {
        _serviceProvider = testsFixture.ServiceProvider;
        _movieService = _serviceProvider.GetRequiredService<IMovieService>();
    }

    [Fact]
    public async Task CreateAndGet()
    {
        // Arrange
        var movie = new Movie(3, "Name");

        // Act
        var insertedId = await _movieService.CreateAsync(movie);
        var insertedMovie = await _movieService.GetByIdAsync(insertedId);

        // Assert
        insertedMovie.Should().NotBeNull();
        insertedMovie!.Name.Should().Be("Name");
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
        insertedMovies.Should().SatisfyRespectively(
            first =>
            {
                first.Name.Should().Be(firstMovie.Name);
            },
            secondMovie =>
            {
                secondMovie.Name.Should().Be(secondMovie.Name);
            }
            );
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

        // Act
        var insertedId = await _movieService.CreateAsync(movie);
        var movieUpdate = new Movie(insertedId, "UpdatedName");
        await _movieService.UpdateAsync(movieUpdate);
        var updatedMovie = await _movieService.GetByIdAsync(insertedId);

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

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        var connectionFactory = _serviceProvider.GetRequiredService<ISqlConnectionFactory>();
        await using var connection = connectionFactory.Create();
        await connection.ExecuteAsync("DELETE FROM Movies");
    }
}