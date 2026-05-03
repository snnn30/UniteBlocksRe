namespace UniteBlocksRe.Domain.Common;

public abstract record Entity
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public virtual bool Equals(Entity? other) => other != null && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}
