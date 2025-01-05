using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Makaretu;

/// <summary>
///   Asserting an <see cref="Exception"/>.
/// </summary>
public static class ExceptionAssert
{
    public static async Task<T> ThrowsAsync<T>(Func<Task> action, string expectedMessage = null) where T : Exception
    {
        try
        {
            await action();
        }
        catch (AggregateException e)
        {
            var match = e.InnerExceptions.OfType<T>().FirstOrDefault();
            if (match == null)
                throw;

            if (expectedMessage != null)
                Assert.AreEqual(expectedMessage, match.Message, "Wrong exception message.");
            return match;

        }
        catch (T e)
        {
            if (expectedMessage != null)
                Assert.AreEqual(expectedMessage, e.Message);
            return e;
        }
        Assert.Fail("Exception of type {0} should be thrown.", typeof(T));

        //  The compiler doesn't know that Assert.Fail will always throw an exception
        return null;
    }

    public static async Task<Exception> ThrowsAsync(Func<Task> action, string expectedMessage = null)
    {
        return await ThrowsAsync<Exception>(action, expectedMessage);
    }
}