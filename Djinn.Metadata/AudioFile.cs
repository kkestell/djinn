namespace Djinn.Metadata;

public abstract class AudioFile
{
    protected readonly Dictionary<string, object> _fields = [];

    public object? Get(Field field)
    {
        return _fields.TryGetValue(field.ToString(), out var value) ? value : null;
    }

    public void Set(Field field, object? value)
    {
        if (value is null)
        {
            _fields.Remove(field.ToString());
        }
        else if (_fields.ContainsKey(field.ToString()))
        {
            _fields[field.ToString()] = value;
        }
        else
        {
            _fields.Add(field.ToString(), value);
        }
    }

    public void Clear()
    {
        _fields.Clear();
    }

    public abstract void Load();

    public abstract void Save();
}
