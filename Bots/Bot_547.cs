namespace auto_Bot_547;
using ChessChallenge.API;

// My ugly piece of code, so that it may remain under the brain capacity. Farewell chessbot 'NotQuiteRandomGamblerAndPruner'
// Made by Taco

// Final bot brain capacity: 939 

public class Bot_547 : IChessBot
{
    int searchDepth;
    int currentDepth;
    int lastEvaluation = -20000000;
    int monteCarloSearchDepth;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();


        bool myColor = board.IsWhiteToMove;
        int nrOfPieces = BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard);

        bool endGameStarted = (nrOfPieces <= 10 || board.PlyCount > 75);
        bool isOpening = board.PlyCount < 12;
        bool enoughTimeLeft = timer.MillisecondsRemaining > 25000;


        monteCarloSearchDepth = board.PlyCount > 12 ? 2 : 1;
        searchDepth = lastEvaluation > 900000 ? 1000000 - lastEvaluation : ((endGameStarted && enoughTimeLeft ? 5 : 4) + monteCarloSearchDepth);
        searchDepth += (timer.MillisecondsRemaining > 240000) ? 1 : 0;
        monteCarloSearchDepth += isOpening ? 1 : 0;
        currentDepth = searchDepth;
        Move bestMove = moves[0];
        Move currentMove = moves[0];

        int bestEvaluation = -2000000;
        lastEvaluation = Search(searchDepth, -2000000, 1000000);

        //DivertedConsole.Write((int)board.PlyCount + "\t: " + bestMove.ToString() + "  \tTime taken: " + timer.MillisecondsElapsedThisTurn + "  \tEvaluation: " + lastEvaluation + "    \t<- mybot");

        return bestMove;

        int Search(int depth, int alpha, int beta)
        {
            currentDepth = depth;
            if (depth == 0)
            {
                return evaluate();
            }
            Move[] moves = board.GetLegalMoves();
            if (board.IsInCheckmate()) return -1000000 + (searchDepth - currentDepth);
            if (board.IsDraw()) return 0;

            OrderMoveArray(moves);

            if (depth <= monteCarloSearchDepth)
            {
                for (int i = 0; i < 3 && i < moves.Length; i++)
                {
                    board.MakeMove(moves[i]);
                    int evaluation = -Search(depth - 1, -beta, -alpha);
                    board.UndoMove(moves[i]);


                    if (evaluation >= beta) return beta;
                    alpha = alpha > evaluation ? alpha : evaluation;
                }
            }
            else
            {
                foreach (Move move in moves)
                {
                    board.MakeMove(move);
                    int evaluation = -Search(depth - 1, -beta, -alpha);
                    board.UndoMove(move);
                    if (depth == searchDepth)
                    {
                        if (evaluation > bestEvaluation)
                        {
                            currentMove = move;
                            bestMove = move;
                            bestEvaluation = evaluation;
                        }
                    }

                    if (evaluation >= beta) return beta;
                    alpha = alpha > evaluation ? alpha : evaluation;
                }
            }
            return alpha;
        }

        int evaluate()
        {

            if (board.IsInCheckmate()) return 1000000;
            if (board.IsDraw()) return 0;

            int evaluation = 0;
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                Piece currentPiece = board.GetPiece(new Square(squareIndex));
                if (currentPiece.IsWhite == board.IsWhiteToMove)
                {
                    evaluation += evaluatePiece(currentPiece);
                }
                else
                {
                    evaluation -= evaluatePiece(currentPiece);
                }
            }
            return evaluation;
        }

        int evaluatePiece(Piece piece)
        {
            int pieceTypeNumber = (int)piece.PieceType;
            if (pieceTypeNumber == 0) return 0;

            int[] pieceValues = { 0, 100, 300, 330, 500, 900, 100000 };
            int evaluation = pieceValues[pieceTypeNumber];

            ulong movesBitboard = BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite);  // a bitboard highlighting each square that can be moved to by the current piece
            ulong reachBitboard = BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, 0, piece.IsWhite);      // a bitboard highlighting each square that can be moved to by the current piece, if there had been no obstructions

            evaluation += BitboardHelper.GetNumberOfSetBits(movesBitboard);
            if (pieceTypeNumber == 6)
            { // if piece is a king
                if (!endGameStarted)
                {
                    int squareIndex = piece.Square.Index;
                    evaluation += (squareIndex == 2 || squareIndex == 6 ? 10 : 0)
                                + (squareIndex == 56 || squareIndex == 62 ? 10 : 0)
                                + (-2 * BitboardHelper.GetNumberOfSetBits(movesBitboard));
                }
            }


            if (pieceTypeNumber == 1)
            { //piece is a pawn
                if (endGameStarted) evaluation += piece.IsWhite ? piece.Square.Rank : 7 - piece.Square.Rank;
                ulong kingBitboard = board.GetPieceBitboard(PieceType.King, piece.IsWhite);
                int kingIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref kingBitboard);
                if (board.PlyCount > 12 && (piece.Square.Index + 8 == kingIndex || piece.Square.Index - 8 == kingIndex)) evaluation += 3;
            }
            return evaluation;
        }

        void OrderMoveArray(Move[] moves)
        {
            int[] moveGuessEvaluation = new int[moves.Length];

            int[] pieceValues = { 0, 100, 300, 330, 500, 900, 100000 };

            ulong unsafeBitboard = 0;    // a bitboard highlighting each square that the opponent can move to
            for (int squareIndex = 0; squareIndex < 64; squareIndex++)
            {
                Square currentSquare = new(squareIndex);
                Piece currentPiece = board.GetPiece(currentSquare);
                if (currentPiece.IsWhite != board.IsWhiteToMove && currentPiece.PieceType != PieceType.None)
                {
                    unsafeBitboard |= BitboardHelper.GetPieceAttacks(currentPiece.PieceType, currentSquare, board, !board.IsWhiteToMove);
                }
            }

            for (int i = 0; i < moves.Length; i++)
            {
                int movePieceType = (int)moves[i].MovePieceType;
                int capturePieceType = (int)moves[i].CapturePieceType;
                moveGuessEvaluation[i] = 10 * pieceValues[capturePieceType]
                                        + pieceValues[(int)moves[i].PromotionPieceType]
                                        + (moves[i].IsCastles ? 101 : 0)
                                        + ((unsafeBitboard >> moves[i].TargetSquare.Index) % 2 == 1 ? -pieceValues[movePieceType] : 0)
                                        + (movePieceType == 6 ? -100 : 0);
            }

            for (int i = 0; i < moves.Length - 1; i++)
            {
                for (int j = i + 1; j > 0; j--)
                {
                    int swapIndex = j - 1;
                    if (moveGuessEvaluation[swapIndex] < moveGuessEvaluation[j])
                    {
                        (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                        (moveGuessEvaluation[j], moveGuessEvaluation[swapIndex]) = (moveGuessEvaluation[swapIndex], moveGuessEvaluation[j]);
                    }
                }
            }
        }
    }
}


