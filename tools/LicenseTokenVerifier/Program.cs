using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

const string publicKeyPath =
    @"D:\StoreLicenseSecrets\license-signing-public.pem";

if (!File.Exists(publicKeyPath))
{
    Console.WriteLine("لم يتم العثور على المفتاح العام.");
    return;
}

Console.WriteLine("الصق LicenseToken ثم اضغط Enter:");

var token = Console.ReadLine();

if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("الترخيص فارغ.");
    return;
}

using var ecdsa = ECDsa.Create();

ecdsa.ImportFromPem(File.ReadAllText(publicKeyPath));

var securityKey = new ECDsaSecurityKey(ecdsa);

var parameters = new TokenValidationParameters
{
    ValidateIssuer = false,
    ValidateAudience = false,

    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,

    IssuerSigningKey = securityKey,

    ClockSkew = TimeSpan.Zero
};

try
{
    var handler = new JwtSecurityTokenHandler();

    var principal = handler.ValidateToken(
        token,
        parameters,
        out var validatedToken);

    if (validatedToken is not JwtSecurityToken jwt ||
        jwt.Header.Alg != SecurityAlgorithms.EcdsaSha256 ||
        jwt.Header.Kid != "license-signing-key-v1")
    {
        Console.WriteLine("بيانات توقيع الترخيص غير مطابقة.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine("===== LICENSE VERIFIED =====");

    Console.WriteLine("التوقيع الرقمي صحيح.");

    foreach (var claim in principal.Claims)
    {
        Console.WriteLine($"{claim.Type}: {claim.Value}");
    }


    Console.WriteLine();
    Console.WriteLine("===== TAMPERING TEST =====");

    var parts = token.Split('.');

    var payloadJson = System.Text.Encoding.UTF8.GetString(
        Base64UrlEncoder.DecodeBytes(parts[1]));

    if (!payloadJson.Contains("\"max_devices\":\"3\""))
    {
        Console.WriteLine("لم يتم العثور على القيمة المتوقعة.");
        return;
    }

    payloadJson = payloadJson.Replace(
        "\"max_devices\":\"3\"",
        "\"max_devices\":\"99\"");

    var modifiedPayload = Base64UrlEncoder.Encode(
        System.Text.Encoding.UTF8.GetBytes(payloadJson));

    var tamperedToken = $"{parts[0]}.{modifiedPayload}.{parts[2]}";

    try
    {
        handler.ValidateToken(
            tamperedToken,
            parameters,
            out _);

        Console.WriteLine("فشل الاختبار: تم قبول الترخيص المعدل!");
    }
    catch (SecurityTokenException)
    {
        Console.WriteLine("نجح الاختبار: تم رفض الترخيص المعدل.");
    }
}
catch (SecurityTokenException ex)
{
    Console.WriteLine("فشل التحقق من الترخيص:");
    Console.WriteLine(ex.Message);
}
catch (Exception ex)
{
    Console.WriteLine("حدث خطأ:");
    Console.WriteLine(ex.Message);
}