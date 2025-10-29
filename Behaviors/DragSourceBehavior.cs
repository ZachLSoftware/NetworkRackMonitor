using Microsoft.Xaml.Behaviors;
using RackMonitor.Adorners;
using RackMonitor.ViewModels;
using System;
using System.Diagnostics; // Added for Debug.WriteLine
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace RackMonitor.Behaviors
{
    public class DragSourceBehavior : Behavior<FrameworkElement>
    {
        private Point startPoint;
        private bool isDragging = false;
        private bool dragStarted = false;
        private DragAdorner _dragAdorner = null;
        private AdornerLayer _adornerLayer = null;

        public static readonly string DataFormat = "RackSlotViewModel";

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += AssociatedObject_PreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove += AssociatedObject_PreviewMouseMove;
            AssociatedObject.PreviewMouseLeftButtonUp += AssociatedObject_PreviewMouseLeftButtonUp;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewMouseLeftButtonDown -= AssociatedObject_PreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove -= AssociatedObject_PreviewMouseMove;
            AssociatedObject.PreviewMouseLeftButtonUp -= AssociatedObject_PreviewMouseLeftButtonUp;
            RemoveAdorner(); 
        }

        private void AssociatedObject_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            isDragging = false;
            dragStarted = false;

            if (AssociatedObject.DataContext is SlotViewModel slotVM && slotVM.Device != null)
            {
                startPoint = e.GetPosition(AssociatedObject);
                isDragging = true;
                AssociatedObject.CaptureMouse();

            }

        }

        private void AssociatedObject_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (AssociatedObject.IsMouseCaptured && isDragging && !dragStarted && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(AssociatedObject);
                Vector diff = startPoint - currentPosition;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (AssociatedObject.DataContext is SlotViewModel slotVM && slotVM.Device != null)
                    {
                        dragStarted = true; 
                        CreateAndShowAdorner(startPoint);

                        AssociatedObject.GiveFeedback += AssociatedObject_GiveFeedback;

                        DataObject dragData = new DataObject(DataFormat, slotVM);
                        AssociatedObject.Opacity = 0.5; 


                        try
                        {
                            DragDropEffects result = DragDrop.DoDragDrop(AssociatedObject, dragData, DragDropEffects.Move);

                        }

                        finally // Ensure cleanup happens regardless of result or exceptions
                        {
                            Debug.WriteLine("PreviewMouseMove: Cleaning up after DoDragDrop...");
                            AssociatedObject.GiveFeedback -= AssociatedObject_GiveFeedback;
                            RemoveAdorner();
                            AssociatedObject.Opacity = 1.0; 
                            isDragging = false; 
                            AssociatedObject.ReleaseMouseCapture(); 
                        }
                        e.Handled = true;
                    }
                    else
                    {
                        isDragging = false;
                        AssociatedObject.ReleaseMouseCapture();
                    }
                }
            }
            else if (isDragging && e.LeftButton != MouseButtonState.Pressed)
            {
                isDragging = false;
                dragStarted = false;
                AssociatedObject.ReleaseMouseCapture();
            }
        }

        private void AssociatedObject_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (AssociatedObject.IsMouseCaptured && isDragging)
            {
                isDragging = false;
                dragStarted = false;
                AssociatedObject.ReleaseMouseCapture();

                RemoveAdorner();
            }
        }

        private void AssociatedObject_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            try
            {
                if (_dragAdorner == null || _adornerLayer == null)
                { Debug.WriteLine("GiveFeedback: Adorner or Layer is null!"); e.UseDefaultCursors = true; e.Handled = true; return; }

                Point currentMouseScreenPos = GetScreenMousePosition();

                if (currentMouseScreenPos.X != 0 || currentMouseScreenPos.Y != 0) 
                {
                    _dragAdorner.UpdatePosition(currentMouseScreenPos);
                    _adornerLayer.Update(AssociatedObject); 
                }
                else
                {
                    e.UseDefaultCursors = true;
                }


                e.UseDefaultCursors = false;
                Mouse.SetCursor(Cursors.Hand);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                e.UseDefaultCursors = true;
                e.Handled = true;
            }
        }

        // --- Adorner Helper Methods ---

        private void CreateAndShowAdorner(Point initialMousePos)
        {
            if (_adornerLayer != null) // Avoid recreating if somehow called twice
            {
                return;
            }

            // More robust layer finding: Start from AssociatedObject and walk up
            DependencyObject current = AssociatedObject;
            while (current != null && _adornerLayer == null)
            {
                if (current is AdornerDecorator decorator)
                {
                    _adornerLayer = decorator.AdornerLayer;
                }
                else
                {
                    // Try GetAdornerLayer directly on the current element
                    _adornerLayer = AdornerLayer.GetAdornerLayer(current as Visual);
                }
                current = VisualTreeHelper.GetParent(current);
            }

            // Fallback: Try Window level if still not found
            if (_adornerLayer == null)
            {
                var window = Window.GetWindow(AssociatedObject);
                if (window != null)
                {
                    var decorator = FindVisualChild<AdornerDecorator>(window);
                    if (decorator != null)
                    {
                        _adornerLayer = decorator.AdornerLayer;
                    }
                }
            }


            if (_adornerLayer != null)
            {
                FrameworkElement visual = CreateDragVisual(AssociatedObject);
                if (visual != null)
                {
                    _dragAdorner = new DragAdorner(AssociatedObject, visual, initialMousePos);
                    try
                    {
                        _adornerLayer.Add(_dragAdorner);
                    }
                    catch (Exception ex)
                    {
                        _dragAdorner = null; // Ensure it's null if add failed
                    }
                }
            }
        }

        private void RemoveAdorner()
        {
            if (_dragAdorner != null)
            {
                if (_adornerLayer != null)
                {
                    try
                    {
                        _adornerLayer.Remove(_dragAdorner);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"RemoveAdorner: Error removing adorner: {ex.Message}");
                    }
                }
                else
                {
                    Debug.WriteLine("RemoveAdorner: _adornerLayer was null, cannot remove.");
                }
                _dragAdorner = null;
                _adornerLayer = null; // Clear layer ref too
                Mouse.SetCursor(Cursors.Arrow); // Restore default cursor
            }
            else
            {
                Mouse.SetCursor(Cursors.Arrow);
            }
        }

        private FrameworkElement CreateDragVisual(FrameworkElement draggedElement)
        {
            if (draggedElement == null || draggedElement.ActualWidth == 0 || draggedElement.ActualHeight == 0)
            {
                return null;
            }
            VisualBrush visualBrush = new VisualBrush(draggedElement) { Opacity = 0.8 };
            System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle
            {
                Width = draggedElement.ActualWidth,
                Height = draggedElement.ActualHeight,
                Fill = visualBrush,
                IsHitTestVisible = false
            };
            return rect;
        }

        // --- Utility Helper Methods ---
        private Point GetScreenMousePosition()
        {
            Win32Point w32Mouse;
            if (GetCursorPos(out w32Mouse))
            {
                var source = PresentationSource.FromVisual(Application.Current.MainWindow ?? AssociatedObject) as HwndSource;
                if (source != null)
                {
                    var transform = source.CompositionTarget.TransformFromDevice;
                    return transform.Transform(new Point(w32Mouse.X, w32Mouse.Y));
                }
                else 
                {
                    return AssociatedObject.PointToScreen(Mouse.GetPosition(AssociatedObject));
                }
            }
            return new Point(0, 0); 
        }

        // --- P/Invoke declaration for GetCursorPos ---
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(out Win32Point pt);

        // --- Struct definition for P/Invoke ---
        [StructLayout(LayoutKind.Sequential)]
        public struct Win32Point { public int X; public int Y; };

        public static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }
}

