using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using R3;
using UniteBlocksRe.Nodes;
using UniteBlocksRe.src.Models.Entities;
using UniteBlocksRe.src.Models.ValueObjects.BlocksOperation;
using UniteBlocksRe.src.Models.ValueObjects.Simulation;

namespace UniteBlocksRe.src.Nodes.PlayerScene.Operation;

public class EnemyInputSource : IOperationInputSource
{
    private readonly ReactiveProperty<MoveDirection> _moveDirection = new(MoveDirection.None);
    private readonly ReactiveProperty<RotateDirection> _rotateDirection = new(RotateDirection.None);
    private readonly ReactiveProperty<bool> _isDropActive = new(false);
    private readonly Subject<Unit> _switchBomb = new();

    public ReadOnlyReactiveProperty<MoveDirection> MoveDirectionState => _moveDirection;
    public ReadOnlyReactiveProperty<RotateDirection> RotateDirectionState => _rotateDirection;
    public ReadOnlyReactiveProperty<bool> IsDropActiveState => _isDropActive;
    public Observable<Unit> SwitchBomb => _switchBomb;

    private readonly OperatingBlocksEntity _operating;
    private readonly NOperationManager _manager;

    private CancellationTokenSource _loopCts;

    private const float BaseThinkTime = 0.3f;

    private readonly BoardEvaluationWeights _boardWeights;
    private ExplodeEvaluationWeights _explodeWeights;
    private readonly ActionSelector _actionSelector;

    public EnemyInputSource(OperatingBlocksEntity operating, NOperationManager manager)
    {
        _operating = operating;
        _manager = manager;

        _boardWeights = new BoardEvaluationWeights
        {
            BlockSizeWeight = 10f,
            SameColorAdjacentWeight = 10f,
            HeightPenalty = -4f,
            ObstaclePenalty = -20f,
        };
        _explodeWeights = new ExplodeEvaluationWeights { Weight = 10f };
        _actionSelector = new ActionSelector(_boardWeights, _explodeWeights);

        _manager.OnOperationExecuted.Subscribe(result =>
        {
            switch (result.Type)
            {
                case OperationType.Spawn:
                    StartNpcLoop();
                    break;
                case OperationType.Settle:
                    StopNpcLoop();
                    break;
            }
        });
    }

    private void StartNpcLoop()
    {
        _loopCts?.Cancel();
        _loopCts = new CancellationTokenSource();

        _ = StartThinkLoop(_loopCts.Token);
    }

    private void StopNpcLoop()
    {
        _loopCts?.Cancel();
        _moveDirection.Value = MoveDirection.None;
        _rotateDirection.Value = RotateDirection.None;
        _isDropActive.Value = false;
    }

    // プランを消化し終えるor操作失敗で再評価する
    private async Task StartThinkLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var best = _actionSelector.GetBestMoveBlock(_operating);
                var steps = best.Instructions.Steps;

                if (steps.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(BaseThinkTime), ct);
                    continue;
                }

                foreach (var step in steps)
                {
                    bool success = false;
                    if (step is DropStep drop)
                    {
                        success = await SendDropInputAndWait(drop, ct);
                    }
                    else if (step is MoveStep move)
                    {
                        success = await SendMoveInputAndWait(move, ct);
                    }
                    else if (step is RotateStep rotate)
                    {
                        success = await SendRotateInputAndWait(rotate, ct);
                    }

                    if (!success)
                    {
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(BaseThinkTime), ct);
                }
            }
        }
        catch (OperationCanceledException) { } // 正常終了
    }

    public async Task<bool> SendDropInputAndWait(DropStep dropStep, CancellationToken ct)
    {
        _isDropActive.Value = true;

        try
        {
            for (var i = 0; i < dropStep.Count; i++)
            {
                var result = await _manager
                    .OnOperationExecuted.Where(r => r.Type == OperationType.Drop)
                    .FirstAsync(ct);

                if (!result.Sucess)
                {
                    return false;
                }

                if (i == dropStep.Count - 1)
                {
                    await result.Task;
                }
            }

            return true;
        }
        finally
        {
            _isDropActive.Value = false;
        }
    }

    // Moveは長押しで繰り返し実行されるため、入力を固定する
    public async Task<bool> SendMoveInputAndWait(MoveStep moveStep, CancellationToken ct)
    {
        _moveDirection.Value = moveStep.Direction;

        try
        {
            for (var i = 0; i < moveStep.Count; i++)
            {
                var result = await _manager
                    .OnOperationExecuted.Where(r => r.Type == OperationType.Move)
                    .FirstAsync(ct);

                if (!result.Sucess)
                {
                    return false;
                }

                if (i == moveStep.Count - 1) // 最後だけアニメーションを待機
                {
                    await result.Task;
                }
            }

            return true;
        }
        finally
        {
            _moveDirection.Value = MoveDirection.None;
        }
    }

    // Rotateは長押しでも一回だけ実行なので、毎回入力を切り替える
    public async Task<bool> SendRotateInputAndWait(RotateStep rotateStep, CancellationToken ct)
    {
        try
        {
            for (var i = 0; i < rotateStep.Count; i++)
            {
                _rotateDirection.Value = rotateStep.Direction;
                var result = await _manager
                    .OnOperationExecuted.Where(r => r.Type == OperationType.Rotate)
                    .FirstAsync(ct);
                if (!result.Sucess)
                {
                    return false;
                }
                _rotateDirection.Value = RotateDirection.None;

                await result.Task;
            }
            return true;
        }
        finally
        {
            _rotateDirection.Value = RotateDirection.None;
        }
    }
}
