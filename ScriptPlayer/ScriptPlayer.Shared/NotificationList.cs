using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScriptPlayer.Shared.Properties;

namespace ScriptPlayer.Shared
{
    public class NotificationList : Control
    {
        private readonly UIElementCollection _children;

        protected override int VisualChildrenCount => _children.Count;

        public NotificationList()
        {
            _children = new UIElementCollection(this, this);
        }

        protected override Visual GetVisualChild(int index)
        {
            return _children[index];
        }

        private void AddChild(UIElement element)
        {
            if (element == null)
                return;

            if (!_children.Contains(element))
            {
                _children.Add(element);

                OnVisualChildrenChanged(element, null);

                InvalidateMeasure();
                InvalidateArrange();
            }
        }

        private void RemoveChild(UIElement element)
        {
            if (element == null)
                return;

            if (_children.Contains(element))
            {
                _children.Remove(element);

                OnVisualChildrenChanged(null, element);

                InvalidateMeasure();
                InvalidateArrange();
            }
        }

        private readonly List<Notification> _notifications = new List<Notification>();

        public void AddNotification(object content, TimeSpan duration, string group = null)
        {
            Notification existing = null;

            if(!string.IsNullOrWhiteSpace(group))
                existing = _notifications.FirstOrDefault(n => String.Equals(n.Group, group, StringComparison.InvariantCultureIgnoreCase));


            if (existing != null)
            {
                existing.Content = content;

                VanishingContainer container = GetContainer(existing);
                container.Vanish(duration);
            }
            else
            {
                var newNotification = new Notification
                {
                    Group = group,
                    Content = content
                };

                _notifications.Add(newNotification);

                VanishingContainer control = new VanishingContainer { Content = newNotification };
                control.Gone += ControlOnGone;
                AddChild(control);
                control.Vanish(duration);
            }
        }

        private VanishingContainer GetContainer(Notification notification)
        {
            foreach(UIElement element in _children)
                if(element is VanishingContainer container)
                    if (container.Content == notification)
                        return container;
            return null;
        }

        private void ControlOnGone(object sender, EventArgs eventArgs)
        {
            if(sender is VanishingContainer container)
                if (container.Content is Notification notification)
                    _notifications.Remove(notification);
            RemoveChild(sender as UIElement);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            double maxWidth = 0.0;
            double totalHeight = 0.0;

            foreach (UIElement element in _children)
            {
                element.Measure(constraint);
                maxWidth = Math.Max(maxWidth, element.DesiredSize.Height);
                totalHeight += element.DesiredSize.Width;
            }

            return new Size(maxWidth, totalHeight);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            double currentY = 0.0;

            foreach (UIElement element in _children)
            {
                element.Arrange(new Rect(new Point(0, currentY), element.DesiredSize));
                currentY += element.DesiredSize.Height;
            }

            return arrangeBounds;
        }
    }

    public class Notification : INotifyPropertyChanged
    {
        private object _content;

        public object Content
        {
            get => _content;
            set
            {
                if (value == _content) return;
                _content = value;
                OnPropertyChanged();
            }
        }

        public string Group { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
