using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RackMonitor.Data
{
    class GlobalSettingsDto
    {
        public bool Ping { get; set; }
        public bool WoL { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
