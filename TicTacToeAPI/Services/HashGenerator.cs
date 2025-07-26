using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;
using TicTacToeAPI.Models.DTO;

namespace TicTacToeAPI.Services
{
    public static class HashGenerator
    {
        public static string GenerateRequestHash(MoveRequest moveRequest)
        {
            string requestJson = JsonConvert.SerializeObject(moveRequest, Formatting.None);
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(requestJson));
                return Convert.ToBase64String(hashBytes);
            }
        }
        public static string GenerateETag(int version)
        {
            byte[] versionBytes = BitConverter.GetBytes(version);

            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(versionBytes);

            return Convert.ToBase64String(hashBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
