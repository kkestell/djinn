namespace Djinn.Metadata;

public abstract class AudioFile(Stream stream)
{
    protected readonly Stream AudioStream = stream;
    protected readonly Dictionary<string, object> Fields = [];

    public object? Get(Field field)
    {
        return Fields.TryGetValue(field.ToString(), out var value) ? value : null;
    }

    public void Set(Field field, object? value)
    {
        if (value is null)
        {
            Fields.Remove(field.ToString());
        }
        else if (Fields.ContainsKey(field.ToString()))
        {
            Fields[field.ToString()] = value;
        }
        else
        {
            Fields.Add(field.ToString(), value);
        }
    }

    public void Clear()
    {
        Fields.Clear();
    }
    
    public abstract void Load();

    public abstract Stream Save();
}
