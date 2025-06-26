using OtpNet;

public static class MfaHelper
{
    public static string GenerateSecret()
    {
        var secret = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(secret);
    }
}
