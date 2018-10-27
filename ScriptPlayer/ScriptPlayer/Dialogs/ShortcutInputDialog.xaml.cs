using System.Linq;
using System.Windows;
using System.Windows.Input;
using ScriptPlayer.Shared;

namespace ScriptPlayer.Dialogs
{
    /// <summary>
    /// Interaction logic for ShortcutInputDialog.xaml
    /// </summary>
    public partial class ShortcutInputDialog : Window
    {
        public static readonly DependencyProperty ShortcutProperty = DependencyProperty.Register(
            "Shortcut", typeof(string), typeof(ShortcutInputDialog), new PropertyMetadata(default(string)));

        public string Shortcut
        {
            get => (string) GetValue(ShortcutProperty);
            set => SetValue(ShortcutProperty, value);
        }
        public ShortcutInputDialog()
        {
            InitializeComponent();
        }

        private void ShortcutInputDialog_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key[] modifiers = { Key.LeftAlt, Key.LeftCtrl, Key.LeftShift, Key.RightAlt, Key.RightCtrl, Key.RightShift };

            if (modifiers.Contains(e.Key))
                return;

            ModifierKeys activeMods = GlobalCommandManager.GetActiveModifierKeys();

            string shortcut = GlobalCommandManager.GetShortcut(e.Key, activeMods);
            Shortcut = shortcut;
          
            e.Handled = true;
            DialogResult = true;
        }
    }
}
