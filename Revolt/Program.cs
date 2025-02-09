global using System;
global using System.Linq;

using System.Reflection;

namespace Revolt;

public class Program {

    static void Main(string[] args) {
        if (args.Length > 0) {
            Console.WriteLine("unknown argument");
        }

        Console.Title = "Revolt";
        Renderer.Initialize();
    }

    public static void WriteBanner() {
        Ansi.WriteLine(@"  ______                _ _");
        Ansi.WriteLine(@"  | ___ \              | | |");
        Ansi.WriteLine(@"  | |_/ /_____   _____ | | |_");
        Ansi.WriteLine(@"  |    // _ \ \ / / _ \| | __|");
        Ansi.WriteLine(@"  | |\ \  __/\ V / (_) | | |_ ");
        Ansi.WriteLine(@"  \_| \_\___| \_/ \___/|_|\__|");

        Version ver = Assembly.GetExecutingAssembly().GetName()?.Version;
        string version = $"{ver?.Major ?? 0}.{ver?.Minor ?? 0}.{ver?.Build ?? 0}.{ver?.Revision ?? 0}";
        Ansi.WriteLine($"{version,30}");
    }
}