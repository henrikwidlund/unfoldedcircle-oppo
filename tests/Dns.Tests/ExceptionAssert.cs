using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DnsTests;

/// <summary>
///   Asserting an <see cref="Exception"/>.
/// </summary>
public static class ExceptionAssert
{
    public static void Throws<T>(Action action, string expectedMessage = null) where T : Exception
    {
        try
        {
            action();
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
        catch (Exception e)
        {
            Assert.Fail("Exception of type {0} should be thrown not {1}.", typeof(T), e.GetType());
        }

        Assert.Fail("Expected Exception of type {0} but nothing was thrown.", typeof(T));
    }
}