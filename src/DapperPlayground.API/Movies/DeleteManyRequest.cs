using DapperPlayground.API.Enums;

namespace DapperPlayground.API.Movies;

public record DeleteManyRequest(int StartIdx, int Count, DeleteManyType DeleteManyType = DeleteManyType.In);
