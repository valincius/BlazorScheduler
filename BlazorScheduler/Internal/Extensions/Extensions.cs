﻿using System;
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
            int diff = (7 + (dayOfWeek - dt.DayOfWeek)) % 7;
            return dt.AddDays(diff).Date;
        }

        public static bool Overlaps<T>(this (T start, T end) dt, (T start, T end) other) where T : IComparable<T> =>
            dt.start.CompareTo(other.end) <= 0 && other.start.CompareTo(dt.end) <= 0;

        public static bool Between<T>(this T item, T start, T end, bool inclusive = true) where T : IComparable<T>
        {
            return inclusive
                ? item.CompareTo(start) >= 0 && item.CompareTo(end) <= 0
                : item.CompareTo(start) > 0 && item.CompareTo(end) < 0;
        }

        public static string AsString(this IEnumerable<string> enumerable)
		{
            return string.Join(" ", enumerable);
		}

        private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int size)
        {
            using IEnumerator<TSource> e = source.GetEnumerator();
            while (e.MoveNext())
            {
                TSource[] chunk = new TSource[size];
                chunk[0] = e.Current;

                for (int i = 1; i < size; i++)
                {
                    if (!e.MoveNext())
                    {
                        Array.Resize(ref chunk, i);
                        yield return chunk;
                        yield break;
                    }

                    chunk[i] = e.Current;
                }

                yield return chunk;
            }
        }
        public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int size)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            return ChunkIterator(source, size);
        }
    }
}