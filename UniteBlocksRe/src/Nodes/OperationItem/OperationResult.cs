using System.Threading.Tasks;

namespace UniteBlocksRe.Nodes.OperationItem;

public record OperationResult(bool Sucess, Task Task)
{
    public static OperationResult Succeeded(Task task) => new(true, task ?? Task.CompletedTask);

    public static OperationResult Failed() => new(false, Task.CompletedTask);
}
