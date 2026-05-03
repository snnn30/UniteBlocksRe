using UniteBlocksRe.Domain.Boards.Operations;

namespace UniteBlocksRe.Domain;

public static class BoardService
{
    public static FallResult Fall(Board state) => FallService.Execute(state);

    public static ExplodeResult Explode(Board state) => ExplodeService.Execute(state);

    public static UniteResult Unite(Board state) => UniteService.Execute(state);

    public static ObstaclePlaceResult ObstaclePlace(Board state, int count) =>
        ObstaclePlaceService.Execute(state, count);

    public static (Board FinalState, IReadOnlyList<BoardOperationStep> History) Process(
        Board initialBoard
    )
    {
        var history = new List<BoardOperationStep>();
        var currentBoard = initialBoard;

        while (true)
        {
            var fall = Fall(currentBoard);
            if (fall.HasFalled)
            {
                history.Add(fall);
                currentBoard = fall.After;
            }

            var unite = Unite(currentBoard);
            if (unite.HasUnited)
            {
                history.Add(unite);
                currentBoard = unite.After;
            }

            var explode = Explode(currentBoard);

            if (explode.HasExploded)
            {
                history.Add(explode);
                currentBoard = explode.After;
            }
            else
            {
                break;
            }
        }

        return (currentBoard, history);
    }
}
