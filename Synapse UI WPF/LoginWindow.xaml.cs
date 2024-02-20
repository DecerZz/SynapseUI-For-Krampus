using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Synapse_UI_WPF.Interfaces;
using Synapse_UI_WPF.Static;

namespace Synapse_UI_WPF
{
    public partial class LoginWindow
    {
        public BackgroundWorker LoginWorker = new BackgroundWorker();
        private bool FakeClose;

        public LoginWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            LoginWorker.DoWork += LoginWorker_DoWork;

            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = WebInterface.RandomString(WebInterface.Rnd.Next(10, 32));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
            Environment.Exit(0);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (FakeClose) return;

            Environment.Exit(0);
        }

        private void MiniButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FakeClose = true;

            var Register = new RegisterWindow();
            Register.Show();
            Close();
        }
        private void TextBlock_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            FakeClose = true;

            var Reset = new ResetWindow();
            Reset.Show();
            Close();
        }

        
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (LoggingIn) return;
            LoginButton.Content = "Logging in...";
            LoggingIn = true;
            LoginWorker.RunWorkerAsync();
        }

        
        private bool LoggingIn;
        
        private void LoginWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    "You have successfully logged into your Synapse account!\n\nYou can now restart Synapse X to use the software.",
                    "Synapse X", MessageBoxButton.OK, MessageBoxImage.Information);
            });
            Environment.Exit(0);
            return;

        }
    }
}
