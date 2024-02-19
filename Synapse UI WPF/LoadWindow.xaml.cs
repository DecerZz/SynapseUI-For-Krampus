#define USE_UPDATE_CHECKS
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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

        public LoadWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var Result = MessageBox.Show(
                    $"Synapse has encountered an exception. Please report the following text below to 3dsboy08 on Discord (make sure to give the text, not an image):\n\n{((Exception)args.ExceptionObject)}\n\nIf you would like this text copied to your clipboard, press \'Yes\'.",
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
            var ProcList = Process.GetProcessesByName(
                Path.GetFileName(AppDomain.CurrentDomain.FriendlyName));
            var Current = Process.GetCurrentProcess();
            foreach (var Proc in ProcList)
            {
                if (Proc.Id == Current.Id) continue;
                try
                {
                    Proc.Kill();
                }
                catch (Exception)
                {
                }
            }

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

        [Obfuscation(Feature = "virtualization", Exclude = false)]
        private void ChangeWhitelist(string Token)
        {
            var Result = WebInterface.Change(Token);
            switch (Result)
            {
                case WebInterface.ChangeResult.OK:
                {
                    return;
                }
                case WebInterface.ChangeResult.INVALID_TOKEN:
                {
                    DataInterface.Delete("token");
                    Dispatcher.Invoke(() =>
                    {
                        var LoginUI = new LoginWindow();
                        LoginUI.Show();
                        Close();
                    });
                    return;
                }
                case WebInterface.ChangeResult.EXPIRED_TOKEN:
                {
                    DataInterface.Delete("token");
                    Dispatcher.Invoke(() =>
                    {
                        Topmost = false;
                        MessageBox.Show(
                            "Your login has expired. Please relogin.",
                            "Synapse X", MessageBoxButton.OK, MessageBoxImage.Error);

                        var LoginUI = new LoginWindow();
                        LoginUI.Show();
                        Close();
                    });
                    return;
                }
                case WebInterface.ChangeResult.ALREADY_EXISTING_HWID:
                {
                    DataInterface.Delete("token");
                    Dispatcher.Invoke(() =>
                    {
                        Topmost = false;
                        MessageBox.Show(
                            "You seem to already have a Synapse whitelist on this PC. Please restart Synapse and login into that account.",
                            "Synapse X", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    Environment.Exit(0);
                    return;
                }
                case WebInterface.ChangeResult.NOT_ENOUGH_TIME:
                {
                    Dispatcher.Invoke(() =>
                    {
                        Topmost = false;
                        MessageBox.Show(
                            "You have changed your whitelist too recently. Please wait 24 hours from your last whitelist change and try again.",
                            "Synapse X", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    Environment.Exit(0);
                    return;
                }
                case WebInterface.ChangeResult.INVALID_REQUEST:
                case WebInterface.ChangeResult.UNKNOWN:
                {
                    Dispatcher.Invoke(() =>
                    {
                        Topmost = false;
                        MessageBox.Show(
                            "Failed to change whitelist to Synapse account. Please contact 3dsboy08 on Discord.",
                            "Synapse X", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    Environment.Exit(0);
                    return;
                }
            }
        }

        [Obfuscation(Feature = "virtualization", Exclude = false)]
        private void Login(string Username, string Password)
        {
            var Result = WebInterface.Login(Username, Password);

            switch (WebInterface.LoginResult.OK)
            {
                case WebInterface.LoginResult.OK:
                {
                    DataInterface.Delete("login");
                    DataInterface.Save("token", "retardedtokenxdxdd");
                    return;
                }
                case WebInterface.LoginResult.INVALID_USER_PASS:
                {
                    DataInterface.Delete("login");

                    Dispatcher.Invoke(() =>
                    {
                        Topmost = false;
                        MessageBox.Show(
                            "Your login password has changed. Please relogin.",
                            "Synapse X", MessageBoxButton.OK, MessageBoxImage.Error);

                       // var LoginUI = new LoginWindow();
                       // LoginUI.Show();
                        Close();
                    });

                    return;
                }
                case WebInterface.LoginResult.NOT_MIGRATED:
                {
                    DataInterface.Delete("login");

                    Dispatcher.Invoke(() =>
                    {
                        Topmost = false;
                        MessageBox.Show(
                            "Your account does not seem to be migrated, but was already logged in. Please contact 3dsboy08 on Discord.",
                            "Synapse X", MessageBoxButton.OK, MessageBoxImage.Error);

                        //var LoginUI = new LoginWindow();
                        //LoginUI.Show();
                        Close();
                    });

                    break;
                }

                case WebInterface.LoginResult.INVALID_REQUEST:
                case WebInterface.LoginResult.UNKNOWN:
                {
                    Dispatcher.Invoke(() =>
                    {
                        Topmost = false;
                        MessageBox.Show(
                            "Failed to login to Synapse account. Please contact 3dsboy08 on Discord.",
                            "Synapse X", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    Environment.Exit(0);
                    return;
                }
            }
        }

        [Obfuscation(Feature = "virtualization", Exclude = false)]
        private void LoadWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            WebInterface.InitHwid();
            WebInterface.VerifyWebsite(this);

            SetStatusText(InitStrings.CheckingWhitelist, 25);

            if (DataInterface.Exists("login"))
            {
                Data.Login Login;
                try
                {
                    Login = DataInterface.Read<Data.Login>("login");
                }
                catch (Exception)
                {
                    Dispatcher.Invoke(() =>
                    {
                        //var LoginUI = new LoginWindow();
                        //LoginUI.Show();
                        //Close();
                    });
                    return;
                }

                SetStatusText(InitStrings.ChangingWhitelist, 25);
                //this.Login(Login.Username, Login.Password);
            }

            var WlStatus = WebInterface.Check();
            if (WlStatus != WebInterface.WhitelistCheckResult.OK)
            {
                switch (WlStatus)
                {
                    case WebInterface.WhitelistCheckResult.NO_RESULTS:
                    {
                        if (DataInterface.Exists("token"))
                        {
                            string Token;
                            try
                            {
                                Token = DataInterface.Read<string>("token");
                            }
                            catch (Exception)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    //var LoginUI = new LoginWindow();
                                    //LoginUI.Show();
                                    //Close();
                                });
                                return;
                            }

                            SetStatusText(InitStrings.ChangingWhitelist, 25);
                            ChangeWhitelist(Token);
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                var LoginUI = new LoginWindow();
                                LoginUI.Show();
                                Close();
                            });
                        }
                        break;
                    }
                    case WebInterface.WhitelistCheckResult.UNAUTHORIZED_HWID:
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Topmost = false;
                            MessageBox.Show(
                                "You do not have a valid Synapse X licence. You will now be shown a prompt to redeem a key to an existing account.",
                                "Synapse X", MessageBoxButton.OK, MessageBoxImage.Warning);
                            var Redeem = new RedeemWindow();
                            Redeem.Show();
                            Close();
                        });
                        break;
                    }
                    case WebInterface.WhitelistCheckResult.EXPIRED_LICENCE:
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Topmost = false;
                            MessageBox.Show(
                                "Your Synapse X licence is expired. You will now be shown a prompt to redeem a key to an existing account.",
                                "Synapse X", MessageBoxButton.OK, MessageBoxImage.Warning);
                            var Redeem = new RedeemWindow();
                            Redeem.Show();
                            Close();
                        });
                        break;
                    }
                    default:
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Topmost = false;
                            MessageBox.Show(
                                "Error while trying to check your whitelist status. Please report this to 3dsboy08.",
                                "Synapse X", MessageBoxButton.OK, MessageBoxImage.Error);
                            Environment.Exit(0);
                        });
                        break;
                    }
                }
            }
            else
            {
                if (!DataInterface.Exists("token"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        var LoginUI = new LoginWindow();
                        LoginUI.Show();
                        Close();
                    });
                    return;
                }
            }

            SetStatusText(InitStrings.DownloadingData, 50);
            Dispatcher.Invoke(() =>
            {
                var Main = new MainWindow();
                Main.Show();
                Close();
            });
        }
    }
}
