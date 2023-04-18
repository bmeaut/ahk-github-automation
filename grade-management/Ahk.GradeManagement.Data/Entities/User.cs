using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Neptun { get; set; }
        public string EduEmail { get; set; }
        public string GithubUser { get; set; }
    }
}
