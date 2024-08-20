using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ScriptPlayer.Shared.Beats;

namespace ScriptPlayer.Shared.Controls
{
    public class BarBar : TimelineBaseControl
    {
        public event EventHandler<Bar> SelectionChanged;

        public event EventHandler<RequestEventArgs<Bar>> RequestNewBar; 
        public event EventHandler<Bar> BarRightClicked;

        public static readonly DependencyProperty SelectedBarProperty = DependencyProperty.Register(
            "SelectedBar", typeof(Bar), typeof(BarBar), new PropertyMetadata(default(Bar)));

        public Bar SelectedBar
        {
            get { return (Bar) GetValue(SelectedBarProperty); }
            set { SetValue(SelectedBarProperty, value); }
        }

        public static readonly DependencyProperty BarsProperty = DependencyProperty.Register(
            "Bars", typeof(BarCollection), typeof(BarBar), new PropertyMetadata(default(BarCollection), OnBarsPropertyChanged));

        private static void OnBarsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((BarBar) d).BarsChanged(e.OldValue as BarCollection, e.NewValue as BarCollection);
        }

        private void BarsChanged(BarCollection oldValue, BarCollection newValue)
        {
            if (oldValue != null)
                oldValue.Changed -= BarsOnChanged;

            if (newValue != null)
                newValue.Changed += BarsOnChanged;

            InvalidateVisual();
        }

        private void BarsOnChanged(object sender, EventArgs eventArgs)
        {
            InvalidateVisual();
        }

        public BarCollection Bars
        {
            get { return (BarCollection) GetValue(BarsProperty); }
            set { SetValue(BarsProperty, value); }
        }

        public static readonly DependencyProperty TactsProperty = DependencyProperty.Register(
            "Tacts", typeof(TactCollection), typeof(BarBar), new PropertyMetadata(default(TactCollection)));

        private double _mouseDownX;
        private TimeSpan _mouseDownT;
        private bool _shift;
        private bool _control;
        private Bar _selectedBar;
        private bool _selectedStart;
        private double _selectOffset;
        private bool _down;
        private double _mouseMoveX;

        const double maxDragDistance = 5;

        public TactCollection Tacts
        {
            get { return (TactCollection) GetValue(TactsProperty); }
            set { SetValue(TactsProperty, value); }
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            TimeSpan time = TimeFromX(e.GetPosition(this).X);
            var bars = Bars.Get(time);
            Bar bar = bars.FirstOrDefault();

            if (bar != null)
                OnBarRightClicked(bar);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _mouseDownX = e.GetPosition(this).X;
            _mouseMoveX = _mouseDownX;

            _mouseDownT = TimeFromX(_mouseDownX);
            _shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            _control = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            if (!_shift && !_control)
            {
                // Move/Modify
                FindClosestBarEnd(_mouseDownX, out _selectedBar, out _selectedStart, out _selectOffset);

                if (_selectedBar != null)
                {
                    _down = true;
                    Mouse.Capture(this);
                    SetSelectedBar(_selectedBar);
                }
            }

            if (_shift)
            {
                FindClosestTact(_mouseDownX, out Tact closestTact, out int index);
                if (closestTact == null)
                    return;

                _selectedBar = GetNewBar(closestTact.BeatsPerBar);
                _selectedBar.Tact = closestTact;
                _selectedBar.Start = index;
                _selectedBar.Length = 1;

                Bars.Add(_selectedBar);
                SetSelectedBar(_selectedBar);

                // Add
                _down = true;
                Mouse.Capture(this);
            }
            else if (_control)
            {
                FindClosestBar(_mouseDownX, out Bar bar);
                if (bar != null)
                {
                    Bars.Remove(bar);
                    if (SelectedBar == bar)
                    {
                        SetSelectedBar(null);
                    }
                }
            }
            else
            {
                FindBar(_mouseDownX, out Bar bar);
                if (bar != null)
                {
                    SetSelectedBar(bar);
                }
            }
        }

        private void SetSelectedBar(Bar bar)
        {
            SelectedBar = bar;
            OnSelectionChanged(bar);
        }

