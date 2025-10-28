using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace RackMonitor.Adorners
{
    public class DragAdorner : Adorner
    {
        private readonly UIElement _visual; // The visual representation (e.g., Rectangle with VisualBrush)
        private Point _positionRelativeToParent; // Top-left of adorner relative to AdornedElement's PARENT
        private readonly Point _clickOffsetWithinVisual; // Offset from visual's top-left to the initial mouse click point

        public DragAdorner(UIElement adornedElement, UIElement visual, Point initialMousePositionRelativeToAdorned)
            : base(adornedElement)
        {
            _visual = visual;
            // Calculate where the mouse clicked WITHIN the visual itself
            _clickOffsetWithinVisual = initialMousePositionRelativeToAdorned;
            Debug.WriteLine($"DragAdorner: Initial Click Offset within visual = ({_clickOffsetWithinVisual.X},{_clickOffsetWithinVisual.Y})");

            // Calculate the initial position relative to the adorned element's PARENT
            // Get screen position of initial click
            Point initialClickScreenPos = adornedElement.PointToScreen(initialMousePositionRelativeToAdorned);
            UpdatePosition(initialClickScreenPos); // Call update to set initial position correctly

            IsHitTestVisible = false;
        }

        // Method to update based on CURRENT MOUSE SCREEN COORDINATES
        public void UpdatePosition(Point currentMouseScreenCoordinates)
        {
            // Get the parent of the adorned element (the coordinate space for ArrangeOverride)
            var parent = VisualTreeHelper.GetParent(AdornedElement) as UIElement;
            if (parent == null) return; // Should not happen in normal visual tree

            // Convert current mouse SCREEN position to be relative to the PARENT
            Point currentMouseRelativeToParent = parent.PointFromScreen(currentMouseScreenCoordinates);

            // Calculate the desired top-left position for the visual RELATIVE TO THE PARENT
            // This is the current mouse position (relative to parent) minus the offset where the click originally happened within the visual
            Point newPosition = (Point)(currentMouseRelativeToParent - _clickOffsetWithinVisual);

            if (_positionRelativeToParent != newPosition)
            {
                _positionRelativeToParent = newPosition;
                // Debug.WriteLine($"DragAdorner.UpdatePosition: New Position Relative to Parent = ({_positionRelativeToParent.X},{_positionRelativeToParent.Y})");
                InvalidateArrange(); // Request re-layout
            }
        }


        // Override ArrangeOverride to position the visual relative to the AdornedElement's PARENT
        protected override Size ArrangeOverride(Size finalSize)
        {
            _visual?.Arrange(new Rect(_positionRelativeToParent, _visual.DesiredSize));
            // Return the size of the parent element, as the adorner conceptually covers that area
            return finalSize;
        }

        // --- MeasureOverride, VisualChildrenCount, GetVisualChild remain the same ---
        protected override Size MeasureOverride(Size constraint)
        {
            _visual?.Measure(constraint);
            return _visual?.DesiredSize ?? new Size(0, 0);
        }
        protected override int VisualChildrenCount => _visual != null ? 1 : 0;
        protected override Visual GetVisualChild(int index)
        {
            if (index == 0 && _visual != null) return _visual;
            else throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}

