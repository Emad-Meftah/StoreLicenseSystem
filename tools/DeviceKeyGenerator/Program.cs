using System.Security.Cryptography;
using System.Text;

var privateKeyPath = Path.Combine(
    AppContext.BaseDirectory,
    "device-private-key.txt");

// نبحث أيضاً في مجلد المشروع عند التشغيل عبر dotnet run.
if (!File.Exists(privateKeyPath))
{
    privateKeyPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "tools",
        "DeviceKeyGenerator",
        "device-private-key.txt");
}

if (!File.Exists(privateKeyPath))
{
    Console.WriteLine("لم يتم العثور على ملف المفتاح الخاص.");
    return;
}

var privateKey = Convert.FromBase64String(
    File.ReadAllText(privateKeyPath).Trim());

var deviceId = Guid.Parse(
    "4b6574fc-d7dc-477c-8b8a-b0da1e15569e");

var deviceIdentifier = "TEST-DEVICE-005";

var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

var nonce = Guid.NewGuid();

var payload = string.Join(
    "\n",
    deviceId.ToString("D"),
    deviceIdentifier,
    timestamp,
    nonce.ToString("D"));

using var ecdsa = ECDsa.Create();

ecdsa.ImportPkcs8PrivateKey(
    privateKey,
    out _);

var signature = ecdsa.SignData(
    Encoding.UTF8.GetBytes(payload),
    HashAlgorithmName.SHA256,
    DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

Console.WriteLine("===== VALIDATION REQUEST =====");
Console.WriteLine($"DeviceId: {deviceId}");
Console.WriteLine($"DeviceIdentifier: {deviceIdentifier}");
Console.WriteLine($"Timestamp: {timestamp}");
Console.WriteLine($"Nonce: {nonce}");
Console.WriteLine($"Signature: {Convert.ToBase64String(signature)}");