using System;
using System.Diagnostics;

namespace Orbit {
    public static class DiagnosticsExtensions
    {
        public static Stopwatch Restart(this Stopwatch sw, Action first)
        {
            first?.Invoke();
            sw.Restart();
            return sw;
        }

    }
}