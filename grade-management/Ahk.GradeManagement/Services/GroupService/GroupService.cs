using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Services.GroupService
{
    public class GroupService : IGroupService
    {
        private AhkDbContext Context { get; set; }

        public GroupService(AhkDbContext context)
        {
            Context = context;
        }

        public async Task SaveGroupAsync(Group group)
        {
            Context.Groups.Add(group);
            await Context.SaveChangesAsync();
        }

        public async Task<List<Group>> ListGroupsAsync()
        {
            return Context.Groups.ToList();
        }

        public async Task<List<Student>> ListStudentsAsync(int groupId)
        {
            var studentGroups = Context.StudentGroups.Where(g => g.GroupId == groupId).ToList();
            var students = new List<Student>();
            foreach (var studentGroup in studentGroups)
            {
                var student = studentGroup.Student;
                students.Add(student);
            }
            return students;
        }

        public async Task DeleteGroupAsync(int groupId)
        {
            var group = Context.Groups.Find(groupId);
            Context.Remove(group);
            await Context.SaveChangesAsync();
        }
    }
}
