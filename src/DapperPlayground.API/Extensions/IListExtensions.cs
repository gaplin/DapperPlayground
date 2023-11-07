namespace DapperPlayground.API.Extensions;

public static class IListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        var random = new Random(420); // order consistent between calls
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
}
