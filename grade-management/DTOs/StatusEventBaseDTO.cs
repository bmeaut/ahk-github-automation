using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class StatusEventBaseDTO
    {
        public int Id { get; set; }
        public string Repository { get; set; }


        public string Type { get; }
        public DateTime Timestamp { get; set; }
    }
}
