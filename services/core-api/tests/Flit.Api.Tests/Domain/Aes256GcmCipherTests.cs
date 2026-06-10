using System.Security.Cryptography;
using FluentAssertions;
using Flit.Modules.Auth.Domain;
using Xunit;

namespace Flit.Api.Tests.Domain;

/// <summary>
/// Tests del cifrador AES-256-GCM para MFA secrets (ADR-0010).
/// Verifican roundtrip, deteccion de tampering, separacion por KeyId, validacion de key length.
/// </summary>
public class Aes256GcmCipherTests
{
    private static byte[] NewMasterKey()
    {
        var k = new byte[32];
        RandomNumberGenerator.Fill(k);
        return k;
    }

    [Fact]
    public void Encrypt_then_Decrypt_roundtrip_returns_original_secret()
    {
        var cipher = new Aes256GcmSecretCipher(NewMasterKey(), "v1");
        const string plaintext = "JBSWY3DPEHPK3PXP"; // ejemplo base32

        var encrypted = cipher.Encrypt(plaintext);
        var decrypted = cipher.Decrypt(encrypted);

        decrypted.Should().Be(plaintext);
        encrypted.KeyId.Should().Be("v1");
        encrypted.Nonce.Length.Should().Be(12);
        encrypted.Ciphertext.Length.Should().BeGreaterThan(plaintext.Length); // +tag
    }

    [Fact]
    public void Encrypt_produces_different_ciphertext_each_call_same_plaintext()
    {
        var cipher = new Aes256GcmSecretCipher(NewMasterKey(), "v1");
        const string plaintext = "JBSWY3DPEHPK3PXP";

        var a = cipher.Encrypt(plaintext);
        var b = cipher.Encrypt(plaintext);

        // Nonce aleatorio por cifrado → ciphertext distinto.
        a.Ciphertext.Should().NotBeEquivalentTo(b.Ciphertext);
        a.Nonce.Should().NotBeEquivalentTo(b.Nonce);
    }

    [Fact]
    public void Decrypt_with_wrong_keyId_throws()
    {
        var key = NewMasterKey();
        var cipherV1 = new Aes256GcmSecretCipher(key, "v1");
        var cipherV2 = new Aes256GcmSecretCipher(key, "v2");

        var encryptedV1 = cipherV1.Encrypt("ABCDEFGHIJ");

        Action act = () => cipherV2.Decrypt(encryptedV1);
        act.Should().Throw<InvalidOperationException>().WithMessage("*key_id*");
    }

    [Fact]
    public void Decrypt_with_tampered_ciphertext_throws()
    {
        var cipher = new Aes256GcmSecretCipher(NewMasterKey(), "v1");
        var encrypted = cipher.Encrypt("ABCDEFGHIJ");

        // Flip un bit del ciphertext.
        encrypted.Ciphertext[0] ^= 0x01;

        Action act = () => cipher.Decrypt(encrypted);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Constructor_rejects_invalid_key_length()
    {
        Action act16 = () => new Aes256GcmSecretCipher(new byte[16], "v1");
        Action act64 = () => new Aes256GcmSecretCipher(new byte[64], "v1");

        act16.Should().Throw<ArgumentException>();
        act64.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromEnv_decodes_base64_key()
    {
        var key = NewMasterKey();
        var base64 = Convert.ToBase64String(key);

        var cipher = Aes256GcmSecretCipher.FromEnv(base64, "v1");
        var encrypted = cipher.Encrypt("XYZ");
        cipher.Decrypt(encrypted).Should().Be("XYZ");
    }
}
