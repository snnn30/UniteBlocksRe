using System;

namespace UniteBlocksRe.Common;

public abstract class Entity<T> : IEquatable<Entity<T>>
    where T : Entity<T>
{
    private static int _nextId = 0;

    public int Id { get; init; }

    protected Entity()
    {
        Id = _nextId++;
    }

    protected Entity(int id)
    {
        Id = id;
    }

    public bool Equals(Entity<T> other) => other != null && Id == other.Id;

    public override bool Equals(object obj) => obj is Entity<T> other && Equals(other);

    public override int GetHashCode() => Id;

    public static bool operator ==(Entity<T> left, Entity<T> right) => Equals(left, right);

    public static bool operator !=(Entity<T> left, Entity<T> right) => !Equals(left, right);
}
