using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Entities
{
    public class Grade
    {
        public int Id { get; set; }
        public string GithubRepoName { get; set; }
        public int GithubPrNumber { get; set; }
        public Uri GithubPrUrl { get; set; }
        public DateTime Date { get; set; }
        public bool Confirmed { get; set; }
        public Uri Origin { get; set; }

        public int AssignmentId { get; set; }
        public int StudentId { get; set; }
        public ICollection<Point> Points { get; set; }
        public Assignment Assignment { get; set; }
        public Student Student { get; set; }
    }
}
