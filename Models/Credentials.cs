using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RackMonitor.Models
{
    public class Credentials
    {
        public string Username { get; set; }
        public string EncryptedPassword { get; set; }

        public Credentials(string username, string encryptedPassword)
        {
            Username = username;
            EncryptedPassword = encryptedPassword;
        }
    }
}
