using System.Collections.Generic;
using UniteBlocksRe.src.Models.BoardServices;

namespace UniteBlocksRe.src.Models;

public static class BoardService
{
    public static FallResult Fall(BoardEntity board) => FallService.Execute(board);

    public static ExplodeResult Explode(BoardEntity board) => ExplodeService.Execute(board);

    public static UniteResult Unite(BoardEntity board) => UniteService.Execute(board);

    public static ObstaclePlaceResult ObstaclePlace(BoardEntity board, int count) =>
        ObstaclePlaceService.Execute(board, count);

    public static ProcessResult Process(BoardEntity board)
    {
        var history = new List<IProcessStep>();

        while (true)
        {
            var fall = Fall(board);
            if (fall.HasFalled)
            {
                history.Add(fall);
            }

            var unite = Unite(board);
            if (unite.HasUnited)
            {
                history.Add(unite);
            }

            var explode = Explode(board);
            if (explode.HasExploded)
            {
                history.Add(explode);
            }
            else
            {
                break;
            }
        }

        return new ProcessResult(history);
    }
}

public interface IProcessStep { }

public record ProcessResult(IEnumerable<IProcessStep> Steps);
