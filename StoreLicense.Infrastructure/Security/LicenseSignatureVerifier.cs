using System.Security.Cryptography;
using System.Globalization;
using System.Text;

namespace StoreLicense.Infrastructure.Security;

public static class LicenseSignatureVerifier
{
    public static bool Verify(
        string publicKey,
        string signature,
        Guid deviceId,
        string deviceIdentifier,
        long timestamp,
        Guid nonce)
    {
        if (string.IsNullOrWhiteSpace(publicKey) ||
            string.IsNullOrWhiteSpace(signature) ||
            string.IsNullOrWhiteSpace(deviceIdentifier))
        {
            return false;
        }

        try
        {
            var publicKeyBytes = Convert.FromBase64String(publicKey);
            var signatureBytes = Convert.FromBase64String(signature);

            var payload = BuildPayload(
                deviceId,
                deviceIdentifier,
                timestamp,
                nonce);

            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var ecdsa = ECDsa.Create();

            ecdsa.ImportSubjectPublicKeyInfo(
                publicKeyBytes,
                out var bytesRead);

            if (bytesRead != publicKeyBytes.Length)
                return false;

            return ecdsa.VerifyData(
                payloadBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (FormatException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private static string BuildPayload(
        Guid deviceId,
        string deviceIdentifier,
        long timestamp,
        Guid nonce)
    {
        return string.Join(
            "\n",
            deviceId.ToString("D"),
            deviceIdentifier,
            timestamp.ToString(CultureInfo.InvariantCulture),
            nonce.ToString("D"));
    }
}