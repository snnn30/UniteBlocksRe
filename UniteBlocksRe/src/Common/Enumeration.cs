using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UniteBlocksRe.Common;

public abstract class Enumeration<T> : IComparable
    where T : Enumeration<T>
{
    public string Name { get; private set; }

    public int Id { get; private set; }

    protected Enumeration(int id, string name) => (Id, Name) = (id, name);

    private static readonly IReadOnlyList<T> _cachedAll = typeof(T)
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Select(f => f.GetValue(null))
        .Cast<T>()
        .ToList()
        .AsReadOnly();

    /// <summary>
    /// 定義されているすべての列挙値を列挙します。
    /// </summary>
    public static IReadOnlyList<T> GetAll() => _cachedAll;

    public static T FromId(int id) =>
        GetAll().FirstOrDefault(x => x.Id == id)
        ?? throw new ArgumentException($"Invalid ID for {typeof(T).Name}: {id}");

    public override string ToString() => Name;

    public override bool Equals(object obj) => obj is T other && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public int CompareTo(object obj) => obj is T other ? Id.CompareTo(other.Id) : 1;
}
