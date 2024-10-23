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

    public static void DrawBanner() {
        Console.WriteLine(@"  ______                _ _");
        Console.WriteLine(@"  | ___ \              | | |");
        Console.WriteLine(@"  | |_/ /_____   _____ | | |_");
        Console.WriteLine(@"  |    // _ \ \ / / _ \| | __|");
        Console.WriteLine(@"  | |\ \  __/\ V / (_) | | |_ ");
        Console.WriteLine(@"  \_| \_\___| \_/ \___/|_|\__|");

        Version ver = Assembly.GetExecutingAssembly().GetName()?.Version;
        string version = $"{ver?.Major ?? 0}.{ver?.Minor ?? 0}.{ver?.Build ?? 0}.{ver?.Revision ?? 0}";
        Console.WriteLine($"{version,30}");
    }
}