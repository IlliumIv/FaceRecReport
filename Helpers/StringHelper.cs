using System.Text;
using System.Globalization;
using System.Security.Cryptography;

namespace Macroscop_FaceRecReport.Helpers
{
    public static class StringHelper
    {
        public static string CreateMD5(this string input)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++) sb.Append(hashBytes[i].ToString("X2", CultureInfo.CurrentCulture));
            return sb.ToString();
        }
    }
}