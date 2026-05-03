using System.Reflection;

namespace UniteBlocksRe.Domain.Common;

public abstract class Enumeration<T> : IComparable
    where T : Enumeration<T>
{
    public string Name { get; private set; }

    public int Id { get; private set; }

    protected Enumeration(int id, string name) => (Id, Name) = (id, name);

    public static IEnumerable<T> All =>
        typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();

    public static T FromId(int id) =>
        All.FirstOrDefault(x => x.Id == id)
        ?? throw new ArgumentException($"Invalid ID for {typeof(T).Name}");

    public override string ToString() => Name;

    public override bool Equals(object? obj) => obj is T other && Id == other.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public int CompareTo(object? obj) => obj is T other ? Id.CompareTo(other.Id) : 1;
}
