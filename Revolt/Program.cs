global using System;
global using System.Linq;

namespace Revolt;

public class Program {

    static void Main(string[] args) {
        Console.Title = "Revolt";

        Renderer.Start();

        while (true) {
            ConsoleKeyInfo key = Console.ReadKey();

            switch (key.Key) {
            case ConsoleKey.Escape : return;
            case ConsoleKey.F4     : Ansi.ClearScreen(); break;
            case ConsoleKey.F5     : Renderer.Redraw(true); break;
            }

        }
    }
}
