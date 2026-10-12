using System.Data;
using Dapper;

namespace PTL.Lambda.Shared;

/// <summary>
/// Builds the GuidIdTableType-shaped table-valued parameter every cleanup stored procedure takes
/// (ProficiencyTestingDatabase/Object Scripts/Types/GuidIdTableType.sql - a single Id uniqueidentifier
/// column). Centralised here so each Lambda's repository builds it the same way instead of
/// duplicating the DataTable-construction loop three times.
/// </summary>
public static class GuidTableValuedParameter
{
    private const string TypeName = "GuidIdTableType";

    /// <summary>The pure, unit-testable part - no Dapper/ADO.NET call happens here.</summary>
    public static DataTable BuildTable(IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        foreach (var id in ids)
        {
            table.Rows.Add(id);
        }

        return table;
    }

    public static SqlMapper.ICustomQueryParameter Create(IEnumerable<Guid> ids) => BuildTable(ids).AsTableValuedParameter(TypeName);
}
