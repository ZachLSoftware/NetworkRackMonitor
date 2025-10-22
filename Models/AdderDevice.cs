using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RackMonitor.Models
{
    class AdderDevice : RackDevice
    {
        private string _model;
        public string Model
        {
            get { return _model; }
            set { _model = value; }
        }

        public AdderDevice() : base()
        {

            DeviceType = "Adder";
        }
    }
}
