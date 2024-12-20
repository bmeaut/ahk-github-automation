﻿using GradeManagement.Shared.Enums;

namespace GradeManagement.Data.Models;

public class SubjectTeacher : ISoftDelete
{
    public long Id { get; set; }
    public Subject Subject { get; set; }
    public long SubjectId { get; set; }
    public User User { get; set; }
    public long UserId { get; set; }
    public UserRoleOnSubject Role { get; set; }
    public bool IsDeleted { get; set; }
}
