using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RainWorldRandomizer.MiscClasses;

/// <summary>
/// A variation of <see cref="ConditionalWeakTable{TKey,TValue}"/> which allows the GC to reclaim values, as the version of Unity Rain World uses does not properly implement it.
/// </summary>
public class GCConditionalWeakTable<TKey, TValue> where TKey : class where TValue : class
{
    private static readonly IEqualityComparer<object> _comparer = new KeyComparer();
    private readonly Dictionary<object, TValue> _dict = new(_comparer);
    private int _lastGCCount = 0;
    private readonly List<KeyValuePair<object, TValue>> _toRemove = [];

    public TValue GetOrCreateValue(TKey obj)
    {
        if (obj is null) throw new ArgumentNullException(nameof(obj));

        if (TryGetValue(obj, out var v))
        {
            return v;
        }
        TValue value = Activator.CreateInstance<TValue>();
        _dict[new WeakRefWithHash(obj)] = value;
        CullDead();
        return value;
    }

    public bool Add(TKey key, TValue val)
    {
        if (_dict.ContainsKey(key)) return false;

        _dict[new WeakRefWithHash(key)] = val;
        CullDead();
        return true;
    }

    /// <summary>
    /// Taken from 1.5 Slugbase.
    /// </summary>
    private void CullDead()
    {
        // Assume the referenced objects are long-lived
        // Only cull dead refs when the garbage collector runs
        int gcCount = GC.CollectionCount(2);
        if (gcCount != _lastGCCount)
            _lastGCCount = gcCount;
        else return;

        // Search for dead references
        foreach (KeyValuePair<object, TValue> pair in _dict)
        {
            if (pair.Key is not WeakReference wr || (!wr.IsAlive))
                _toRemove.Add(pair);
        }
        // Remove them from the dictionary
        for (int i = _toRemove.Count - 1; i >= 0; i--)
        {
            _dict.Remove(_toRemove[i].Key);
        }
        _toRemove.Clear();
    }

    public bool TryGetValue(TKey obj, out TValue value)
    {
        if (obj == null)
        {
            value = default;
            return false;
        }
        return _dict.TryGetValue(obj, out value);
    }

    /// <summary>
    /// Taken from 1.5 Slugbase.
    /// </summary>
    private class WeakRefWithHash(object target) : WeakReference(target)
    {
        private readonly int hash = target.GetHashCode();
        public override int GetHashCode() => hash;
    }

    /// <summary>
    /// Taken from 1.5 Slugbase.
    /// </summary>
    private class KeyComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y)
        {
            // Treat WeakReferences and the objects they reference as equal
            // ... and treat WeakReferences that reference the same object as equal
            if (x is WeakReference wrx)
            {
                x = wrx.Target;
                if (!wrx.IsAlive) x = wrx;
            }
            if (y is WeakReference wry)
            {
                y = wry.Target;
                if (!wry.IsAlive) y = wry;
            }
            return x == y;
        }

        public int GetHashCode(object obj)
        {
            // WeakRefWithHash already overrides GetHashCode, so no change needs to be made
            return obj.GetHashCode();
        }
    }
}