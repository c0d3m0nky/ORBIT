using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Orbit
{
    public class Random
    {
        private static readonly char[] _defaultCharacterList =
        {
            '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?', '@', 'A', 'B', 'C', 'D', 'E',
            'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', '\\', ']', '^', '_', '`', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
            'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '{', '|', '}', '~', '¡', '¢', '£', '¤', '¥', '¦', '§', '¨', '©', 'ª', '«', '¬', '®', '¯', '°', '±',
            '²', '³', '´', 'µ', '¶', '·', '¸', '¹', 'º', '»', '¼', '½', '¾', '¿', 'À', 'Á', 'Â', 'Ã', 'Ä', 'Å', 'Æ', 'Ç', 'È', 'É', 'Ê', 'Ë', 'Ì', 'Í', 'Î', 'Ï', 'Ð', 'Ñ', 'Ò', 'Ó', 'Ô', 'Õ', 'Ö',
            '×', 'Ø', 'Ù', 'Ú', 'Û', 'Ü', 'Ý', 'Þ', 'ß', 'à', 'á', 'â', 'ã', 'ä', 'å', 'æ', 'ç', 'è', 'é', 'ê', 'ë', 'ì', 'í', 'î', 'ï', 'ð', 'ñ', 'ò', 'ó', 'ô', 'õ', 'ö', '÷', 'ø', 'ù', 'ú', 'û',
            'ü', 'ý', 'þ', 'ÿ'
        };

        public const string AsciiChars = "0123456789abcdefghijklmnopqrestuvwxyzABCDEFGHIJKLMNOPQRESTUVWXYZ";
        public const string Digits = "0123456789";
        public const string hex = "0123456789abcdef";
        public const string HEX = "0123456789ABCDEF";

        // DO NOT USE DIRECTLY, use RNG()
        private static RNGCryptoServiceProvider _rng;

        public static RNGCryptoServiceProvider RNG()
        {
            return _rng ?? (_rng = new RNGCryptoServiceProvider());
        }

        #region Statics

        // ReSharper disable BuiltInTypeReferenceStyle
        private static Dictionary<Type, NumericTypeMap> NumericTypes = new Dictionary<Type, NumericTypeMap>
        {
            {typeof(Int64), new NumericTypeMap(8, b => BitConverter.ToInt64(b, 0))},
            {typeof(Int32), new NumericTypeMap(4, b => BitConverter.ToInt32(b, 0))},
            {typeof(Int16), new NumericTypeMap(2, b => BitConverter.ToInt16(b, 0))},
            {typeof(Byte), new NumericTypeMap(1, b => b[0])},
            {
                typeof(SByte), new NumericTypeMap(1, ba =>
                {
                    var b = ba[0];

                    return sbyte.MaxValue - Convert.ToSByte(byte.MaxValue - b);
                })
            },
            {typeof(Boolean), new NumericTypeMap(1, b => b[0] > 127)},
            {typeof(UInt64), new NumericTypeMap(8, b => BitConverter.ToUInt64(b, 0))},
            {typeof(UInt32), new NumericTypeMap(4, b => BitConverter.ToUInt32(b, 0))},
            {typeof(UInt16), new NumericTypeMap(2, b => BitConverter.ToUInt16(b, 0))},

            {typeof(Single), new NumericTypeMap(4, b => BitConverter.ToSingle(b, 0))},
            {typeof(Double), new NumericTypeMap(8, b => BitConverter.ToDouble(b, 0))}
        };
        // ReSharper restore BuiltInTypeReferenceStyle

        public static string GenerateRandomString(int minLength, int maxLength) => GenerateRandomString(minLength, maxLength, _defaultCharacterList);

        public static string GenerateRandomString(int minLength, int maxLength, string chars) =>
            GenerateRandomString(minLength, maxLength, chars.ToCharArray());

        public static string GenerateRandomString(int minLength, int maxLength, Func<char, bool> charSelector) =>
            GenerateRandomString(minLength, maxLength, _defaultCharacterList.Where(charSelector).ToArray());

        public static string GenerateRandomString(int minLength, int maxLength, char[] chars)
        {
            if (minLength > maxLength || minLength < 1) throw new Exception($"Invalid number range: {minLength} to {maxLength}");

            var sb = new StringBuilder();

            const int maxLengthByteCount = 4;

            // Rather than rolling the rng multiple times we're going to gett all the randoms we will possibly need
            var randomBytes = new byte[maxLength + maxLengthByteCount];

            RNG().GetBytes(randomBytes);

            var randomInt = BitConverter.ToInt32(randomBytes.Take(maxLengthByteCount).ToArray(), 0);
            var randomLength = minLength == maxLength ? minLength : (randomInt % (maxLength - minLength)) + minLength;

            for (var bi = maxLengthByteCount; bi < randomBytes.Length; bi++)
            {
                sb.Append(chars[randomBytes[bi] % chars.Length]);
                if (sb.Length >= randomLength) break;
            }

            return sb.ToString();
        }

        public static byte[] Bytes(long byteLength)
        {
            var randomBytes = new byte[byteLength];

            RNG().GetBytes(randomBytes);

            return randomBytes;
        }

        public static T Number<T>() where T : IComparable
        {
            var t = typeof(T);

            if (!NumericTypes.ContainsKey(t)) throw new NotImplementedException($"{typeof(Random).FullName}.{nameof(Number)}<{t.Name}>() not supported");

            var map = NumericTypes[typeof(T)];
            var randomBytes = new byte[map.ByteLength];

            RNG().GetBytes(randomBytes);

            return (T) map.Convert(randomBytes);
        }

        private static int BytesToIntInRange(int min, int max, byte[] bytes)
        {
            var randomInt = BitConverter.ToInt32(bytes, 0);

            if (min >= 0) return (Math.Abs(randomInt) % (max - min)) + min;

            var uMin = ConvertIntToUIntRange(min);
            var uMax = ConvertIntToUIntRange(max);
            var uRandom = ConvertIntToUIntRange(randomInt);
            var uRandomInRange = (uRandom % (uMax - uMin)) + uMin;

            return ConvertUIntToIntRange(uRandomInRange);
        }

        public static int Number(int min, int max)
        {
            if (min == max) return min;

            if (min > max)
            {
                var t = min;

                min = max;
                max = t;
            }

            var randomBytes = new byte[4];

            RNG().GetBytes(randomBytes);

            return BytesToIntInRange(min, max, randomBytes);
        }

        public static int[] Numbers(int min, int max, int count)
        {
            if (count < 1) throw new ArgumentException("count must be greater than 0");

            var res = new List<int>();

            if (min == max)
            {
                for (int i = 0; i < count; i++) res.Add(min);
                return res.ToArray();
            }

            if (min > max)
            {
                var t = min;

                min = max;
                max = t;
            }

            var randomBytes = new byte[4 * count];

            RNG().GetBytes(randomBytes);

            for (int i = 0; i < count; i++)
            {
                var bytes = randomBytes.Skip(i * 4).Take(4).ToArray();

                res.Add(BytesToIntInRange(min, max, bytes));
            }

            return res.ToArray();
        }

        public static byte Number(byte min, byte max)
        {
            if (min == max) return min;

            if (min > max)
            {
                var t = min;

                min = max;
                max = t;
            }

            var randomBytes = new byte[1];

            RNG().GetBytes(randomBytes);

            return randomBytes[0];
        }

        public static short Number(short min, short max)
        {
            if (min == max) return min;

            if (min > max)
            {
                var t = min;

                min = max;
                max = t;
            }

            var randomBytes = new byte[2];

            RNG().GetBytes(randomBytes);

            var randomInt = BitConverter.ToInt32(randomBytes, 0);

            return Convert.ToInt16((randomInt % (max - min)) + min);
        }

        private static int ConvertUIntToIntRange(uint i)
        {
            return int.MaxValue - Convert.ToInt32(uint.MaxValue - i);
        }

        private static uint ConvertIntToUIntRange(int i)
        {
            if (i < 0) return uint.MinValue + Convert.ToUInt32(i - int.MinValue);
            else return uint.MaxValue - Convert.ToUInt32(int.MaxValue - i);
        }

        #endregion

        private readonly char[] _characterList;

        public Random()
        {
            _characterList = _defaultCharacterList;
        }

        public Random(string chars)
        {
            _characterList = chars.ToCharArray();
        }

        public Random(Func<char, bool> charSelector)
        {
            _characterList = _defaultCharacterList.Where(charSelector).ToArray();
        }

        public Random(char[] chars)
        {
            _characterList = chars;
        }

        public string GenerateString(int minLength, int maxLength) => GenerateRandomString(minLength, maxLength, _characterList);

        public byte[] RandomBytes(long byteLength) => Bytes(byteLength);

        public T Next<T>() where T : IComparable => Number<T>();

        public int Next(int min, int max) => Number(min, max);

        public short Next(short min, short max) => Number(min, max);

        public byte Next(byte min, byte max) => Number(min, max);

        private class NumericTypeMap
        {
            public NumericTypeMap(int byteLength, Func<byte[], object> convert)
            {
                ByteLength = byteLength;
                Convert = convert;
            }

            public int ByteLength { get; }
            public Func<byte[], object> Convert { get; }
        }
    }
}