using System;
using System.Threading.Tasks;

namespace UniteBlocksRe.Nodes.OperationItem;

/// <summary>
/// 操作の結果を保持するレコード。
/// </summary>
/// <param name="Sucess">操作が成功するかどうか。</param>
/// <param name="NewState">操作を適用した場合の新しい状態。</param>
/// <param name="Apply">
/// 状態の更新と演出アニメーションを実行する関数。
/// Sucessがfalseだった場合、Task.CompletedTaskを返す関数になる。
/// </param>
public record OperationResult(bool Sucess, OperationState NewState, Func<Task> Apply)
{
    public static OperationResult Succeeded(OperationState newState, Func<Task> apply) =>
        new(true, newState, apply ?? (() => Task.CompletedTask));

    public static OperationResult Failed(OperationState currentState) =>
        new(false, currentState, () => Task.CompletedTask);
}
