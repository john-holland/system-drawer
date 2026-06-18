using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>SHA-256 lobby password hashing with session salt.</summary>
public static class LobbyPasswordHash
{
    public static string Hash(string password, string sessionName)
    {
        if (string.IsNullOrEmpty(password))
            return "";
        string salt = string.IsNullOrEmpty(sessionName) ? "Drawer 2" : sessionName;
        string payload = password + "|" + salt;
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var sb = new StringBuilder(bytes.Length * 2);
        for (int i = 0; i < bytes.Length; i++)
            sb.Append(bytes[i].ToString("x2"));
        return sb.ToString();
    }

    public static bool Verify(string password, string sessionName, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash))
            return string.IsNullOrEmpty(password);
        string actual = Hash(password, sessionName);
        return FixedTimeEquals(actual, expectedHash);
    }

    static bool FixedTimeEquals(string a, string b)
    {
        if (a == null || b == null)
            return false;
        int diff = a.Length ^ b.Length;
        int n = Math.Min(a.Length, b.Length);
        for (int i = 0; i < n; i++)
            diff |= a[i] ^ b[i];
        return diff == 0 && a.Length == b.Length;
    }
}
