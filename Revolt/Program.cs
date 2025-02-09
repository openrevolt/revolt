global using System;
global using System.Linq;

namespace Revolt;

public class Program {
    static void Main(string[] args) {
        if (args.Length > 0) {
            Console.WriteLine("unknown argument");
        }

        Console.Title = "Revolt";
        Renderer.Initialize();
    }
}