using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Data.Internal
{
    public class AssignmentRepository
    {
        public AhkDbContext Context { get; set; }

        public async Task AddAssignment(Assignment assignment)
        {
            Context.Assignments.Add(assignment);
            await Context.SaveChangesAsync();
        }

        public async Task<List<Assignment>> ListAssignments()
        {
            return Context.Assignments.ToList();
        }


    }
}
