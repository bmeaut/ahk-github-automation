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
        public DateTimeOffset Date { get; set; }
        public bool IsConfirmed { get; set; }
        public string Origin { get; set; }


        public ICollection<Point> Points { get; set; }
        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; }
    }
}
