namespace PTL.Lambda.RemoveCustomerData.Tests;

public class SqlRecordUpdateCleanupRepositoryTests
{
    [Fact]
    public void ExtractIds_ReturnsEachRecordsId_InOrder()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var batch = new List<RecordToUpdate>
        {
            new() { Id = first, Type = RecordType.Customer },
            new() { Id = second, Type = RecordType.Customer }
        };

        var ids = SqlRecordUpdateCleanupRepository.ExtractIds(batch);

        Assert.Equal([first, second], ids);
    }

    [Fact]
    public void ExtractIds_ReturnsEmptyList_WhenBatchIsEmpty()
    {
        var ids = SqlRecordUpdateCleanupRepository.ExtractIds([]);

        Assert.Empty(ids);
    }
}
