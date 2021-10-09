namespace Ahk.GitHub.Monitor.Services
{
    public class SetGradeEvent
    {
        public SetGradeEvent(string neptun, string repository, int prNumber, string prUrl, string actor, string origin, double[] results)
        {
            this.Neptun = neptun;
            this.Repository = repository;
            this.PrNumber = prNumber;
            this.PrUrl = prUrl;
            this.Actor = actor;
            this.Origin = origin;
            this.Results = results;
        }

        public string Neptun { get; }
        public string Repository { get; }
        public int PrNumber { get; }
        public string PrUrl { get; }
        public string Actor { get; }
        public string Origin { get; }
        public double[] Results { get; }
    }
}
