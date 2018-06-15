using System;
using System.Collections.Generic;

namespace Gear.Components
{
    public static class ExceptionExtensions
    {
        static string DetailException(Exception ex, int indent)
        {
            var exceptionMessages = new List<string>();
            var top = true;
            while (ex != default)
            {
                var indentation = new string(' ', indent * 3);
                if (string.IsNullOrWhiteSpace(ex.StackTrace))
                    exceptionMessages.Add($"{indentation}{(top ? "-- " : "   ")}{ex.GetType().Name}: {ex.Message}".Replace($"{Environment.NewLine}", $"{Environment.NewLine}{indentation}"));
                else
                    exceptionMessages.Add($"{indentation}{(top ? "-- " : "   ")}{ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}".Replace($"{Environment.NewLine}", $"{Environment.NewLine}{indentation}"));
                if (ex is AggregateException aggregateEx)
                {
                    foreach (var aggregatedEx in aggregateEx.InnerExceptions)
                        exceptionMessages.Add(DetailException(aggregatedEx, indent + 1));
                    break;
                }
                else
                    ex = ex.InnerException;
                top = false;
            }
            return string.Join(string.Join("{0}{0}", Environment.NewLine), exceptionMessages);
        }

        /// <summary>
        /// Creates an indented textual representation of an exception and all of its inner exceptions, including exception types, messages, and stack traces, and traversing multiple inner exceptions in the case of <see cref="AggregateException"/>
        /// </summary>
        /// <param name="ex">The exception for which to generate the textual representation</param>
        public static string GetFullDetails(this Exception ex) => DetailException(ex, 0);
    }
}
