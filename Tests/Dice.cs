using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Random = Orbit.Random;

namespace Tests
{
    public class Dice
    {
        [SetUp]
        public void Setup() { }

        [Test]
        public void Test1()
        {
            var iters = 1000000;
            var raw = new List<StatRoll>();
            var attributeRolls = new List<(StatRoll[] rolls, int sum)>();

            for (int i = 0; i < iters; i++)
            {
                var attr = new StatRoll[6];

                for (int a = 0; a < 6; a++)
                {
                    var r = new StatRoll(FourD6Drop1());

                    raw.Add(r);
                    attr[a] = r;
                }

                attributeRolls.Add((attr, attr.Sum(a => a.Sum)));
            }

            var msg = $"Overall: {raw.Average(r => r.Sum)} Stat Set: {attributeRolls.Average(a => a.sum)}";
            
            Console.WriteLine(msg);
            
            Assert.Pass(msg);
        }

        private int[] ThreeD6() => D6(3);

        private int[] FourD6Drop1()
        {
            var r = D6(4).ToList();
            var m = r.Min();

            r.Remove(m);

            return r.ToArray();
        }

        private int[] D6(int d)
        {
            var r = new int[d];

            for (var i = 0; i < d; i++) r[i] = Random.Number(1, 6);

            return r;
        }

        private class StatRoll
        {
            public StatRoll(int[] rolls)
            {
                if (rolls.Length != 3) throw new Exception("Not a Stat Roll");

                Rolls = rolls;
                Sum = rolls.Sum();
            }

            public int[] Rolls { get; }
            public int Sum { get; }
        }
    }
}