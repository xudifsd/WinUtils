using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.RegularExpressions;

namespace Utils
{
    public class Find : IRunnable
    {
        public Find() { }

        public int Run(string[] args)
        {
            var cmd = new RootCommand
            {
                new Argument<string>("path", "The start point of find"),
                new Option<string?>("-type", "[d|f]"),
                new Option<string?>("-regex", "regex"),
            };

            cmd.Handler = CommandHandler.Create<string, string?, string?>(Walk);

            return cmd.Invoke(args);
        }

        private static int Walk(string path, string? type, string? regex)
        {
            if (type != null && type != "d" && type != "f")
            {
                Console.Error.WriteLine($"unexpected type {type}");
                return 1;
            }

            WalkDirectoryTree(new DirectoryInfo(path), type, regex);
            return 0;
        }

        private static void OutputPathAccordingly(string path, string? regex)
        {
            bool output = true;
            if (regex != null)
            {
                Match m = Regex.Match(path, regex);
                output = m.Success;
            }

            if (output)
            {
                Console.Out.WriteLine(path);
            }
        }

        private static void WalkDirectoryTree(DirectoryInfo root, string? type, string? regex)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            try
            {
                files = root.GetFiles();
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e)
            {
                Console.Error.WriteLine(e.Message);
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (files != null)
            {
                if (type == null || type == "f")
                {
                    foreach (System.IO.FileInfo fi in files)
                    {
                        OutputPathAccordingly(fi.FullName, regex);
                    }
                }

                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    if (type == null || type == "d")
                    {
                        OutputPathAccordingly(dirInfo.FullName, regex);
                    }
                    WalkDirectoryTree(dirInfo, type, regex);
                }
            }
        }
    }
}
