using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class WebhookTokenDTO
    {
        public string Id { get; set; }
        public string Secret { get; set; }
        public string Description { get; set; }
    }
}
