using System.Security.Cryptography;
using DevHabit.Api.Options;
using DevHabit.Api.Services;
using Microsoft.Extensions.Options;

namespace DevHabit.UnitTests.Services;

public sealed class EncryptionServiceTests
{
    private readonly EncryptionService _sut;

    public EncryptionServiceTests()
    {
        IOptions<EncryptionOptions> encryptionOptions = Options.Create(new EncryptionOptions
        {
            Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        });

        _sut = new EncryptionService(encryptionOptions);
    }

    [Fact]
    public void Decrypt_ShouldReturnPlainText_WhenDecryptingCorrectCiphertext()
    {
        const string plainText = "sensitive data";
        string ciphertext = _sut.Encrypt(plainText);

        string decryptedCiphertext = _sut.Decrypt(ciphertext);

        Assert.Equal(plainText, decryptedCiphertext);
    }
}
