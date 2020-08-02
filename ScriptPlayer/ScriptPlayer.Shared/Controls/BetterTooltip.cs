using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ScriptPlayer.Shared.Controls
{
    public static class BetterToolTip
    {
        public static readonly DependencyProperty ToolTipProperty = DependencyProperty.RegisterAttached(
            "ToolTip", typeof(FrameworkElement), typeof(BetterToolTip), new PropertyMetadata(default(FrameworkElement), OnToolTipPropertyChanged));

        private static void OnToolTipPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement parent))
                return;

            parent.MouseEnter -= BetterToolTip_MouseEnter;
            parent.MouseLeave -= BetterToolTip_MouseLeave;

            if (e.NewValue != null)
            {
                parent.MouseEnter += BetterToolTip_MouseEnter;
                parent.MouseLeave += BetterToolTip_MouseLeave;
            }
        }

        private static Dictionary<FrameworkElement, Popup> _popUps = new Dictionary<FrameworkElement, Popup>();

        private static void BetterToolTip_MouseLeave(object sender, MouseEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            if (_popUps.ContainsKey(element))
                _popUps[element].IsOpen = false;
        }

        private static void BetterToolTip_MouseEnter(object sender, MouseEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            Popup popup;

            if (_popUps.ContainsKey(element))
                popup = _popUps[element];
            else
            {
                popup = new Popup();
                popup.Margin = new Thickness(0);
                popup.Child = GetToolTip(element);
                popup.PlacementTarget = element;
                popup.Placement = ToolTipService.GetPlacement(element);
                popup.StaysOpen = true;
                _popUps.Add(element, popup);
            }

            popup.IsOpen = true;
        }

        public static void SetToolTip(DependencyObject element, FrameworkElement value)
        {
            element.SetValue(ToolTipProperty, value);
        }

        public static FrameworkElement GetToolTip(DependencyObject element)
        {
            return (FrameworkElement) element.GetValue(ToolTipProperty);
        }
    }
}
