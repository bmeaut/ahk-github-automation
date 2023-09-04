using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Entities.StatusTracking
{
    public class Assignee
    {
        public int Id { get; set; }
        public string GithubUser { get; set; }

        public int PullRequestEventId { get; set; }
        public PullRequestEvent PullRequestEvent { get; set; }
    }
}
