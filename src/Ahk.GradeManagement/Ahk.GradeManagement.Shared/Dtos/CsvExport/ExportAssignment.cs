namespace Ahk.GradeManagement.Shared.Dtos.CsvExport;

public class ExportAssignment
{
    public string NeptunCode { get; set; }
    public List<ExportScore> Scores { get; set; }
    public long SumOfScores { get; set; }
}
