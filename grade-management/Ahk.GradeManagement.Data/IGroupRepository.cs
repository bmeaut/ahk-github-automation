using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Data
{
    public interface IGroupRepository
    {
        public Task AddGroup(Group group);

        public Task<List<Group>> ListGroups();

        public Task<List<Student>> ListStudentsInGroup(int groupId);

        public Task DeleteGroup(int groupId);
    }
}
