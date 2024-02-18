using System.Security.Cryptography;
using System.Text;

namespace AccountSyncData.Consumer.Encryption
{
    public static class SHACryptor
    {
        public static string GenerateSaltKey(int size)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }
    }
}