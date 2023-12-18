namespace auto_Bot_539;
using ChessChallenge.API;
using System;



public class Bot_539 : IChessBot
{
    int[] mg_weights = { 0, 82, 337, 365, 477, 1025, 0 };
    int[] eg_weights = { 0, 94, 281, 297, 512, 936, 0 };

    int[] gamephaseInc = { 0, 0, 1, 1, 2, 4, 0, };
    int[] kingMg = { -15, 36, 12, -54, 8, -28, 24, 14, };
    int[] pawns = { 0, -2, -1, 3, 3, 1, -2, 0, };
    int[] mg_piece_activity = { 0, -2, 5, 7, 2, -1, -1 };
    int[] eg_piece_activity = { 0, -1, 1, 1, 1, 1, 0 };

    const int infinity = 10000;
    const int MAX_MOVES = 20;
    const int MAX_WAIT_TIME = 200;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        Move bestMove = Move.NullMove;

        int bestEval = -infinity;
        int i = 1;

        for (; i < MAX_MOVES; i++)
        {
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int eval = -Search(board, i, -infinity, infinity);

                if (eval > bestEval)
                {
                    bestMove = move;
                    bestEval = eval;
                }
                board.UndoMove(move);
            }
            if (timer.MillisecondsElapsedThisTurn >= MAX_WAIT_TIME)
                break;
        }
        return bestMove != Move.NullMove ? bestMove : moves[0];
    }
    private int Evaluate(Board board)
    {

        int[] mg_eval = { 0, 0 };
        int[] eg_eval = { 0, 0 };
        int gamePhase = 0;

        PieceList[] pieceLists = board.GetAllPieceLists();

        foreach (PieceList pieces in pieceLists)
        {
            int color = Convert.ToInt32(pieces.IsWhitePieceList);
            PieceType pieceType = pieces.TypeOfPieceInList;

            if (pieceType == PieceType.King)
            {
                Square kingSquare = board.GetKingSquare(pieces.IsWhitePieceList);
                Square opponentKingSquare = board.GetKingSquare(!pieces.IsWhitePieceList);

                mg_eval[color] += kingMg[kingSquare.File];
                mg_eval[color] -= (pieces.IsWhitePieceList ? kingSquare.Rank : kingSquare.Rank ^ 7) * 10;
                mg_eval[color] -= BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(PieceType.Queen, kingSquare, board, pieces.IsWhitePieceList)) * 5;

                eg_eval[color] -= (kingSquare.Rank ^ 4) * 4;
                eg_eval[color] -= (kingSquare.File ^ 4) * 4;
                eg_eval[color] += 14 - (Math.Abs(kingSquare.File - opponentKingSquare.File) + Math.Abs(kingSquare.Rank - opponentKingSquare.Rank));
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                Piece piece = pieces.GetPiece(i);
                Square square = piece.Square;

                mg_eval[color] += mg_weights[(int)pieceType];
                eg_eval[color] += eg_weights[(int)pieceType];

                int activity = BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(pieceType, square, board, pieces.IsWhitePieceList));
                mg_eval[color] += activity * mg_piece_activity[(int)pieceType];
                eg_eval[color] += activity * eg_piece_activity[(int)pieceType];

                gamePhase += gamephaseInc[(int)pieceType];

                if (pieceType == PieceType.Pawn)
                {
                    int rankBonus = (pieces.IsWhitePieceList ? square.Rank : square.Rank ^ 7) * 7;

                    mg_eval[color] += rankBonus * pawns[square.File];

                    eg_eval[color] += rankBonus;
                }
                else if (pieceType == PieceType.Knight)
                {
                    eg_eval[color] += (-(square.Rank ^ 4) + 4) * 2;
                    eg_eval[color] += (-(square.File ^ 4) + 4) * 2;
                }
            }
        }

        int side2move = Convert.ToInt32(board.IsWhiteToMove);

        int mgScore = mg_eval[side2move] - mg_eval[side2move ^ 1];
        int egScore = eg_eval[side2move] - eg_eval[side2move ^ 1];

        int mgPhase = gamePhase;
        if (mgPhase > 24) mgPhase = 24;
        int egPhase = 24 - mgPhase;

        return (mgScore * mgPhase + egScore * egPhase) / 24;

    }

    private int Search(Board board, int depth, int alpha, int beta, bool onlyCaptures = false)
    {
        if (board.IsInCheckmate())
            return -infinity;

        if (board.IsDraw())
            return 0;

        if (depth == 0)
        {
            if (board.IsInCheck())
                return Search(board, 1, alpha, beta);

            return Search(board, 1, alpha, beta, true);
        }

        int eval = Evaluate(board);
        if (onlyCaptures)
        {
            if (eval >= beta)
                return beta;
            alpha = Math.Max(alpha, eval);
        }

        Move[] moves = GetLegalMovesSorted(board, onlyCaptures);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            if (onlyCaptures)
                eval = -Search(board, 1, -beta, -alpha, onlyCaptures);
            else
                eval = -Search(board, depth - 1, -beta, -alpha, onlyCaptures);
            board.UndoMove(move);

            if (eval >= beta)
                return beta;

            alpha = Math.Max(alpha, eval);
        }
        return alpha;

    }
    private Move[] GetLegalMovesSorted(Board board, bool CapturesOnly = false)
    {
        Move[] moves = board.GetLegalMoves(CapturesOnly);
        int[] scores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];

            if (move.IsCapture)
                scores[i] += 10 * mg_weights[(int)move.CapturePieceType] - mg_weights[(int)move.MovePieceType];

            if (move.IsPromotion)
                scores[i] += mg_weights[(int)move.PromotionPieceType];

            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
                scores[i] -= mg_weights[(int)move.MovePieceType];

        }
        for (int i = 0; i < moves.Length - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                int swapIndex = j - 1;
                if (scores[swapIndex] < scores[j])
                {
                    (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                    (scores[j], scores[swapIndex]) = (scores[swapIndex], scores[j]);
                }
            }
        }
        return moves;
    }

}
