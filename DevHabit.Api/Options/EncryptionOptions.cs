namespace DevHabit.Api.Options;

public sealed class EncryptionOptions
{
    public const string Section = "Encryption";

    public required string Key { get; init; }
}
