using RackMonitor.Attributes;
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
        [Order(1)]
        [PropertyVisibility(true)]
        public string Model
        {
            get { return _model; }
            set { 
                _model = value;
                OnPropertyChanged(nameof(Model));
            }
        }

        public AdderDevice() : base()
        {

            DeviceType = "Adder";
        }
    }
}
