namespace Server.Helpers;

class TimedDict<TKey, TValue> {
    private readonly TimeSpan ttl = TimeSpan.FromMinutes(5);
    private readonly Dictionary<TKey, (TValue value, DateTime inserted)> dict = new();

    public void Set(TKey key, TValue value) {
        dict[key] = (value, DateTime.UtcNow);
    }

    public bool TryGet(TKey key, out TValue value) {
        if (dict.TryGetValue(key, out var entry)) {
            if (DateTime.UtcNow - entry.inserted < ttl) {
                value = entry.value;
                return true;
            }
            dict.Remove(key);
        }
        value = default;
        return false;
    }
    
    public void Remove(TKey key) {
        dict.Remove(key);
    }
}