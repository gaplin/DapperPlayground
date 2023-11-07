namespace DapperPlayground.API.Enums;

/// <summary>
/// 0 - Where In <br/>
/// 1 - Table valued parameter <br/>
/// 2 - Bulk insert with temp table
/// </summary>
public enum DeleteManyType
{
    In,
    Tvp,
    Bulk
}
