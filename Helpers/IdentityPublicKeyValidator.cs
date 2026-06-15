using System.Security.Cryptography;
using System.Text.Json;

namespace SecureMessaging.Api.Helpers;

public static class IdentityPublicKeyValidator
{
    public static string ValidateAndNormalize(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !HasString(root, "kty", "EC") ||
                !HasString(root, "crv", "P-256") ||
                !HasNonEmptyString(root, "x") ||
                !HasNonEmptyString(root, "y"))
            {
                throw new InvalidOperationException(
                    "Identity public key must be a valid P-256 EC public JWK.");
            }

            var x = DecodeBase64Url(root.GetProperty("x").GetString()!);
            var y = DecodeBase64Url(root.GetProperty("y").GetString()!);

            if (x.Length != 32 || y.Length != 32)
            {
                throw new InvalidOperationException(
                    "Identity public key must be a valid P-256 EC public JWK.");
            }

            using var key = ECDiffieHellman.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = x, Y = y }
            });

            var parameters = key.ExportParameters(false);

            return JsonSerializer.Serialize(new
            {
                kty = "EC",
                crv = "P-256",
                x = EncodeBase64Url(parameters.Q.X!),
                y = EncodeBase64Url(parameters.Q.Y!)
            });
        }
        catch (JsonException)
        {
            throw new InvalidOperationException(
                "Identity public key must be valid JSON.");
        }
        catch (Exception exception) when (
            exception is FormatException or CryptographicException)
        {
            throw new InvalidOperationException(
                "Identity public key must be a valid P-256 EC public JWK.");
        }
    }

    private static bool HasString(JsonElement root, string propertyName, string expected)
    {
        return root.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            string.Equals(value.GetString(), expected, StringComparison.Ordinal);
    }

    private static bool HasNonEmptyString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(value.GetString());
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var normalized = value
            .Replace('-', '+')
            .Replace('_', '/')
            .PadRight((value.Length + 3) / 4 * 4, '=');

        return Convert.FromBase64String(normalized);
    }

    private static string EncodeBase64Url(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
