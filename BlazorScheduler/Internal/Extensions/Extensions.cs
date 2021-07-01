using System;
using System.Collections.Generic;
using System.Drawing;

namespace BlazorScheduler.Internal.Extensions
{
	internal static class Extensions
	{
        public static DateTime RoundUp(this DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }

        public static DateTime GetPrevious(this DateTime dt, DayOfWeek dayOfWeek, bool includeToday = true)
        {
            if (dt.DayOfWeek == dayOfWeek)
            {
                if (includeToday)
                {
                    return dt;
                }
                dt.AddDays(1);
            }
            int diff = (7 + (dt.DayOfWeek - dayOfWeek)) % 7;
            return dt.AddDays(-diff).Date;
        }

        public static DateTime GetNext(this DateTime dt, DayOfWeek dayOfWeek, bool includeToday = true)
        {
            if (dt.DayOfWeek == dayOfWeek)
            {
                if (includeToday)
                {
                    return dt;
                }
                dt.AddDays(-1);
            }
            return dt.AddDays(DayOfWeek.Saturday - dt.DayOfWeek).Date;
        }

        public static bool Overlaps<T>(this (T, T) dt, (T, T) other) where T : IComparable<T> =>
            dt.Item1.CompareTo(other.Item2) <= 0 && other.Item1.CompareTo(dt.Item2) <= 0;

        public static bool Between<T>(this T item, T start, T end, bool inclusive = true) where T : IComparable<T>
        {
            return inclusive
                ? item.CompareTo(start) >= 0 && item.CompareTo(end) <= 0
                : item.CompareTo(start) > 0 && item.CompareTo(end) < 0;
        }

        public static string ToRgbString(this Color color)
        {
            return $"rgb({color.R}, {color.G}, {color.B})";
        }

        public static string AsString(this IEnumerable<string> enumerable)
		{
            return string.Join(" ", enumerable);
		}

        public static Color GetAltColor(this Color color)
        {
            double luminance = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
            return luminance > 0.5 ? Color.Black : Color.White;
        }
    }
}
