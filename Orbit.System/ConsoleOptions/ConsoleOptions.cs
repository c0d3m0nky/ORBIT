using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Orbit
{
    public class ConsoleOptions<T>
    {
        private readonly bool _multiline;
        private readonly bool _allowCancel;
        private readonly string _cancelChar;
        private readonly string _cancelStr;
        private readonly Dictionary<int, Option<T>> _options;

        public ConsoleOptions(string question, IEnumerable<Option<T>> options, string cancelText = "Cancel")
        {
            var multiline = false;
            _options = options.ToDictionary((o, i) => i, (o, i) =>
            {
                multiline = _multiline || o.Description.Contains("\n");
                return o;
            });

            _multiline = multiline;
            Question = question;
            _allowCancel = !cancelText.IsNullOrWhitespace();

            if (_allowCancel)
            {
                _cancelStr = cancelText.Trim();
                _cancelChar = _cancelStr.First().ToString().ToLower();
                _multiline = _multiline || _cancelStr.Contains("\n");
            }
        }

        private string Question { get; }

        public IReadOnlyDictionary<int, Option<T>> Options => new ReadOnlyDictionary<int, Option<T>>(_options);

        public Option<T> Ask()
        {
            if (Options.All(o => !o.Value.Show)) return null;

            var cc = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleOptions.AskDividerColor;
            Console.WriteLine();
            Console.WriteLine("-----");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleOptions.AskColor;

            Option<T> opt = null;

            do
            {
                Console.WriteLine(Question);
                PrintOptions();
                var resp = ConsoleOptions.InputOrScript();

                if (string.Equals(resp, _cancelChar, StringComparison.CurrentCultureIgnoreCase)) break;

                opt = _options.GetValueOrDefault(resp.ParseInt());
            } while (opt == null);

            Console.ForegroundColor = cc;

            return opt;
        }

        private void PrintOptions()
        {
            var fg = Console.ForegroundColor;
            var format = _multiline
                ? @"-- {0} --
{1}"
                : "\t{0} -- {1}";

            Options.Where(o => o.Value.Show).ForEach(p =>
            {
                if (p.Value.Color.HasValue) Console.ForegroundColor = p.Value.Color.Value;
                Console.WriteLine(format, p.Key, p.Value.Description);
                Console.ForegroundColor = fg;
            });

            if (_allowCancel) Console.WriteLine($"\t{_cancelChar} -- {_cancelStr}");
        }
    }
}