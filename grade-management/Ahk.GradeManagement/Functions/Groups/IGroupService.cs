using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Functions.Groups
{
    public interface IGroupService
    {
        public Task SaveGroup(Group group);
        public Task<List<Group>> ListGroups();
        public Task<List<Student>> ListStudents(int groupId);
        public Task DeleteGroup(int groupId);
    }
}
