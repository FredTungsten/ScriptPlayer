using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ScriptPlayer.Shared.Controls
{
    public class PageSelector : ContentControl
    {
        public static readonly DependencyProperty ElementsProperty = DependencyProperty.Register(
            "Elements", typeof(ObservableCollection<UIElement>), typeof(PageSelector), new PropertyMetadata(null, OnElementsChanged));

        private static void OnElementsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PageSelector)d).ElementsChanged(e.OldValue as ObservableCollection<UIElement>, e.NewValue as ObservableCollection<UIElement>);
        }

        private void ElementsChanged(ObservableCollection<UIElement> oldValue, ObservableCollection<UIElement> newValue)
        {
            if (oldValue != null)
                oldValue.CollectionChanged -= Elements_CollectionChanged;

            if (newValue != null)
                newValue.CollectionChanged += Elements_CollectionChanged;

            RefreshActiveContent();
        }

        private void Elements_CollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            RefreshActiveContent();
        }

        public ObservableCollection<UIElement> Elements
        {
            get => (ObservableCollection<UIElement>)GetValue(ElementsProperty);
            set => SetValue(ElementsProperty, value);
        }

        public static readonly DependencyProperty DesignModeContentIdentifierProperty = DependencyProperty.Register(
            "DesignModeContentIdentifier", typeof(string), typeof(PageSelector), new PropertyMetadata(default(string), DesignModeContentIdentifierPropertyChanged));

        private static void DesignModeContentIdentifierPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PageSelector)d).RefreshActiveContent();
        }

        public string DesignModeContentIdentifier
        {
            get => (string)GetValue(DesignModeContentIdentifierProperty);
            set => SetValue(DesignModeContentIdentifierProperty, value);
        }

        public static readonly DependencyProperty ActiveContentIdentifierProperty = DependencyProperty.Register(
            "ActiveContentIdentifier", typeof(string), typeof(PageSelector), new PropertyMetadata(default(string), ActiveContentIdentifierPropertyChanged));

        private static void ActiveContentIdentifierPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PageSelector)d).ElementsChanged(e.OldValue as ObservableCollection<UIElement>, e.NewValue as ObservableCollection<UIElement>);
        }

        public string ActiveContentIdentifier
        {
            get => (string)GetValue(ActiveContentIdentifierProperty);
            set => SetValue(ActiveContentIdentifierProperty, value);
        }

        public static readonly DependencyProperty ContentIdentifierProperty = DependencyProperty.RegisterAttached(
            "ContentIdentifier", typeof(string), typeof(PageSelector), new PropertyMetadata(default(string)));

        public static void SetContentIdentifier(DependencyObject element, string value)
        {
            element.SetValue(ContentIdentifierProperty, value);
        }

        public static string GetContentIdentifier(DependencyObject element)
        {
            return (string)element.GetValue(ContentIdentifierProperty);
        }

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.RegisterAttached(
            "Header", typeof(object), typeof(PageSelector), new PropertyMetadata(default(object)));

        public static void SetHeader(DependencyObject element, object value)
        {
            element.SetValue(HeaderProperty, value);
        }

        public static object GetHeader(DependencyObject element)
        {
            return (object) element.GetValue(HeaderProperty);
        }

        public PageSelector()
        {
            Elements = new ObservableCollection<UIElement>();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            RefreshActiveContent();
        }

        private void RefreshActiveContent()
        {
            if (Elements == null) return;

            string id = DesignerProperties.GetIsInDesignMode(this)
                ? DesignModeContentIdentifier
                : ActiveContentIdentifier;

            foreach (UIElement element in Elements)
            {
                if (GetContentIdentifier(element) != id) continue;

                Content = element;
                return;
            }

            Content = null;
        }
    }
}
