using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Data.Internal;

namespace Ahk.GradeManagement.Functions.Groups
{
    public class GroupService : IGroupService
    {
        private readonly IGroupRepository repo;

        public GroupService(IGroupRepository repo) => this.repo = repo;

        public async Task SaveGroup(Group group)
        {
            await this.repo.AddGroup(group);
        }

        public async Task<List<Group>> ListGroups()
        {
            return await this.repo.ListGroups();
        }

        public async Task<List<Student>> ListStudents(int groupId)
        {
            return await this.repo.ListStudentsInGroup(groupId);
        }

        public async Task DeleteGroup(int groupId)
        {
            await this.repo.DeleteGroup(groupId);
        }
    }
}
