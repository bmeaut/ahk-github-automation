namespace GradeManagement.Bll.Services.Utils;

using System.Security.Cryptography;
using System.Text;

public class RsaKeyGenerator
{
    public string PrivateKey { get; private set; }
    public string PublicKey { get; private set; }

    public RsaKeyGenerator()
    {
        using var rsa = RSA.Create(2048);

        PrivateKey = ExportPrivateKey(rsa);
        PublicKey = ExportPublicKey(rsa);
    }

    private string ExportPrivateKey(RSA rsa)
    {
        var privateKeyBytes = rsa.ExportRSAPrivateKey();
        var base64PrivateKey = Convert.ToBase64String(privateKeyBytes);
        var sb = new StringBuilder();
        sb.AppendLine("-----BEGIN RSA PRIVATE KEY-----");
        for (int i = 0; i < base64PrivateKey.Length; i += 64)
        {
            sb.AppendLine(base64PrivateKey.Substring(i, Math.Min(64, base64PrivateKey.Length - i)));
        }

        sb.AppendLine("-----END RSA PRIVATE KEY-----");
        return sb.ToString();
    }

    private string ExportPublicKey(RSA rsa)
    {
        var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        var base64PublicKey = Convert.ToBase64String(publicKeyBytes);
        var sb = new StringBuilder();
        sb.AppendLine("-----BEGIN PUBLIC KEY-----");
        for (int i = 0; i < base64PublicKey.Length; i += 64)
        {
            sb.AppendLine(base64PublicKey.Substring(i, Math.Min(64, base64PublicKey.Length - i)));
        }
        sb.AppendLine("-----END PUBLIC KEY-----");
        return sb.ToString();
    }
}
