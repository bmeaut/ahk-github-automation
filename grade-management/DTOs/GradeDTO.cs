using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class GradeDTO
    {
        public int Id { get; set; }
        public string GithubRepoName { get; set; }
        public int GithubPrNumber { get; set; }
        public Uri GithubPrUrl { get; set; }
        public DateTimeOffset Date { get; set; }
        public bool IsConfirmed { get; set; }
        public string Origin { get; set; }
    }
}
