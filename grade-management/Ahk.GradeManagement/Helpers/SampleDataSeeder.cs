using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Data.Enums;

namespace Ahk.GradeManagement.Helpers
{
    public class SampleDataSeeder
    {
        public AhkDbContext Context { get; set; }

        public SampleDataSeeder(AhkDbContext context)
        {
            this.Context = context;
        }

        public void SeedData()
        {
            var subjects = CreateSubjectData();
            if (!Context.Subjects.Any())
            {
                Context.Subjects.AddRange(subjects);
                Context.SaveChanges();
            }

            var teachers = CreateTeacherData();
            if (!Context.Teachers.Any())
            {
                Context.Teachers.AddRange(teachers);
                Context.SaveChanges();
            }

            var groups = CreateGroupData(subjects[0], subjects[1]);
            if (!Context.Groups.Any())
            {
                Context.Groups.AddRange(groups);
                Context.SaveChanges();
            }

            var assignments = CreateAssignmentData(subjects[0], subjects[1]);
            if (!Context.Assignments.Any())
            {
                Context.Assignments.AddRange(assignments);
                Context.SaveChanges();
            }

            var exercises = CreateExerciseData(assignments[0], assignments[1]);
            if (!Context.Exercises.Any())
            {
                Context.Exercises.AddRange(exercises);
                Context.SaveChanges();
            }

            var students = CreateStudentData();
            if (!Context.Students.Any())
            {
                Context.Students.AddRange(students);
                Context.SaveChanges();
            }

            var studentAssignments = CreateStudentAssignmentData(students, assignments);
            if (!Context.StudentAssignments.Any())
            {
                Context.StudentAssignments.AddRange(studentAssignments);
                Context.SaveChanges();
            }

            var studentGroups = CreateStudentGroupData(students, groups);
            if (!Context.StudentGroups.Any())
            {
                Context.StudentGroups.AddRange(studentGroups);
                Context.SaveChanges();
            }

            var studentSubjects = CreateStudentSubjectData(students, subjects);
            if (!Context.StudentSubjects.Any())
            {
                Context.StudentSubjects.AddRange(studentSubjects);
                Context.SaveChanges();
            }

            var teacherGroups = CreateTeacherGroupData(teachers, groups);
            if (!Context.TeacherGroups.Any())
            {
                Context.TeacherGroups.AddRange(teacherGroups);
                Context.SaveChanges();
            }

            var teacherSubjects = CreateTeacherSubjectData(teachers, subjects);
            if (!Context.TeacherSubjects.Any())
            {
                Context.TeacherSubjects.AddRange(teacherSubjects);
                Context.SaveChanges();
            }
        }

        public void ClearData()
        {
            Context.Assignments.RemoveRange(Context.Assignments);
            Context.Exercises.RemoveRange(Context.Exercises);
            Context.Groups.RemoveRange(Context.Groups);
            Context.StudentAssignments.RemoveRange(Context.StudentAssignments);
            Context.Students.RemoveRange(Context.Students);
            Context.StudentSubjects.RemoveRange(Context.StudentSubjects);
            Context.Subjects.RemoveRange(Context.Subjects);
            Context.TeacherGroups.RemoveRange(Context.TeacherGroups);
            Context.Teachers.RemoveRange(Context.Teachers);
            Context.TeacherSubjects.RemoveRange(Context.TeacherSubjects);

            Context.SaveChanges();
        }

        private static List<Subject> CreateSubjectData()
        {
            List<Subject> data = new List<Subject>
            {
                new Subject
                {
                    Name = "subject1",
                    Semester = "2023/24/1",
                    SubjectCode = "AAAAAA",
                    GithubOrg = "bmeaut",
                    AhkConfig = "config1",
                },
                new Subject
                {
                    Name = "subject2",
                    Semester = "2023/24/1",
                    SubjectCode = "BBBBBBBB",
                    GithubOrg = "bmeaut",
                    AhkConfig = "config2",
                },
                new Subject
                {
                    Name = "subject3",
                    Semester = "2023/24/1",
                    SubjectCode = "CCCCCCC",
                    GithubOrg = "bmeaut",
                    AhkConfig = "config3",
                },
            };

            return data;
        }

        private static List<Teacher> CreateTeacherData()
        {
            List<Teacher> teachers = new List<Teacher>
            {
                new Teacher
                {
                    Name = "teacher1",
                    Neptun = "XXXXXX",
                    EduEmail = "aaaa@edu.bme.hu",
                    GithubUser = "githubuser1",
                    Role = Role.Teacher,
                },
                new Teacher
                {
                    Name = "teacher2",
                    Neptun = "YYYYYY",
                    EduEmail = "bbbbbb@edu.bme.hu",
                    GithubUser = "githubuser2",
                    Role = Role.Teacher,
                },
            };

            return teachers;
        }

