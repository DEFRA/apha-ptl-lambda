namespace PTL.Lambda.DeleteAttachments.Tests;

public class SqlAttachmentCleanupRepositoryTests
{
    [Fact]
    public void SplitByAttachmentType_SeparatesInternalAndPublishedIds()
    {
        var internalId = Guid.NewGuid();
        var publishedId = Guid.NewGuid();
        var batch = new List<AttachmentToDelete>
        {
            new() { Id = internalId, AttachmentType = AttachmentType.InternalAttachment },
            new() { Id = publishedId, AttachmentType = AttachmentType.PublishedAttachment }
        };

        var (internalIds, publishedIds) = SqlAttachmentCleanupRepository.SplitByAttachmentType(batch);

        Assert.Equal([internalId], internalIds);
        Assert.Equal([publishedId], publishedIds);
    }

    [Fact]
    public void SplitByAttachmentType_ReturnsEmptyLists_WhenBatchIsEmpty()
    {
        var (internalIds, publishedIds) = SqlAttachmentCleanupRepository.SplitByAttachmentType([]);

        Assert.Empty(internalIds);
        Assert.Empty(publishedIds);
    }

    [Fact]
    public void SplitByAttachmentType_PutsEverything_InOneBucket_WhenOnlyOneTypePresent()
    {
        var batch = new List<AttachmentToDelete>
        {
            new() { Id = Guid.NewGuid(), AttachmentType = AttachmentType.InternalAttachment },
            new() { Id = Guid.NewGuid(), AttachmentType = AttachmentType.InternalAttachment }
        };

        var (internalIds, publishedIds) = SqlAttachmentCleanupRepository.SplitByAttachmentType(batch);

        Assert.Equal(2, internalIds.Count);
        Assert.Empty(publishedIds);
    }
}
