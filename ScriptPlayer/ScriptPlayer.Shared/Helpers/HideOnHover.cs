using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.Shared
{
    public static class HideOnHover
    {
        public static readonly DependencyProperty OverridesHideOnHoverProperty = DependencyProperty.RegisterAttached(
            "OverridesHideOnHover", typeof(bool), typeof(HideOnHover), new PropertyMetadata(default(bool), OnIgnoresHideOnHoverPropertyChanged));

        private static void OnIgnoresHideOnHoverPropertyChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(element))
                return;

            bool value = (bool)e.NewValue;

            ContextMenu menu = element as ContextMenu;
            if (menu == null) return;

            if (value)
            {
                menu.Opened += OnContextMenuOpening;
                menu.Closed += OnContextMenuClosing;
            }
            else
            {
                menu.Opened -= OnContextMenuOpening;
                menu.Closed -= OnContextMenuClosing;
            }
        }

        private static void OnContextMenuClosing(object sender, RoutedEventArgs routedEventArgs)
        {
            ContextMenuClosing(sender as ContextMenu);
        }

        private static void OnContextMenuOpening(object sender, RoutedEventArgs routedEventArgs)
        {
            ContextMenuOpening(sender as ContextMenu);
        }

        public static void SetOverridesHideOnHover(DependencyObject element, bool value)
        {
            element.SetValue(OverridesHideOnHoverProperty, value);
        }

        public static bool GetOverridesHideOnHover(DependencyObject element)
        {
            return (bool) element.GetValue(OverridesHideOnHoverProperty);
        }

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
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            ((FrameworkElement)sender).Opacity = 0.0;
        }

        public static void ContextMenuOpening(ContextMenu menu)
        {
            FrameworkElement target = FindPlacementTarget(menu);
            if(target != null)
                target.Opacity = 1.0;
        }

        private static FrameworkElement FindPlacementTarget(ContextMenu menu)
        {
            FrameworkElement element = menu.PlacementTarget as FrameworkElement;

            while (element != null)
            {
                if (GetIsActive(element))
                {
                    return element;
                }

                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }

            return null;
        }

        public static void ContextMenuClosing(ContextMenu menu)
        {
            FrameworkElement target = FindPlacementTarget(menu);
            if (target != null)
                if(!target.IsMouseOver)
                    target.Opacity = 0.0;
        }

        private static void OnMouseEnter(object sender, MouseEventArgs mouseEventArgs)
        {
            ((FrameworkElement) sender).Opacity = 1.0;
        }
    }
}