        private static List<Group> CreateGroupData(Subject subject1, Subject subject2)
        {
            List<Group> groups = new List<Group>
            {
                new Group
                {
                    Name = "group1",
                    Room = "QBF10",
                    Time = "Monday 10:15",
                    Subject = subject1,
                },
                new Group
                {
                    Name = "group2",
                    Room = "IE211",
                    Time = "Monday 8:15",
                    Subject = subject2,
                },
            };

            return groups;
        }

        private static List<Assignment> CreateAssignmentData(Subject subject1, Subject subject2)
        {
            List<Assignment> assignments = new List<Assignment>
            {
                new Assignment
                {
                    Name = "assignment1",
                    DeadLine = DateTimeOffset.Now,
                    ClassroomAssignment = new Uri("http://testassignment"),
                    Subject = subject1,
                },
                new Assignment
                {
                    Name = "assignment2",
                    DeadLine = DateTimeOffset.Now,
                    ClassroomAssignment = new Uri("http://testassignment"),
                    Subject = subject1,
                },
                new Assignment
                {
                    Name = "assignment3",
                    DeadLine = DateTimeOffset.Now,
                    ClassroomAssignment = new Uri("http://testassignment"),
                    Subject = subject2,
                },
            };

            return assignments;
        }

        private static List<Exercise> CreateExerciseData(Assignment assignment1, Assignment assignment2)
        {
            List<Exercise> exercises = new List<Exercise>
            {
                new Exercise
                {
                    Name = "ex1",
                    AvailablePoints = 5,
                    Assignment = assignment1,
                },
                new Exercise
                {
                    Name = "ex2",
                    AvailablePoints = 5,
                    Assignment = assignment1,
                },
                new Exercise
                {
                    Name = "ex3",
                    AvailablePoints = 5,
                    Assignment = assignment2,
                },
            };

            return exercises;
        }

        private static List<Student> CreateStudentData()
        {
            List<Student> students = new List<Student>
            {
                new Student
                {
                    Name = "student1",
                    Neptun = "XXXXXX",
                    EduEmail = "student1@edu.bme.hu",
                    GithubUser = "githubuser1",
                },
                new Student
                {
                    Name = "student2",
                    Neptun = "YYYYYY",
                    EduEmail = "student2@edu.bme.hu",
                    GithubUser = "githubuser2",
                },
            };

            return students;
        }

        private static List<StudentAssignment> CreateStudentAssignmentData(List<Student> students, List<Assignment> assignments)
        {
            List<StudentAssignment> studentAssignments = new List<StudentAssignment>
            {
                new StudentAssignment
                {
                    Student = students[0],
                    Assignment = assignments[0],
                },
                new StudentAssignment
                {
                    Student = students[0],
                    Assignment = assignments[1],
                },
                new StudentAssignment
                {
                    Student = students[1],
                    Assignment = assignments[2],
                },
            };

            return studentAssignments;
        }

        private static List<StudentGroup> CreateStudentGroupData(List<Student> students, List<Group> groups)
        {
            List<StudentGroup> studentGroups = new List<StudentGroup>
            {
                new StudentGroup
                {
                    Student = students[0],
                    Group = groups[0],
                },
                new StudentGroup
                {
                    Student = students[1],
                    Group = groups[1],
                },
            };

            return studentGroups;
        }

        private static List<StudentSubject> CreateStudentSubjectData(List<Student> students, List<Subject> subjects)
        {
            List<StudentSubject> studentSubjects = new List<StudentSubject>
            {
                new StudentSubject
                {
                    Subject = subjects[0],
                    Student = students[0],
                },
                new StudentSubject
                {
                    Subject = subjects[1],
                    Student = students[0],
                },
                new StudentSubject
                {
                    Subject = subjects[0],
                    Student = students[1],
                },
            };

            return studentSubjects;
        }

        private static List<TeacherGroup> CreateTeacherGroupData(List<Teacher> teachers, List<Group> groups)
        {
            List<TeacherGroup> teacherGroups = new List<TeacherGroup>
            {
                new TeacherGroup
                {
                    Teacher = teachers[0],
                    Group = groups[0],
                },
                new TeacherGroup
                {
                    Teacher = teachers[0],
                    Group = groups[1],
                },
            };

            return teacherGroups;
        }

        private static List<TeacherSubject> CreateTeacherSubjectData(List<Teacher> teachers, List<Subject> subjects)
        {
            List<TeacherSubject> teacherSubjects = new List<TeacherSubject>
            {
                new TeacherSubject
                {
                    Teacher = teachers[0],
                    Subject = subjects[0],
                },
                new TeacherSubject
                {
                    Teacher = teachers[0],
                    Subject = subjects[1],
                },
                new TeacherSubject
                {
                    Teacher = teachers[1],
                    Subject = subjects[1],
                },
            };

            return teacherSubjects;
        }
    }
}
