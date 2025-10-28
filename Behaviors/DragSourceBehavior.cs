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
            // Reset flags
            isDragging = false;
            dragStarted = false;

            if (AssociatedObject.DataContext is SlotViewModel slotVM && slotVM.Device != null)
            {
                startPoint = e.GetPosition(AssociatedObject);
                isDragging = true; // Indicate potential drag start
                AssociatedObject.CaptureMouse(); // Capture mouse immediately
                Debug.WriteLine("PreviewMouseLeftButtonDown: Potential drag started.");
                // Do NOT set e.Handled=true here, might interfere with other controls needing click
            }
            else
            {
                Debug.WriteLine("PreviewMouseLeftButtonDown: No device or wrong DataContext, ignoring.");
            }
        }

        private void AssociatedObject_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // Only proceed if mouse is captured (ensures button is still down) and drag hasn't officially started
            if (AssociatedObject.IsMouseCaptured && isDragging && !dragStarted && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(AssociatedObject);
                Vector diff = startPoint - currentPosition;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    Debug.WriteLine("PreviewMouseMove: Drag threshold exceeded.");
                    if (AssociatedObject.DataContext is SlotViewModel slotVM && slotVM.Device != null)
                    {
                        dragStarted = true; // Mark drag as officially started

                        // Create and show the adorner BEFORE starting DoDragDrop
                        CreateAndShowAdorner(startPoint); // Pass the initial position where the drag started relative to element

                        AssociatedObject.GiveFeedback += AssociatedObject_GiveFeedback; // Subscribe to track mouse

                        DataObject dragData = new DataObject(DataFormat, slotVM);
                        AssociatedObject.Opacity = 0.5; // Make original semi-transparent

                        Debug.WriteLine("PreviewMouseMove: Starting DoDragDrop...");
                        try
                        {
                            DragDropEffects result = DragDrop.DoDragDrop(AssociatedObject, dragData, DragDropEffects.Move);
                            Debug.WriteLine($"PreviewMouseMove: DoDragDrop finished with result: {result}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"PreviewMouseMove: Exception during DoDragDrop: {ex.Message}");
                        }
                        finally // Ensure cleanup happens regardless of result or exceptions
                        {
                            Debug.WriteLine("PreviewMouseMove: Cleaning up after DoDragDrop...");
                            AssociatedObject.GiveFeedback -= AssociatedObject_GiveFeedback; // Unsubscribe
                            RemoveAdorner(); // Remove the visual
                            AssociatedObject.Opacity = 1.0; // Restore opacity
                            isDragging = false; // Reset state
                            AssociatedObject.ReleaseMouseCapture(); // IMPORTANT: Release mouse capture
                        }
                        e.Handled = true; // Prevent further mouse move handling once drag starts
                    }
                    else
                    {
                        Debug.WriteLine("PreviewMouseMove: Drag threshold met, but DataContext invalid?");
                        isDragging = false; // Reset if context is lost
                        AssociatedObject.ReleaseMouseCapture();
                    }
                }
            }
            // Handle case where mouse button released without meeting drag threshold
            else if (isDragging && e.LeftButton != MouseButtonState.Pressed)
            {
                isDragging = false;
                dragStarted = false;
                AssociatedObject.ReleaseMouseCapture();
                Debug.WriteLine("PreviewMouseMove: Mouse button released before drag threshold met.");
            }
        }

        private void AssociatedObject_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (AssociatedObject.IsMouseCaptured && isDragging)
            {
                isDragging = false;
                dragStarted = false; // Reset just in case
                AssociatedObject.ReleaseMouseCapture();
                // If the drag never *officially* started (DoDragDrop wasn't called),
                // ensure any potentially created adorner is removed.
                RemoveAdorner();
                Debug.WriteLine("PreviewMouseLeftButtonUp: Drag cancelled before starting, released capture.");
            }
        }

        private void AssociatedObject_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            try
            {
                if (_dragAdorner == null || _adornerLayer == null)
                { Debug.WriteLine("GiveFeedback: Adorner or Layer is null!"); e.UseDefaultCursors = true; e.Handled = true; return; }

                // Get CURRENT mouse position in SCREEN coordinates
                Point currentMouseScreenPos = GetScreenMousePosition();
                // Debug.WriteLine($"GiveFeedback: MouseScreen=({currentMouseScreenPos.X},{currentMouseScreenPos.Y})");

                if (currentMouseScreenPos.X != 0 || currentMouseScreenPos.Y != 0) // Check if GetScreenMousePosition worked
                {
                    // Pass SCREEN coordinates to the adorner's update method
                    _dragAdorner.UpdatePosition(currentMouseScreenPos);
                    _adornerLayer.Update(AssociatedObject); // Tell layer to update
                }
                else
                {
                    Debug.WriteLine("GiveFeedback: Failed to get screen mouse position.");
                    e.UseDefaultCursors = true; // Fallback if position invalid
                }


                e.UseDefaultCursors = false;
                Mouse.SetCursor(Cursors.Hand);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GiveFeedback Error: {ex.Message}");
                e.UseDefaultCursors = true;
                e.Handled = true;
            }
        }

        // --- Adorner Helper Methods ---

        private void CreateAndShowAdorner(Point initialMousePos)
        {
            Debug.WriteLine("CreateAndShowAdorner: Attempting to create adorner...");
            if (_adornerLayer != null) // Avoid recreating if somehow called twice
            {
                Debug.WriteLine("CreateAndShowAdorner: Layer already exists?");
                return;
            }

            // More robust layer finding: Start from AssociatedObject and walk up
            DependencyObject current = AssociatedObject;
            while (current != null && _adornerLayer == null)
            {
                if (current is AdornerDecorator decorator)
                {
                    _adornerLayer = decorator.AdornerLayer;
                    Debug.WriteLine("CreateAndShowAdorner: Found AdornerLayer in an AdornerDecorator.");
                }
                else
                {
                    // Try GetAdornerLayer directly on the current element
                    _adornerLayer = AdornerLayer.GetAdornerLayer(current as Visual);
                    if (_adornerLayer != null)
                    {
                        Debug.WriteLine($"CreateAndShowAdorner: Found AdornerLayer directly on {current.GetType().Name}.");
                    }
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
                        Debug.WriteLine("CreateAndShowAdorner: Found AdornerLayer via Window's AdornerDecorator.");
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
                        Debug.WriteLine("CreateAndShowAdorner: Adorner successfully added.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"CreateAndShowAdorner: Error adding adorner: {ex.Message}");
                        _dragAdorner = null; // Ensure it's null if add failed
                    }
                }
                else
                {
                    Debug.WriteLine("CreateAndShowAdorner: Failed to create drag visual.");
                }
            }
            else
            {
                Debug.WriteLine("CreateAndShowAdorner: FAILED to find AdornerLayer!");
            }
        }

        private void RemoveAdorner()
        {
            if (_dragAdorner != null)
            {
                Debug.WriteLine("RemoveAdorner: Removing adorner...");
                if (_adornerLayer != null)
                {
                    try
                    {
                        _adornerLayer.Remove(_dragAdorner);
                        Debug.WriteLine("RemoveAdorner: Adorner removed.");
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
            Debug.WriteLine($"CreateDragVisual: Created visual with size {rect.Width}x{rect.Height}");
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
                    Debug.WriteLine("GetScreenMousePosition: HwndSource not found, using less reliable method.");

                    return AssociatedObject.PointToScreen(Mouse.GetPosition(AssociatedObject));
                }
            }
            Debug.WriteLine("GetScreenMousePosition: GetCursorPos failed.");
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
            // ... (unchanged) ...
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

