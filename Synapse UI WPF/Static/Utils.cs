using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Synapse_UI_WPF.Static
{
    public static class Utils
    {
        
        public static string Sha512(string Input, bool IsFile = false)
        {
            var bytes = IsFile ? File.ReadAllBytes(Input) : Encoding.ASCII.GetBytes(Input);
            using (var hash = SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);
                var hashedInputStringBuilder = new StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("X2"));
                return hashedInputStringBuilder.ToString();
            }
        }
    }
}
