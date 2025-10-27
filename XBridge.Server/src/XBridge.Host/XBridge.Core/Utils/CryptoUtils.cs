using System;
using System.Security.Cryptography;
using System.Text;


namespace XBridge.Host.XBridge.Core.Utils
{
public static class CryptoUtils
{
public static string ComputeSHA256(string input)
{
using var sha = SHA256.Create();
var bytes = Encoding.UTF8.GetBytes(input);
var hash = sha.ComputeHash(bytes);
var sb = new StringBuilder();
foreach (var b in hash) sb.Append(b.ToString("x2"));
return sb.ToString();
}
}
}