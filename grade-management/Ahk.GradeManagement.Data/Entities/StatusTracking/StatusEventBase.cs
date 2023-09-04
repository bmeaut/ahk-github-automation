using System;
using System.ComponentModel.DataAnnotations;

namespace Ahk.GradeManagement.Data.Entities
{
    
    public abstract class StatusEventBase
    {
        public int Id { get; set; }
        public string Repository { get; set; }

        
        public abstract string Type { get; }
        public DateTime Timestamp { get; set; }
    }
}
