using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data.Entities;

namespace Ahk.GradeManagement.Data.Internal
{
    public class GroupRepository
    {
        public AhkDbContext Context { get; set; }

        public async Task AddGroup(Group group)
        {
            Context.Groups.Add(group);
            await Context.SaveChangesAsync();
        }

        public async Task<List<Group>> ListGroups()
        {
            return Context.Groups.ToList();
        }

        public async Task<List<Student>> ListStudentsInGroup(int groupId)
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

        public async Task DeleteGroup(int groupId)
        {
            var group = Context.Groups.Find(groupId);
            Context.Remove(group);
            await Context.SaveChangesAsync();
        }
    }
}
