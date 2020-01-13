using System;
using System.Collections.Generic;

namespace Orbit {
    public class DefaultConsole : IConsole
    {
        private Dictionary<ConsoleOptions.Color, ConsoleColor> _colorMap = new Dictionary<ConsoleOptions.Color, ConsoleColor>
        {
            {ConsoleOptions.Color.Cancel, ConsoleColor.Magenta},
            {ConsoleOptions.Color.Info, ConsoleColor.DarkYellow},
            {ConsoleOptions.Color.Error, ConsoleColor.Red},
            {ConsoleOptions.Color.Ask, ConsoleColor.DarkCyan},
            {ConsoleOptions.Color.AskDivider, ConsoleColor.Green}
        };

        internal DefaultConsole() { }

        public ConsoleColor CancelColor
        {
            get => _colorMap[ConsoleOptions.Color.Cancel];
            set => _colorMap[ConsoleOptions.Color.Cancel] = value;
        }

        public ConsoleColor InfoColor
        {
            get => _colorMap[ConsoleOptions.Color.Info];
            set => _colorMap[ConsoleOptions.Color.Info] = value;
        }

        public ConsoleColor ErrorColor
        {
            get => _colorMap[ConsoleOptions.Color.Error];
            set => _colorMap[ConsoleOptions.Color.Error] = value;
        }

        public ConsoleColor AskDividerColor
        {
            get => _colorMap[ConsoleOptions.Color.AskDivider];
            set => _colorMap[ConsoleOptions.Color.AskDivider] = value;
        }

        public ConsoleColor AskColor
        {
            get => _colorMap[ConsoleOptions.Color.Ask];
            set => _colorMap[ConsoleOptions.Color.Ask] = value;
        }

        public void WriteLine() => Console.WriteLine();

        public void WriteLine(string value, ConsoleOptions.Color color)
        {
            var currentColor = Console.ForegroundColor;

            Console.ForegroundColor = color == ConsoleOptions.Color.Default ? currentColor : _colorMap[color];
            Console.WriteLine(value);
            Console.ForegroundColor = currentColor;
        }

        public void Write(string value, ConsoleOptions.Color color)
        {
            var currentColor = Console.ForegroundColor;

            Console.ForegroundColor = color == ConsoleOptions.Color.Default ? currentColor : _colorMap[color];
            Console.Write(value);
            Console.ForegroundColor = currentColor;
        }

        public ConsoleKeyInfo ReadKey() => Console.ReadKey(true);
        public string ReadLine() => Console.ReadLine();
    }
}