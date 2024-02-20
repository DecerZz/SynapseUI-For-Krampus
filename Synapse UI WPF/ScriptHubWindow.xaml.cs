using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Synapse_UI_WPF.Interfaces;
using Synapse_UI_WPF.Static;

namespace Synapse_UI_WPF
{

    class NewEntry
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Picture { get; set; }
        public string Description { get; set; }
    }
    public partial class ScriptHubWindow
    {
        private readonly MainWindow Main;

        private readonly Dictionary<string, NewEntry> DictData = new Dictionary<string, NewEntry>();

        private NewEntry CurrentEntry;
        private Data.ScriptHubHolder Data;

        private bool IsExecuting;

        private BackgroundWorker ExecuteWorker = new BackgroundWorker();

        public ScriptHubWindow(MainWindow _Main)
        {
            Main = _Main;
            //Data = _Data;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ExecuteWorker.DoWork += ExecuteWorker_DoWork;

            InitializeComponent();
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

            var TScriptHub = Globals.Theme.ScriptHub;
            ThemeInterface.ApplyWindow(this, TScriptHub.Base);
            ThemeInterface.ApplyLogo(IconBox, TScriptHub.Logo);
            ThemeInterface.ApplySeperator(TopBox, TScriptHub.TopBox);
            ThemeInterface.ApplyLabel(TitleBox, TScriptHub.TitleBox);
            ThemeInterface.ApplyListBox(ScriptBox, TScriptHub.ScriptBox);
            ThemeInterface.ApplyTextBox(DescriptionBox, TScriptHub.DescriptionBox);
            ThemeInterface.ApplyButton(MiniButton, TScriptHub.MinimizeButton);
            ThemeInterface.ApplyButton(ExecuteButton, TScriptHub.ExecuteButton);
            ThemeInterface.ApplyButton(CloseButton, TScriptHub.CloseButton);

            var Entries = new Dictionary<int, NewEntry> (){
                [1] = new NewEntry { Name = "Dex", Url = "loadstring(game:HttpGet(\"https://raw.githubusercontent.com/infyiff/backup/main/dex.lua\"))()", Description = "A version of the popular Dex explorer.", Picture = "https://devforum-uploads.s3.dualstack.us-east-2.amazonaws.com/uploads/original/4X/d/c/3/dc31a109b3ee55701bbfedb5e0f2a79f80e7ebd8.png" },
                [2] = new NewEntry { Name = "Infinite Yield", Url = "loadstring(game:HttpGet('https://raw.githubusercontent.com/EdgeIY/infiniteyield/master/source'))()", Description = "Admin command script.", Picture = "https://www.softlay.com/downloads/wp-content/uploads/Infinite-Yield-Script-Roblox-Pic-1.jpg" },
                [3] = new NewEntry { Name = "Hydroxide", Url = "local owner = \"Upbolt\"\r\nlocal branch = \"revision\"\r\n\r\nlocal function webImport(file)\r\n    return loadstring(game:HttpGetAsync((\"https://raw.githubusercontent.com/%s/Hydroxide/%s/%s.lua\"):format(owner, branch, file)), file .. '.lua')()\r\nend\r\n\r\nwebImport(\"init\")\r\nwebImport(\"ui/main\")", Description = "Lua runtime introspection and network capturing tool for games on the Roblox engine.", Picture = "https://camo.githubusercontent.com/c4e4b790671404d056f4f348bfae7ff6d15399aca4b648c92f967a258d337286/68747470733a2f2f63646e2e646973636f72646170702e636f6d2f6174746163686d656e74732f3639343732363633363133383030343539332f3734323430383534363333343933333030322f756e6b6e6f776e2e706e67" },
                [4] = new NewEntry { Name = "Simple Spy", Url = "loadstring(game:HttpGet(\"https://github.com/exxtremestuffs/SimpleSpySource/raw/master/SimpleSpy.lua\"))()", Description = "SimpleSpy is a penetration testing tool designed to intercept remote calls from the client to the server.", Picture = "https://i.ytimg.com/vi/5qwk8S92Mhw/hq720.jpg" },
                [5] = new NewEntry { Name = "Unnamed ESP", Url = "pcall(function() loadstring(game:HttpGet('https://raw.githubusercontent.com/ic3w0lf22/Unnamed-ESP/master/UnnamedESP.lua'))() end)", Description = "Player ESP for Roblox, fully undetectable, uses built in drawing API if the exploit supports it.", Picture = "https://static.wixstatic.com/media/7f1913_11357da4e80946bc8eacac683298dec2~mv2.png/v1/fill/w_640,h_360,al_c,q_85,usm_0.66_1.00_0.01,enc_auto/7f1913_11357da4e80946bc8eacac683298dec2~mv2.png" }
            };

            foreach (KeyValuePair<int, NewEntry> Script in Entries)
            {
                //Console.WriteLine(Entries[Script.Key].Name);
                DictData[Entries[Script.Key].Name] = Entries[Script.Key];
                ScriptBox.Items.Add(Entries[Script.Key].Name);
            }
        }

        private void ScriptBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ScriptBox.SelectedIndex == -1)
            {
                return;
            }

            CurrentEntry = DictData[ScriptBox.Items[ScriptBox.SelectedIndex].ToString()];
            DescriptionBox.Text = CurrentEntry.Description;

            ScriptPictureBox.Source = new BitmapImage(new Uri(CurrentEntry.Picture));
        }

        public bool IsOpen()
        {
            return Dispatcher.Invoke(() =>
            {
                return Application.Current.Windows.Cast<Window>().Any(x => x == this);
            });
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsExecuting) return;
            if (CurrentEntry == null) return;

            if (!Main.Ready())
            {
                ExecuteButton.Content = "Not attached!";

                new Thread(() =>
                {
                    Thread.Sleep(1500);
                    if (!IsOpen()) return;

                    Dispatcher.Invoke(() =>
                    {
                        ExecuteButton.Content = Globals.Theme.ScriptHub.ExecuteButton.TextNormal;
                    });
                }).Start();

                return;
            }

            ExecuteButton.Content = Globals.Theme.ScriptHub.ExecuteButton.TextYield;
            IsExecuting = true;

            ExecuteWorker.RunWorkerAsync();
        }

        private void ExecuteWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string ScriptContent;

            try
            {
                ScriptContent = CurrentEntry.Url;
            }
            catch (Exception)
            {
                if (!IsOpen()) return;

                Dispatcher.Invoke(() =>
                {
                    IsExecuting = false;
                    ExecuteButton.Content = Globals.Theme.ScriptHub.ExecuteButton.TextNormal;

                    Topmost = false;
                    MessageBox.Show(
                        "Synapse failed to download script from the script hub. Check your internet connection.",
                        "Synapse X", MessageBoxButton.OK, MessageBoxImage.Error);
                    Topmost = true;
                });

                return;
            }

            Dispatcher.Invoke(() =>
            {
                if (!IsOpen()) return;

                IsExecuting = false;
                ExecuteButton.Content = Globals.Theme.ScriptHub.ExecuteButton.TextNormal;

                Main.Execute(ScriptContent);
            });
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MiniButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Main.ScriptHubOpen = false;
        }
    }
}
