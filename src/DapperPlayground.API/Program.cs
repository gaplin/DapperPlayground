using DapperPlayground.API.ConnectionFactories;
using DapperPlayground.API.Movies;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Local.json", true, false)
    .AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    opts.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

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

movies.MapPost("/createMany", async (CreateManyRequest request, IMovieService service) =>
{
    await service.CreateManyAsync(request.Quantity, request.CreateManyType);
    return Results.Ok();
});

movies.MapPost("/resetIdentity", async (IMovieService service) =>
{
    await service.ResetIdentityAsync();
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

movies.MapDelete("/deleteMany", async ([FromBody] DeleteManyRequest request, IMovieService service) =>
{
    await service.DeleteManyAsync(request);
    return Results.Ok();
});

movies.MapDelete("/", async (IMovieService service) =>
{
    await service.DeleteAllAsync();
    return Results.Ok();
});

app.Run();