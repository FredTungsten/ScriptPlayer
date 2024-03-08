using System.Security.Cryptography;
using System.Text;

namespace ScriptPlayer.Shared.TheHandyV2
{
    public static class Sha256HashHelper
    {
        public static string Calculate(byte[] data)
        {
            var sha256 = new SHA256Managed();
            byte[] hash = sha256.ComputeHash(data);
            return BytesToHexString(hash);
        }

        public static string BytesToHexString(byte[] bytes)
        {
            StringBuilder builder = new StringBuilder(bytes.Length * 2);

            foreach (byte theByte in bytes)
            {
                builder.Append(theByte.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}