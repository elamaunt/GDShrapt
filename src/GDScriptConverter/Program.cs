using System;
using System.IO;

namespace GDScriptConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            RETURN:
            if (args == null || args.Length < 2)
            {
                Console.WriteLine("GDScriptConverter parse and convert all godot scripts to their C# eqvivalent.");
                Console.WriteLine("All code style will be rewritten to C# standarts.");
                Console.WriteLine("");
                Console.WriteLine("Usage:");
                Console.WriteLine("GDScriptConverter <path to godot project root directory> <empty destination directory>");
                return;
            }

            var from = args[0];
            var to = args[1];

            if (from.IsNullOrEmpty() || to.IsNullOrEmpty() || !Directory.Exists(from))
                goto RETURN;

            var parser = new GDScriptParser();

            ProcessDirectory(from, to, (filePath, destinationPath) =>
            {
                var extension = Path.GetExtension(filePath);

                if (extension.EndsWith(".gd", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Parsing script file: '{filePath}'");
                    var tree = parser.Parse(filePath);
                }
                else
                {
                    Console.WriteLine($"Simple copy file: '{filePath}'");
                    File.Copy(file.FullName, tempPath, true);
                }
            });

        }

        static void ProcessDirectory(string path1, string path2, Action<string, string> fileHandler)
        {
            Directory.CreateDirectory(path2);
            Console.WriteLine($"Processing directory: '{path2}'");

            DirectoryInfo dir = new DirectoryInfo(path1);

            if (!dir.Exists)
                return;

            foreach (var subdir in dir.EnumerateDirectories())
            {
                var tempPath = Path.Combine(path2, subdir.Name);
                ProcessDirectory(subdir.FullName, tempPath, fileHandler);
            }

            foreach (var file in dir.EnumerateFiles())
            {
                var tempPath = Path.Combine(path2, file.Name);
                fileHandler(file.FullName, tempPath);
            }
        }
    }
}
