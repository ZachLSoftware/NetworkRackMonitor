using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RackMonitor.Services
{
    class ArpService
    {
        public List<ArpItem> GetArpResult()
        {
            using (Process process = Process.Start(new ProcessStartInfo("arp", "-a")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            }))
            {
                var output = process.StandardOutput.ReadToEnd();
                return ParseArpResult(output);
            }
        }

        private List<ArpItem> ParseArpResult(string output)
        {
            var lines = output.Split('\n');

            var result = from line in lines
                            let item = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            where item.Count() == 4
                            select new ArpItem()
                            {
                                Ip = item[0],
                                MacAddress = item[1],
                                Type = item[2]
                            };

            return result.ToList();
        }
    }
    
    public class ArpItem
    {
        public string Ip { get; set; }

        public string MacAddress { get; set; }

        public string Type { get; set; }
    }
}
