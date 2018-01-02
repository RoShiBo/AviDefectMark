using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AviDefMark
{
    public class TransformPanel : Panel
    {


        public static readonly DependencyProperty TransformProperty =
            DependencyProperty.Register(
                "Transform", typeof(Transform), typeof(TransformPanel),
                new FrameworkPropertyMetadata(Transform.Identity,
                    FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty ImageProperty =
           DependencyProperty.Register(
               "DisplayImage", typeof(string), typeof(TransformPanel));

        public static readonly DependencyProperty StentProperty =
          DependencyProperty.Register(
              "Stent", typeof(string), typeof(TransformPanel));

        public static readonly DependencyProperty PanelProperty =
          DependencyProperty.Register(
              "Panel", typeof(string), typeof(TransformPanel));

        public static readonly DependencyProperty NumOfRecordsProperty =
         DependencyProperty.Register(
             "NumOfRecords", typeof(string), typeof(TransformPanel));

        public static readonly DependencyProperty NumOfSavedRecordsProperty =
        DependencyProperty.Register(
            "NumOfSavedRecords", typeof(string), typeof(TransformPanel));

        public static readonly DependencyProperty PositionProperty =
            DependencyProperty.RegisterAttached(
                "Position", typeof(Point?), typeof(TransformPanel),
                new PropertyMetadata(PositionPropertyChanged));

        public Transform Transform
        {
            get { return (Transform)GetValue(TransformProperty); }
            set { SetValue(TransformProperty, value); }
        }

       
        public string DisplayImage
        {
            get { return (string)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        

        public string Stent
        {
            get { return (string)GetValue(StentProperty); }
            set { SetValue(StentProperty, value); }
        }

        

        public string Panel
        {
            get { return (string)GetValue(PanelProperty); }
            set { SetValue(PanelProperty, value); }
        }

        public string NumOfRecords
        {
            get { return (string)GetValue(NumOfRecordsProperty); }
            set { SetValue(NumOfRecordsProperty, value); }
        }

        public string NumOfSavedRecords
        {
            get { return (string)GetValue(NumOfSavedRecordsProperty); }
            set { SetValue(NumOfSavedRecordsProperty, value); }
        }


        public static Point? GetPosition(UIElement element)
        {
            return (Point?)element.GetValue(PositionProperty);
        }

        public static void SetPosition(UIElement element, Point? value)
        {
            element.SetValue(PositionProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var infiniteSize = new Size(double.PositiveInfinity,
                                        double.PositiveInfinity);

            foreach (UIElement element in InternalChildren)
            {
                element.Measure(infiniteSize);
            }

            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement element in InternalChildren)
            {
                ArrangeElement(element, GetPosition(element));
            }

            return finalSize;
        }

        private void ArrangeElement(UIElement element, Point? position)
        {
            var arrangeRect = new Rect(element.DesiredSize);

            if (position.HasValue && Transform != null)
            {
                arrangeRect.Location = Transform.Transform(position.Value);
            }

            element.Arrange(arrangeRect);
        }

        private static void PositionPropertyChanged(
            DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var element = (UIElement)obj;
            var panel = VisualTreeHelper.GetParent(element) as TransformPanel;

            if (panel != null)
            {
                panel.ArrangeElement(element, (Point?)e.NewValue);
            }
        }

      
    }
    
}
