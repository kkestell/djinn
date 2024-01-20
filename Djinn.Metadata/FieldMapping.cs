namespace Djinn.Metadata;

public record FieldMapping(Field Field, string[] Keys, Type Type)
{
    public bool Matches(string key)
    {
        return Keys.Any(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
    }
}
