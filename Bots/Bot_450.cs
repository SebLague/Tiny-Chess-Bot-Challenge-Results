namespace auto_Bot_450;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_450 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    PieceType[] pieces = System.Enum.GetValues(typeof(PieceType)).Cast<PieceType>().ToArray();
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    int[] pawnTable = { 0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
         5,  5, 10, 25, 25, 10,  5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5, -5,-10,  0,  0,-10, -5,  5,
         5, 10, 10,-20,-20, 10, 10,  5,
         0,  0,  0,  0,  0,  0,  0,  0
        };


    Board board;
    float lastTime = 0;
    int lastDepth;
    bool isWhite;
    int turn = 0;

    Random rand = new Random(0);


    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.isWhite = board.IsWhiteToMove;
        Move bestMove;

        // if(lastTime>0 && turn>30 && timer.MillisecondsRemaining / lastTime > 60) { lastDepth = 6; }
        // else if (lastTime > 0 && timer.MillisecondsRemaining / lastTime < 20) { lastDepth = 4; }

        if (timer.MillisecondsRemaining < 10000) { if (lastDepth == 6) DivertedConsole.Write("Changing to depth 4"); lastDepth = 4; }
        else { lastDepth = 4; }

        // DivertedConsole.Write(lastDepth + " - " + timer.MillisecondsRemaining / lastTime + " - " + lastTime);
        DivertedConsole.Write(turn);
        bestMove = SearchRoot(lastDepth);

        lastTime = timer.MillisecondsElapsedThisTurn;
        turn++;

        return bestMove;
    }

    private Move SearchRoot(int depth)
    {
        List<Move> moves = board.GetLegalMoves().ToList();
        int bestEvaluation = -1000000000;
        Move bestMove = moves[0];


        foreach (Move move in moves)
        {
            board.MakeMove(move);


            int evaluation = -Search(depth - 1, -1000000000, 1000000000, move);

            if (evaluation > bestEvaluation)
            {
                bestEvaluation = evaluation;
                bestMove = move;

            }

            board.UndoMove(move);
        }

        //DivertedConsole.Write(bestEvaluation);
        return bestMove;
    }

    private int Search(int depth, int alpha, int beta, Move currentMove)
    {
        if (depth == 0)
        {
            return Evaluate(currentMove);
        }

        List<Move> moves = board.GetLegalMoves().ToList();

        if (board.IsInCheckmate())
        {
            return -1000000000;
        }
        if (board.IsDraw())
        {
            return 50;
        }

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            int evaluation = -Search(depth - 1, -beta, -alpha, currentMove);

            board.UndoMove(move);

            if (evaluation >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, evaluation);
        }

        return alpha;
    }

    private int Evaluate(Move move)
    {
        int perspective = (isWhite) ? 1 : -1;

        int whiteEval = CountMaterial(true);
        int blackEval = CountMaterial(false);

        int evaluation = 3 * (whiteEval - blackEval) * perspective;

        evaluation += 2 * Activity();

        evaluation += MoveFlags(move) / 2;

        evaluation += KingToTheSide();

        return evaluation;
    }

    private int CountMaterial(bool isWhite)
    {
        int material = 0;
        int n_pawns = 0;
        for (int i = 1; i < 7; i++)
        {
            PieceList pieceList = board.GetPieceList(pieces[i], isWhite);
            int pieceCount = pieceList.Count;
            material += pieceValues[i] * pieceCount;

            if (pieces[i] == PieceType.Bishop && pieceCount == 2)
            {
                material += 50;
            }
            else if (pieces[i] == PieceType.Pawn)
            {
                n_pawns = pieceCount;
                material += pieceCount * pieceCount / 4;

                material += PieceSquare(isWhite, pawnTable, pieceList);
                material -= PieceSquare(!isWhite, pawnTable, board.GetPieceList(PieceType.Pawn, !isWhite));

            }
            else if (pieces[i] == PieceType.Rook)
            {
                material += 3 * (8 - n_pawns);
                if (pieceCount == 2)
                {
                    material -= 25;
                }
            }
            else if (pieces[i] == PieceType.Knight)
            {
                material += 3 * n_pawns;
                if (pieceCount == 2)
                {
                    material -= 25;
                }
            }
        }
        return material;
    }

    private int PieceSquare(bool isWhite, int[] table, PieceList list)
    {
        if (list.Count == 0) return 0;
        List<int> nFiles = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };

        int eval = 0;
        ulong bb = 0;
        foreach (Piece piece in list)
        {
            int square = isWhite ? piece.Square.File + 8 * (7 - piece.Square.Rank) : piece.Square.File + 8 * (piece.Square.Rank);
            eval += table[square];

            nFiles.Remove(piece.Square.File);
            bb |= BitboardHelper.GetPawnAttacks(piece.Square, isWhite);
        }

        bb &= board.GetPieceBitboard(PieceType.Pawn, isWhite);
        eval += 5 * BitboardHelper.GetNumberOfSetBits(bb);

        return eval / list.Count - 10 * (nFiles.Count() - 8 - list.Count);
    }

    private int Activity()
    {
        return board.GetLegalMoves().Length + 3 * board.GetLegalMoves(true).Length;
    }

    private int MoveFlags(Move move)
    {
        int eval = 0;

        if (turn < 12)
        {
            if (move.MovePieceType == PieceType.King && !move.IsCastles) { eval -= 100; }
            if (move.MovePieceType != PieceType.Pawn && (move.StartSquare.Rank > 0 && move.StartSquare.Rank < 7)) { eval -= 75; }
            if (move.MovePieceType == PieceType.Queen) { eval -= 75; }
        }

        return eval;
    }

    private int KingToTheSide()
    {
        int eval = 0;

        if (turn > 36)
        {
            Square kingS = board.GetPieceList(PieceType.King, !isWhite)[0].Square;
            eval += (int)Math.Pow(3.5 - kingS.Rank, 2) + (int)Math.Pow(3.5 - kingS.File, 2);
        }
        return 3 * eval;
    }
}