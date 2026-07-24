namespace Ahk.Web.Data.Entities;

/// <summary>
/// Append-only grade result — the relational form of <c>StudentResult</c>. Never updated: every evaluation
/// or teacher action inserts a new row, and the current grade is the latest one. <see cref="Confirmed"/>
/// distinguishes an automated evaluation result (false) from a teacher-approved grade (true).
/// </summary>
public class GradeRecord : ICourseScoped
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public Course? Course { get; set; }

    public int SubmissionId { get; set; }

    public Submission? Submission { get; set; }

    public int? StudentId { get; set; }

    public Student? Student { get; set; }

    /// <summary>
    /// Neptun as recorded at grading time. Denormalized deliberately: the CSV export reports the code that was
    /// on the result, and a grade is a point-in-time record.
    /// </summary>
    public string Neptun { get; set; } = string.Empty;

    public int? PrNumber { get; set; }

    public string? PrUrl { get; set; }

    public DateTimeOffset Date { get; set; }

    /// <summary>Who produced it: a teacher's GitHub login, or "grade-management-api" for automated results.</summary>
    public string? Actor { get; set; }

    /// <summary>Where it came from: the commit URL for automated results, or the PR comment for chatops.</summary>
    public string? Origin { get; set; }

    /// <summary>False for automated evaluation results; true once a teacher approves/overrides via /ahk ok.</summary>
    public bool Confirmed { get; set; }

    public ICollection<GradeExercisePoint> Points { get; } = new List<GradeExercisePoint>();
}

/// <summary>
/// Points for one exercise of a <see cref="GradeRecord"/> — the relational form of the embedded
/// <c>ExerciseWithPoint</c> collection. Exercise names stay free-form (positional "ex0"/"ex1" carried forward
/// from the previous result, or the evaluator's exerciseName), matching the original semantics.
/// </summary>
public class GradeExercisePoint
{
    public int Id { get; set; }

    public int GradeRecordId { get; set; }

    public GradeRecord? GradeRecord { get; set; }

    public string Name { get; set; } = string.Empty;

    public double Point { get; set; }

    /// <summary>Preserves positional order, which is significant for the /ahk ok "5 3.5 0" chatops form.</summary>
    public int Order { get; set; }
}
