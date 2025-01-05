using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;

namespace Makaretu.Dns;

/// <summary>
///   Maintains a sequence of recent messages.
/// </summary>
/// <remarks>
///   <b>RecentMessages</b> is used to determine if a message has already been
///   processed within the specified <see cref="Interval"/>.
/// </remarks>
public class RecentMessages(TimeProvider timeProvider)
{
    private readonly TimeProvider _timeProvider = timeProvider;
    
    /// <summary>
    ///   Recent messages.
    /// </summary>
    /// <value>
    ///   The key is the Base64 encoding of the MD5 hash of 
    ///   a message and the value is when the message was seen.
    /// </value>
    private readonly ConcurrentDictionary<string, DateTime> _messages = new(StringComparer.OrdinalIgnoreCase);

    public RecentMessages() : this(TimeProvider.System) { }

    /// <summary>
    /// The number of messages.
    /// </summary>
    public int Count => _messages.Count;
    
    /// <summary>
    ///   The time interval used to determine if a message is recent.
    /// </summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(1);
    
    /// <summary>
    /// Checks if a message has been added to the recent message list.
    /// </summary>
    /// <param name="message">The message to look for.</param>
    public bool HasMessage(byte[] message) => _messages.ContainsKey(GetId(message));

    /// <summary>
    ///   Try adding a message to the recent message list.
    /// </summary>
    /// <param name="message">
    ///   The binary representation of a message.
    /// </param>
    /// <returns>
    ///   <b>true</b> if the message, did not already exist; otherwise,
    ///   <b>false</b> the message exists within the <see cref="Interval"/>.
    /// </returns>
    public bool TryAdd(byte[] message)
    {
        Prune();
        return _messages.TryAdd(GetId(message), _timeProvider.GetLocalNow().DateTime);
    }

    /// <summary>
    ///   Remove any messages that are stale.
    /// </summary>
    /// <returns>
    ///   The number messages that were pruned.
    /// </returns>
    /// <remarks>
    ///   Anything older than an <see cref="Interval"/> ago is removed.
    /// </remarks>
    public int Prune()
    {
        var dead = _timeProvider.GetLocalNow().DateTime - Interval;

        return _messages.Count(x => x.Value < dead && _messages.TryRemove(x.Key, out _));
    }

    /// <summary>
    ///   Gets a unique ID for a message.
    /// </summary>
    /// <param name="message">
    ///   The binary representation of a message.
    /// </param>
    /// <returns>
    ///   The Base64 encoding of the MD5 hash of the <paramref name="message"/>.
    /// </returns>
    public static string GetId(byte[] message) => Convert.ToBase64String(SHA1.HashData(message));
}