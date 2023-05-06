using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Functions.Assignments
{
    public interface IAssignmentService
    {
        public Task SaveAssignment(Assignment assignment);
        public Task<List<Assignment>> List();
    }
}
