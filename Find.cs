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
                new Option<bool>(new[] { "--ignore-case", "-i" }, description: "ignore case when searching",
                    getDefaultValue: () => false),
            };

            cmd.Handler = CommandHandler.Create<string, string?, string?, bool>(Walk);

            return cmd.Invoke(args);
        }

        private static int Walk(string path, string? type, string? regex, bool ignoreCase)
        {
            if (type != null && type != "d" && type != "f")
            {
                return 1;
            }

            RegexOptions options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            Console.WriteLine($"ignoreCase is {ignoreCase}");

            WalkDirectoryTree(new DirectoryInfo(path), type, regex, options);
            return 0;
        }

        private static void OutputPathAccordingly(string path, string? regex, RegexOptions options)
        {
            bool output = true;
            if (regex != null)
            {
                Match m = Regex.Match(path, regex, options);
                output = m.Success;
            }

            if (output)
            {
                Console.Out.WriteLine(path);
            }
        }

        private static void WalkDirectoryTree(DirectoryInfo root, string? type, string? regex, RegexOptions options)
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
                        OutputPathAccordingly(fi.FullName, regex, options);
                    }
                }

                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    if (type == null || type == "d")
                    {
                        OutputPathAccordingly(dirInfo.FullName, regex, options);
                    }
                    WalkDirectoryTree(dirInfo, type, regex, options);
                }
            }
        }
    }
}
