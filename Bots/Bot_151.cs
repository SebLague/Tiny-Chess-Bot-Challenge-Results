namespace auto_Bot_151;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_151 : IChessBot
{
    // Adds alpha-beta pruning in search
    public Bot_151()
    {
        for (int i = 0; i < 12; i++)
        {
            bonuses[i] = decode(i);
        }
    }

    struct Node
    {
        public Move? move;
        public int score;
        public Node(Move? move, int score)
        {
            this.move = move;
            this.score = score;
        }
    }

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 1, 3, 3, 5, 9, 10 };

    // encoded bonus tables into 4 bits each. basically a hex digit a square
    ulong[] bonuses4BitEncoded = {
        3679493948365249331, 3698354910480559923, 3702348336692815923, 3679759771450551091,
        8324507638868382896, 8616961243766044211, 8464402909655370870, 8536705626098334261,
        8473989754189755749, 8694967273287121481, 5173134702987290256, 7388325181603460980,
        9611370395500739034, 8468302649965992571, 3991708199661706410, 5189894196857322940,
        5076230116626220019, 3784562331441488326, 2695551116230764234, 610086452657121240,
        12049722898641358096, 6879907284540823885, 15828030931128108700, 12799320452428683447,
        4785078954081792, 282578821623040, 281475268729856, 281475269655040,
        11004222269478774916, 10779853804781800327, 6456644324026062922, 5362553090199083784,
        5883060895017956964, 3842300325061347153, 8157825962269506357, 3965485853230777350,
        6072354615139658957, 2901786392607235531, 11040369202050022847, 533702091712601245,
        5855652187812918181, 1306750580650650009, 3679855179141196665, 5761018906897562,
        5959888243263190896, 8473444396761344948, 7388624318532209559, 3997660986565438327,
    };
    int[][] bonuses = new int[12][];

    int[] decode(int row)
    {
        int[] decoded = new int[64];
        for (int index = 0; index < 64; index += 4)
        {
            for (int offset = 0; offset < 4; offset++)
            {
                decoded[index + offset] = (int)((bonuses4BitEncoded[4 * row + offset] & (0xFUL << index)) >> index);
            }
        }

        return decoded;
    }

    public Move Think(Board board, Timer timer)
    {
        // TODO: Make depth some function of remaining time and game progression
        Node bestMove = alphaBetaSearch(board, 4, int.MinValue, int.MaxValue, true);
        return bestMove.move ?? board.GetLegalMoves()[0];
    }

    Node alphaBetaSearch(Board board, int depth, int alpha, int beta, bool maximizing)
    {
        // search capture moves first, big a/b pruning speed up. 
        // use hashset for a faster way than simply sorting by IsCapture.
        // tested against quicksort below, which is much slower
        // List<Move> moves = board.GetLegalMoves().ToList();
        // moves.Sort((a, b) => a.IsCapture.CompareTo(b.IsCapture));

        List<Move> moves = board.GetLegalMoves(true).ToList();
        HashSet<Move> otherMoves = board.GetLegalMoves(false).ToHashSet();
        otherMoves.ExceptWith(moves);
        moves.AddRange(otherMoves);

        if (depth == 0 || moves.Count == 0)
            return new Node(null, scoreBoard(board));

        Move bestMove = moves[0];
        int value = maximizing ? int.MinValue : int.MaxValue;

        foreach (var m in moves)
        {
            // todo: turn into negamax, i couldn't get it working :(
            if (maximizing)
            {
                board.MakeMove(m);
                value = Math.Max(value, alphaBetaSearch(board, depth - 1, alpha, beta, false).score);
                board.UndoMove(m);
                if (value > beta) break;
                if (value > alpha)
                {
                    alpha = value;
                    bestMove = m;
                }
            }
            else
            {
                board.MakeMove(m);
                value = Math.Min(value, alphaBetaSearch(board, depth - 1, alpha, beta, true).score);
                board.UndoMove(m);
                if (value < alpha) break;
                if (value < beta)
                {
                    beta = value;
                    bestMove = m;
                }

            }
        }

        return new Node(bestMove, value);
    }

    // TODO: Add some memoisation to `scoreBoard`
    int scoreBoard(Board board)
    {
        if (board.IsInCheckmate()) return int.MaxValue;

        var pieceLists = board.GetAllPieceLists();
        int[] scores = { 0, 0 };
        int[] pieceCounts = { 0, 0 };

        foreach (var pieceList in pieceLists)
            foreach (var piece in pieceList)
            {
                var colorIndex = piece.IsWhite ? 0 : 1;
                scores[colorIndex] += 100 * pieceValues[(int)piece.PieceType] + 3 * rankPosition(piece, pieceCounts[0] <= 3 || pieceCounts[1] <= 3);
                if (!piece.IsKing && !piece.IsPawn) pieceCounts[colorIndex]++;
            }

        var moveIndex = board.IsWhiteToMove ? 0 : 1;
        return scores[moveIndex] - scores[1 - moveIndex];
    }

    int rankPosition(Piece p, bool endgame)
    {
        var endGameIndex = endgame ? 6 : 0;
        return bonuses[endGameIndex + (int)p.PieceType - 1][p.Square.Index];
    }
}
