using PTL.Lambda.Shared;

namespace PTL.Lambda.Shared.Tests;

public class CleanupResultTests
{
    [Fact]
    public void Constructor_AssignsAllProperties()
    {
        var invokedAtUtc = DateTime.UtcNow;

        var result = new CleanupResult("DeleteAttachments", "Stub", "message", invokedAtUtc);

        Assert.Equal("DeleteAttachments", result.Service);
        Assert.Equal("Stub", result.Status);
        Assert.Equal("message", result.Message);
        Assert.Equal(invokedAtUtc, result.InvokedAtUtc);
    }
}
