using System.Security.Cryptography;
using System.Text;

namespace fluxel.Utils; 

public class Hashing {
    public static string GetHash(string input) => BitConverter.ToString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", "").ToLower();
    public static string GetHash(byte[] input) => BitConverter.ToString(SHA256.Create().ComputeHash(input)).Replace("-", "").ToLower();
    public static string GetHash(Stream input) => BitConverter.ToString(SHA256.Create().ComputeHash(input)).Replace("-", "").ToLower();
}