using System.Reflection;

namespace Bee.Base
{
    /// <summary>
    /// Extension methods for <see cref="Exception"/>.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Unwraps an exception to its core cause by removing common wrapper layers.
        /// <list type="bullet">
        /// <item><description>If the exception is an <see cref="AggregateException"/>, returns the first inner exception (after flattening).</description></item>
        /// <item><description>If the exception is a <see cref="TargetInvocationException"/>, returns its inner exception.</description></item>
        /// <item><description>Otherwise, returns the exception itself.</description></item>
        /// </list>
        /// </summary>
        /// <param name="ex">The exception to process.</param>
        /// <returns>The innermost exception; never null.</returns>
        public static Exception Unwrap(this Exception ex)
        {
            ArgumentNullException.ThrowIfNull(ex);

            while (true)
            {
                if (ex is AggregateException aex && aex.InnerExceptions.Count > 0)
                {
                    ex = aex.Flatten().InnerExceptions[0];
                    continue;
                }
                if (ex is TargetInvocationException tie && tie.InnerException != null)
                {
                    ex = tie.InnerException;
                    continue;
                }
                return ex;
            }
        }
    }
}
