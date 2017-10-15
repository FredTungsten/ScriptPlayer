using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Navigation;

namespace ScriptPlayer.DockTest
{
    public class DockContainer : ContentControl
    {
        private bool _down;

        public DockContainer()
        {

        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _down = true;
            CaptureMouse();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!_down) return;

            DragDrop.DoDragDrop(this, null, DragDropEffects.Move);
        }
    }

    public class SplitPanel : Panel
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation", typeof(Orientation), typeof(SplitPanel), new FrameworkPropertyMetadata(default(Orientation), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange));

        public Orientation Orientation
        {
            get { return (Orientation) GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            double width = 0;
            double height = 0;

            Size actualSize;

            if(Orientation == Orientation.Horizontal)
                actualSize = new Size(availableSize.Width / InternalChildren.Count, availableSize.Height);
            else
                actualSize = new Size(availableSize.Width, availableSize.Height / InternalChildren.Count);

            foreach (UIElement child in InternalChildren)
            {
                child.Measure(actualSize);

                if (Orientation == Orientation.Horizontal)
                {
                    width += child.DesiredSize.Width;
                    height = Math.Max(height, child.DesiredSize.Height);
                }
                else
                {
                    width = Math.Max(height, child.DesiredSize.Width);
                    height += child.DesiredSize.Height;
                }
            }

            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size actualSize;

            if (Orientation == Orientation.Horizontal)
                actualSize = new Size(finalSize.Width / InternalChildren.Count, finalSize.Height);
            else
                actualSize = new Size(finalSize.Width, finalSize.Height / InternalChildren.Count);

            double x = 0;
            double y = 0;

            foreach (UIElement child in InternalChildren)
            {
                child.Arrange(new Rect(new Point(x,y), actualSize));

                if (Orientation == Orientation.Horizontal)
                {
                    x += actualSize.Width;
                }
                else
                {
                    y += actualSize.Height;
                }
            }

            return finalSize;
        }
    }

    [ContentProperty(nameof(Items))]
    public class DockContainerPanel : Control
    {
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            "Items", typeof(DockCollection), typeof(DockContainerPanel), new PropertyMetadata(default(DockCollection)));

        public static readonly DependencyProperty DockTypeProperty = DependencyProperty.Register(
            "DockType", typeof(DockType), typeof(DockContainerPanel), new PropertyMetadata(default(DockType)));

        public DockType DockType
        {
            get { return (DockType)GetValue(DockTypeProperty); }
            set { SetValue(DockTypeProperty, value); }
        }

        public DockCollection Items
        {
            get { return (DockCollection)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }
    }

    public class DockCollection : IList<DockContainer>, IList
    {
        private readonly List<DockContainer> _containers = new List<DockContainer>();
        public IEnumerator<DockContainer> GetEnumerator()
        {
            return _containers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(DockContainer item)
        {
            _containers.Add(item);
        }

        public int Add(object value)
        {
            return ((IList)_containers).Add((DockContainer) value);
        }

        public bool Contains(object value)
        {
            return _containers.Contains(value);
        }

        public void Clear()
        {
            _containers.Clear();
        }

        public int IndexOf(object value)
        {
            return _containers.IndexOf((DockContainer)value);
        }

        public void Insert(int index, object value)
        {
            _containers.Insert(index, (DockContainer)value);
        }

        public void Remove(object value)
        {
            _containers.Remove((DockContainer) value);
        }

        void IList.RemoveAt(int index)
        {
            _containers.RemoveAt(index);
        }

        object IList.this[int index]
        {
            get => _containers[index];
            set => _containers[index] = (DockContainer)value;
        }

        public bool Contains(DockContainer item)
        {
            return _containers.Contains(item);
        }

        public void CopyTo(DockContainer[] array, int arrayIndex)
        {
            _containers.CopyTo(array, arrayIndex);
        }

        public bool Remove(DockContainer item)
        {
            return _containers.Remove(item);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_containers).CopyTo(array, index);
        }

        public int Count
        {
            get => _containers.Count;
        }

        public object SyncRoot { get; }
        public bool IsSynchronized { get; }

        public bool IsReadOnly
        {
            get => false;
        }

        public bool IsFixedSize { get; }

        public int IndexOf(DockContainer item)
        {
            return _containers.IndexOf(item);
        }

        public void Insert(int index, DockContainer item)
        {
            _containers.Insert(index, item);
        }

        void IList<DockContainer>.RemoveAt(int index)
        {
            _containers.RemoveAt(index);
        }

        public DockContainer this[int index]
        {
            get => _containers[index];
            set => _containers[index] = value;
        }
    }

    public enum DockType
    {
        Single,
        Vertical,
        Horizontal,
        Tabs
    }
}
