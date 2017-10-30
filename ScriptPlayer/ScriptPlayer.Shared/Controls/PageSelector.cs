using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.Shared.Controls
{
    public class PageSelector : ContentControl
    {
        public static readonly DependencyProperty ElementsProperty = DependencyProperty.Register(
            "Elements", typeof(List<UIElement>), typeof(PageSelector), new PropertyMetadata(new List<UIElement>()));

        public List<UIElement> Elements
        {
            get { return (List<UIElement>) GetValue(ElementsProperty); }
            set { SetValue(ElementsProperty, value); }
        }

        public static readonly DependencyProperty DesignModeContentIdentifierProperty = DependencyProperty.Register(
            "DesignModeContentIdentifier", typeof(string), typeof(PageSelector), new PropertyMetadata(default(string), DesignModeContentIdentifierPropertyChanged));

        private static void DesignModeContentIdentifierPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!DesignerProperties.GetIsInDesignMode(d)) return;
            ((PageSelector)d).ActiveContentIdentifierChanged((string)e.OldValue, (string)e.NewValue);
        }

        public string DesignModeContentIdentifier
        {
            get { return (string) GetValue(DesignModeContentIdentifierProperty); }
            set { SetValue(DesignModeContentIdentifierProperty, value); }
        }

        public static readonly DependencyProperty ActiveContentIdentifierProperty = DependencyProperty.Register(
            "ActiveContentIdentifier", typeof(string), typeof(PageSelector), new PropertyMetadata(default(string), ActiveContentIdentifierPropertyChanged));

        private static void ActiveContentIdentifierPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(d)) return;
            ((PageSelector) d).ActiveContentIdentifierChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void ActiveContentIdentifierChanged(string oldValue, string newValue)
        {
            if (Elements == null) return;
            foreach (UIElement element in Elements)
            {
                if (GetContentIdentifier(element) == newValue)
                {
                    Content = element;
                    break;
                }
            }
        }

        public string ActiveContentIdentifier
        {
            get { return (string) GetValue(ActiveContentIdentifierProperty); }
            set { SetValue(ActiveContentIdentifierProperty, value); }
        }

        public static readonly DependencyProperty ContentIdentifierProperty = DependencyProperty.RegisterAttached(
            "ContentIdentifier", typeof(string), typeof(PageSelector), new PropertyMetadata(default(string)));

        public static void SetContentIdentifier(DependencyObject element, string value)
        {
            element.SetValue(ContentIdentifierProperty, value);
        }

        public static string GetContentIdentifier(DependencyObject element)
        {
            return (string) element.GetValue(ContentIdentifierProperty);
        }
    }
}
