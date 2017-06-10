using System;
using System.ComponentModel;
using System.Windows;

namespace ScriptPlayer.WebPlayerConnector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly KiirooPlatformEmulator _emulator;

        public MainWindow()
        {
            InitializeComponent();
            _emulator = new KiirooPlatformEmulator();
            _emulator.OnKiirooPlatformEvent += EmulatorOnOnKiirooPlatformEvent;
            _emulator.StartServer();
        }

        private void EmulatorOnOnKiirooPlatformEvent(object sender, string s)
        {
            txtDebug.AppendText(DateTime.Now.ToString("T") + s + "\r\n");
            txtDebug.ScrollToEnd();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            _emulator.StopServer();
        }
    }
}
