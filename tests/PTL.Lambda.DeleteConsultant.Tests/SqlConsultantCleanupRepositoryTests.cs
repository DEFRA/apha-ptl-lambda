namespace PTL.Lambda.DeleteConsultant.Tests;

public class SqlConsultantCleanupRepositoryTests
{
    [Fact]
    public void SplitByConsultantType_SeparatesInternalAndExternalIds()
    {
        var internalId = Guid.NewGuid();
        var externalId = Guid.NewGuid();
        var batch = new List<ConsultantToDelete>
        {
            new() { Id = internalId, ConsultantType = ConsultantType.Internal },
            new() { Id = externalId, ConsultantType = ConsultantType.External }
        };

        var (internalIds, externalIds) = SqlConsultantCleanupRepository.SplitByConsultantType(batch);

        Assert.Equal([internalId], internalIds);
        Assert.Equal([externalId], externalIds);
    }

    [Fact]
    public void SplitByConsultantType_ReturnsEmptyLists_WhenBatchIsEmpty()
    {
        var (internalIds, externalIds) = SqlConsultantCleanupRepository.SplitByConsultantType([]);

        Assert.Empty(internalIds);
        Assert.Empty(externalIds);
    }

    [Fact]
    public void SplitByConsultantType_PutsEverything_InOneBucket_WhenOnlyOneTypePresent()
    {
        var batch = new List<ConsultantToDelete>
        {
            new() { Id = Guid.NewGuid(), ConsultantType = ConsultantType.External },
            new() { Id = Guid.NewGuid(), ConsultantType = ConsultantType.External }
        };

        var (internalIds, externalIds) = SqlConsultantCleanupRepository.SplitByConsultantType(batch);

        Assert.Empty(internalIds);
        Assert.Equal(2, externalIds.Count);
    }
}
