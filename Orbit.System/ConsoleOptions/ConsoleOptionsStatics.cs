using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Orbit
{
    public static class ConsoleOptions
    {
        public static bool CancelPromptShown = false;
        public static IEnumerator<string> Script = null;
        public static TimeSpan ScriptSleep = TimeSpan.Zero;
        public static ConsoleColor CancelColor = ConsoleColor.Magenta;
        public static ConsoleColor InfoColor = ConsoleColor.DarkYellow;
        public static ConsoleColor ErrorColor = ConsoleColor.Red;
        public static ConsoleColor AskDividerColor = ConsoleColor.Green;
        public static ConsoleColor AskColor = ConsoleColor.DarkCyan;

        private static void ShowCancelPrompt()
        {
            if (!CancelPromptShown)
            {
                var currentColor = Console.ForegroundColor;

                Console.ForegroundColor = InfoColor;
                Console.WriteLine();
                Console.Write("When you see prompts in ");
                Console.ForegroundColor = CancelColor;
                Console.Write(CancelColor.ToString());
                Console.ForegroundColor = InfoColor;
                Console.Write(" it means you can enter 'c' to cancel");
                Console.WriteLine();
                CancelPromptShown = true;
                Console.ForegroundColor = currentColor;
            }
        }

        private static (string question, Action setColor, Action resetColor, bool isMultiline, bool isEmpty) QuestionMeta(string question, bool allowCancel)
        {
            var currentColor = Console.ForegroundColor;
            Action nullAction = () => { };
            Action setCancelColor = () => Console.ForegroundColor = CancelColor;
            Action resetCancelColor = () => Console.ForegroundColor = currentColor;

            question = question.IfNullOrWhitespace("").Trim();

            return (question, setColor: allowCancel ? setCancelColor : nullAction, allowCancel ? resetCancelColor : nullAction, question.Contains("\n"), question.IsNullOrWhitespace());
        }

        internal static string InputOrScript()
        {
            const string pauseChar = "*";

            bool handledByScript(out string r)
            {
                r = null;

                if (Script == null) return false;

                do
                {
                    var end = !Script.GetNext(out r);

                    if (end)
                    {
                        Script = null;
                        r = null;
                        return false;
                    }

                    if (r == pauseChar)
                    {
                        Console.Write("Script Paused. Enter to continue, Ctrl+c to end");
                        Console.ReadLine();
                    }
                } while (r != pauseChar);

                Console.WriteLine(r);
                if (ScriptSleep > TimeSpan.Zero) Thread.Sleep((int) ScriptSleep.TotalMilliseconds);

                return true;
            }

            if (!handledByScript(out var resp)) return Console.ReadLine();

            return resp;
        }

        public static bool? YesNoCancel(string question)
        {
            ShowCancelPrompt();
            bool? yn = null;
            var meta = QuestionMeta(question, true);

            do
            {
                meta.setColor();

                if (meta.isMultiline) Console.WriteLine($"{meta.question}");
                else Console.Write($"{meta.question} ");

                meta.resetColor();

                var resp = InputOrScript();

                if (string.Equals(resp, "c", StringComparison.CurrentCultureIgnoreCase)) break;

                yn = resp.ParseBool();
            } while (!yn.HasValue);

            return yn;
        }

        public static bool YesNo(string question)
        {
            bool? yn;
            var meta = QuestionMeta(question, true);

            do
            {
                meta.setColor();

                if (meta.isMultiline) Console.WriteLine($"{question}");
                else Console.Write($"{question} ");

                meta.resetColor();

                yn = InputOrScript().ParseBool();
            } while (!yn.HasValue);

            return yn.Value;
        }

        public static Dictionary<string, object> GetFields(string question, Func<string, string> mutate = null, Func<string, bool> isValid = null, bool allowCancel = true)
        {
            var meta = QuestionMeta(question, allowCancel);
            var explain = $"Enter fields as key:value";

            Console.WriteLine(question);
            Console.WriteLine(explain);
            var fields = new Dictionary<string, object>();

            do
            {
                var field = GetInput("", s => s.IfNullOrWhitespace(""), allowCancel: allowCancel);

                if (field == null) return null;

                if (field == "")
                {
                    if (YesNo("Done?")) break;

                    continue;
                }

                var spl = field.Trim().Split(new[] {':', '='}, StringSplitOptions.RemoveEmptyEntries).Select(s => s?.Trim()).ToArray();

                if (spl.Length != 2 || spl.Any(s => s.IsNullOrWhitespace()))
                {
                    Console.ForegroundColor = ErrorColor;
                    Console.WriteLine();
                    Console.WriteLine("Invalid entry");
                    Console.WriteLine(explain);
                    meta.resetColor();
                    continue;
                }

                var key = spl[0];
                var val = spl[1].ParseBestGuess();

                while (fields.ContainsKey(key))
                {
                    var replace = YesNoCancel("Key exists. Replace value?");

                    if (!replace.HasValue) return null;

                    if (!replace.Value)
                    {
                        key = GetInput("Key:");

                        if (key == null) return null;
                    }
                }

                fields[key] = val;
            } while (true);

            return fields;
        }

        public static Task<string> GetInput(string question, Func<string, Task<bool>> isValid = null, Func<string, Task<string>> mutate = null, bool allowCancel = true)
            => GetInput(question, isValid, mutate ?? Task.FromResult, null, allowCancel);

        public static Task<T> GetInput<T>(string question, Func<string, Task<bool>> isValid, Func<string, Task<T>> mutate, bool allowCancel = true)
            => GetInput(question, isValid, mutate, null, allowCancel);

        public static Task<T> GetInput<T>(string question, Func<string, Task<T>> mutate, Func<T, Task<bool>> isValid = null, bool allowCancel = true)
            => GetInput(question, null, mutate, isValid, allowCancel);

        public static string GetInput(string question)
            => GetInput(question, null, null, (Func<string, bool>) null, true);

        public static string GetInput(string question, Func<string, bool> isValid)
            => GetInput(question, isValid, null, (Func<string, bool>) null, true);

        public static string GetInput(string question, Func<string, bool> isValid, Func<string, string> mutate, bool allowCancel = true)
            => GetInput(question, isValid, mutate ?? (s => s), null, allowCancel);

        public static T GetInput<T>(string question, Func<string, bool> isValid, Func<string, T> mutate, bool allowCancel = true)
            => GetInput(question, isValid, mutate, null, allowCancel);

        public static T GetInput<T>(string question, Func<string, T> mutate, Func<T, bool> isValid = null, bool allowCancel = true)
            => GetInput(question, null, mutate, isValid, allowCancel);

        public static T GetInput<T>(string question, Func<string, bool> preValid, Func<string, T> mutate, Func<T, bool> postValid, bool allowCancel)
        {
            ValidateGetInput(question, mutate);

            if (allowCancel) ShowCancelPrompt();

            var meta = QuestionMeta(question, true);

            preValid = preValid ?? (s => true);
            postValid = postValid ?? (s => true);

            do
            {
                var resp = GetInputAsk(question, meta);

                if (allowCancel && string.Equals(resp, "c", StringComparison.CurrentCultureIgnoreCase)) return default;

                if (preValid(resp))
                {
                    var mut = mutate(resp);

                    if (postValid(mut)) return mut;
                }
            } while (true);
        }

        public static async Task<T> GetInput<T>(string question, Func<string, Task<bool>> preValid, Func<string, Task<T>> mutate, Func<T, Task<bool>> postValid, bool allowCancel)
        {
            ValidateGetInput(question, mutate);

            if (allowCancel) ShowCancelPrompt();

            var meta = QuestionMeta(question, true);

            preValid ??= (s => Task.FromResult(true));
            postValid ??= s => Task.FromResult(true);

            do
            {
                var resp = GetInputAsk(question, meta);

                if (allowCancel && string.Equals(resp, "c", StringComparison.CurrentCultureIgnoreCase)) return default;

                if (await preValid(resp))
                {
                    var mut = await mutate(resp);

                    if (await postValid(mut)) return mut;
                }
            } while (true);
        }

        private static string GetInputAsk(string question, (string question, Action setColor, Action resetColor, bool isMultiline, bool isEmpty) meta)
        {
            meta.setColor();

            if (meta.isMultiline || meta.isEmpty) Console.WriteLine($"{question}");
            else Console.Write($"{question} ");

            meta.resetColor();

            return InputOrScript();
        }


        private static void ValidateGetInput(string question, Delegate mutate)
        {
            if (question.IsNullOrWhitespace()) throw new ArgumentException($"{nameof(question)} cannot be null or empty");

            if (mutate == null) throw new ArgumentException($"{nameof(mutate)} cannot be null");
        }
    }
}