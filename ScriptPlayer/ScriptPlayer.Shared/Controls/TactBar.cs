using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ScriptPlayer.Shared.Beats;

namespace ScriptPlayer.Shared.Controls
{
    public class TactBar : TimelineBaseControl
    {
        public event EventHandler<Tact> SelectionChanged;

        public event EventHandler<Tuple<Tact, TimeSpan>> TactRightClicked; 

        public static readonly DependencyProperty TactsProperty = DependencyProperty.Register(
            "Tacts", typeof(TactCollection), typeof(TactBar), new PropertyMetadata(default(TactCollection), OnTactsPropertyChanged));

        private static void OnTactsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TactBar) d).OnTactsChanged(e.OldValue as TactCollection, e.NewValue as TactCollection);
        }

        public static readonly DependencyProperty SelectedTactProperty = DependencyProperty.Register(
            "SelectedTact", typeof(Tact), typeof(TactBar), new PropertyMetadata(default(Tact)));

        public Tact SelectedTact
        {
            get { return (Tact) GetValue(SelectedTactProperty); }
            set { SetValue(SelectedTactProperty, value); }
        }

        private void OnTactsChanged(TactCollection oldValue, TactCollection newValue)
        {
            if (oldValue != null)
                oldValue.Changed -= TactsOnChanged;

            if (newValue != null)
                newValue.Changed += TactsOnChanged;
        }

        private void TactsOnChanged(object sender, EventArgs eventArgs)
        {
            InvalidateVisual();
        }

        private double _mouseDownX;
        private bool _down;
        private Tact _selectedTact;
        private bool _selectedStart;
        private double _selectOffset;
        private bool _shift;
        private bool _control;
        private TimeSpan _mouseDownT;
        private double _mouseMoveX;

        const double maxDragDistance = 5;

        public TactCollection Tacts
        {
            get { return (TactCollection) GetValue(TactsProperty); }
            set { SetValue(TactsProperty, value); }
        }

        public TactBar()
        {
            
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            TimeSpan time = TimeFromX(e.GetPosition(this).X);
            var tacts = Tacts.Get(time);
            Tact tact = tacts.FirstOrDefault();

            if (tact != null)
                OnTactRightClicked(tact, time);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _mouseDownX = e.GetPosition(this).X;
            _mouseDownT = TimeFromX(_mouseDownX);
            _shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            _control = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

            if(!_shift && !_control)
            {
                // Move/Modify
                FindClosestTact(_mouseDownX, out _selectedTact, out _selectedStart, out _selectOffset);

                if (_selectedTact != null)
                {
                    _down = true;
                    Mouse.Capture(this);
                    SetSelectedTact(_selectedTact);
                }
            }

            if (_shift)
            {
                _selectedTact = new Tact()
                {
                    Beats = 2,
                    BeatsPerBar = 4,
                    Start = _mouseDownT,
                    End = _mouseDownT,
                };

                Tacts.Add(_selectedTact);
                
                // Add
                _down = true;
                Mouse.Capture(this);

                SetSelectedTact(_selectedTact);
            }
            else if (_control)
            {
                // TODO
                // Delete with confirmation?
            }
            else
            {
                FindClosestTact(_mouseDownX, out Tact tact);
                if(tact != null)
                    SetSelectedTact(tact);
            }
        }

        private void FindClosestTact(double x, out Tact closestTact)
        {
            closestTact = null;
            
            foreach (Tact tact in Tacts)
            {
                double startX = XFromTime(tact.Start);
                double endX = XFromTime(tact.End);

                if (x >= startX && x <= endX)
                {
                    closestTact = tact;
                    return;
                }
            }
        }

        private void FindClosestTact(double x, out Tact closestTact, out bool isStart, out double selectOffset)
        {
            double shortestDistance = double.MaxValue;

            closestTact = null;
            isStart = false;
            selectOffset = 0;

            foreach (Tact tact in Tacts)
            {
                double startX = XFromTime(tact.Start);
                double startDistance = Math.Abs(startX - x);

                if (startDistance <= maxDragDistance && startDistance < shortestDistance)
                {
                    shortestDistance = startDistance;
                    closestTact = tact;
                    isStart = true;
                    selectOffset = startX - x;
                }

                double endX = XFromTime(tact.End);
                double endDistance = Math.Abs(endX - x);

                if (endDistance <= maxDragDistance && endDistance < shortestDistance)
                {
                    shortestDistance = endDistance;
                    closestTact = tact;
                    isStart = false;
                    selectOffset = endX - x;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            double x = e.GetPosition(this).X;
            TimeSpan t = TimeFromX(x);

            if (!_down)
            {
                FindClosestTact(x, out Tact tact, out bool _, out double _);

                if (tact != null)
                    this.Cursor = Cursors.SizeWE;
                else
                    this.Cursor = null;

                return;
            }

            if (!_shift && !_control)
            {
                _mouseMoveX = x;
                UpdateMouseMove();
            }
            else if (_shift)
            {
                SetSelectedTact(_mouseDownT, t);
            }

            InvalidateVisual();
        }

        private void UpdateMouseMove()
        {
            if (_down && !_shift && !_control)
            {
                double correctedX = _mouseMoveX + _selectOffset;
                TimeSpan newTimestamp = TimeFromX(correctedX);

                if (_selectedStart)
                {
                    if (newTimestamp < _selectedTact.End)
                        _selectedTact.Start = newTimestamp;
                }
                else
                {
                    if (newTimestamp > _selectedTact.Start)
                        _selectedTact.End = newTimestamp;
                }
            }
        }

        private void SetSelectedTact(TimeSpan t1, TimeSpan t2)
        {
            if (t1 > t2)
            {
                _selectedTact.Start = t2;
                _selectedTact.End = t1;
            }
            else
            {
                _selectedTact.Start = t1;
                _selectedTact.End = t2;
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

                if (_selectedStart)
                {
                    _selectedTact.Start = newTimestamp;
                }
                else
                {
                    _selectedTact.End = newTimestamp;
                }
            }
            else
            {
                SetSelectedTact(_mouseDownT, t);   
            }

            InvalidateVisual();

            _down = false;
            _selectedTact = null;
            ReleaseMouseCapture();
        }

        protected override void Render(TimeBasedRenderContext context)
        {
            UpdateMouseMove();
            context.DrawingContext.DrawRectangle(Background, null, context.FullRect);

            if (Tacts != null)
            {
                int index = 0;

                foreach (Tact tact in Tacts.OrderBy(t => t.Start))
                {
                    index++;

                    if (!context.TimeFrame.Intersects(tact.TimeFrame))
                        continue;

                    double xStart = XFromTime(tact.Start);
                    double xEnd = XFromTime(tact.End);

                    if (xEnd < xStart)
                    {
                        double temp = xEnd;
                        xEnd = xStart;
                        xStart = temp;
                    }
                    
                    Rect r = new Rect(xStart, context.FullRect.Y, xEnd - xStart, context.FullRect.Height);
                    Brush background = index % 2 == 0 ? Brushes.DarkBlue : Brushes.DarkGreen;
                    context.DrawingContext.DrawRectangle(background, new Pen(Brushes.DodgerBlue, 1), r);

                    //for(int barIndex = 0; barIndex < tact.Beats / tact.BeatsPerBar; barIndex++)

                    foreach (int beatIndex in tact.GetBeatIndices(context.TimeFrame))
                    {
                        TimeSpan beat = tact.TranslateIndex(beatIndex);
                        double x = XFromTime(beat);
                        double lineWidth = beatIndex % tact.BeatsPerBar == 0 ? 3 : 1;

                        context.DrawingContext.DrawLine(new Pen(Brushes.White, lineWidth), new Point(x,context.FullRect.Top), new Point(x, context.FullRect.Bottom));
                    }
                }
            }
        }

        protected virtual void OnTactRightClicked(Tact tact, TimeSpan time)
        {
            TactRightClicked?.Invoke(this, new Tuple<Tact, TimeSpan>(tact, time));
        }

        protected void SetSelectedTact(Tact tact)
        {
            SelectedTact = tact;
            OnSelectionChanged(tact);
        }

        protected virtual void OnSelectionChanged(Tact e)
        {
            SelectionChanged?.Invoke(this, e);
        }
    }
}