        private Bar GetNewBar(int beatsPerBarFallback)
        {
            Bar bar = OnRequestNewBar();
            if(bar == null)
                bar = new Bar
                {
                    Rythm = new Rythm(beatsPerBarFallback),
                    Subdivisions = 1
                };

            return bar;
        }

        private void FindBar(double x, out Bar closestBar)
        {
            closestBar = null;

            foreach (Bar bar in Bars)
            {
                double startX = XFromTime(bar.GetStartTime());
                double endX = XFromTime(bar.GetEndTime());

                if (startX <= x && endX >= x)
                {
                    closestBar = bar;
                    return;
                }
            }
        }

        private void FindClosestBar(double x, out Bar closestBar)
        {
            closestBar = null;
            double shortestDistance = double.MaxValue;

            foreach (Bar bar in Bars)
            {
                double startX = XFromTime(bar.GetStartTime());
                double endX = XFromTime(bar.GetEndTime());

                if (startX <= x && endX >= x)
                {
                    closestBar = bar;
                    return;
                }

                double startDistance = Math.Abs(startX - x);

                if (startDistance <= maxDragDistance && startDistance < shortestDistance)
                {
                    shortestDistance = startDistance;
                    closestBar = bar;
                }
                
                double endDistance = Math.Abs(endX - x);

                if (endDistance <= maxDragDistance && endDistance < shortestDistance)
                {
                    shortestDistance = endDistance;
                    closestBar = bar;
                }
            }
        }

        private void FindClosestTact(double x, out Tact closestTact, out int index)
        {
            double shortestDistance = double.MaxValue;

            TimeSpan time = TimeFromX(x);
            closestTact = null;
            index = 0;
            
            foreach (Tact tact in Tacts)
            {
                double startX = XFromTime(tact.Start);
                double endX = XFromTime(tact.End);

                double distance;

                // included
                if (startX <= x && endX >= x)
                {
                    distance = 0;
                }
                else
                {
                    distance = Math.Min(Math.Abs(startX - x), Math.Abs(endX - x));
                }

                if (distance < maxDragDistance && distance < shortestDistance)
                {
                    closestTact = tact;
                    index = tact.GetClosestIndex(time);

                    if(distance == 0)
                        return;
                }
            }
        }

