using System.Text;
using System.Globalization;
using System.Security.Cryptography;

namespace FaceRecReport.Helpers;

public static class StringHelper
{
    public static string CreateMD5(this string input)
    {
        var inputBytes = Encoding.ASCII.GetBytes(input);
        var hashBytes = MD5.HashData(inputBytes);
        var sb = new StringBuilder();

        for (var i = 0; i < hashBytes.Length; i++)
            sb.Append(hashBytes[i].ToString("X2", CultureInfo.CurrentCulture));

        return sb.ToString();
    }

    public static string ReplaceWhitespace(this string input)
    {
        var chars = input.ToCharArray().Where(c => !char.IsWhiteSpace(c)).ToArray();

        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = chars[i];

            if (chars[i] == ',')
            {
                chars[i + 1] = ' ';
            }
        }

        return new(chars);
    }
}