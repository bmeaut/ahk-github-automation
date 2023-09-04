using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class SubjectDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Semester { get; set; }
        public string SubjectCode { get; set; }
        public string GithubOrg { get; set; }
        public string AhkConfig { get; set; }
    }
}
