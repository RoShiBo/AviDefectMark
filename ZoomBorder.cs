using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace AviDefMark
{
    public class ZoomBorder : Border
    {
        public ZoomBorder()
        {
            PanTo(new Point(1000, 1000));
        }
        public UIElement child = null;

        public Point Origin
        {
            get { return (Point)GetValue(OriginProperty); }
            set { SetValue(OriginProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Origin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OriginProperty =
            DependencyProperty.Register("Origin", typeof(Point), typeof(ZoomBorder), new PropertyMetadata(new Point(0,0)));

        public Point Start
        {
            get { return (Point)GetValue(StartProperty); }
            set { SetValue(StartProperty, value); }
        }

        private Point MouseRightPressed = new Point();
        private Point MouseRightReleased = new Point();
        public Rect MarkedAreaRectangle = new Rect();
        System.Windows.Shapes.Rectangle rect;

       

        // Using a DependencyProperty as the backing store for Start.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartProperty =
            DependencyProperty.Register("Start", typeof(Point), typeof(ZoomBorder), new PropertyMetadata(new Point(0, 0)));

        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {
            this.child = element;
            if (child != null)
            {
                TransformGroup group = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                group.Children.Add(st);
                TranslateTransform tt = new TranslateTransform();
                group.Children.Add(tt);
                child.RenderTransform = group;
                child.RenderTransformOrigin = new Point(0.0, 0.0);
                this.MouseWheel += child_MouseWheel;
                this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                this.MouseRightButtonUp += ZoomBorder_MouseRightButtonUp;
              
                this.MouseMove += child_MouseMove;
                this.PreviewMouseRightButtonDown += new MouseButtonEventHandler(
                  child_PreviewMouseRightButtonDown);
               
            }
        }

      

        private void ZoomBorder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point curLoc = e.GetPosition(child);
           

            
            MarkedAreaRectangle.Location = MouseRightPressed;
            MarkedAreaRectangle.Size = (Size)Point.Subtract(curLoc, MouseRightPressed);

            rect = new System.Windows.Shapes.Rectangle();
            rect.Stroke = new SolidColorBrush(Colors.Red);
            rect.StrokeThickness = 5;
            
            rect.Width = MarkedAreaRectangle.Width;
            rect.Height = MarkedAreaRectangle.Height;
            rect.Margin = new Thickness(MouseRightPressed.X, MouseRightPressed.Y, 0, 0);
          
            TransformPanel c = child as TransformPanel;

            c.Children.Add(rect);

            MouseRightReleased = e.GetPosition(child);
          
            int i = 0;

            while (c.Children.Count > 2 && i<c.Children.Count)
            {

                System.Windows.Shapes.Rectangle r = c.Children[i] as System.Windows.Shapes.Rectangle;

                if (r != null)
                    if (r.StrokeThickness == 2)
                        c.Children.RemoveAt(i);
                    else
                        i++;

                else
                    i++;
            }                  
            
        }

        public void Reset()
        {
            if (child != null)
            {
                // reset zoom
                var st = GetScaleTransform(child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                var tt = GetTranslateTransform(child);
                tt.X = 0.0;
                tt.Y = 0.0;
            }
        }

        #region Child Events

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);

                double zoom = e.Delta > 0 ? .2 : -.2;
                if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                    return;

                Point relative = e.GetPosition(child);
                double abosuluteX;
                double abosuluteY;

                abosuluteX = relative.X * st.ScaleX + tt.X;
                abosuluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX += zoom;
                st.ScaleY += zoom;

                tt.X = abosuluteX - relative.X * st.ScaleX;
                tt.Y = abosuluteY - relative.Y * st.ScaleY;
            }
        }

        public void PanTo(Point p)
        {
            if (child != null)
            {
                Origin = new Point(0, 0);
                Start = new Point(0, 0);
                Reset();
                
                var tt = GetTranslateTransform(child);
                Origin = new Point(tt.X, tt.Y);
                Vector v = Start - p;
                tt.X = Origin.X - v.X;
                tt.Y = Origin.Y - v.Y;
            }
        }
        
        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                var tt = GetTranslateTransform(child);
                Start = e.GetPosition(this);
                Origin = new Point(tt.X, tt.Y);
                this.Cursor = Cursors.Hand;
                child.CaptureMouse();

            }
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                child.ReleaseMouseCapture();
                this.Cursor = Cursors.Arrow;
            }
        }

        void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                Point curLoc = e.GetPosition(child);
                TransformPanel cc = child as TransformPanel;

                foreach (UIElement item in cc.Children)
                {
                    System.Windows.Shapes.Rectangle r = item as System.Windows.Shapes.Rectangle;
                    if (r != null)
                    {
                        Rect rr = new Rect(new Point(r.Margin.Left, r.Margin.Top), new Point(r.Margin.Left + r.Width, r.Margin.Top + r.Height));
                        if (rr.Contains(curLoc))
                        {
                            cc.Children.Remove(item);
                            return;
                        }

                    }
                }


                return;
            }
            MouseRightPressed = e.GetPosition(child);
        }

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (child != null)
            {
                if (child.IsMouseCaptured)
                {
                    var tt = GetTranslateTransform(child);
                    Vector v = Start - e.GetPosition(this);
                    tt.X = Origin.X - v.X;
                    tt.Y = Origin.Y - v.Y;
                }

                if(e.RightButton == MouseButtonState.Pressed && MouseRightPressed.X !=0 && MouseRightPressed.Y !=0)
                {
                    
                    
                    Point curLoc = e.GetPosition(child);
                    MarkedAreaRectangle.Location = MouseRightPressed;
                    MarkedAreaRectangle.Size = (Size)Point.Subtract(curLoc, MouseRightPressed);

                    rect = new System.Windows.Shapes.Rectangle();
                    rect.Stroke = new SolidColorBrush(Colors.Red);
                    rect.StrokeThickness =2;
                   
                    rect.Width = MarkedAreaRectangle.Width;
                    rect.Height = MarkedAreaRectangle.Height;
                    rect.Margin = new Thickness(MouseRightPressed.X, MouseRightPressed.Y, 0, 0);
                 
                    TransformPanel c = child as TransformPanel;
                    System.Windows.Shapes.Rectangle tt = c.Children[c.Children.Count - 1] as System.Windows.Shapes.Rectangle;

                    if (tt != null)
                        if (tt.StrokeThickness == 2)
                            tt.Visibility = Visibility.Hidden;
                    c.Children.Add(rect);
                  
                }
            }
        }

        public Point PanToPoint
        {
            get { return (Point)GetValue(PanToPointProperty); }
            set { SetValue(PanToPointProperty, value); PanTo(PanToPoint); }
        }

        // Using a DependencyProperty as the backing store for PanToPoint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PanToPointProperty =
            DependencyProperty.Register("PanToPoint", typeof(Point), typeof(ZoomBorder), new PropertyMetadata(new Point(0,0)));

      
        public void ClearClassifications()
        {
            int i = 0;
            TransformPanel c = child as TransformPanel;
            while (c.Children.Count > 1 && i < c.Children.Count)
            {

                System.Windows.Shapes.Rectangle r = c.Children[i] as System.Windows.Shapes.Rectangle;

                if (r != null)
                    c.Children.RemoveAt(i);
                else
                    i++;

               
            }
        }

        
      
       


        #endregion
    }
}
