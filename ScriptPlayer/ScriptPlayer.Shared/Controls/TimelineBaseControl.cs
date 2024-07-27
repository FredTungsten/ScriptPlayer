using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JetBrains.Annotations;

namespace ScriptPlayer.Shared
{
    public abstract class TimelineBaseControl : Control
    {
        public event EventHandler<TimeSpan> TimeMouseDown;
        public event EventHandler<TimeSpan> TimeMouseRightDown;
        public event EventHandler<TimeSpan> TimeMouseRightMove;
        public event EventHandler<TimeSpan> TimeMouseRightUp;

        public static readonly DependencyProperty TimeFrameContextProperty = DependencyProperty.Register(
            "TimeFrameContext", typeof(TimeFrameContext), typeof(TimelineBaseControl), new PropertyMetadata(default(TimeFrameContext), OnTimeFrameContextPropertyChanged));

        public TimeFrameContext TimeFrameContext
        {
            get { return (TimeFrameContext) GetValue(TimeFrameContextProperty); }
            set { SetValue(TimeFrameContextProperty, value); }
        }

        private static void OnTimeFrameContextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TimelineBaseControl)d).TimeFrameContextPropertyChanged(e.OldValue as TimeFrameContext, e.NewValue as TimeFrameContext);
        }

        private void TimeFrameContextPropertyChanged(TimeFrameContext oldValue, TimeFrameContext newValue)
        {
            if(oldValue != null)
                oldValue.PropertyChanged -= TimeFrameContextOnPropertyChanged;

            if (newValue != null)
                newValue.PropertyChanged += TimeFrameContextOnPropertyChanged;

            VisualPropertyChanged();
        }

        private void TimeFrameContextOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            VisualPropertyChanged();
        }

        private bool _rightdown;
        private double _lastMousePosition;

        public TimelineBaseControl()
        {
            TimeFrameContext = new TimeFrameContext();
        }
        
        protected virtual void VisualPropertyChanged()
        {
            RefreshRightMove();
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            double x = e.GetPosition(this).X;
            TimeSpan position = TimeFromX(x);

            OnTimeMouseDown(position);
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            CaptureMouse();
            _rightdown = true;
            _lastMousePosition = e.GetPosition(this).X;
            OnTimeMouseRightDown(TimeFromX(_lastMousePosition));
            e.Handled = true;
        }

        protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e)
        {
            if (!_rightdown)
                base.OnPreviewMouseRightButtonUp(e);
            else
            {
                _rightdown = false;
                _lastMousePosition = e.GetPosition(this).X;
                ReleaseMouseCapture();
                OnTimeMouseRightUp(TimeFromX(_lastMousePosition));
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_rightdown)
            {
                _lastMousePosition = e.GetPosition(this).X;
                RefreshRightMove();
                e.Handled = true;
            }
        }

        private void RefreshRightMove()
        {
            if (_rightdown)
                OnTimeMouseRightMove(TimeFromX(_lastMousePosition));
        }

        protected TimeSpan TimeFromX(double x)
        {
            /*
            double relativeProgress = (x / ActualWidth) - Midpoint;
            TimeSpan position = Progress + TotalDisplayedDuration.Multiply(relativeProgress);
            return position;
            */
        
            return this.TimeFrameContext.TotalDisplayedDuration.Multiply(x / ActualWidth) + (TimeFrameContext.Progress - TimeFrameContext.TotalDisplayedDuration.Multiply(TimeFrameContext.Midpoint));
        }

        protected double XFromTime(TimeSpan time)
        {
            /*
            TimeSpan timeFrom = Progress - TotalDisplayedDuration.Multiply(Midpoint);
            TimeSpan timeTo = Progress + TotalDisplayedDuration.Multiply(1 - Midpoint);
            double relativePosition = (time - timeFrom).Divide(timeTo - timeFrom);
            double absolutePosition = relativePosition * ActualWidth;
            return absolutePosition;
            */

            return (time - (TimeFrameContext.Progress - TimeFrameContext.TotalDisplayedDuration.Multiply(TimeFrameContext.Midpoint))).Divide(TimeFrameContext.TotalDisplayedDuration) * ActualWidth;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (TimeFrameContext == null)
                return;

            TimeSpan timeFrom = TimeFrameContext.Progress - TimeFrameContext.TotalDisplayedDuration.Multiply(TimeFrameContext.Midpoint);
            TimeSpan timeTo = TimeFrameContext.Progress + TimeFrameContext.TotalDisplayedDuration.Multiply(1 - TimeFrameContext.Midpoint);

            Rect fullRect = new Rect(new Point(), new Size(ActualWidth, ActualHeight));
            drawingContext.PushClip(new RectangleGeometry(fullRect));

            TimeBasedRenderContext context = new TimeBasedRenderContext
            {
                DrawingContext = drawingContext,
                TimeFrom = timeFrom,
                TimeTo = timeTo,
                FullRect = fullRect
            };

            context.Prepare();

            Render(context);

            drawingContext.Pop();
        }

        protected abstract void Render(TimeBasedRenderContext context);

        protected bool IsTimeVisible(TimeSpan time)
        {
            if (time < TimeFrameContext.Progress - TimeFrameContext.TotalDisplayedDuration.Multiply(TimeFrameContext.Midpoint))
                return false;
            if (time > TimeFrameContext.Progress + TimeFrameContext.TotalDisplayedDuration.Multiply(1 - TimeFrameContext.Midpoint))
                return false;

            return true;
        }

        protected virtual void OnTimeMouseDown(TimeSpan e)
        {
            TimeMouseDown?.Invoke(this, e);
        }

        protected virtual void OnTimeMouseRightDown(TimeSpan e)
        {
            TimeMouseRightDown?.Invoke(this, e);
        }

        protected virtual void OnTimeMouseRightUp(TimeSpan e)
        {
            TimeMouseRightUp?.Invoke(this, e);
        }

        protected virtual void OnTimeMouseRightMove(TimeSpan e)
        {
            TimeMouseRightMove?.Invoke(this, e);
        }
    }

    public class TimeFrameContext : INotifyPropertyChanged
    {
        private TimeSpan _totalDisplayedDuration;
        private double _midpoint;
        private TimeSpan _progress;

        public TimeFrameContext()
        {
            TotalDisplayedDuration = TimeSpan.FromSeconds(5);
            Progress = TimeSpan.Zero;
            Midpoint = 0.5;
        }

        public TimeSpan TotalDisplayedDuration
        {
            get => _totalDisplayedDuration;
            set
            {
                if (value.Equals(_totalDisplayedDuration)) return;
                _totalDisplayedDuration = value;
                OnPropertyChanged();
            }
        }

        public double Midpoint
        {
            get => _midpoint;
            set
            {
                if (value.Equals(_midpoint)) return;
                _midpoint = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan Progress
        {
            get => _progress;
            set
            {
                if (value.Equals(_progress)) return;
                _progress = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TimeBasedRenderContext
    {
        public TimeSpan TimeFrom { get; set; }
        public TimeSpan TimeTo { get; set; }
        public TimeSpan TotalDuration { get; set; }

        public Rect FullRect { get; set; }
        public DrawingContext DrawingContext { get; set; }

        public void Prepare()
        {
            TotalDuration = TimeTo - TimeFrom;
        }       

        public double GetRelativeXPosition(TimeSpan timePos)
        {
            return (timePos - TimeFrom).Divide(TotalDuration);
        }

        public double GetAbsoluteXPosition(TimeSpan timePos)
        {
            return GetRelativeXPosition(timePos) * FullRect.Width + FullRect.X;
        }
    }
}
