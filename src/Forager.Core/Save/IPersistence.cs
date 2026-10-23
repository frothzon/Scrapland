namespace Forager.Core.Save;

/// <summary>Storage backend for save files (Godot <c>user://</c> in game, in-memory in tests).</summary>
public interface IPersistence
{
    bool Exists { get; }

    void Save(string json);

    bool TryLoad(out string json);

    void Delete();
}

/// <summary>Simple in-memory persistence for tests and previews.</summary>
public sealed class InMemoryPersistence : IPersistence
{
    private string? _payload;

    public bool Exists => _payload is not null;

    public string? Payload => _payload;

    public void Save(string json) => _payload = json;

    public bool TryLoad(out string json)
    {
        json = _payload ?? string.Empty;
        return _payload is not null;
    }

    public void Delete() => _payload = null;
}
