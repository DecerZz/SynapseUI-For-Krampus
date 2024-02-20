#define USE_UPDATE_CHECKS

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Net;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json;
using Synapse_UI_WPF.Controls;
using Synapse_UI_WPF.Interfaces;
using Synapse_UI_WPF.Static;
using WebSocketSharp;
using static System.Net.Mime.MediaTypeNames;
using static Synapse_UI_WPF.Interfaces.ThemeInterface;
using Process = System.Diagnostics.Process;

namespace Synapse_UI_WPF
{
    public partial class MainWindow
    {

        public delegate void InteractMessageEventHandler(object sender, string Input);
        public event InteractMessageEventHandler InteractMessageRecieved;


        private int RobloxIdOverride;

        private int RobloxIdTemp;

        private static int RbxId;
        public bool ConnectedToKrampus = false;

        public bool OptionsOpen;

        public bool ScriptHubOpen;
        private bool ScriptHubInit;
        public bool IsInlineUpdating;

        private static ThemeInterface.TAttachStrings AttachStrings;

        private readonly string BaseDirectory;
        private readonly string ScriptsDirectory;

        public static BackgroundWorker Worker = new BackgroundWorker();
        public static BackgroundWorker HubWorker = new BackgroundWorker();

        public MainWindow()
        {
            Cef.EnableHighDPISupport();
            var settings = new CefSettings();
            settings.SetOffScreenRenderingBestPerformanceArgs();
            Cef.Initialize(settings);

            InitializeComponent();

            HubWorker.DoWork += HubWorker_DoWork;
            StreamReader InteractReader = null;
            StreamReader LaunchReader;

            if (DataInterface.Exists("savedpid"))
            {
                var Saved = DataInterface.Read<Data.SavedPid>("savedpid");

                try
                {
                    if (Process.GetProcessById(Saved.Pid).StartTime == Saved.StartTime)
                    {
                        RbxId = Saved.Pid;
                    }

                    DataInterface.Delete("savedpid");
                }
                catch (Exception)
                {
                    DataInterface.Delete("savedpid");
                }
            }

            WebSocketInterface.Start(24892, this);

            var TMain = Globals.Theme.Main;
            ThemeInterface.ApplyWindow(this, TMain.Base);
            ThemeInterface.ApplyLogo(IconBox, TMain.Logo);
            ThemeInterface.ApplySeperator(TopBox, TMain.TopBox);
            ThemeInterface.ApplyFormatLabel(TitleBox, TMain.TitleBox, Globals.Version);
            ThemeInterface.ApplyListBox(ScriptBox, TMain.ScriptBox);
            ThemeInterface.ApplyButton(MiniButton, TMain.MinimizeButton);
            ThemeInterface.ApplyButton(CloseButton, TMain.ExitButton);
            ThemeInterface.ApplyButton(ExecuteButton, TMain.ExecuteButton);
            ThemeInterface.ApplyButton(ClearButton, TMain.ClearButton);
            ThemeInterface.ApplyButton(OpenFileButton, TMain.OpenFileButton);
            ThemeInterface.ApplyButton(ExecuteFileButton, TMain.ExecuteFileButton);
            ThemeInterface.ApplyButton(SaveFileButton, TMain.SaveFileButton);
            ThemeInterface.ApplyButton(OptionsButton, TMain.OptionsButton);
            ThemeInterface.ApplyButton(AttachButton, TMain.AttachButton);
            ThemeInterface.ApplyButton(ScriptHubButton, TMain.ScriptHubButton);

            ScaleTransform.ScaleX = Globals.Options.WindowScale;
            ScaleTransform.ScaleY = Globals.Options.WindowScale;

            AttachStrings = TMain.BaseStrings;

            BaseDirectory = Directory.GetParent(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).FullName;


            ScriptsDirectory = Path.Combine(BaseDirectory, "scripts");
            if (!Directory.Exists((ScriptsDirectory))) Directory.CreateDirectory(ScriptsDirectory);

            foreach (var FilePath in Directory.GetFiles(ScriptsDirectory))
            {
                ScriptBox.Items.Add(Path.GetFileName(FilePath));
            }

            foreach (var FilePath in Directory.GetFiles("bin/tabs/"))
            {
                Console.WriteLine(Path.GetFileName(FilePath));
                CreateTab(Path.GetFileName(FilePath).Split(".".ToCharArray())[0]);
            }

        }


