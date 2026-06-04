using System.ClientModel.Primitives;

namespace Dial.Sharp;

/// <summary>
/// Extension hook for adding <see cref="PipelinePolicy"/> instances to DIAL outbound requests.
/// </summary>
public sealed class DialRequestPolicies
{
    private static readonly Entry[] Empty = [];
    private Entry[] _entries = Empty;

    public void AddPolicy(PipelinePolicy policy, PipelinePosition position = PipelinePosition.PerCall)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var newEntry = new Entry(policy, position);

        while (true)
        {
            var current = Volatile.Read(ref _entries);
            var updated = new Entry[current.Length + 1];
            Array.Copy(current, updated, current.Length);
            updated[current.Length] = newEntry;

            if (Interlocked.CompareExchange(ref _entries, updated, current) == current)
            {
                return;
            }
        }
    }

    internal void ApplyTo(RequestOptions requestOptions)
    {
        var snapshot = Volatile.Read(ref _entries);
        foreach (var entry in snapshot)
        {
            requestOptions.AddPolicy(entry.Policy, entry.Position);
        }
    }

    private readonly struct Entry(PipelinePolicy policy, PipelinePosition position)
    {
        public PipelinePolicy Policy { get; } = policy;
        public PipelinePosition Position { get; } = position;
    }
}