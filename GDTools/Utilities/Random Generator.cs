﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GDTools.Utilities
{
    public static class Random_Generator
    {
        public static Random Random = new(int.Parse($"{DateTime.Now.Day}{DateTime.Now.Second}{DateTime.Now.Millisecond}"));
        public static string RandomString(int length)
        {
            const string chars = "123456789abcdefghjklmnopqrstuvwxyz";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Shuffles a list securely.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = Random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static long RandomLong(long min, long max)
        {
            long result = Between((Int32)(min >> 32), (Int32)(max >> 32));
            result = (result << 32);
            result = result | (long)Between((Int32)min, (Int32)max);
            return result;
        }
        public static string RandomUUID()
        {
            return $"{RandomLong(60000000, 99999999)}";
        }
        public static string RandomUDID()
        {
            return $"S" +
                $"{RandomLong(1000000000, 9999999999)}" +
                $"{RandomLong(1000000000, 9999999999)}" +
                $"{RandomLong(1000000000, 9999999999)}" +
                $"{RandomLong(1000000000, 9999999999)}";
        }
        public static int Between(int a, int b) { return Random.Next(a, b); }
    }
}
