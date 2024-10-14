global using System;
global using System.Linq;

namespace Revolt;

internal class Program {

    static void Main(string[] args) {
        Console.Title = "Revolt";

        Renderer.Start();

        while (true) {
            ConsoleKeyInfo key = Console.ReadKey();

            switch (key.Key) {
            case ConsoleKey.Escape : return;
            case ConsoleKey.F5     : Renderer.Redraw(); break;

            }
        }
    }
}
