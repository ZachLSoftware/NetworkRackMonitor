using RackMonitor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RackMonitor.Behaviors
{
    public class DragDropData
    {
        public SlotViewModel SourceSlot { get; }
        public SlotViewModel TargetSlot { get; }

        public DragDropData(SlotViewModel source, SlotViewModel target)
        {
            SourceSlot = source;
            TargetSlot = target;
        }
    }
}
