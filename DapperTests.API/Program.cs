using DapperTests.API.ConnectionFactories;
using DapperTests.API.Movies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>(
    serviceProvider =>
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("Database")
                               ?? throw new ApplicationException("Database connection string is missing");

        return new SqlConnectionFactory(connectionString);
    }
    );

builder.Services.AddScoped<IMovieService, MovieService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var movies = app.MapGroup("movies");

movies.MapGet("/", async (IMovieService service) =>
{
    var movies = await service.GetAsync();
    return Results.Ok(movies);
});

movies.MapGet("/{id}", async (int id, IMovieService service) =>
{
    var movie = await service.GetByIdAsync(id);
    return movie is null ?
        Results.NotFound() :
        Results.Ok(movie);
});

movies.MapPost("/", async (Movie movie, IMovieService service) =>
{
    await service.CreateAsync(movie);
    return Results.Ok();
});

movies.MapPost("/createMany", async (int count, IMovieService service) =>
{
    await service.CreateManyAsync(count);
    return Results.Ok();
});

movies.MapPut("/", async (Movie movie, IMovieService service) =>
{
    await service.UpdateAsync(movie);
    return Results.Ok();
});

movies.MapDelete("/{id}", async (int id, IMovieService service) =>
{
    await service.DeleteAsync(id);
    return Results.Ok();
});

app.Run();