using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagement.Data.Models
{
	public class SubjectDTO
	{
        public long Id { get; set; }
        public string Name { get; set; }
        public string NeptunSubjectCode { get; set; }
        public List<CourseDTO> CourseDtos { get; set; }
	}
}
