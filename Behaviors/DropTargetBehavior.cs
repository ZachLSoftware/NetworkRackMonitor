using Microsoft.Xaml.Behaviors;
using RackMonitor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RackMonitor.Behaviors
{
    public class DropTargetBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty DropCommandProperty = DependencyProperty.Register(nameof(DropCommand), typeof(ICommand), typeof(DropTargetBehavior), new PropertyMetadata(null));

        public ICommand DropCommand
        {
            get { return (ICommand)GetValue(DropCommandProperty); }
            set { SetValue(DropCommandProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AllowDrop = true;
            AssociatedObject.DragOver += AssociatedObject_DragOver;
            AssociatedObject.Drop += AssociatedObject_Drop;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.AllowDrop = false;
            AssociatedObject.DragOver -= AssociatedObject_DragOver;
            AssociatedObject.Drop -= AssociatedObject_Drop;
        }

        private void AssociatedObject_DragOver(object sender, DragEventArgs e)
        {
            if(AssociatedObject.DataContext is SlotViewModel targetSlotVM)
            {
                if (e.Data.GetDataPresent(DragSourceBehavior.DataFormat))
                {
                    SlotViewModel SourceSlotVM = e.Data.GetData(DragSourceBehavior.DataFormat) as SlotViewModel;

                    if (SourceSlotVM != targetSlotVM)
                    {
                        e.Effects = DragDropEffects.Move;
                    }
                    else
                    {
                        e.Effects = DragDropEffects.None;
                    }
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;

        }

        private void AssociatedObject_Drop(object sender, DragEventArgs e)
        {
            if (AssociatedObject.DataContext is SlotViewModel targetSlotVM)
            {
                if (e.Data.GetDataPresent(DragSourceBehavior.DataFormat))
                {
                    SlotViewModel sourceSlotVM = e.Data.GetData(DragSourceBehavior.DataFormat) as SlotViewModel;

                    if (sourceSlotVM != null && sourceSlotVM != targetSlotVM)
                    {
                        var dropData = new DragDropData(sourceSlotVM, targetSlotVM);
                        if (DropCommand?.CanExecute(dropData) == true)
                        {
                            DropCommand.Execute(dropData);
                        }
                    }
                }
            }
            e.Handled = true;

        }
    }
}
