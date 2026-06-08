using System.Text.Json;

namespace Dial.Sharp.Rest;

/// <summary>Invokes the DIAL code interpreter operations (<c>/v1/ops/code_interpreter</c>).</summary>
public interface IDialCodeInterpreter
{
    /// <summary>Invokes the given code interpreter <paramref name="operation"/> with <paramref name="payload"/>.</summary>
    Task<JsonElement> InvokeAsync(string operation, object payload, CancellationToken cancellationToken = default);
}
