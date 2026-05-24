using System.Globalization;
using System.Text;

namespace YemekliYilan.Api.Services;

public static class UsernameNormalizer
{
    public static string Normalize(string username)
    {
        var lowerText = username.Trim().ToLower(new CultureInfo("tr-TR"));

        lowerText = lowerText
            .Replace("ı", "i")
            .Replace("ğ", "g")
            .Replace("ü", "u")
            .Replace("ş", "s")
            .Replace("ö", "o")
            .Replace("ç", "c");

        var builder = new StringBuilder();

        foreach (var character in lowerText)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}