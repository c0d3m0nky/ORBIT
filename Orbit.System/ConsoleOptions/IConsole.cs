using System;

namespace Orbit {
    public interface IConsole
    {
        ConsoleColor CancelColor { get; }
        ConsoleColor InfoColor { get; }
        ConsoleColor ErrorColor { get; }
        ConsoleColor AskDividerColor { get; }
        ConsoleColor AskColor { get; }

        void WriteLine();
        void WriteLine(string value, ConsoleOptions.Color color);
        void Write(string value, ConsoleOptions.Color color);
        ConsoleKeyInfo ReadKey();
        string ReadLine();
    }
}