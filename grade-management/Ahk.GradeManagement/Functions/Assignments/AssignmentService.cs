using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Functions.Assignments
{
    public class AssignmentService : IAssignmentService
    {
        private readonly IAssignmentRepository repo;

        public AssignmentService(IAssignmentRepository repo) => this.repo = repo;

        public async Task SaveAssignment(Assignment assignment)
        {
            await this.repo.AddAssignment(assignment);
        }

        public async Task<List<Assignment>> List()
        {
            return await this.repo.ListAssignments();
        }
    }
}
