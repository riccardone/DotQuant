using System.Security.Cryptography;
using System.Text;
using DotQuant.Api.Contracts;

namespace DotQuant.Api.Services;

public class IdGenerator : IIdGenerator
{
    public string GenerateId(string prefix)
    {
        return $"{prefix}{Guid.NewGuid()}";
    }

    public string GenerateIdForForeignId(string foreignId)
    {
        // Compute a hash of the input string
        using var sha1 = SHA1.Create();
        byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(foreignId));

        // Take the first 16 bytes of the hash to create a GUID
        byte[] guidBytes = new byte[16];
        Array.Copy(hashBytes, guidBytes, 16);

        // Set the variant and version fields to ensure a valid GUID
        guidBytes[8] = (byte)((guidBytes[8] & 0xBF) | 0x80); // Variant
        guidBytes[7] = (byte)((guidBytes[7] & 0x4F) | 0x40); // Version

        return new Guid(guidBytes).ToString();
    }
}