using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using R3;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.OperatingBlocks;
using UniteBlocksRe.src.Nodes.NodeExtensions;

namespace UniteBlocksRe.Nodes.PlayScreen.PlayerScene.Operation;

public class EnemyInputSource : IOperationInputSource
{
    private readonly ReactiveProperty<MoveInput> _moveInput = new(MoveInput.None);
    private readonly ReactiveProperty<RotateInput> _rotateInput = new(RotateInput.None);
    private readonly ReactiveProperty<bool> _dropInput = new(false);
    private readonly Subject<Unit> _switchInput = new();

    public ReadOnlyReactiveProperty<MoveInput> MoveInputState => _moveInput;
    public ReadOnlyReactiveProperty<RotateInput> RotateInputState => _rotateInput;
    public ReadOnlyReactiveProperty<bool> DropInputState => _dropInput;
    public Observable<Unit> SwitchInputState => _switchInput;

    private readonly IPlayerContext _context;
    private readonly NpcDecisionMaker _decisionMaker;

    private CancellationTokenSource? _planCts;

    private const float BaseThinkTime = 0.4f;

    public EnemyInputSource(IPlayerContext context, NpcDecisionMaker decisionMaker)
    {
        _context = context;
        _decisionMaker = decisionMaker;

        _context.OperationManager.OnOperationExecuted.Subscribe(result =>
        {
            if (result.Type == OperationType.Settle)
            {
                _planCts?.Cancel();
                ResetInputState();
            }
            else if (result.Type == OperationType.Spawn || !result.Sucess)
            {
                _ = ThinkInput();
            }
        });

        Observable
            .Interval(TimeSpan.FromSeconds(3))
            .Subscribe(_ =>
            {
                var useBomb = _decisionMaker.ShouldUseBomb(
                    _context.Board.Model,
                    _context.Queue.Model.Next.Parent,
                    _context.Queue.Model.Next.Child
                );
                if (context.BombGauge.IsBombActive != useBomb)
                {
                    _switchInput.OnNext(Unit.Default);
                }
            });
    }

    private async Task ThinkInput()
    {
        _planCts?.Cancel();
        _planCts = new CancellationTokenSource();

        if (_context.OperationManager.Item.Model == null)
        {
            throw new InvalidOperationException("_context.OperationManager.Item.Modelがnull");
        }

        var destination = _decisionMaker.GetBestDestination(
            _context.OperationManager.Item.Model,
            _context.Board.Model
        );

        try
        {
            foreach (var step in destination.Steps)
            {
                await ExecuteStepAsync(step, _planCts.Token);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task ExecuteStepAsync(StepInfo step, CancellationToken ct)
    {
        if (step.Operation is not StuckOperation)
        {
            ResetInputState();
            await TimerExtensions.Delay(TimeSpan.FromSeconds(BaseThinkTime), cancellationToken: ct);
        }

        switch (step.Operation)
        {
            case MoveOperation move:
                await ExecuteMove(move, step.Count, ct);
                break;
            case RotateOperation rotate:
                await ExecuteRotate(rotate, step.Count, ct);
                break;
            case DropOperation drop:
                await ExecuteDrop(step.Count, ct);
                break;
            case StuckOperation:
                _dropInput.Value = true;
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private async Task ExecuteMove(MoveOperation move, int count, CancellationToken ct)
    {
        _moveInput.Value = move.Direction == MoveDirection.Left ? MoveInput.Left : MoveInput.Right;
        await WaitForOperation(OperationType.Move, count, ct);
    }

    private async Task ExecuteRotate(RotateOperation rotate, int count, CancellationToken ct)
    {
        var direction = rotate.Direction == RotateDirection.ACW ? RotateInput.ACW : RotateInput.CW;

        // 回転は1回ずつ入力をリセットして送る必要があるためループ
        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                ResetInputState();
                await TimerExtensions.Delay(
                    TimeSpan.FromSeconds(BaseThinkTime),
                    cancellationToken: ct
                );
            }
            _rotateInput.Value = direction;
            await WaitForOperation(OperationType.Rotate, 1, ct);
        }
    }

    private async Task ExecuteDrop(int count, CancellationToken ct)
    {
        _dropInput.Value = true;
        await WaitForOperation(OperationType.Drop, count, ct);
    }

    private Task WaitForOperation(OperationType type, int count, CancellationToken ct)
    {
        return _context
            .OperationManager.OnOperationExecuted.Where(result => result.Type == type)
            .Take(count)
            .LastAsync(ct);
    }

    private void ResetInputState()
    {
        _moveInput.Value = MoveInput.None;
        _rotateInput.Value = RotateInput.None;
        _dropInput.Value = false;
    }
}
