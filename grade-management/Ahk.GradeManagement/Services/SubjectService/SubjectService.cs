using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Services.SubjectService
{
    public class SubjectService : ISubjectService
    {
        public AhkDbContext Context { get; set; }

        public SubjectService(AhkDbContext context) => this.Context = context;

        public IReadOnlyCollection<Subject> GetAllSubjects()
        {
            return Context.Subjects.ToList().AsReadOnly();
        }
    }
}
