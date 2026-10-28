namespace EFCore.Performance.Api.DTOs;

public sealed record SqlDiagnosticsResponse(
    string ProjectionBeforeSql,
    string ProjectionAfterSql,
    string MaterializationBeforeSql,
    string MaterializationAfterSql,
    string SqlInspectionBeforeSql,
    string SqlInspectionAfterSql,
    string NPlusOneAfterSql,
    string IndexNote);
