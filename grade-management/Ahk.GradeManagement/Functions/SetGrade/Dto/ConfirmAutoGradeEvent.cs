namespace Ahk.GradeManagement.SetGrade
{
    public class ConfirmAutoGradeEvent
    {
        public ConfirmAutoGradeEvent(string neptun, string repository, int prNumber, string prUrl, string actor, string origin)
        {
            this.Neptun = neptun;
            this.Repository = repository;
            this.PrNumber = prNumber;
            this.PrUrl = prUrl;
            this.Actor = actor;
            this.Origin = origin;
        }

        public string Neptun { get; }
        public string Repository { get; }
        public int PrNumber { get; }
        public string PrUrl { get; }
        public string Actor { get; }
        public string Origin { get; }
    }
}
