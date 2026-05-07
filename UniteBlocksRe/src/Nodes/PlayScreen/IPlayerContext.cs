using UniteBlocksRe.Nodes.PlayScreen.Operation;
using UniteBlocksRe.src.Nodes.PlayScreen;

namespace UniteBlocksRe.Nodes.PlayScreen;

public interface IPlayerContext
{
    NOperationManager OperationManager { get; }
    NBoard Board { get; }
    NBlockQueue Queue { get; }
    NBombGauge BombGauge { get; }
    NObstacleManager ObstacleManager { get; }
    NObstacleCounter ObstacleCounter { get; }
    IOperationInputSource InputSource { get; }
    IPlayerContext OpponentContext { get; }
}
