using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Orbit
{
    public static class ConsoleOptions
    {
        public enum Color
        {
            Default = -1,
            Ask,
            AskDivider,
            Cancel,
            Info,
            Error
        }

        public static IEnumerator<string> Script = null;
        public static TimeSpan ScriptSleep = TimeSpan.Zero;
        public static IConsole Console = new DefaultConsole();
        
        private const string ScriptPauseChar = "*";
        private static bool CancelPromptShown = false;

        private static void ShowCancelPrompt()
        {
            if (!CancelPromptShown)
            {
                Console.WriteLine();
                Console.Write("When you see prompts in ", Color.Info);
                Console.Write(Console.CancelColor.ToString(), Color.Cancel);
                Console.Write(" it means you can enter 'c' to cancel", Color.Info);
                Console.WriteLine();
                CancelPromptShown = true;
            }
        }

        private static (string question, bool isMultiline, bool isEmpty, Color color) QuestionMeta(string question, bool allowCancel)
        {
            question = question.IfNullOrWhitespace("").Trim();

            return (question, question.Contains("\n"), question.IsNullOrWhitespace(), allowCancel ? Color.Cancel : Color.Default);
        }

        internal static SecureString InputMaskedOrScript()
        {
            var ss = new SecureString();

            if (HandledByScript(out var resp)) resp.ForEach(c => ss.AppendChar(c));
            else
            {
                do
                {
                    var key = Console.ReadKey();

                    if (key.Key == ConsoleKey.Enter) break;

                    if (key.Key == ConsoleKey.Escape) return null;

                    if (key.Key == ConsoleKey.Backspace)
                    {
                        if (ss.Length > 0)
                        {
                            ss.RemoveAt(ss.Length - 1);
                            Console.Write("\b \b", Color.Default);
                        }
                    }
                    else
                    {
                        ss.AppendChar(key.KeyChar);
                        Console.Write("*", Color.Default);
                    }
                } while (true);
            }

            return ss;
        }

        internal static string InputOrScript()
        {
            return !HandledByScript(out var resp) ? Console.ReadLine() : resp;
        }

        private static bool HandledByScript(out string r)
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

                if (r == ScriptPauseChar)
                {
                    Console.Write("Script Paused. Enter to continue, Ctrl+c to end", Color.Default);
                    Console.ReadLine();
                }
            } while (r != ScriptPauseChar);

            Console.WriteLine(r, Color.Default);
            if (ScriptSleep > TimeSpan.Zero) Thread.Sleep((int) ScriptSleep.TotalMilliseconds);

            return true;
        }

        public static bool? YesNoCancel(string question)
        {
            ShowCancelPrompt();
            bool? yn = null;
            var meta = QuestionMeta(question, true);

            do
            {
                if (meta.isMultiline) Console.WriteLine($"{meta.question}", meta.color);
                else Console.Write($"{meta.question} ", meta.color);

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
                if (meta.isMultiline) Console.WriteLine($"{question}", meta.color);
                else Console.Write($"{question} ", meta.color);

                yn = InputOrScript().ParseBool();
            } while (!yn.HasValue);

            return yn.Value;
        }

        public static Dictionary<string, object> GetFields(string question, Func<string, string> mutate = null, Func<string, bool> isValid = null, bool allowCancel = true)
        {
            var meta = QuestionMeta(question, allowCancel);
            var explain = $"Enter fields as key:value";

            Console.WriteLine(question, meta.color);
            Console.WriteLine(explain, meta.color);

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
                    Console.WriteLine();
                    Console.WriteLine("Invalid entry", Color.Error);
                    Console.WriteLine(explain, Color.Error);
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
            => GetInput(question, null, s => s, null, true);

        public static string GetInput(string question, Func<string, bool> isValid)
            => GetInput(question, isValid, s => s, (Func<string, bool>) null, true);

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

            var meta = QuestionMeta(question, allowCancel);

            preValid ??= (s => true);
            postValid ??= (s => true);

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

            var meta = QuestionMeta(question, allowCancel);

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

        private static string GetInputAsk(string question, (string question, bool isMultiline, bool isEmpty, Color color) meta)
        {
            if (meta.isMultiline || meta.isEmpty) Console.WriteLine($"{question}", meta.color);
            else Console.Write($"{question} ", meta.color);

            return InputOrScript();
        }

        public static SecureString GetMaskedInput(string question)
        {
            ValidateGetInput(question, (Func<string, string>) (s => s));

            var meta = QuestionMeta(question, false);

            if (meta.isMultiline || meta.isEmpty) Console.WriteLine($"{question}", meta.color);
            else Console.Write($"{question} ", meta.color);

            return InputMaskedOrScript();
        }

        private static void ValidateGetInput(string question, Delegate mutate)
        {
            if (question.IsNullOrWhitespace()) throw new ArgumentException($"{nameof(question)} cannot be null or empty");

            if (mutate == null) throw new ArgumentException($"{nameof(mutate)} cannot be null");
        }
    }
}