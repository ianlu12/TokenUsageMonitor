using System.Text;

namespace TokenUsageMonitor.Services;

/// <summary>
/// Reversible XOR obfuscation for API keys at rest.
/// NOTE: this is obfuscation, NOT encryption. It stops casual text editors
/// from revealing keys but does not protect against a determined attacker.
/// This is a documented limitation of the first version.
/// </summary>
public static class KeyObfuscator
{
    private static readonly byte[] Seed =
    {
        0x7A, 0x1B, 0x3C, 0x4D, 0x55, 0x66, 0x77, 0x88,
        0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xF1, 0x23,
    };

    public static string Obfuscate(string? plain)
    {
        if (string.IsNullOrEmpty(plain))
        {
            return string.Empty;
        }

        byte[] input = Encoding.UTF8.GetBytes(plain);
        byte[] output = new byte[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            output[i] = (byte)(input[i] ^ Seed[i % Seed.Length]);
        }

        return Convert.ToBase64String(output);
    }

    public static string Deobfuscate(string? obfuscated)
    {
        if (string.IsNullOrEmpty(obfuscated))
        {
            return string.Empty;
        }

        try
        {
            byte[] input = Convert.FromBase64String(obfuscated);
            byte[] output = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                output[i] = (byte)(input[i] ^ Seed[i % Seed.Length]);
            }

            return Encoding.UTF8.GetString(output);
        }
        catch (FormatException)
        {
            // Legacy data stored in plain text: return as-is.
            return obfuscated;
        }
    }
}