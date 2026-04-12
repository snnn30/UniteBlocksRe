using System;
using System.Collections;
using System.Collections.Generic;

namespace UniteBlocksRe.Helpers;

public class BiMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
    private readonly Dictionary<TKey, TValue> _forward;
    private readonly Dictionary<TValue, TKey> _reverse;

    private readonly BiMap<TValue, TKey> _inverse;

    public BiMap()
    {
        _forward = [];
        _reverse = [];
        _inverse = new BiMap<TValue, TKey>(this);
    }

    // Inverse作成用のコンストラクタ
    // 型を逆に持っているものを受け取り、純のものを作る、という考え方で作成している
    private BiMap(BiMap<TValue, TKey> original)
    {
        _forward = original._reverse;
        _reverse = original._forward;
        _inverse = original;
    }

    public int Count => _forward.Count;

    public TValue this[TKey key] => _forward[key];

    public bool TryGetValue(TKey key, out TValue value) => _forward.TryGetValue(key, out value);

    public bool ContainsKey(TKey key) => _forward.ContainsKey(key);

    public BiMap<TValue, TKey> Inverse => _inverse;

    public bool RemoveByKey(TKey key)
    {
        if (!_forward.TryGetValue(key, out var value))
        {
            return false;
        }

        _forward.Remove(key);
        _reverse.Remove(value);
        return true;
    }

    public bool RemoveByValue(TValue value)
    {
        if (!_reverse.TryGetValue(value, out var key))
        {
            return false;
        }

        _reverse.Remove(value);
        _forward.Remove(key);
        return true;
    }

    public void Add(TKey key, TValue value)
    {
        if (_forward.ContainsKey(key))
        {
            throw new ArgumentException($"key {key} は既に存在している");
        }
        if (_reverse.ContainsKey(value))
        {
            throw new ArgumentException($"value {value} は既に存在している");
        }

        _forward[key] = value;
        _reverse[value] = key;
    }

    public void ForceAdd(TKey key, TValue value)
    {
        if (_forward.TryGetValue(key, out var oldValue))
        {
            _reverse.Remove(oldValue);
        }
        if (_reverse.TryGetValue(value, out var oldKey))
        {
            _forward.Remove(oldKey);
        }

        _forward[key] = value;
        _reverse[value] = key;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _forward.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
