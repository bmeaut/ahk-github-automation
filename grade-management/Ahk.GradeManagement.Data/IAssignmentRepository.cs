using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Data
{
    public interface IAssignmentRepository
    {
        public Task AddAssignment(Assignment assignment);
        public Task<List<Assignment>> ListAssignments();

    }
}
