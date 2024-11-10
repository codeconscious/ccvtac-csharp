namespace CCVTAC.Console;

internal class Comparers
{
    public sealed class CaseInsensitiveStringComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y)
        {
            return (x, y) switch
            {
                (null, null) => true,
                (null, _) or (_, null) => false,
                _ => string.Equals(x.Trim(), y.Trim(), StringComparison.OrdinalIgnoreCase),
            };
        }

        public int GetHashCode(string obj)
        {
            return obj.ToLower().GetHashCode();
        }
    }
}
