using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils
{
    class Program
    {
        private static readonly Dictionary<string, IRunnable> UTILS_MAP = new Dictionary<string, IRunnable>
        {
            {"find", new Find()}, // TODO make it a class instead of instance
            {"ack", new Ack()},
            {"upgrade", new Upgrade()},
            {"drop", new Drop()},
        };

        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Need at least a cmd: " + string.Join(",", UTILS_MAP.Keys.ToArray()));
                return 1;
            }
            else
            {
                IRunnable runnable;
                if (UTILS_MAP.TryGetValue(args[0], out runnable))
                {
                    return runnable.Run(args.Skip(1).ToArray());
                }
                else
                {
                    Console.Error.WriteLine($"Could not fond runnable program {args[0]}");
                    return 2;
                }
            }
        }
    }
}
