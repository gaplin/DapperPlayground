namespace DapperPlayground.API.Enums;

/// <summary>
/// 0 - Normal <br/>
/// 1 - Faster <br/>
/// 2 - Bulk <br/>
/// 3 - TVP <br/>
/// 4 - TVP SqlDataRecord
/// </summary>
public enum CreateManyType
{
    Normal,
    Faster,
    Bulk,
    Tvp,
    TvpSqlDataRecord
}
