using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace ScriptPlayer.Shared
{
    public static class HideOnHover
    {
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.RegisterAttached(
            "IsActive", typeof(bool), typeof(HideOnHover), new PropertyMetadata(default(bool), OnIsActivePropertyChanged));

        private static void OnIsActivePropertyChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(element))
                return;

            bool value = (bool) e.NewValue;

            FrameworkElement frameworkElement = element as FrameworkElement;
            if (frameworkElement == null) return;

            if (value)
            {
                frameworkElement.MouseEnter += OnMouseEnter;
                frameworkElement.MouseLeave += OnMouseLeave;
                if (!frameworkElement.IsMouseOver)
                    frameworkElement.Opacity = 0.0;
            }
            else
            {
                frameworkElement.MouseEnter -= OnMouseEnter;
                frameworkElement.MouseLeave -= OnMouseLeave;
                frameworkElement.Opacity = 1.0;
            }
        }

        public static void SetIsActive(DependencyObject element, bool value)
        {
            element.SetValue(IsActiveProperty, value);
        }

        public static bool GetIsActive(DependencyObject element)
        {
            return (bool) element.GetValue(IsActiveProperty);
        }

        private static void OnMouseLeave(object sender, MouseEventArgs mouseEventArgs)
        {
            ((FrameworkElement)sender).Opacity = 0.0;
        }

        private static void OnMouseEnter(object sender, MouseEventArgs mouseEventArgs)
        {
            ((FrameworkElement) sender).Opacity = 1.0;
        }
    }
}
