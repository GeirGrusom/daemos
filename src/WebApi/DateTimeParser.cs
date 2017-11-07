// <copyright file="DateTimeParser.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Daemos.WebApi
{
    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;

    internal static class StringExtensions
    {
        public static string GetPeriodValue(this string input)
        {
            return input.Substring(0, input.Length - 1);
        }
    }

    public class DateTimeParser
    {
        private static readonly string[] DateFormats =
        {
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
            "yyyy'-'MM'-'dd'Z'"
        };

        private static readonly string[] DurationFormats =
        {
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
            "yyyy'-'MM'-'dd'Z'"
        };

        public static bool TryParseDateTime(string dateTime, out DateTime? result)
        {
            if (dateTime == null)
            {
                result = null;
                return true;
            }
            if (string.Equals(dateTime, "now", StringComparison.Ordinal))
            {
                result = DateTime.UtcNow;
                return true;
            }
            if (string.Equals(dateTime, "tomorrow", StringComparison.Ordinal))
            {
                result = DateTime.UtcNow.AddDays(1);
                return true;
            }

            if (DateTime.TryParseExact(dateTime, DateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out DateTime res))
            {
                result = res;
                return true;
            }
            else if (TryParseDuration(dateTime,DateTime.UtcNow, out result))
            {
                return true;
            }
            result = null;
            return false;

        }

        private static readonly Regex TimeSpanRegex = new Regex("^P([0-9]+Y)?([0-9]+M)?([0-9]+D)?(?:T([0-9]+H)?([0-9]+M)?([0-9]+S)?)?$", RegexOptions.Compiled);

        private static bool TryParseDuration(string duration, DateTime start, out DateTime? result)
        {
            if (duration == null)
            {
                result = null;
                return true;
            }

            var match = TimeSpanRegex.Match(duration);
            if (match.Success)
            {
                if (match.Groups[1].Success)
                {
                    start = start.AddYears(int.Parse(match.Groups[1].Value.GetPeriodValue(), CultureInfo.InvariantCulture));
                }
                if (match.Groups[2].Success)
                {
                    start = start.AddMonths(int.Parse(match.Groups[2].Value.GetPeriodValue(), CultureInfo.InvariantCulture));
                }
                if (match.Groups[3].Success)
                {
                    start = start.AddDays(int.Parse(match.Groups[3].Value.GetPeriodValue(), CultureInfo.InvariantCulture));
                }

                if (match.Groups[4].Success)
                {
                    start = start.AddHours(int.Parse(match.Groups[4].Value.GetPeriodValue(), CultureInfo.InvariantCulture));
                }

                if (match.Groups[5].Success)
                {
                    start = start.AddMinutes(int.Parse(match.Groups[5].Value.GetPeriodValue(), CultureInfo.InvariantCulture));
                }
                if (match.Groups[6].Success)
                {
                    start = start.AddSeconds(int.Parse(match.Groups[6].Value.GetPeriodValue(), CultureInfo.InvariantCulture));
                }

                result = start;
                return true;
            }

            result = null;
            return false;

        }
    }
}
