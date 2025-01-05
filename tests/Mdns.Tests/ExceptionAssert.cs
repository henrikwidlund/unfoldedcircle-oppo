using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Makaretu.Mdns;

/// <summary>
///   Asserting an <see cref="Exception"/>.
/// </summary>
public static class ExceptionAssert
{
    public static async Task ThrowsAsync<T>(Func<Task> action, string expectedMessage = null) where T : Exception
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
            return;

        }
        catch (T e)
        {
            if (expectedMessage != null)
                Assert.AreEqual(expectedMessage, e.Message);
            return;
        }
        Assert.Fail("Exception of type {0} should be thrown.", typeof(T));
    }
}