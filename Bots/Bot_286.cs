namespace auto_Bot_286;
using ChessChallenge.API;
using System;
using System.Linq;
using static System.Math;

public class Bot_286 : IChessBot
{
    const int positiveInfinity = 9999999;
    const int negativeInfinity = -positiveInfinity;
    int baseDepth = 3;

    public Move Think(Board board, Timer timer)
    {
        System.Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);
        MoveOrder(board, moves);
        int bestScore = negativeInfinity;
        int bestMoveIndex = 0;
        bool lowtime = timer.MillisecondsRemaining < timer.OpponentMillisecondsRemaining &&
            timer.MillisecondsRemaining < 10000;
        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);
            int evaluation = -Search(board, baseDepth, negativeInfinity, positiveInfinity, lowtime, board.IsWhiteToMove);
            if (board.GameRepetitionHistory.Contains(board.ZobristKey))
            {
                evaluation -= 100; //This is a really ugly way to avoid 3 fold repetition. I'm sorry.
            }
            board.UndoMove(moves[i]);
            if (evaluation > bestScore)
            {
                bestScore = evaluation;
                bestMoveIndex = i;
            }
        }
        return moves[bestMoveIndex];
    }

    //Evaluates the position based on material and piece mobility
    public int Evaluate(Board board, bool lowtime, bool turn)
    {
        PieceList[] pieces = board.GetAllPieceLists();
        int whiteMaterial = pieces[0].Count * 100 + pieces[1].Count * 300 + pieces[2].Count *
                320 + pieces[3].Count * 500 + pieces[4].Count * 900;
        int blackMaterial = pieces[6].Count * 100 + pieces[7].Count * 300 + pieces[8].Count *
            320 + pieces[9].Count * 500 + pieces[10].Count * 900;
        int material = turn ? whiteMaterial - blackMaterial : blackMaterial - whiteMaterial;
        if (lowtime)
        {
            return material;
        }
        int whiteScore = 0;
        int blackScore = 0;
        ulong blockers = board.AllPiecesBitboard;
        for (int i = 0; i < pieces.Length; i++)
        {
            int score = 0;
            foreach (Piece piece in pieces[i])
            {
                if (i == 0 || i == 6)
                {
                    score += BitboardPopCount(BitboardHelper.GetPawnAttacks(piece.Square, piece.IsWhite)) * 5;
                }
                else if (i == 1 || i == 7)
                {
                    score += BitboardPopCount(BitboardHelper.GetKnightAttacks(piece.Square)) * 12;
                }
                else if (i == 2 || i == 8)
                {
                    score += BitboardPopCount(BitboardHelper.GetSliderAttacks(PieceType.Bishop,
                        piece.Square, blockers)) * 12;
                }
                else if (i == 3 || i == 9)
                {
                    score += BitboardPopCount(BitboardHelper.GetSliderAttacks(PieceType.Rook,
                                               piece.Square, blockers)) * 10;
                }
                else if (i == 4 || i == 10)
                {
                    score += BitboardPopCount(BitboardHelper.GetSliderAttacks(PieceType.Queen,
                                               piece.Square, blockers)) * 10;
                }
            }
            if (i < 6)
            {
                whiteScore += score;
            }
            else
            {
                blackScore += score;
            }
        }
        ulong whiteKing = board.GetPieceBitboard(PieceType.King, true);
        ulong blackKing = board.GetPieceBitboard(PieceType.King, false);
        int blackKingScore = KingSafetyBonus(blackKing);
        int whiteKingScore = KingSafetyBonus(whiteKing);
        if ((whiteMaterial + blackMaterial) > 2000)
        {
            baseDepth = 3;
            blackKingScore = -blackKingScore;
            whiteKingScore = -whiteKingScore;
        }
        else if (!lowtime)
        {
            baseDepth = 5;
        }
        return material + (turn ? whiteScore + whiteKingScore - blackScore - blackKingScore :
            blackScore + blackKingScore - whiteScore - whiteKingScore);
    }

    public int Search(Board board, int depth, int alpha, int beta, bool lowtime, bool turn)
    {
        if (depth == 0)
        {
            return Evaluate(board, lowtime, turn);
        }
        if (board.IsInCheckmate())
        {
            return negativeInfinity - depth; //scuffed way to get it to prioritize mate in fewer moves
        }
        if (board.IsDraw())
        {
            return 0;
        }
        System.Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);
        MoveOrder(board, moves);
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int evaluation = -Search(board, depth - 1, -beta, -alpha, lowtime, !turn);
            board.UndoMove(move);
            if (evaluation >= beta)
            {
                return beta;
            }
            alpha = Max(alpha, evaluation);
        }
        return alpha;
    }

    //Orders moves to optimize alpha beta pruning
    void MoveOrder(Board board, System.Span<Move> moves)
    {
        int[] scores = new int[moves.Length];
        bool whiteToMove = board.IsWhiteToMove;
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            int scoreGuess = 0;

            //Prioritizes high value captures
            if (move.IsCapture)
            {
                scoreGuess += 100;
            }

            //Prioritizes pawn promotions
            if (move.IsPromotion)
            {
                scoreGuess += 900;
            }

            //Avoids moving to squares attacked by enemy pawns
            ulong pawnAttackBitboard = BitboardHelper.GetPawnAttacks(move.TargetSquare, whiteToMove);
            ulong enemyPawnBitboard = board.GetPieceBitboard(PieceType.Pawn, !whiteToMove);
            if ((pawnAttackBitboard & enemyPawnBitboard) != 0)
            {
                scoreGuess -= 200;
            }

            //Prioritizes giving check
            ulong allPieces = board.AllPiecesBitboard;
            ulong enemyKing = board.GetPieceBitboard(PieceType.King, !whiteToMove);
            ulong pieceAttackBitboard = BitboardHelper.GetPieceAttacks(move.MovePieceType, move.TargetSquare,
                allPieces, whiteToMove);
            if ((pieceAttackBitboard & enemyKing) != 0)
            {
                scoreGuess += 100;
            }
            scores[i] = scoreGuess;
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
    }

    /*Bitboard population count algorithm found on chessprogramming wiki. I understand about half
     of this but it seems to be working.*/
    static int BitboardPopCount(ulong x)
    {
        x = x - ((x >> 1) & 0x5555555555555555);
        x = (x & 0x3333333333333333) + ((x >> 2) & 0x3333333333333333);
        x = (x + (x >> 4)) & 0x0f0f0f0f0f0f0f0f;
        x = (x * 0x0101010101010101) >> 56;
        return (int)x;
    }

    int KingSafetyBonus(ulong king)
    {
        ulong kingUp = king;
        ulong kingDown = king;
        for (int j = 0; j < 8; j++)
        {
            kingUp = kingUp << 8;
            kingDown = kingDown >> 8;
            if (kingUp == 0 || kingDown == 0)
            {
                return j * 50;
            }
        }
        return 0;
    }
}