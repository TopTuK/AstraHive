using AH.AppDomain.Users;

namespace AH.Application.Tests.Domain.Users;

public sealed class UserCertificateTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly string Thumbprint = new('a', UserCertificate.ThumbprintLength);

    private static UserCertificate Register(string? objectKey = "certificates/1/a.pfx") =>
        UserCertificate.Register(1, Thumbprint, "01AB", Now.AddDays(-1), Now.AddYears(2), objectKey, issuedById: 2, Now);

    [Fact]
    public void Register_LowercaseThumbprint_StoresUppercase()
    {
        var certificate = Register();

        Assert.Equal(Thumbprint.ToUpperInvariant(), certificate.Thumbprint);
        Assert.Equal(Now, certificate.IssuedAt);
        Assert.True(certificate.IsActive(Now));
    }

    [Fact]
    public void Register_ShortThumbprint_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserCertificate.Register(1, "ABCD", "01", Now, Now.AddYears(1), null, null, Now));
    }

    [Fact]
    public void Register_ExpiryBeforeStart_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            UserCertificate.Register(1, Thumbprint, "01", Now, Now.AddDays(-1), null, null, Now));
    }

    [Fact]
    public void IsActive_AfterExpiry_ReturnsFalse()
    {
        var certificate = Register();

        Assert.False(certificate.IsActive(Now.AddYears(3)));
    }

    [Fact]
    public void Revoke_ActiveCertificate_DeactivatesAndDropsObjectKey()
    {
        var certificate = Register();

        certificate.Revoke(Now.AddHours(1));

        Assert.True(certificate.IsRevoked);
        Assert.Equal(Now.AddHours(1), certificate.RevokedAt);
        Assert.Null(certificate.ObjectKey);
        Assert.False(certificate.IsActive(Now.AddHours(2)));
    }

    [Fact]
    public void Revoke_RevokedCertificate_ThrowsInvalidOperationException()
    {
        var certificate = Register();
        certificate.Revoke(Now);

        Assert.Throws<InvalidOperationException>(() => certificate.Revoke(Now));
    }
}
