

using Ganss.Excel;


namespace Api.Features.DocumentIntelligence;

public class BenefitsForExcelExport
{
    [Column(Letter = "A")]
    public string Benefit { get; set; } = string.Empty;
    [Column(Letter = "B")]
    public string InNetworkConditions { get; set; } = string.Empty;
    [Column(Letter = "C")]
    public string OutOfNetworkConditions { get; set; } = string.Empty;
    [Column(Letter = "D")]
    public string Limitations { get; set; } = string.Empty;

}

public class TestCasesToExport
{
    [Column(Letter = "A")]
    public string Benefit { get; set; } = string.Empty;
    [Column(Letter = "B")]
    public string Name { get; set; } = string.Empty;
    [Column(Letter = "C")]
    public string Description { get; set; } = string.Empty;
    [Column(Letter = "D")]
    public string Type { get; set; } = string.Empty;
    [Column(Letter = "E")]
    public string ExpectedResult { get; set; } = string.Empty;
    [Column(Letter = "F")]
    public string IcdCodes { get; set; } = string.Empty;
    [Column(Letter = "G")]
    public string ProcedureCodes { get; set; } = string.Empty;
    [Column(Letter = "H")]
    public string PlaceOfService { get; set; } = string.Empty;
}