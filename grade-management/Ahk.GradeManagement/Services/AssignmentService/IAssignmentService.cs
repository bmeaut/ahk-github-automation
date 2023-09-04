using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Services.AssignmentService
{
    public interface IAssignmentService
    {
        public Task SaveAssignmentAsync(Assignment assignment);
        public Task<List<Assignment>> ListAsync();
    }
}
