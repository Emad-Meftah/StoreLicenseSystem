using System.Security.Cryptography;

const string secretsDirectory = @"D:\StoreLicenseSecrets";

Directory.CreateDirectory(secretsDirectory);

var privateKeyPath = Path.Combine(
    secretsDirectory,
    "license-signing-private.pem");

var publicKeyPath = Path.Combine(
    secretsDirectory,
    "license-signing-public.pem");

// منع الكتابة فوق مفاتيح موجودة بالخطأ.
if (File.Exists(privateKeyPath) || File.Exists(publicKeyPath))
{
    Console.WriteLine(
        "توجد مفاتيح سابقة. تم إيقاف العملية لحمايتها.");

    return;
}

using var ecdsa = ECDsa.Create(
    ECCurve.NamedCurves.nistP256);

var privateKey = ecdsa.ExportPkcs8PrivateKeyPem();

var publicKey = ecdsa.ExportSubjectPublicKeyInfoPem();

File.WriteAllText(privateKeyPath, privateKey);

File.WriteAllText(publicKeyPath, publicKey);

Console.WriteLine("تم إنشاء مفاتيح توقيع الترخيص بنجاح.");

Console.WriteLine($"Private Key: {privateKeyPath}");
Console.WriteLine($"Public Key: {publicKeyPath}");

Console.WriteLine();
Console.WriteLine("===== PUBLIC KEY =====");
Console.WriteLine(publicKey);

Console.WriteLine();
Console.WriteLine("احتفظ بالمفتاح الخاص سرياً.");