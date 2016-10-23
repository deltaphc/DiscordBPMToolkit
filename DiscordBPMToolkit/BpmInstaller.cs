using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Octokit;

namespace DiscordBPMToolkit
{
    class BpmInstaller
    {
        private readonly string DBTTempFolder =
            Path.GetTempPath() + Path.DirectorySeparatorChar + "DiscordBPMToolkit" + Path.DirectorySeparatorChar;

        private double progress = 0;

        public BpmInstaller()
        {
            IsInstalling = false;
        }

        public async Task Install(bool usePTB)
        {
            IsInstalling = true;

            if (!Directory.Exists(DBTTempFolder))
                Directory.CreateDirectory(DBTTempFolder);

            ProgressValue = 0;
            InstallationStarted?.Invoke();

            Initializing?.Invoke();
            await Task.Run((Action)ExtractExeResources);
            Initialized?.Invoke();
            ProgressValue += 0.10;

            BpmDownloading?.Invoke();
            bool success = await DownloadLatestBpm();
            if (!success)
            {
                ProgressValue = 0;
                IsInstalling = false;
                BpmDownloadError?.Invoke("Unable to download BPM! See Output for details.");
                return;
            }
            BpmDownloaded?.Invoke();
            ProgressValue += 0.10;

            BpmExtracting?.Invoke();
            await ExtractLatestBpm();
            BpmExtracted?.Invoke();
            ProgressValue += 0.10;

            InstallScriptStarting?.Invoke();
            success = await RunBpmInstallScript(usePTB);
            if (!success)
            {
                ProgressValue = 0;
                IsInstalling = false;
                InstallScriptError?.Invoke("Encountered error(s). See Output for details.");
                return;
            }
            InstallScriptFinished?.Invoke();
            ProgressValue += 0.10;

            CleanUpStarting?.Invoke();
            await Task.Run((Action)CleanUpTempFiles);
            CleanUpFinished?.Invoke();

            ProgressValue = 1.0;
            IsInstalling = false;
            InstallationFinished?.Invoke();
        }

        private void ExtractExeResources()
        {
            File.WriteAllBytes(DBTTempFolder + "node.exe", Properties.Resources.Node);
            File.WriteAllBytes(DBTTempFolder + "7za.exe", Properties.Resources.SevenZip);
        }

        private async Task<bool> DownloadLatestBpm()
        {
            ReleaseAsset latestAsset = null;
            try
            {
                var github = new GitHubClient(new ProductHeaderValue("DiscordBPMToolkit"));
                var latestRelease = await github.Repository.Release.GetLatest("ByzantineFailure", "BPM-for-Discord");
                foreach (var asset in latestRelease.Assets)
                {
                    if (asset.Name.EndsWith(".7z"))
                    {
                        latestAsset = asset;
                        break;
                    }
                }
                if (latestAsset == null) return false;
            }
            catch
            {
                return false;
            }

            var archivePath = DBTTempFolder + "BPM-for-Discord-latest.7z";
            using (var wc = new WebClient())
            {
                wc.DownloadFile(latestAsset.BrowserDownloadUrl, archivePath);
            }
            if (!File.Exists(archivePath)) return false;

            return true;
        }

        private async Task ExtractLatestBpm()
        {
            var sevenZipPath = DBTTempFolder + "7za.exe";
            var archivePath = DBTTempFolder + "BPM-for-Discord-latest.7z";
            var pi = new ProcessStartInfo(sevenZipPath, "x -bb1 -o\"" + DBTTempFolder + "\" -- \"" + archivePath + "\"");
            pi.CreateNoWindow = true;
            pi.WindowStyle = ProcessWindowStyle.Hidden;
            pi.UseShellExecute = false;
            pi.RedirectStandardOutput = true;
            using (var p = Process.Start(pi))
            {
                string output = await p.StandardOutput.ReadLineAsync();
                while (output != null)
                {
                    ProgressValue += (0.2 / 2500); // 20% progress taken by this step divided by approximate output lines generated
                    output = await p.StandardOutput.ReadLineAsync();
                }
                p.WaitForExit();
            }
        }

        private async Task<bool> RunBpmInstallScript(bool usePTB)
        {
            bool success = true;
            var workingFolder = DBTTempFolder + "discord\\";
            var pi = new ProcessStartInfo("cmd.exe", "/C \"\"" + DBTTempFolder + "node.exe\" \"" + workingFolder + "index.js\" \"" + DBTTempFolder + "discord\"");
            pi.Arguments += usePTB ? " --isPTB" : "";
            pi.Arguments += " 2>&1\""; //Redirects stderr to stdout
            pi.WorkingDirectory = workingFolder;
            pi.CreateNoWindow = true;
            pi.WindowStyle = ProcessWindowStyle.Hidden;
            pi.UseShellExecute = false;
            pi.RedirectStandardOutput = true;
            pi.RedirectStandardInput = true;
            using (var p = Process.Start(pi))
            {
                string output = await p.StandardOutput.ReadLineAsync();
                while (output != null)
                {
                    if (output.Contains("Error"))
                        success = false;
                    InstallScriptOutput?.Invoke(output);
                    ProgressValue += 0.01;
                    output = await p.StandardOutput.ReadLineAsync();
                }

                p.StandardInput.WriteLine(); //Simulate pressing enter in case it has a "press any key" prompt
                p.WaitForExit();
            }
            return success;
        }

        private void CleanUpTempFiles()
        {
            Directory.Delete(DBTTempFolder + "discord", true);
            foreach (string filename in Directory.EnumerateFiles(DBTTempFolder))
            {
                File.Delete(filename);
            }
        }

        public event Action InstallationStarted;
        public event Action Initializing;
        public event Action Initialized;
        public event Action BpmDownloading;
        public event Action<string> BpmDownloadError;
        public event Action BpmDownloaded;
        public event Action BpmExtracting;
        public event Action BpmExtracted;
        public event Action InstallScriptStarting;
        public event Action<string> InstallScriptOutput;
        public event Action<string> InstallScriptError;
        public event Action InstallScriptFinished;
        public event Action CleanUpStarting;
        public event Action CleanUpFinished;
        public event Action InstallationFinished;
        public event Action<double, double> ProgressChanged;

        public bool IsInstalling { get; private set; }
        public double ProgressValue
        {
            get { return progress; }
            private set
            {
                double oldValue = progress;
                progress = value;
                ProgressChanged?.Invoke(oldValue, progress);
            }
        }
        public string TempFolder => DBTTempFolder;
    }
}
