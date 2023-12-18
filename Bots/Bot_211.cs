namespace auto_Bot_211;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_211 : IChessBot
{

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    const int depth = 4;
    bool AreWhite;
    int N = 2;
    public Move Think(Board board, Timer timer)
    {

        AreWhite = board.IsWhiteToMove;
        float TotalPieceValue = CountMaterial(AreWhite);
        float TotalEnemyPieceValue = CountMaterial(!AreWhite);
        int SearchedPositions = 0;
        int EvaluatedPositions = 0;
        int Pruned = 0;
        int RazorPruned = 0;
        int IterativeDepth = 0;
        float CurrentEval = Evaluate();

        Move[] lastFifteenMoves = board.GameMoveHistory.Skip(Math.Max(0, board.GameMoveHistory.Length - 15)).ToArray();
        float Search(int depth, float beta, float alpha)
        {
            SearchedPositions++;
            if (depth == 0) return Evaluate();

            Move[] Moves = board.GetLegalMoves();

            if (Moves.Length == 0)
            {
                if (board.IsInCheckmate())
                {
                    return float.NegativeInfinity;
                }
                return 0;
            }

            Array.Sort(Moves, (move1, move2) => GuessEval(move2).CompareTo(GuessEval(move1)));

            foreach (Move move in Moves)
            {
                board.MakeMove(move);

                float Eval = -Search(depth - 1, -alpha, -beta);

                board.UndoMove(move);

                if (Eval >= beta)
                {
                    Pruned++;
                    return beta;
                }

                alpha = Math.Max(Eval, alpha);
            }

            return alpha;
        }

        Move SearchIterate(int MaxMoveTime, int depth)
        {
            Move[] allMoves = board.GetLegalMoves();
            Array.Sort(allMoves, (move1, move2) => GuessEval(move2).CompareTo(GuessEval(move1)));
            Move BestMove = allMoves[0];
            float BestEval = float.NegativeInfinity;

            foreach (Move move in allMoves)
            {
                board.MakeMove(move);
                if ((!board.IsRepeatedPosition()) && lastFifteenMoves.Where(x => x.Equals(move)).Count() < 3)
                {
                    float e = -Search(depth, float.PositiveInfinity, float.NegativeInfinity);
                    if (e > BestEval)
                    {
                        BestEval = e;
                        BestMove = move;
                    }
                }

                board.UndoMove(move);
            }

            if (timer.MillisecondsElapsedThisTurn < MaxMoveTime / 10)
            {
                BestMove = SearchIterate(MaxMoveTime, IterativeDepth + 1);
            }
            else
            {
                DivertedConsole.Write("Searched Positions: " + SearchedPositions + "\nEvaluated Positions: " + EvaluatedPositions + "\nPruned Positions: " + Pruned + "\nRazor Pruned: " + RazorPruned + "\nBest Evaluation: " + BestEval + "\nTime: " + timer.MillisecondsElapsedThisTurn + "\nDepth Searched:" + depth);
            }
            return BestMove;
        }

        float Evaluate()
        {
            EvaluatedPositions++;
            float OurBoard = CountMaterial(AreWhite);
            float TheirBoard = CountMaterial(!AreWhite);
            float CurrentEval = TheirBoard - OurBoard;
            if (CurrentEval < 200)
            {

                CurrentEval += (TheirBoard - 10000) / 10;

            }
            CurrentEval += board.IsInCheck() ? -100 : 0;

            return CurrentEval;// * ((IterativeDepth % 2 == 0) ? 1 : -1);
        }

        float GuessEval(Move move)
        {
            float Guess = move.IsPromotion ? pieceValues[(int)move.PromotionPieceType] : 0;
            if (move.IsCapture)
            {
                Guess += (move.MovePieceType < move.CapturePieceType) ? 500 : 100;
            }

            Guess += board.SquareIsAttackedByOpponent(move.TargetSquare) ? -100 : 0;
            if (TotalPieceValue < 11200 && move.MovePieceType == PieceType.King)
            {
                Guess += -100;
            }
            return Guess;
        }

        float CountMaterial(bool IsWhite) //Counts Material and its worth for a side
        {
            return (board.GetPieceList(PieceType.Pawn, IsWhite).Count * pieceValues[1])
            + (board.GetPieceList(PieceType.Bishop, IsWhite).Count * pieceValues[2])
            + (board.GetPieceList(PieceType.Knight, IsWhite).Count * pieceValues[3])
            + (board.GetPieceList(PieceType.Rook, IsWhite).Count * pieceValues[4])
            + (board.GetPieceList(PieceType.Queen, IsWhite).Count * pieceValues[5]);
        }

        return SearchIterate(0, 4);
    }





}