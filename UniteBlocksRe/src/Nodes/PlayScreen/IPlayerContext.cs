using UniteBlocksRe.Nodes.PlayScreen.Operation;

namespace UniteBlocksRe.Nodes.PlayScreen;

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
