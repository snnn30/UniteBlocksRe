using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using R3;
using UniteBlocksRe.Models;
using UniteBlocksRe.Models.OperatingBlocks;
using UniteBlocksRe.Nodes.PlayScreen;
using UniteBlocksRe.Nodes.PlayScreen.Operation;

namespace UniteBlocksRe.Nodes.PlayerScene.Operation;

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

    private CancellationTokenSource _planCts;

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
                _ = Play();
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

    private async Task Play()
    {
        _planCts?.Cancel();
        _planCts = new CancellationTokenSource();

        var destination = _decisionMaker.GetBestDestination(
            _context.OperationManager.Item.Model,
            _context.Board.Model
        );

        var steps = destination.Steps.ToList();

        try
        {
            foreach (var step in steps)
            {
                if (step.Operation is StuckOperation stuck)
                {
                    _dropInput.Value = true;
                }
                else if (step.Operation is MoveOperation move)
                {
                    ResetInputState();
                    await Task.Delay(TimeSpan.FromSeconds(BaseThinkTime), _planCts.Token);
                    _moveInput.Value =
                        move.Direction == MoveDirection.Left ? MoveInput.Left : MoveInput.Right;
                    await _context
                        .OperationManager.OnOperationExecuted.Where(result =>
                            result.Type == OperationType.Move
                        )
                        .Take(step.Count)
                        .LastAsync();
                }
                else if (step.Operation is RotateOperation rotate)
                {
                    for (var i = 0; i < step.Count; i++)
                    {
                        ResetInputState();
                        await Task.Delay(TimeSpan.FromSeconds(BaseThinkTime), _planCts.Token);
                        _rotateInput.Value =
                            rotate.Direction == RotateDirection.ACW
                                ? RotateInput.ACW
                                : RotateInput.CW;
                        await _context
                            .OperationManager.OnOperationExecuted.Where(result =>
                                result.Type == OperationType.Rotate
                            )
                            .Take(1)
                            .LastAsync();
                    }
                }
                else if (step.Operation is DropOperation drop)
                {
                    ResetInputState();
                    await Task.Delay(TimeSpan.FromSeconds(BaseThinkTime), _planCts.Token);
                    _dropInput.Value = true;
                    await _context
                        .OperationManager.OnOperationExecuted.Where(result =>
                            result.Type == OperationType.Drop
                        )
                        .Take(step.Count)
                        .LastAsync();
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private void ResetInputState()
    {
        _moveInput.Value = MoveInput.None;
        _rotateInput.Value = RotateInput.None;
        _dropInput.Value = false;
    }
}