        public void SetTitle(string Str, int Delay = 0)
        {
            Dispatcher.Invoke(() =>
            {
                TitleBox.Content =
                    ThemeInterface.ConvertFormatString(Globals.Theme.Main.TitleBox, Str);
            });

            if (Delay != 0)
            {
                new Thread(() =>
                {
                    Thread.Sleep(Delay);
                    Dispatcher.Invoke(() =>
                    {
                        TitleBox.Content =
                            ThemeInterface.ConvertFormatString(Globals.Theme.Main.TitleBox, Globals.Version);
                    });
                }).Start();
            }
        }


        public bool Ready()
        {
            //var ProcList = Process.GetProcessesByName("RobloxPlayerBeta");
            //return ProcList.Length != 0 && ProcList[0].Id == RbxId;
            return true;
        }

        public void Execute(string data)
        {
            if (data.Length == 0) return;
            Dispatcher.Invoke(() =>
            {
                try
                {
                    var text = File.ReadAllLines("../launch.cfg");
                    string[] dd = text[0].Split("|".ToCharArray());
                    using (var ws = new WebSocket("wss://loader.live/?login_token=\"" + dd[0] + "\""))
                    {
                        ws.Connect();
                        ws.Send("<SCRIPT>" + data);
                        ws.Close();
                    }
                } catch {
                    MessageBox.Show("Unable to execute or find token! Make sure UI is installed correctly!", "Synapse X", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = WebInterface.RandomString(WebInterface.Rnd.Next(10, 32));
        }

        private void Browser_MonacoReady()
        {
            Browser.SetTheme(Globals.Theme.Main.Editor.Light ? MonacoTheme.Light : MonacoTheme.Dark);
            if (!File.Exists($"bin/tabs/{Globals.CurrentTab}.txt")) File.WriteAllText($"bin/tabs/{Globals.CurrentTab}.txt", "");
            Browser.SetText(File.ReadAllText($"bin/tabs/{Globals.CurrentTab}.txt"));
        }

        public void Attach()
        {
            if (Worker.IsBusy || IsInlineUpdating) return;

            //Worker.RunWorkerAsync();
        }

        public void SetEditor(string Text)
        {
            if (!File.Exists($"bin/tabs/{CurrentTab()}.txt")) File.WriteAllText($"bin/tabs/{CurrentTab()}.txt", "");
            File.WriteAllText($"bin/tabs/{CurrentTab()}.txt", Text);
            Browser.SetText(Text);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {

            if (!File.Exists($"bin/tabs/{CurrentTab()}.txt")) File.WriteAllText($"bin/tabs/{CurrentTab()}.txt", "");
            File.WriteAllText($"bin/tabs/{CurrentTab()}.txt", Browser.GetText());
            Environment.Exit(0);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Environment.Exit(0);
        }

        private void OptionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (OptionsOpen) return;

            var Options = new OptionsWindow(this);
            Options.Show();
        }

        private void MiniButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SetEditor("");
        }

        private void IconBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(
                "Synapse X was developed by 3dsboy08, brack4712, Louka, DefCon42, and Eternal.\r\n\r\nAdditional credits:\r\n    - Rain: Emotional support and testing",
                "Synapse X Credits", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var OpenDialog = new OpenFileDialog
            {
                Filter = "Script Files (*.lua, *.txt)|*.lua;*.txt", Title = "Synapse X - Open File", FileName = ""
            };

            if (OpenDialog.ShowDialog() != true) return;

            try
            {
                Console.WriteLine(OpenDialog.FileName);
                SetEditor(File.ReadAllText(OpenDialog.FileName));
            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }

        private void ExecuteFileButton_Click(object sender, RoutedEventArgs e)
        {
            var OpenDialog = new OpenFileDialog
            {
                Filter = "Script Files (*.lua, *.txt)|*.lua;*.txt", Title = "Synapse X - Execute File", FileName = ""
            };

            if (OpenDialog.ShowDialog() != true) return;

            try
            {
                Execute(File.ReadAllText(OpenDialog.FileName));
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to read file. Check if it is accessible.", "Synapse X", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            var SaveDialog = new SaveFileDialog { Filter = "Script Files (*.lua, *.txt)|*.lua;*.txt", FileName = "" };

            SaveDialog.FileOk += (o, args) =>
            {
                File.WriteAllText(SaveDialog.FileName, Browser.GetText());
            };

            SaveDialog.ShowDialog();
        }

        private void AttachButton_Click(object sender, RoutedEventArgs e)
        {
            bool IsAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            if (IsAdmin)
            {
                SetTitle("Attach doesn't work, Manually launch krampus!", 3000);
                //ProcessCreator.CreateProcess(0);
                //Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Globals.KrampusPath));
            }
            else
            {
                SetTitle("Attach doesn't work, Manually launch krampus!", 3000);
                //SetTitle("Not enough Permissions, Please run as Admin!", 3000);
            }
        }

        private void ScriptHubButton_Click(object sender, RoutedEventArgs e)
        {
            if (ScriptHubOpen) return;
            if (ScriptHubInit) return;

            ScriptHubOpen = true;
            ScriptHubInit = true;

            ScriptHubButton.Content = Globals.Theme.Main.ScriptHubButton.TextYield;
            HubWorker.RunWorkerAsync();
        }

        private void HubWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            Dispatcher.Invoke(() =>
            {
                ScriptHubInit = false;
                ScriptHubButton.Content = Globals.Theme.Main.ScriptHubButton.TextNormal;

                var ScriptHub = new ScriptHubWindow(this);
                ScriptHub.Show();
            });
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists($"bin/tabs/{CurrentTab()}.txt")) File.WriteAllText($"bin/tabs/{CurrentTab()}.txt", "");
            File.WriteAllText($"bin/tabs/{CurrentTab()}.txt", Browser.GetText());
            Task.Run(() => Execute(Browser.GetText()));
        }

        private void ExecuteItem_Click(object sender, RoutedEventArgs e)
        {
            if (ScriptBox.SelectedIndex == -1) return;

            try
            {
                var Element = ScriptBox.Items[ScriptBox.SelectedIndex].ToString();

                Execute(File.ReadAllText(Path.Combine(ScriptsDirectory, Element)));
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to read file. Check if it is accessible.", "Synapse X", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void LoadItem_Click(object sender, RoutedEventArgs e)
        {
            if (ScriptBox.SelectedIndex == -1) return;

            try
            {
                var Element = ScriptBox.Items[ScriptBox.SelectedIndex].ToString();

                SetEditor(File.ReadAllText(Path.Combine(ScriptsDirectory, Element)));
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to read file. Check if it is accessible.", "Synapse X", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void RefreshItem_Click(object sender, RoutedEventArgs e)
        {
            ScriptBox.Items.Clear();

            foreach (var FilePath in Directory.GetFiles(ScriptsDirectory))
            {
                ScriptBox.Items.Add(Path.GetFileName(FilePath));
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (TabSystem.SelectedIndex == -1) return;

            try
            {
                //var Tab = TabSystem.Items[TabSystem.SelectedIndex].ToString();
                Console.WriteLine(CurrentTab()); 
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to close tab.", "Synapse X", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClearTab_Click(object sender, RoutedEventArgs e)
        {
            if (TabSystem.SelectedIndex == -1) return;

            try
            {
                Console.WriteLine(CurrentTab());
            }
            catch (Exception)
            {
                MessageBox.Show("Failed to clear tab.", "Synapse X", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private object CurrentTab()
        {
            var Tab = TabSystem.SelectedItem as TabItem;
            return Tab.Header;
        }
        private void CreateTab(string Name)
        {
                TabItem NewTab = new TabItem();
                NewTab.Header = Name;
                var Converter = new BrushConverter();
                NewTab.Background = (System.Windows.Media.Brush)Converter.ConvertFromString("#696969");
                NewTab.Foreground = (System.Windows.Media.Brush)Converter.ConvertFromString("#FFFFFF");
                NewTab.BorderBrush = (System.Windows.Media.Brush)Converter.ConvertFromString("#545454");
                TabSystem.Items.Insert(TabSystem.Items.Count - 1, NewTab);
                Dispatcher.BeginInvoke(new Action(() => TabSystem.SelectedIndex = TabSystem.Items.Count-2));
                
        }

        private void TabSystem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Globals.CurrentTab = CurrentTab();
            if (e.AddedItems.Contains(AddTabButton))
            {
                CreateTab("Tab " + (TabSystem.Items.Count));
            } else
            {
                if (!File.Exists($"bin/tabs/{CurrentTab()}.txt")) File.WriteAllText($"bin/tabs/{CurrentTab()}.txt", "");
                SetEditor(File.ReadAllText($"bin/tabs/{CurrentTab()}.txt"));
            }
            e.Handled = true;
        }
    }
}
