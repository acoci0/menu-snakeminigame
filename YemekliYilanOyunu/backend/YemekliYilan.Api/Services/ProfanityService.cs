using System.Globalization;
using System.Text;

namespace YemekliYilan.Api.Services;

public static class ProfanityService
{
    private static readonly string[] BannedWords =
    {
        "salak", "aptal", "gerizekali", "gerizekalı", "mal",
        "amk", "aq", "orospu", "pic", "piç", "got", "göt",
        "kahpe", "surtuk", "sürtük", "orospu çocuğu", "orospu çocuğu","amk", "aq","yavşak", "yavşak", "anan", "ananı", "ananı sikeyim", "ananı sikiyim", "ananı sikerim","götlek", "gotlek", "götünek", "gotünek",
        "fuck", "shit", "bitch", "asshole", "pipi", "dick", "cunt", "amcık", "amcı", "yarrak", "yarak", "sikerim", "sikim", "sikiyim", "sik", "sikik", "sikiş", "sikişim", "sikme", "sikmeyi", "sikmiyorum", "sikiyorum", "sikiyor", "sikiyo", "sikiyo", "siktir", "siktir", "siktir", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "siktr", "seks", "sex", "porn"
    };

    public static bool ContainsBadWord(string username)
    {
        var normalizedUsername = Normalize(username);

        return BannedWords.Any(word =>
            normalizedUsername.Contains(Normalize(word))
        );
    }

    private static string Normalize(string text)
    {
        var lowerText = text.ToLower(new CultureInfo("tr-TR"));

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