using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ScriptPlayer.Shared.Beats;

namespace ScriptPlayer.Shared.Controls
{
    public class TactBar : TimelineBaseControl
    {
        public event EventHandler<Tact> TactRightClicked; 

        public static readonly DependencyProperty TactsProperty = DependencyProperty.Register(
            "Tacts", typeof(TactCollection), typeof(TactBar), new PropertyMetadata(default(TactCollection)));

        private double _mouseDownX;
        private bool _down;
        private Tact _selectedTact;
        private bool _selectedStart;
        private double _selectOffset;
        private bool _shift;
        private bool _control;
        private TimeSpan _mouseDownT;

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
                OnTactRightClicked(tact);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _mouseDownX = e.GetPosition(this).X;
            _mouseDownT = TimeFromX(_mouseDownX);
            _shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
            _control = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

            if(!_shift && !_control)
            {
                // Move/Modify
                FindClosestTact(_mouseDownX, out _selectedTact, out _selectedStart, out _selectOffset);

                if (_selectedTact != null)
                {
                    _down = true;
                    Mouse.Capture(this);
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

                double correctedX = x + _selectOffset;
                TimeSpan newTimestamp = TimeFromX(correctedX);

                if (_selectedStart)
                {
                    if(newTimestamp < _selectedTact.End)
                        _selectedTact.Start = newTimestamp;
                }
                else
                {
                    if(newTimestamp > _selectedTact.Start)
                        _selectedTact.End = newTimestamp;
                }
            }
            else if (_shift)
            {
                SetSelectedTact(_mouseDownT, t);
            }

            InvalidateVisual();
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
            context.DrawingContext.DrawRectangle(Background, null, context.FullRect);

            if (Tacts != null)
            {
                foreach (Tact tact in Tacts.Get(context.TimeFrom, context.TimeTo))
                {
                    double xStart = XFromTime(tact.Start);
                    double xEnd = XFromTime(tact.End);

                    Rect r = new Rect(xStart, context.FullRect.Y, xEnd - xStart, context.FullRect.Height);
                    context.DrawingContext.DrawRectangle(Brushes.DarkBlue, new Pen(Brushes.DodgerBlue, 1), r);

                    foreach (TimeSpan beat in tact.GetBeats(context.TimeFrom, context.TimeTo))
                    {
                        double x = XFromTime(beat);
                        context.DrawingContext.DrawLine(new Pen(Brushes.White, 1), new Point(x,context.FullRect.Top), new Point(x, context.FullRect.Bottom));
                    }
                }
            }
        }

        protected virtual void OnTactRightClicked(Tact e)
        {
            TactRightClicked?.Invoke(this, e);
        }
    }
}
