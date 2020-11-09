using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Net;

namespace Utils
{
    class Upgrade : IRunnable
    {
        public Upgrade() { }

        public int Run(string[] args)
        {
            var cmd = new RootCommand
            {
                new Option<string>(new[] { "--url", "-u" }, description: "url to download",
                    getDefaultValue: () => "https://github.com/xudifsd/WinUtils/releases/latest/download/WinUtils.zip"),
            };

            cmd.Handler = CommandHandler.Create<string>(Download);

            return cmd.Invoke(args);
        }

        public int Download(string url)
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            var directory = new Uri(System.IO.Path.GetDirectoryName(path)).LocalPath;
            var selfFileName = System.IO.Path.GetFileName(path);

            var tmpFile = System.IO.Path.GetTempFileName();
            var tmpBatFile = System.IO.Path.GetTempFileName() + ".bat";
            var tmpDir = System.IO.Path.GetTempFileName();
            System.IO.File.Delete(tmpDir);
            System.IO.Directory.CreateDirectory(tmpDir);

            using (var client = new WebClient())
            {
                client.DownloadFile(url, tmpFile);
            }

            System.IO.Compression.ZipFile.ExtractToDirectory(tmpFile, tmpDir, true);

            using (var batFile = new System.IO.StreamWriter(System.IO.File.Create(tmpBatFile)))
            {
                batFile.WriteLine("@ECHO OFF");
                batFile.WriteLine("TIMEOUT /t 1 /nobreak > NUL");
                batFile.WriteLine("TASKKILL /IM \"{0}\" > NUL", selfFileName);
                batFile.WriteLine("robocopy \"{0}\" \"{1}\" /E /MOV ", tmpDir, directory);
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(tmpBatFile);
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = directory;
            System.Diagnostics.Process.Start(startInfo);

            Environment.Exit(0);
            return 0;
        }
    }
}
