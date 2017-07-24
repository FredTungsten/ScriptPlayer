using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ScriptPlayer.Shared
{
    /// <summary>
    /// Interaction logic for LaunchSimulator.xaml
    /// </summary>
    public partial class LaunchSimulator : UserControl
    {
        public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
            "Position", typeof(double), typeof(LaunchSimulator), new PropertyMetadata(default(double)));

        public double Position
        {
            get { return (double) GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }

        public static readonly DependencyProperty PositionChangesPerSecondProperty = DependencyProperty.Register(
            "PositionChangesPerSecond", typeof(double), typeof(LaunchSimulator), new PropertyMetadata(6.0));

        public double PositionChangesPerSecond
        {
            get { return (double) GetValue(PositionChangesPerSecondProperty); }
            set { SetValue(PositionChangesPerSecondProperty, value); }
        }

        //private double _targetPosition;
        //private double _targetSpeed;

        public LaunchSimulator()
        {
            InitializeComponent();
        }

        public void SetPosition(byte position, byte speed)
        {
            double delta = Math.Abs(Position - position);
            double absoluteSpeed = PositionChangesPerSecond * (speed+1);
            TimeSpan duration = TimeSpan.FromSeconds(delta / absoluteSpeed);

            DoubleAnimation positionAnimation = new DoubleAnimation(Position, position, new Duration(duration), FillBehavior.HoldEnd);
            BeginAnimation(PositionProperty, positionAnimation);
        }

    }
}
