using System.Security.Cryptography;
using System.Text;

namespace MyVexBooks.Helpers
{
    public class EncriptacionHelper
    {
        public static string GetHash(string texto)
        {
            texto += "SGTCURSO1";
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(texto); 
            byte[] hashBytes = sha256.ComputeHash(bytes); 
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2")); 
            }
            return sb.ToString();
        }
    }
}
