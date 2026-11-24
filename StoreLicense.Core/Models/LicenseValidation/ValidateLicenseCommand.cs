namespace StoreLicense.Core.Models.LicenseValidation;

public class ValidateLicenseCommand
{
    public Guid DeviceId { get; set; }

    public string DeviceIdentifier { get; set; } = string.Empty;

    public long Timestamp { get; set; }

    public Guid Nonce { get; set; }

    public string Signature { get; set; } = string.Empty;
}