        private void FindClosestBarEnd(double x, out Bar closestBar, out bool isStart, out double selectOffset)
        {
            double shortestDistance = double.MaxValue;
            bool wasInside = false;

            closestBar = null;
            isStart = false;
            selectOffset = 0;

            foreach (Bar bar in Bars)
            {
                double startX = XFromTime(bar.GetStartTime());
                double endX = XFromTime(bar.GetEndTime());

                bool isInside = x >= startX && x <= endX;
                bool betterInside = isInside && !wasInside;
                
                double startDistance = Math.Abs(startX - x);
                double endDistance = Math.Abs(endX - x);

                if (startDistance <= maxDragDistance && (startDistance <= shortestDistance || (startDistance == shortestDistance && betterInside)))
                {
                    shortestDistance = startDistance;
                    closestBar = bar;
                    isStart = true;
                    selectOffset = startX - x;
                    wasInside = isInside;
                }
                
                if (endDistance <= maxDragDistance && (endDistance < shortestDistance || (endDistance == shortestDistance && betterInside)))
                {
                    shortestDistance = endDistance;
                    closestBar = bar;
                    isStart = false;
                    selectOffset = endX - x;
                    wasInside = isInside;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            double x = e.GetPosition(this).X;
            
            if (!_down)
            {
                FindClosestBarEnd(x, out Bar bar, out bool _, out double _);

                if (bar != null)
                    this.Cursor = Cursors.SizeWE;
                else
                    this.Cursor = null;

                return;
            }

            _mouseMoveX = x;

            UpdateMouseMove();

            InvalidateVisual();
        }

        private void UpdateMouseMove()
        {
            if (_down)
            {
                if (!_shift && !_control)
                {
                    double correctedX = _mouseMoveX + _selectOffset;
                    TimeSpan newTimestamp = TimeFromX(correctedX);
                    int newIndex = _selectedBar.GetClosestIndex(newTimestamp);
                    
                    if (_selectedStart)
                    {
                        int end = _selectedBar.Start + _selectedBar.Length;
                        
                        if (end > newIndex)
                        {
                            int newLength = end - newIndex;
                            _selectedBar.Start = newIndex;
                            _selectedBar.Length = newLength;
                        }
                    }
                    else
                    {
                        if (_selectedBar.Start < newIndex)
                        {
                            _selectedBar.Length = newIndex - _selectedBar.Start;
                        }
                    }
                }
                else if (_shift)
                {
                    TimeSpan t = TimeFromX(_mouseMoveX);
                    SetSelectedBeat(_mouseDownT, t);
                }
            }
        }

        private void SetSelectedBeat(TimeSpan t1, TimeSpan t2)
        {
            int i1 = _selectedBar.GetClosestIndex(t1);
            int i2 = _selectedBar.GetClosestIndex(t2);

            if (i1 > i2)
            {
                _selectedBar.Start = i2;
                _selectedBar.Length = i1 - i2;
            }
            else if (i1 < i2)
            {
                _selectedBar.Start = i1;
                _selectedBar.Length = i2 - i1;
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!_down)
                return;

            double x = e.GetPosition(this).X;
            TimeSpan t = TimeFromX(x);

            if (!_shift && !_control)
            {
                double correctedX = x + _selectOffset;
                TimeSpan newTimestamp = TimeFromX(correctedX);
                int newIndex = _selectedBar.GetClosestIndex(newTimestamp);

                if (_selectedStart)
                {
                    int end = _selectedBar.Start + _selectedBar.Length;

                    if (end > newIndex)
                    {
                        int newLength = end - newIndex;
                        _selectedBar.Start = newIndex;
                        _selectedBar.Length = newLength;
                    }
                }
                else
                {
                    if (_selectedBar.Start < newIndex)
                    {
                        _selectedBar.Length = newIndex - _selectedBar.Start;
                    }
                }
            }
            else if (_shift)
            {
                SetSelectedBeat(_mouseDownT, t);
            }

            InvalidateVisual();

            _down = false;
            _selectedBar = null;
            ReleaseMouseCapture();
        }

        protected override void Render(TimeBasedRenderContext context)
        {
            UpdateMouseMove();

            context.DrawingContext.DrawRectangle(Background, null, context.FullRect);

            if (Bars != null)
            {
                int i = 0;

                foreach (Bar bar in Bars.OrderBy(b => b.GetEndTime()))
                {
                    i++;

                    TimeFrame barTimeFrame = bar.TimeFrame;

                    if (!barTimeFrame.Intersects(context.TimeFrame))
                        continue;

                    double startX = XFromTime(barTimeFrame.From);
                    double endX = XFromTime(barTimeFrame.To);

                    bar.GetBarIndices(context.TimeFrame, out int firstVisibleBar, out int lastVisibleBar);
                    bar.GetBeatIndices(barTimeFrame, out int firstBeat, out int lastBeat);

                    Rect barRect = new Rect(startX, context.FullRect.Y,endX - startX,context.FullRect.Height);

                    Brush background = i % 2 == 0 ? Brushes.DarkRed : Brushes.DarkMagenta;
                    context.DrawingContext.DrawRectangle(background, new Pen(Brushes.Red, 1), barRect);

                    for (int barNo = firstVisibleBar; barNo <= lastVisibleBar; barNo++)
                    {
                        if (barNo % 2 == 0)
                        {
                            double barStartX = XFromTime(bar.TranslateIndex(barNo * bar.Rythm.Length));
                            double barEndX = XFromTime(bar.TranslateIndex((barNo + 1) * bar.Rythm.Length));

                            if (barStartX > endX)
                                continue;

                            barEndX = Math.Min(barEndX, endX);

                            Rect barRect2 = new Rect(barStartX, context.FullRect.Y, barEndX - barStartX, context.FullRect.Height);
                            context.DrawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(100,0,0,0)), null, barRect2);
                        }
                    }

                    /*
                    RelativePositionCollection relativePositions = bar.Positions;
                    if (relativePositions == null)
                    {
                        relativePositions = new RelativePositionCollection();
                        relativePositions.Add(new RelativePosition() { RelativeTime = 0, Position = 0 });
                        relativePositions.Add(new RelativePosition() { RelativeTime = 1, Position = 99 });
                    }

                    bool invert = relativePositions.First().Position != relativePositions.Last().Position;

                    PathGeometry path = new PathGeometry();
                    PathFigure figure = new PathFigure();
                    figure.IsClosed = false;
                    figure.IsFilled = false;
                    bool started = false;
                    path.Figures.Add(figure);
                    
                    for (int barNo = firstVisibleBar; barNo <= lastVisibleBar; barNo++)
                    {
                        double barStartX = XFromTime(bar.TranslateIndex(barNo * bar.Rythm.Length));
                        double barEndX = XFromTime(bar.TranslateIndex((barNo + 1) * bar.Rythm.Length));
                        double barWidth = barEndX - barStartX;

                        foreach (var relativePoint in relativePositions)
                        {
                            double x = relativePoint.RelativeTime * barWidth + barStartX;
                            double relativeHeight = (1 - (relativePoint.Position / 99.0));
                            if (invert && barNo%2 == 0)
                                relativeHeight = 1 - relativeHeight;
                                    
                            double y = context.FullRect.Y + context.FullRect.Height * relativeHeight;
                            Point p = new Point(x,y);

                            if (!started)
                            {
                                figure.StartPoint = p;
                                started = true;
                            }
                            else
                            {
                                figure.Segments.Add(new LineSegment(p, true));
                            }
                        }
                    }

                    Pen linePen = new Pen(new SolidColorBrush(Color.FromArgb(100,255,255,255)), 1);

                    context.DrawingContext.PushClip(new RectangleGeometry(new Rect(startX, context.FullRect.Y, endX - startX, context.FullRect.Height)));
                    context.DrawingContext.DrawGeometry(null, linePen, path);
                    context.DrawingContext.Pop();

                */
                    for (int tactIndex = firstBeat; tactIndex <= lastBeat; tactIndex++)
                    {
                        int beatIndex = tactIndex % bar.Rythm.Length;

                        TimeSpan beatPosition = bar.TranslateIndex(tactIndex);
                        double x = XFromTime(beatPosition);

                        Pen pen;
                        if (bar.Rythm[beatIndex])
                        {
                            pen = new Pen(Brushes.White, 4);
                        }
                        else
                        {
                            pen = new Pen(Brushes.White, 0.2);
                        }

                        context.DrawingContext.DrawLine(pen, new Point(x,context.FullRect.Top), new Point(x, context.FullRect.Bottom));
                    }

                    
                }
            }
        }

        protected virtual void OnBarRightClicked(Bar bar)
        {
            BarRightClicked?.Invoke(this, bar);
        }

        protected virtual Bar OnRequestNewBar()
        {
            RequestEventArgs<Bar> eventArgs = new RequestEventArgs<Bar>();
            RequestNewBar?.Invoke(this, eventArgs);
            if (eventArgs.Handled)
                return eventArgs.Value;
            return null;
        }

        protected virtual void OnSelectionChanged(Bar e)
        {
            SelectionChanged?.Invoke(this, e);
        }
    }

    public struct Rythm
    {
        public int Length => Beats.Length - 1;
        public bool[] Beats { get; set; }
        public static Rythm Empty => new Rythm(0);
        public bool IsEmpty => Length == 0;

        public Rythm(int length)
        {
            bool[] beats = new bool[length+1];
            for (int i = 0; i <= length; i++)
            {
                beats[i] = i == 0 || i == length;
            }

            Beats = beats;
        }

        public Rythm(bool[] beats)
        {
            Beats = beats;
        }

        public bool this[int index]
        {
            get => Beats[index];
        }
    }
}
