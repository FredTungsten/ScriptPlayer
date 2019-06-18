using System.Windows;

namespace ScriptPlayer.Shared
{
    public struct Resolution
    {
        public int Horizontal { get; set; }
        public int Vertical { get; set; }

        public Resolution(int horizonal, int vertical)
        {
            Horizontal = horizonal;
            Vertical = vertical;
        }

        public Size ToSize()
        {
            return new Size(Horizontal, Vertical);
        }

        public static bool TryParse(string value, out Resolution resolution)
        {
            resolution = new Resolution();

            if (string.IsNullOrWhiteSpace(value))
                return false;

            int pos = value.IndexOfAny(new char[] {'x', '*'});

            if (pos <= 0)
                return false;

            string horizontal = value.Substring(0, pos).Trim();
            string vertical = value.Substring(pos + 1).Trim();

            if (int.TryParse(horizontal, out int h) && int.TryParse(vertical, out int v))
            {
                resolution = new Resolution(h,v);
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return $"{Horizontal} x {Vertical}";
        }
    }
}