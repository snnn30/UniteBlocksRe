using System.Threading.Tasks;

namespace UniteBlocksRe.Nodes.PlayScreen.Operation;

public record OperationResult(bool Sucess, Task Task, OperationType Type)
{
    public static OperationResult Succeeded(Task task, OperationType type) =>
        new(true, task ?? Task.CompletedTask, type);

    public static OperationResult Failed(OperationType type) =>
        new(false, Task.CompletedTask, type);
}
