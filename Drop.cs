using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

namespace Utils
{
    public class Drop : IRunnable
    {
        private static readonly Dictionary<string, string> SVC_MAPPING = new Dictionary<string, string>
        {
            {"urp", "?root=retail/amd64/app/UrpService"},
            {"reco", "?root=retail/amd64/app/RecommendationWebApi"},
        };

        public Drop() { }

        public int Run(string[] args)
        {
            var envHome = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "HOMEPATH" : "HOME";
            var home = Environment.GetEnvironmentVariable(envHome);
            var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss");
            var basePath = Path.Combine(home, "build", now);

            var cmd = new RootCommand
            {
                new Argument<string>("url", "drop url"),
                new Option<string>(new[] { "--dir", "-d" }, description: "base dir",
                    getDefaultValue: () => basePath),
                new Option<string>(new[] { "--svc", "-s" }, description: "service to download"),
                new Option<bool>(new[] { "--clean", "-c" }, description: "clean PDB files after download",
                    getDefaultValue: () => true),
            };

            cmd.Handler = CommandHandler.Create<string, string, string?, bool>(Download);

            return cmd.Invoke(args);
        }

        private static void CleanPdp(DirectoryInfo root)
        {
            foreach (var file in root.EnumerateFiles("*.pdb"))
            {
                file.Delete();
            }

            System.IO.DirectoryInfo[] subDirs = root.GetDirectories();

            foreach (System.IO.DirectoryInfo dirInfo in subDirs)
            {
                CleanPdp(dirInfo);
            }
        }

        private static int Download(string url, string dir, string? svc, bool clean)
        {
            List<string> args = null;
            if (svc == null)
            {
                args = SVC_MAPPING.Values.ToList();
            }
            else
            {
                args = svc.Split(",").Select(s => SVC_MAPPING.GetValueOrDefault(s, null)).ToList();
                if (args.Any(x => x == null))
                {
                    Console.Error.WriteLine($"found unknown svc in {svc}, vaild: {string.Join(",", SVC_MAPPING.Keys.ToList())}");
                    return 1;
                }
            }

            System.IO.Directory.CreateDirectory(dir);

            bool hasError = false;
            foreach (string arg in args)
            {
                using System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
                var urlWithArg = url + arg;

                pProcess.StartInfo.FileName = "drop.cmd";
                pProcess.StartInfo.Arguments = $"get -a -u {urlWithArg} -d {dir}";
                pProcess.StartInfo.UseShellExecute = false;
                pProcess.StartInfo.RedirectStandardError = true;
                pProcess.StartInfo.RedirectStandardOutput = true;
                pProcess.OutputDataReceived += (a, b) => Console.Out.WriteLine(b.Data);
                pProcess.ErrorDataReceived += (a, b) => Console.Error.WriteLine(b.Data);
                pProcess.StartInfo.CreateNoWindow = true;

                pProcess.Start();
                pProcess.BeginErrorReadLine();
                pProcess.BeginOutputReadLine();
                pProcess.WaitForExit();

                if (pProcess.ExitCode != 0)
                {
                    Console.Error.WriteLine($"failed to get {urlWithArg}");
                    hasError = true;
                }
                // TODO handle Ctrl-C and end pProcess.
            }

            if (clean)
            {
                CleanPdp(new DirectoryInfo(dir));
            }
            return hasError ? 2 : 0;
        }
    }
}
