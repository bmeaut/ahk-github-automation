namespace Ahk.Review.Ui.Models
{
    public class Grade
    {
        public string GithubRepoName { get; set; }
        public int GithubPrNumber { get; set; }
        public Uri GithubPrUrl { get; set; }
        public DateTimeOffset Date { get; set; }
        public bool IsConfirmed { get; set; }
        public string Origin { get; set; }
        public ICollection<Point> Points { get; set; }
        public Assignment Assignment { get; set; }
        public Student Student { get; set; }
    }
}
