using UniteBlocksRe.Nodes;
using UniteBlocksRe.src.Nodes.PlayerScene.Operation;

namespace UniteBlocksRe.src.Nodes.PlayerScene;

public interface IPlayerContext
{
    NOperationManager OperationManager { get; }
    NBoard Board { get; }
    NBlockQueue Queue { get; }
    NBombGauge BombGauge { get; }
    NObstacleCounter ObstacleCounter { get; }
    IOperationInputSource InputSource { get; }
    IPlayerContext OpponentContext { get; }
}
