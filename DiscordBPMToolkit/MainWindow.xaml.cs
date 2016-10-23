using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using System.Windows.Threading;

namespace DiscordBPMToolkit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string InstallingPrefix = "[installing] - ";

        private BpmInstaller installer;
        private bool isDiscordRunning = false;
        private double collapsedHeight;
        private DispatcherTimer timer;

        private Stopwatch stopwatch;
        private long startTime = 0;
        private long endTime = 0;

        public MainWindow()
        {
            InitializeComponent();
            TaskbarItemInfo = new TaskbarItemInfo();
            collapsedHeight = Height;

            // For updating the "Launch/Restart Discord" button
            timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle, Dispatcher);
            timer.Interval = TimeSpan.FromSeconds(1.0);
            timer.Tick += Timer_Tick;
            timer.Start();

            stopwatch = new Stopwatch();

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string discordInstallPath = localAppData + "Discord";
            string discordPTBInstallPath = localAppData + "DiscordPTB";
            if (Directory.Exists(discordInstallPath) && Directory.Exists(discordPTBInstallPath))
                isPTB.Visibility = Visibility.Visible;
            else if (Directory.Exists(discordPTBInstallPath) && !Directory.Exists(discordInstallPath))
                isPTB.IsChecked = true;
            else
                isPTB.IsChecked = false;

            installer = new BpmInstaller();
            installer.ProgressChanged += Installer_ProgressChanged;
            installer.InstallationStarted += Installer_InstallationStarted;
            installer.Initializing += Installer_Initializing;
            installer.Initialized += Installer_Initialized;
            installer.BpmDownloading += Installer_BpmDownloading;
            installer.BpmDownloadError += Installer_BpmDownloadError;
            installer.BpmDownloaded += Installer_BpmDownloaded;
            installer.BpmExtracting += Installer_BpmExtracting;
            installer.BpmExtracted += Installer_BpmExtracted;
            installer.InstallScriptStarting += Installer_InstallScriptStarting;
            installer.InstallScriptOutput += Installer_InstallScriptOutput;
            installer.InstallScriptError += Installer_InstallScriptError;
            installer.InstallScriptFinished += Installer_InstallScriptFinished;
            installer.CleanUpStarting += Installer_CleanUpStarting;
            installer.CleanUpFinished += Installer_CleanUpFinished;
            installer.InstallationFinished += Installer_InstallationFinished;

            installLog.Text =
                $"Discord BPM Toolkit {Properties.Resources.Version}\nby DeltaPHC\n\n";
        }

        private void Installer_ProgressChanged(double oldValue, double newValue)
        {
            installProgress.Value = newValue;
        }

        private void Installer_CleanUpFinished()
        {
            endTime = stopwatch.ElapsedMilliseconds;
            installLog.AppendText($"done in {endTime - startTime}ms\n");
        }

        private void Installer_CleanUpStarting()
        {
            startTime = stopwatch.ElapsedMilliseconds;
            installLog.AppendText("Cleaning up temporary files... ");
            installStatus.Text = "Cleaning up";
        }

        private void Installer_InstallScriptFinished()
        {
            endTime = stopwatch.ElapsedMilliseconds;
            installLog.AppendText($"Install script done in {endTime - startTime}ms\n");
        }

        private void Installer_InstallScriptError(string errorText)
        {
            installStatus.Text = errorText;
            outputExpander.IsExpanded = true;
            installButton.IsEnabled = true;
            launchButton.IsEnabled = true;
            aboutButton.IsEnabled = true;
            Title = Title.Remove(0, InstallingPrefix.Length);
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
        }

        private void Installer_InstallScriptOutput(string output)
        {
            installLog.AppendText(output + "\n");
        }

        private void Installer_InstallScriptStarting()
        {
            startTime = stopwatch.ElapsedMilliseconds;
            installLog.AppendText("Running install script...\n");
            installStatus.Text = "Installing BPM. This may take a minute or two";
        }

        private void Installer_BpmExtracted()
        {
            endTime = stopwatch.ElapsedMilliseconds;
            installLog.AppendText($"done in {endTime - startTime}ms\n");
        }

        private void Installer_BpmExtracting()
        {
            startTime = stopwatch.ElapsedMilliseconds;
            installLog.AppendText("Extracting BPM... ");
            installStatus.Text = "Extracting BPM";
        }

        private void Installer_BpmDownloaded()
        {
            endTime = stopwatch.ElapsedMilliseconds;
            installLog.AppendText($"done in {endTime - startTime}ms\n");
        }

        private void Installer_BpmDownloadError(string errorMessage)
        {
            installLog.AppendText("\n\nThere was a problem downloading BPM. Make sure your internet connection is working. If you have internet, then GitHub may be down, or you may be out of storage space.\n");
            installStatus.Text = errorMessage;
            outputExpander.IsExpanded = true;
            installButton.IsEnabled = true;
            launchButton.IsEnabled = true;
            aboutButton.IsEnabled = true;
            Title = Title.Remove(0, InstallingPrefix.Length);
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
        }

        private void Installer_BpmDownloading()
        {
            startTime = stopwatch.ElapsedMilliseconds;
            installLog.AppendText("Downloading latest BPM for Discord... ");
            installStatus.Text = "Downloading latest BPM for Discord";
        }

        private void Installer_Initialized()
        {
            endTime = stopwatch.ElapsedMilliseconds;
            installLog.AppendText($"done in {endTime - startTime}ms\n");
        }

        private void Installer_Initializing()
        {
            startTime = stopwatch.ElapsedMilliseconds;
            installLog.AppendText("Initializing... ");
            installStatus.Text = "Initializing. One moment, please";
        }

        private void Installer_InstallationFinished()
        {
            stopwatch.Stop();
            installLog.AppendText($"Installation finished in {stopwatch.ElapsedMilliseconds}ms\n\n");
            installLog.AppendText("\nBPM for Discord is now installed!\n");
            installStatus.Text = "Done! Be sure to restart Discord.";
            installButton.IsEnabled = true;
            launchButton.IsEnabled = true;
            aboutButton.IsEnabled = true;
            Title = Title.Remove(0, InstallingPrefix.Length);
        }

        private void Installer_InstallationStarted()
        {
            stopwatch.Reset();
            stopwatch.Start();
            Title = InstallingPrefix + Title;
            installButton.IsEnabled = false;
            launchButton.IsEnabled = false;
            aboutButton.IsEnabled = false;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var discordProcesses = Process.GetProcessesByName("Discord");
            isDiscordRunning = (discordProcesses.Length > 0);

            if (isDiscordRunning)
                launchButton.Content = "Restart Discord";
            else
                launchButton.Content = "Launch Discord";

            if (!installer.IsInstalling)
                launchButton.IsEnabled = true;
            timer.Interval = TimeSpan.FromSeconds(1.0);
        }

        private async void installButton_Click(object sender, RoutedEventArgs e)
        {
            await installer.Install((bool)isPTB.IsChecked);
        }

        private void installProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TaskbarItemInfo.ProgressValue = e.NewValue;
            if (e.NewValue == 1.0)
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
            else
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
        }

        private void outputExpander_Expanded(object sender, RoutedEventArgs e)
        {
            outputExpander.Header = "Hide Output";
            collapsedHeight = Height;
            if (Height < 450.0) Height = 450.0;
        }

        private void outputExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            outputExpander.Header = "Show Output";
            Height = collapsedHeight;
        }

        private void LaunchDiscord()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + System.IO.Path.DirectorySeparatorChar;
            string discordBase = localAppData + ((bool)isPTB.IsChecked ? "DiscordPTB" : "Discord") + System.IO.Path.DirectorySeparatorChar;
            string updateExePath = discordBase + "Update.exe";
            Process.Start(updateExePath, "--processStart Discord.exe");
        }

        private void launchButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isDiscordRunning)
            {
                LaunchDiscord();
            }
            else
            {
                launchButton.IsEnabled = false;
                timer.Stop();
                timer.Interval = TimeSpan.FromSeconds(5.0);
                timer.Start();

                var discordProcesses = Process.GetProcessesByName("Discord");
                foreach (var process in discordProcesses)
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                    process.Close();
                }
                LaunchDiscord();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = installer.IsInstalling;
        }

        private void installLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            installLog.ScrollToEnd();
        }

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            (new AboutWindow()).ShowDialog();
        }
    }
}
