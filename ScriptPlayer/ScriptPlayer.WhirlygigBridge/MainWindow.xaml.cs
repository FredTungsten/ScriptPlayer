using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows;

namespace ScriptPlayer.WhirlygigBridge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread _listenThread;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _listenThread = new Thread(Communicate);
            _listenThread.Start();
        }

        private void Communicate()
        {
            
        }

        private void AddLine(string line)
        {
            if (txtLog.CheckAccess())
            {
                txtLog.AppendText(line + Environment.NewLine);
                txtLog.ScrollToEnd();
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    AddLine(line);
                }));
            }
        }
    }
}
