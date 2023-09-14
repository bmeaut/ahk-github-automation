using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ahk.GradeManagement.Services.AssignmentService
{
    public class AssignmentService : IAssignmentService
    {
        private AhkDbContext Context { get; set; }

        public AssignmentService(AhkDbContext context)
        {
            Context = context;
        }

        public async Task SaveAssignmentAsync(Assignment assignment)
        {
            Context.Assignments.Add(assignment);
            await Context.SaveChangesAsync();
        }

        public async Task<List<Assignment>> ListAsync(string subject)
        {
            return Context.Assignments.Include(a => a.Subject).Where(a => a.Subject.SubjectCode == subject).ToList();
        }

        public async Task<List<Exercise>> ListExercisesAsync(string subject, string assignmentId)
        {
            return Context.Exercises.Include(e => e.Assignment)
                .ThenInclude(a => a.Subject)
                .Where(e => e.Assignment.Subject.SubjectCode == subject && e.AssignmentId.ToString() == assignmentId).ToList();
        }
    }
}
