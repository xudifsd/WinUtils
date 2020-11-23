using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Utils
{
    public class Ack : IRunnable
    {
        public Ack() { }

        public int Run(string[] args)
        {
            var cmd = new RootCommand
            {
                new Argument<string>("pattern", "pattern to search"),
                new Option<bool>(new[] { "--ignore-case", "-i" }, description: "ignore case when searching",
                    getDefaultValue: () => false),
            };

            cmd.Handler = CommandHandler.Create<string, bool>(Search);

            return cmd.Invoke(args);
        }

        private static int Search(string pattern, bool ignoreCase)
        {
            RegexOptions options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
            SearchInDirectory(new DirectoryInfo("."), pattern, options);
            return 0;
        }

        public static bool HasBinaryContent(string content)
        {
            return content.Any(ch => char.IsControl(ch) && ch != '\r' && ch != '\n');
        }

        private static void SearchInFile(System.IO.FileInfo file, string pattern, RegexOptions options)
        {
            try
            {
                bool output = false;
                foreach (var (line, idx) in System.IO.File.ReadLines(file.FullName).Select((value, i) => (value, i)))
                {
                    if (HasBinaryContent(line))
                    {
                        break;
                    }
                    if (Regex.Match(line, pattern, options).Success)
                    {
                        output = true;
                        Console.Out.WriteLine($"{file.FullName}: {idx + 1}: {line}");
                    }
                }
                if (output)
                {
                    Console.Out.WriteLine("");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }

        private static void SearchInDirectory(DirectoryInfo root, string pattern, RegexOptions options)
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
                foreach (System.IO.FileInfo fi in files)
                {
                    SearchInFile(fi, pattern, options);
                }

                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    SearchInDirectory(dirInfo, pattern, options);
                }
            }
        }
    }
}
