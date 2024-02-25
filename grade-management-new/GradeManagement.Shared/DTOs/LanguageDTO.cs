﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeManagement.Data.Models;

public class LanguageDTO
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<CourseDTO> CourseDtos { get; set; }
}
