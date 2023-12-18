namespace auto_Bot_432;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_432 : IChessBot
{
    private int _depth = 7;
    private int _max_depth = 254;
    private int _transposition_depth;
    private readonly ulong[,] _positionalWeights =
    {
        {8683547591809433600, 1388309562400055824, 3915129336154501941, 384307146728703573, 2781419601154818592, 10986381248880408745},
        {6150110960405533511, 2623195334416164162, 3784827174736258659, 384307145385268560, 7393108935958960484, 7224376637290534231},
        {8613882468073314150, 2618391568115529282, 3771313077301437779, 384307145385268560, 2781422900838242664, 4841373122423685940},
        {4294967295, 81665195684152369, 230584288054363203, 8608480568053246632, 308641985531897444, 3611889320058683955}
    };

    private Board board;
    private int _color;
    public struct Transposition
    {
        public ulong ZobristKey;
        public Move move;
        public int evaluation;
        public byte depth;
        public byte flag;
    };

    Transposition[] m_TPTable;

    public Bot_432()
    {
        m_TPTable = new Transposition[0x800000];
    }

    public Move Think(Board board, Timer timer)
    {

        this.board = board;
        _color = board.IsWhiteToMove ? 1 : -1;
        int alpha = -80000000, beta = 80000000;
        _transposition_depth = 0;
        int maxTime = (timer.MillisecondsRemaining + timer.IncrementMilliseconds) / 60;
        for (int i = 1; i <= _depth; i++)
        {
            DivertedConsole.Write(i);
            Search(i, _color, alpha, beta);
            if (timeisok(maxTime, timer)) { break; }
        }

        return m_TPTable[board.ZobristKey & 0x7FFFFF].move;
    }

    bool timeisok(int maxTime, Timer timer)
    {
        return (timer.MillisecondsRemaining >= 10000) ? maxTime <= timer.MillisecondsElapsedThisTurn : maxTime <= 2 * timer.MillisecondsElapsedThisTurn;
    }

    int Search(int depth, int color, int alpha, int beta)
    {
        int startAlpha = alpha;
        ref Transposition transposition = ref m_TPTable[board.ZobristKey & 0x7FFFFF];
        if (transposition.ZobristKey == board.ZobristKey && transposition.depth >= depth)
        {
            if (transposition.flag == 1)
            {
                if (_max_depth >= _transposition_depth)
                {
                    _transposition_depth++;

                    board.MakeMove(transposition.move);
                    transposition.evaluation = -Search(depth, -color, -beta, -alpha);
                    board.UndoMove(transposition.move);
                }

                return transposition.evaluation;

            }
            else if (transposition.flag == 2)
            {

                alpha = Math.Max(alpha, transposition.evaluation);
            }
            else if (transposition.flag == 3)
            {
                beta = Math.Min(beta, transposition.evaluation);
            }
            if (alpha >= beta)
            {
                return transposition.evaluation;
            }
        }

        if (board.IsDraw())
        {
            if (board.PlyCount > 34)
            {
                return 0;
            }
            else
            {
                return 2000;
            }
        }
        if (board.IsInCheckmate())
        {
            return board.PlyCount - 5000000;
        }
        if (depth <= 0) { return Eval() * color; }

        Move[] moves = board.GetLegalMoves();
        OrderMoves(ref moves);
        Move bestMove = moves[0];
        int result = int.MinValue / 2;
        int temp;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            temp = -Search(depth - 1, -color, -beta, -alpha);
            board.UndoMove(move);

            if (temp > result)
            {
                result = temp;
                bestMove = move;
            }

            alpha = Math.Max(result, alpha);
            if (alpha >= beta)
            {
                break;
            }
        }

        transposition.evaluation = result;
        transposition.ZobristKey = board.ZobristKey;
        transposition.move = bestMove;
        if (result <= startAlpha)
        {
            transposition.flag = 3;
        }
        else if (result >= beta)
        {
            transposition.flag = 2;
        }
        else
        {
            transposition.flag = 1;
        }
        transposition.depth = (byte)(depth);

        return result;
    }



    void OrderMoves(ref Move[] otherMoves)
    {
        bool[] sorter = otherMoves.Select(move => !move.IsCapture).ToArray();
        Array.Sort(sorter, otherMoves);
    }

    int Eval()
    {
        int result = 0;
        ulong piecesBitboard = board.AllPiecesBitboard;
        while (piecesBitboard > 0)
        {
            int white = 1;
            int blackShift = 0;
            Square currentSquare = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref piecesBitboard));
            Piece currentPiece = board.GetPiece(currentSquare);
            if (!currentPiece.IsWhite)
            {
                white = -1;
                blackShift = 7;
            }

            var index1 = (int)white * (currentSquare.Rank - blackShift) / 2;
            var index2 = (int)currentPiece.PieceType - 1;

            result +=
                (((int)(_positionalWeights[index1, index2] >> (4 * currentSquare.File + (white * (currentSquare.Rank - blackShift)) % 2 * 32)) & 15)
                + BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(currentPiece.PieceType, currentSquare, board, currentPiece.IsWhite))
                + PieceTypeValue(currentPiece.PieceType)) * white;

        }
        return result * 5;
    }
    int PieceTypeValue(PieceType pieceType)
    {
        switch ((int)pieceType)
        {
            case 1: return 20;
            case 2: return 60;
            case 3: return 60;
            case 4: return 100;
            case 5: return 180;
            default: return 620;
        }
    }
}