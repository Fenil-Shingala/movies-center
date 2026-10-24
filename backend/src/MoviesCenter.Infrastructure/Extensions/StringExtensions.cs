using System.Text.RegularExpressions;

namespace MoviesCenter.Infrastructure.Extensions;

public static partial class StringExtensions
{
    [GeneratedRegex("(?<!^)(?=[A-Z])")]
    private static partial Regex SnakeCaseRegex();

    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return SnakeCaseRegex().Replace(input, "_").ToLowerInvariant();
    }
}
