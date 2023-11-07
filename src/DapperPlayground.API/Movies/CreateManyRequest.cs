using DapperPlayground.API.Enums;

namespace DapperPlayground.API.Movies;

public record CreateManyRequest(int Quantity, CreateManyType CreateManyType);
