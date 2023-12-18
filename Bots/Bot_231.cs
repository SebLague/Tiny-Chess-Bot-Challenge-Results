namespace auto_Bot_231;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_231 : IChessBot
{
    //From https://www.chessprogramming.org/Simplified_Evaluation_Function#Piece-Square_Tables
    /*
    ulong[,] packedTables = {
            { 0x0000888822461121, 0x000419A0122B0000 }, //pawn
            { 0xEDCCDB00C043C132, 0xC032C143DB01EDCC }, //knight
            { 0xBAAAA000A012A112, 0xA042A222A100BAAA }, //bishop
            { 0x0000122290009000, 0x9000900090000001 }, //rook
            { 0xBAA9A000A0119011, 0x0011A111A010BAA9 }, //queen
            { 0xCDDECDDECDDECDDE, 0xBCCDABBB99999999 }, //king
        };
    */
    //int[] pieceValues = { 0, 100, 300, 320, 500, 900, 0 };

    int getPieceValue(PieceType piece) =>
            new[] { 0, 100, 300, 320, 500, 900, 0 }[(int)piece];

    int extractBonusSquare(int table, int index)
    {
        return new[] { 0, 5, 10, 15, 20, 25, 30, 40, 50, -5, -10, -20, -30, -40, -50 }[
            new ulong[,]
            {
            { 0x0000888822461121, 0x000419A0122B0000 }, //pawn
            { 0xEDCCDB00C043C132, 0xC032C143DB01EDCC }, //knight
            { 0xBAAAA000A012A112, 0xA042A222A100BAAA }, //bishop
            { 0x0000122290009000, 0x9000900090000001 }, //rook
            { 0xBAA9A000A0119011, 0x0011A111A010BAA9 }, //queen
            { 0xCDDECDDECDDECDDE, 0xBCCDABBB99999699 }, //king
        }[table, index / 32]
            << ((index / 8 * 4 + ((index & 7) ^ ((index & 7) > 3 ? 0b111 : 0))) % 16 * 4) >> 60];
    }

    ulong fileMask = 0x0101010101010101;

    //ulong getPawnMask(Square square, bool white) => fileMask << square.File + square.Rank * 8 * (white ? 1 : -1);

    int calculateDistanceFromSquare(Square square1, Square square2) => Math.Abs(square1.Rank - square2.Rank) + Math.Abs(square1.File - square2.File);

    public int EvaluateOneSide(Board board, bool white)
    {

        Square kingSqaure = board.GetKingSquare(white);

        //ulong pieceBitboard = board.WhitePiecesBitboard | board.BlackPiecesBitboard;

        float endgameWeight =
            1 - (Math.Clamp(BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard), 8, 24) - 8) / 16;
        float evaluation = 0;

        Square enemyKingSquare = board.GetKingSquare(!white);
        for (int i = 0; i < 6; i++)
        {
            foreach (Piece currentPiece in board.GetAllPieceLists()[i + (white ? 0 : 6)])
            {
                Square currentSqaure = currentPiece.Square;
                int[] endgamePawnBonusValues = { 0, -2, -2, 1, 2, 5, 8, 0 };
                int squareIndex = currentPiece.Square.Index ^ (white ? 0b111000 : 0);
                evaluation += getPieceValue(currentPiece.PieceType)
                    + (currentPiece.IsPawn || currentPiece.IsKing ? (1 - endgameWeight) : 1)
                    * pieceSquareValues[(int)currentPiece.PieceType - 1, squareIndex]
                    + (currentPiece.IsPawn && endgameWeight != 0 ?
                    +endgamePawnBonusValues[7 - squareIndex / 8] + 100 : 0) * endgameWeight;
            }
        }

        evaluation += (Math.Min(
            Math.Min(calculateDistanceFromSquare(kingSqaure, new Square(0))
            , calculateDistanceFromSquare(kingSqaure, new Square(7))),
            Math.Min(calculateDistanceFromSquare(kingSqaure, new Square(56))
            , calculateDistanceFromSquare(kingSqaure, new Square(63)))) - 6) * endgameWeight * 10
            + (board.GetPieceList(PieceType.Bishop, white).Count >= 2 ? 50 : 0)
            + (((white ? board.WhitePiecesBitboard : board.BlackPiecesBitboard) & (fileMask << kingSqaure.File)) == 0 ? 50 * (1 - endgameWeight) : 0)
            + calculateDistanceFromSquare(kingSqaure, enemyKingSquare) * 25 * endgameWeight;
        //string[] castleList = white ? new[] { "e1g1", "e1c1" } : new[] { "e8g8", "e8c8" };
        /*
        foreach (string castleMove in white ? new[] { "e1g1", "e1c1" } : new[] { "e8g8", "e8c8" })
        {
            if (board.GameMoveHistory.Contains(new Move(castleMove, board)))
            {
                evaluation += 50;
                break;
            }
        }
        */
        return (int)evaluation;

    }

    //List<Move> killerMoves = new List<Move>();
    //int scoreMove(Move move) => (move.IsCapture ? getPieceValue(move.CapturePieceType) - getPieceValue(move.MovePieceType) : 0)/* + (!move.IsCapture && killerMoves.Contains(move) ? 50 : 0)/* + (move.IsCastles ? 10 : 0)*/;
    /*
    public struct Transposition
    {
        public ulong zobristHash = 0;
        public int evaluation, depth, flag = 0;
        //1: UPPERBOUND
        //0: EXACT
        //-1: LOWERBOUND
        //-2: INVALID

        public Transposition(ulong zHash, int eval, int d, int f)
        {
            zobristHash = zHash;
            evaluation = eval;
            depth = d;
            flag = f;
        }
    };

    ulong TTMask = 0x7FFFFFF;
    Transposition[] transpositionTable = new Transposition[0x8000000];
    */
    //This code was from "Algorithms Explained – minimax and alpha-beta pruning" by Sebastian Lague
    //https://en.wikipedia.org/wiki/Negamax
    int[,] pieceSquareValues = new int[6, 64];
    Move bestMove;
    public int Search(Board board, Timer timer, int depth, int plyFromRoot, int alpha, int beta)
    {
        //if (timer.MillisecondsElapsedThisTurn > 1000 && depth != 4)
        //  return 0;
        Move[] moves = board.GetLegalMoves().OrderByDescending(move => move.IsCapture ? getPieceValue(move.CapturePieceType) - getPieceValue(move.MovePieceType) + 50 : 0).ToArray();
        //moves = moves.OrderByDescending(move => move.IsCapture ? getPieceValue(move.CapturePieceType) - getPieceValue(move.MovePieceType) + 50 : 0);

        //if (depth <= 2)
        //moves = moves.FindAll(m => m.IsCapture);


        if (board.IsInCheckmate())
            return -2147483647 + plyFromRoot;
        else if (board.IsDraw())
            return 0;


        if (depth == 0)
            //{
            //int eval = (EvaluateOneSide(board, true) - EvaluateOneSide(board, false)) * (board.IsWhiteToMove ? 1 : -1);
            //DivertedConsole.Write(board.CreateDiagram() + "Eval: " + eval);
            return (EvaluateOneSide(board, true) - EvaluateOneSide(board, false)) * (board.IsWhiteToMove ? 1 : -1);
        //}
        int bestEval = -2147483647;

        foreach (Move currentMove in moves)
        {
            /*
            if (currentMove.IsCapture)
            { 
                DivertedConsole.Write(depth + ": Capture");
            }
            */
            board.MakeMove(currentMove);
            int evaluation = -Search(board, timer, depth - 1, plyFromRoot + 1, -beta, -alpha);
            board.UndoMove(currentMove);

            if (evaluation > bestEval)
            {
                bestEval = evaluation;
                if (plyFromRoot == 0)
                    bestMove = currentMove;
            }

            alpha = Math.Max(alpha, bestEval);

            if (beta <= alpha)
            {
                //if (!currentMove.IsCapture)
                //killerMoves.Add(currentMove);
                break;
            }
        }
        return bestEval;
    }
    public Move Think(Board board, Timer timer)
    {
        //BitboardHelper.VisualizeBitboard(getPawnMask(board.GetPieceList(PieceType.Pawn, true)[0].Square, true)) ;
        Move[] moves = board.GetLegalMoves();
        bestMove = moves[0];
        //Play random opening move if bot has first move
        if (board.PlyCount == 0)
            return new Move(new[] { "e2e4", "d2d4", "g1f3" }[new Random().Next(3)], board);
        // Check if bot has only one move
        if (moves.Length == 1)
            return moves[0];
        for (int i = 0; i < 384; i++)
            pieceSquareValues[i / 64, i % 64] = extractBonusSquare(i / 64, i % 64);
        /*
        for (int i = 0; i < 8; i++)
        {
            string line = "";
            for (int j = 0; j < 8; j++)
            {
                line += extractBonusSquare(5, i * 8 + j);
            }
            DivertedConsole.Write(line);
        }
         */
        /*
        int depth = 4;
        Move bestMove = Move.NullMove;

        while (timer.MillisecondsElapsedThisTurn <= 1000 || depth == 4)
        {
            bestMove = bestMoveThisSearch;
            // Checkmate detection
            board.MakeMove(bestMove);
            if (moves.Length == 0)
                break;
            board.UndoMove(bestMove);
            DivertedConsole.Write(depth + ": " + bestMove + ", Eval: " + Search(board, timer, depth, 0, -2147483647, 2147483647));
            //Search(board, timer, depth, 0, -2147483647, 2147483647);
            depth++;
        }*/
        //DivertedConsole.Write(1 - (Math.Clamp(BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard), 8, 16) - 8) / 16);
        if (BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) <= 6)
            Search(board, timer, 6, 0, -2147483647, 2147483647);
        else
            Search(board, timer, 4, 0, -2147483647, 2147483647);
        return bestMove;
    }
}