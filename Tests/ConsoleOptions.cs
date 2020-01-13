using System;
using System.IO;
using System.Net;
using Moq;
using NUnit.Framework;
using Orbit;

namespace Tests
{
    public class ConsoleOptions
    {
        [Test]
        public void MaskedInput()
        {
            var maskText = "This is a test";
            var i = 0;
            var overflowed = false;

            var console = new Mock<IConsole>();

            console.Setup(c => c.ReadKey()).Returns(() =>
            {
                var key = new ConsoleKeyInfo((char) 13, ConsoleKey.Enter, false, false, false);

                if (i < maskText.Length) key = new ConsoleKeyInfo(maskText[i], (ConsoleKey) maskText[i], false, false, false);
                else if (i > maskText.Length) overflowed = true;

                i++;
                return key;
            });

            console
                .Setup(c => c.WriteLine())
                .Callback(() => Console.WriteLine());

            console
                .Setup(c => c.WriteLine(It.IsAny<string>(), It.IsAny<Orbit.ConsoleOptions.Color>()))
                .Callback((string value, Orbit.ConsoleOptions.Color color) => Console.WriteLine(value));

            console
                .Setup(c => c.Write(It.IsAny<string>(), It.IsAny<Orbit.ConsoleOptions.Color>()))
                .Callback((string value, Orbit.ConsoleOptions.Color color) => Console.Write(value));

            Orbit.ConsoleOptions.Console = console.Object;

            var result = Orbit.ConsoleOptions.GetMaskedInput("This text will be masked: ");
            var resultStr = new NetworkCredential("", result).Password;

            Assert.False(overflowed, "Input overflowed");
            Assert.True(resultStr == maskText);
        }
    }
}