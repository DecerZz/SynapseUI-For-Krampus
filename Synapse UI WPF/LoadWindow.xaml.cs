#define USE_UPDATE_CHECKS
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json;
using Synapse_UI_WPF.Interfaces;
using Synapse_UI_WPF.Static;

namespace Synapse_UI_WPF
{
    public partial class LoadWindow
    {
        public const string UiVersion = "14";
        public const uint TVersion = 2;

        public static ThemeInterface.TInitStrings InitStrings;
        public static BackgroundWorker LoadWorker = new BackgroundWorker();

        [DllImport("user32.dll")]
        private static extern Boolean ShowWindow(IntPtr hWnd, Int32 nCmdShow);
        public LoadWindow()
        {
            Process currentProcess = Process.GetCurrentProcess();
            var runningProcess = (from process in Process.GetProcesses() where process.Id != currentProcess.Id && process.ProcessName.Equals(currentProcess.ProcessName, StringComparison.Ordinal) select process).FirstOrDefault();
            if (runningProcess != null)
            {
                ShowWindow(runningProcess.MainWindowHandle, 1);
                Environment.Exit(0);
            }
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var Result = MessageBox.Show(
                    $"Synapse has encountered an exception. Please report the following text below to decerzz on Discord (make sure to give the text, not an image):\n\n{((Exception)args.ExceptionObject)}\n\nIf you would like this text copied to your clipboard, press \'Yes\'.",
                    "Synapse X",
                    MessageBoxButton.YesNo, MessageBoxImage.Error, MessageBoxResult.No);

                if (Result != MessageBoxResult.Yes) return;

                var STAThread = new Thread(
                    delegate ()
                    {
                        Clipboard.SetText(((Exception)args.ExceptionObject).ToString());
                    });

                STAThread.SetApartmentState(ApartmentState.STA);
                STAThread.Start();
                STAThread.Join();

                Thread.Sleep(1000);
            };

            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            LoadWorker.DoWork += LoadWorker_DoWork;

            InitializeComponent();
        }

        public void SetStatusText(string Status, int Percentage)
        {
            Dispatcher.Invoke(() =>
            {
                StatusBox.Content = Status;
                ProgressBox.Value = Percentage;
            });
        }

        private ThemeInterface.TBase MigrateT1ToT2(ThemeInterface.TBase Old)
        {
            Old.Version = 2;

            Old.Main.ExecuteFileButton = new ThemeInterface.TButton
            {
                BackColor = new ThemeInterface.TColor(255, 60, 60, 60),
                TextColor = new ThemeInterface.TColor(255, 255, 255, 255),
                Font = new ThemeInterface.TFont("Segoe UI", 14f),
                Image = new ThemeInterface.TImage(),
                Text = "Execute File"
            };

            return Old;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            if (!File.Exists("bin\\theme-wpf.json"))
            {
                File.WriteAllText("bin\\theme-wpf.json",
                    JsonConvert.SerializeObject(ThemeInterface.Default(), Formatting.Indented));
            }

            try
            {
                Globals.Theme =
                    JsonConvert.DeserializeObject<ThemeInterface.TBase>(File.ReadAllText("bin\\theme-wpf.json"));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to parse theme.json file.\n\nException details:\n" + ex.Message,
                    "Synapse X Theme Parser", MessageBoxButton.OK, MessageBoxImage.Error);
                Globals.Theme = ThemeInterface.Default();
            }

            if (Globals.Theme.Version != TVersion)
            {
                if (Globals.Theme.Version == 1)
                {
                    Globals.Theme = MigrateT1ToT2(Globals.Theme);
                }

                File.WriteAllText("bin\\theme-wpf.json", JsonConvert.SerializeObject(Globals.Theme, Formatting.Indented));
            }

            if (!DataInterface.Exists("options"))
            {
                Globals.Options = new Data.Options
                {
                    AutoLaunch = false,
                    AutoAttach = false,
                    MultiRoblox = false,
                    UnlockFPS = false,
                    IngameChat = false,
                    BetaRelease = false,
                    InternalUI = false,
                    WindowScale = 1d
                };
                DataInterface.Save("options", new Data.OptionsHolder
                {
                    Version = Data.OptionsVersion,
                    Data = JsonConvert.SerializeObject(Globals.Options)
                });
            }
            else
            {
                try
                {
                    var Read = DataInterface.Read<Data.OptionsHolder>("options");
                    if (Read.Version != Data.OptionsVersion)
                    {
                        Globals.Options = new Data.Options
                        {
                            AutoLaunch = false,
                            AutoAttach = false,
                            MultiRoblox = false,
                            UnlockFPS = false,
                            IngameChat = false,
                            InternalUI = false,
                            BetaRelease = false,
                            WindowScale = 1d
                        };
                        DataInterface.Save("options", new Data.OptionsHolder
                        {
                            Version = Data.OptionsVersion,
                            Data = JsonConvert.SerializeObject(Globals.Options)
                        });
                    }
                    else
                    {
                        Globals.Options = JsonConvert.DeserializeObject<Data.Options>(Read.Data);
                    }
                }
                catch (Exception)
                {
                    Globals.Options = new Data.Options
                    {
                        AutoLaunch = false,
                        AutoAttach = false,
                        MultiRoblox = false,
                        UnlockFPS = false,
                        IngameChat = false,
                        InternalUI = false,
                        BetaRelease = false,
                        WindowScale = 1d
                    };
                    DataInterface.Save("options", new Data.OptionsHolder
                    {
                        Version = Data.OptionsVersion,
                        Data = JsonConvert.SerializeObject(Globals.Options)
                    });
                }
            }

            var TLoad = Globals.Theme.Load;
            ThemeInterface.ApplyWindow(this, TLoad.Base);
            ThemeInterface.ApplyLogo(IconBox, TLoad.Logo);
            ThemeInterface.ApplyLabel(TitleBox, TLoad.TitleBox);
            ThemeInterface.ApplyLabel(StatusBox, TLoad.StatusBox);
            ThemeInterface.ApplySeperator(TopBox, TLoad.TopBox);
            InitStrings = TLoad.BaseStrings;

            Title = WebInterface.RandomString(WebInterface.Rnd.Next(10, 32));
            Globals.Context = SynchronizationContext.Current;

            LoadWorker.RunWorkerAsync();
        }
        
        private void LoadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            /* Currently not needed due to Attach not working......
            SetStatusText("Getting Krampus file...", 40);
            try
            {
                string[] files = System.IO.Directory.GetFiles("../", "*.exe");
                Console.WriteLine(files[0]);
                if (files[0].Length != 17) throw new Exception();
                Globals.KrampusPath = files[0];
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    Topmost = false;
                    MessageBox.Show("Unable to find your Krampus exe. Please make sure that the exe is in the same folder with UI and you haven't renamed it!", "Synapse X", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                });
            }
            */

            SetStatusText("Getting Krampus login token...", 80);
            try {
                var text = File.ReadAllLines("../launch.cfg");
                string[] dd = text[0].Split("|".ToCharArray());
                Globals.LoginKey = dd[0];
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    Topmost = false;
                    MessageBox.Show("Unable to find your Krampus login token. Please make sure that the launch.cfg is in the same folder with UI!", "Synapse X", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                });
            }

            Dispatcher.Invoke(() =>
            {
                var Main = new MainWindow();
                Main.Show();
                Close();
            });
        }
    }
}
