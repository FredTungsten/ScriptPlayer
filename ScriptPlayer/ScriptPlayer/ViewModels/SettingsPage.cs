using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptPlayer.ViewModels
{
    public static class SettingsPage
    {
        public static readonly DependencyProperty PageNameProperty = DependencyProperty.RegisterAttached(
            "PageName", typeof(string), typeof(SettingsPage), new PropertyMetadata(default(string), SettingsPagePropertyChanged));

        private static void SettingsPagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (!(d is FrameworkElement element))
                return;

            bool enabled = !string.IsNullOrEmpty(eventArgs.NewValue as string);

            element.MouseRightButtonDown -= ElementOnMouseRightButtonDown;
            if (enabled)
            {
                element.MouseRightButtonDown += ElementOnMouseRightButtonDown;
            }
        }

        private static void ElementOnMouseRightButtonDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if(!(sender is FrameworkElement element))
                return;

            Action<string> handler = FindHandler(element);
            if (handler == null)
                return;

            string page = GetPageName(element);
            if(string.IsNullOrEmpty(page))
                return;

            handler(page);
        }

        private static Action<string> FindHandler(DependencyObject element)
        {
            DependencyObject currentElement = element;

            while (currentElement != null)
            {
                var handler = GetHandler(currentElement);
                if (handler != null)
                    return handler;

                currentElement = VisualTreeHelper.GetParent(currentElement);
            }

            return null;
        }

        public static void SetPageName(DependencyObject element, string value)
        {
            element.SetValue(PageNameProperty, value);
        }

        public static string GetPageName(DependencyObject element)
        {
            return (string) element.GetValue(PageNameProperty);
        }

        public static readonly DependencyProperty HandlerProperty = DependencyProperty.RegisterAttached(
            "Handler", typeof(Action<string>), typeof(SettingsPage), new PropertyMetadata(default(Action<string>)));

        public static void SetHandler(DependencyObject element, Action<string> value)
        {
            element.SetValue(HandlerProperty, value);
        }

        public static Action<string> GetHandler(DependencyObject element)
        {
            return (Action<string>) element.GetValue(HandlerProperty);
        }
    }
}
