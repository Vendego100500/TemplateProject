
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = System.Random;

namespace Utils
{
    public static class Extensions
    {
        #region Random

        public static bool NextBool(this Random random)
        {
            return random.Next(2) == 0;
        }

        #endregion


        #region Dictionary

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
            where TValue : new()
        {
            if (dict.TryGetValue(key, out TValue val))
            {
                return val;
            }

            val = new TValue();
            dict.Add(key, val);

            return val;
        }

        #endregion

        
        #region String

        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static string ToColoredString(this object text, string colorKey)
        {
            return "<color=" + colorKey + "><b>" + text + "</b></color>";
        }

        #endregion


        #region Number

        public static string ToStringNumber(this int number)
        {
            return ((long)number).ToStringNumber();
        }

        public static string ToStringNumber(this float number)
        {
            return ((long)number).ToStringNumber();
        }

        public static string ToStringNumber(this long number)
        {
            return number switch
            {
                < 10000 => number.ToString(),
                < 100000 => number.ToStringNumber(1000, 1, "K"),
                < 1000000 => number / 1000 + "K",
                < 10000000 => number.ToStringNumber(1000000, 2, "M"),
                < 100000000 => number.ToStringNumber(1000000, 1, "M"),
                < 1000000000 => number / 1000000 + "M",
                < 10000000000 => number.ToStringNumber(1000000000, 2, "G"),
                < 100000000000 => number.ToStringNumber(1000000000, 1, "G"),
                < 1000000000000 => number / 1000000000 + "G",
                _ => "999G"
            };
        }

        private static string ToStringNumber(this long number, double divider, int roundDigits, string suffix)
        {
            return $"{RemoveRemainder(number / divider, roundDigits)}{suffix}";
        }

        private static double RemoveRemainder(this double number, int digits)
        {
            double divider = Math.Pow(10, digits);
            return Math.Truncate(number * divider) / divider;
        }

        public static string ToStringTime(this int number)
        {
            int hours = number / 3600;
            if (hours > 0)
            {
                return hours + "h : " + number / 60 + "m";
            }

            return number / 60 + "m : " + number % 60 + "s";
        }

        public static bool IsZero(this float number)
        {
            return -Mathf.Epsilon <= number && number <= Mathf.Epsilon;
        }

        #endregion


        #region Actions

        public static void InvokeSafe(this Action action)
        {
            action?.Invoke();
        }

        public static void InvokeSafe<T>(this Action<T> action, T value)
        {
            action?.Invoke(value);
        }

        public static void InvokeSafe<T1, T2>(this Action<T1, T2> action, T1 value1, T2 value2)
        {
            action?.Invoke(value1, value2);
        }

        public static void InvokeSafe<T1, T2, T3>(this Action<T1, T2, T3> action, T1 value1, T2 value2, T3 value3)
        {
            action?.Invoke(value1, value2, value3);
        }

        #endregion


        #region PointerEventData

        public static Vector3 GetWorldSpacePosition(this PointerEventData eventData)
        {
            if (eventData.pressEventCamera)
            {
                return eventData.pressEventCamera.ScreenToWorldPoint(eventData.position);
            }
            return eventData.position;
        }

        public static Vector3 GetWorldSpaceDelta(this PointerEventData eventData)
        {
            if (eventData.pressEventCamera)
            {
                return eventData.pressEventCamera.ScreenToWorldPoint(eventData.delta) -
                    eventData.pressEventCamera.ScreenToWorldPoint(Vector2.zero);
            }
            return eventData.delta;
        }

        #endregion
    }
}