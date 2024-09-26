namespace PublishResult.PublishToApi;

public class ResultForApi
{
    public string GithubRepositoryUrl { get; set; } = null!;
    public string PullRequestUrl { get; set; } = null!;
    public string StudentNeptun { get; set; } = null!;
    public List<EventScore> Scores { get; set; } = null!;

}

public class EventScore
{
    public long Value { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public string ScoreType { get; set; } = null!;
}
