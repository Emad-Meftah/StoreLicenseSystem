
namespace StoreLicense.Core.Models.Activation;

public class ActivateCommand
{
    public string ActivationCode { get; set; } = string.Empty;

    public string DeviceIdentifier { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public string DevicePublicKey { get; set; } = string.Empty;
}
