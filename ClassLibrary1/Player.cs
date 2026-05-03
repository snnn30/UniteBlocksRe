namespace UniteBlocksRe.Domain;

public class Player // 後々可変エンティティにする
{
    public OperatingBlockPair? Operating { get; private set; }
    public Board Board { get; private set; }
    public BombGauge BombGauge { get; private set; }
    public BlockPairQueue Queue { get; private set; }

    public Player(Board board, BombGauge bombGauge, BlockPairQueue queue)
    {
        Board = board;
        BombGauge = bombGauge;
        Queue = queue;
        Operating = null;
    }

    public void Tick(float deltaTime)
    {
        BombGauge.Tick(deltaTime);
    }

    public bool Spawn()
    {
        if (Operating != null)
        {
            throw new InvalidOperationException("まだ操作中のオブジェクトが存在している");
        }
        if (BombGauge.IsReady)
        {
            if (OperatingBlockPair.TrySpawnSingle(new BombBlock(), Board, out var spawned))
            {
                Operating = spawned;
                return true;
            }
            return false;
        }
        else
        {
            var queue = Queue.Dequeue(out var pair);
            if (OperatingBlockPair.TrySpawnDouble(pair.Parent, pair.Child, Board, out var spawned))
            {
                Operating = spawned;
                Queue = queue;
                return true;
            }
            return false;
        }
    }
}
