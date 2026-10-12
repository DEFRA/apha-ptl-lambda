using System.Data;

namespace PTL.Lambda.Shared.Tests;

public class GuidTableValuedParameterTests
{
    [Fact]
    public void BuildTable_HasSingleIdColumn_AndOneRowPerGuid()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        var table = GuidTableValuedParameter.BuildTable(ids);

        Assert.Single(table.Columns);
        Assert.Equal("Id", table.Columns[0].ColumnName);
        Assert.Equal(typeof(Guid), table.Columns[0].DataType);
        Assert.Equal(3, table.Rows.Count);
        Assert.Equal(ids, table.Rows.Cast<DataRow>().Select(r => (Guid)r["Id"]));
    }

    [Fact]
    public void BuildTable_ReturnsEmptyTable_WhenNoIds()
    {
        var table = GuidTableValuedParameter.BuildTable([]);

        Assert.Empty(table.Rows);
    }

    [Fact]
    public void BuildTable_Throws_WhenIdsIsNull() =>
        Assert.Throws<ArgumentNullException>(() => GuidTableValuedParameter.BuildTable(null!));

    [Fact]
    public void Create_ReturnsNonNullParameter_ForEmptyAndNonEmptyCollections()
    {
        Assert.NotNull(GuidTableValuedParameter.Create([]));
        Assert.NotNull(GuidTableValuedParameter.Create([Guid.NewGuid()]));
    }
}
