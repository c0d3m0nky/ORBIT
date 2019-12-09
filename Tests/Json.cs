using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Orbit;

namespace Tests
{
    public class Json
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void Test1()
        {
            var fib = Series.Generate(new[] {1, 1}, c => c[c.Count - 1] + c[c.Count - 2], c => c.Count < 45);
            var arr = Series.Generate(0, 45).ToArray();
            var jArr = JArray.FromObject(arr);
            var fArr = arr.Where(t => fib.Contains(t)).ToArray();

            jArr.Remove(t => !fib.Contains(t.Value<int>()));

            Assert.True(jArr.Count == fArr.Length && jArr.All(t => fArr.Contains(t.Value<int>())));
        }


        [Test]
        public void Test2()
        {
            var fib = Series.Generate(new[] {1, 1}, c => c[c.Count - 1] + c[c.Count - 2], c => c.Count < 45);
            var arr = Series.Generate(0, 45).ToArray();
            var jObj = JObject.FromObject(arr.ToDictionary(t => t, t => "a"));
            var fArr = arr.Where(t => fib.Contains(t)).ToArray();

            jObj.Remove(p => !fib.Contains(p.Name.ParseInt().Value));

            Assert.True(jObj.Properties().Count() == fArr.Length && jObj.Properties().All(p => fArr.Contains(p.Name.ParseInt().Value)));
        }
    }
}