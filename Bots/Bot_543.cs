namespace auto_Bot_543;
using ChessChallenge.API;

public class Bot_543 : IChessBot
{
    struct TTElement // (16 bytes) no constructor | everything should work fine but eventually its possible to change to version with constructor
    {
        public ulong key;
        public int eval;
        public Move move;
        public short depth;
        public short flags;/* 3 bits data  00000000 00000000 - wrong
                                            00000000 00000001 - ok
                                            00000000 00000010 - ended by beta*/
    }
    public Move Think(Board board, Timer timer)
    {
        ulong ttSize = 1048576; //16 * 1024 * 1024 = 16777216 (~256MB) | 256 * 1024 * 1024 = 268435456 (~4GB) | 1024 * 1024 = 1048576 (~16MB)
        TTElement[] transpositionTable = new TTElement[ttSize];

        int searchedMoves = 0; // debuging

        int depth = 4, bestEval = -2000000000;
        System.Random rng = new();
        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[rng.Next(moves.Length)];
        bool isWhite = board.IsWhiteToMove;

        int hardPiecesCount(bool white) { return board.GetPieceList(PieceType.Queen, white).Count + board.GetPieceList(PieceType.Rook, white).Count; }
        int lightPiecesCount(bool white) { return board.GetPieceList(PieceType.Bishop, white).Count + board.GetPieceList(PieceType.Knight, white).Count; }
        int piecesCount = hardPiecesCount(isWhite) + lightPiecesCount(isWhite) + board.GetPieceList(PieceType.Pawn, isWhite).Count;
        if (30 * piecesCount + moves.Length > 270) //250
            depth = 3;
        else if (30 * piecesCount + moves.Length > 130) //120
            depth = 4;
        else if (30 * piecesCount + moves.Length > 50) //40
            depth = 5;
        else
            depth = 6;

        int pawnValue = 100, knightValue = 290, bishopValue = 320, rookValue = 500, queenValue = 900;
        int getPieceValue(PieceType pieceType)
        {
            switch (pieceType)
            {
                case PieceType.Pawn:
                    return pawnValue;
                case PieceType.Knight:
                    return knightValue;
                case PieceType.Bishop:
                    return bishopValue;
                case PieceType.Rook:
                    return rookValue;
                case PieceType.Queen:
                    return queenValue;
                default:
                    return 0;
            }
        }

        int evaluateForColor(bool white)
        {
            int eval = queenValue * board.GetPieceList(PieceType.Queen, white).Count + rookValue * board.GetPieceList(PieceType.Rook, white).Count + bishopValue * board.GetPieceList(PieceType.Bishop, white).Count + knightValue * board.GetPieceList(PieceType.Knight, white).Count + pawnValue * board.GetPieceList(PieceType.Pawn, white).Count;
            if (board.HasKingsideCastleRight(white)) eval += 40; //30 //TODO (or not): make castle more valuable than stay with castle rights + make for both sides
            if (board.HasQueensideCastleRight(white)) eval += 30; //25

            if (board.IsWhiteToMove == white)
                eval += 2 * board.GetLegalMoves().Length;
            else
            {
                if (board.TrySkipTurn())
                {
                    eval += 2 * board.GetLegalMoves().Length;
                    board.UndoSkipTurn();
                }
            }
            return eval;
        }

        int evaluate()
        {
            int eval = evaluateForColor(board.IsWhiteToMove);
            eval -= evaluateForColor(!board.IsWhiteToMove);
            if (eval % 1000000 == 149) return eval + 1;
            return eval;
        }

        void OrderMoves(Board board, Move[] moves)
        {
            int[] moveScores = new int[moves.Length];
            for (int i = 0; i < moves.Length; i++)
            {
                int score = 0;
                PieceType movePieceType = board.GetPiece(moves[i].StartSquare).PieceType;
                PieceType capturePieceType = board.GetPiece(moves[i].TargetSquare).PieceType;
                PieceType promotionPieceType = moves[i].PromotionPieceType;

                if (capturePieceType != PieceType.None)
                    score = 2 * getPieceValue(capturePieceType) - getPieceValue(movePieceType);

                if (movePieceType == PieceType.Pawn && promotionPieceType != PieceType.None)
                    score += getPieceValue(promotionPieceType);
                else
                {
                    if (board.SquareIsAttackedByOpponent(moves[i].TargetSquare))
                    {
                        score -= 350;
                    }
                }
                moveScores[i] = score;
            }

            for (int i = 0; i < moves.Length - 1; i++)
            {
                for (int j = i + 1; j > 0; j--)
                {
                    int swapIndex = j - 1;
                    if (moveScores[swapIndex] < moveScores[j])
                    {
                        (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                        (moveScores[j], moveScores[swapIndex]) = (moveScores[swapIndex], moveScores[j]);
                    }
                }
            }
        }

        int Search(int localDepth, int scannedDepth, int alpha, int beta)
        {
            ulong index = board.ZobristKey % ttSize;
            TTElement ttElement = transpositionTable[index]; //only to read and save some tokens

            if (ttElement.key == board.ZobristKey && (ttElement.depth >= localDepth || ttElement.eval % 2000000001 > 1999000000))
            {
                if (ttElement.flags == 1) return ttElement.eval; //No need to fix eval ttElement.eval * (- (ttElement.depth - localDepth) % 2)
                if (ttElement.flags == 0 && ttElement.eval <= alpha) return ttElement.eval;
                if (ttElement.flags == 2 && ttElement.eval >= beta) return ttElement.eval;
            }

            if (board.IsInCheckmate()) return -2000000000 + scannedDepth; //Adding scannedDepth is very important
            if (board.IsDraw()) return 149; //Make a draw only in slighty lossing positions
            searchedMoves++;

            if (localDepth == 0) return evaluate();

            transpositionTable[index].key = board.ZobristKey;
            transpositionTable[index].depth = (short)localDepth;
            transpositionTable[index].flags = 0;

            Move[] moves = board.GetLegalMoves();
            OrderMoves(board, moves);

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int eval = -Search(localDepth - 1, scannedDepth + 1, -beta, -alpha);
                if (eval == 149) eval = -149;
                board.UndoMove(move);
                if (eval >= beta)
                {
                    transpositionTable[index].flags = 2;
                    transpositionTable[index].move = move;
                    transpositionTable[index].eval = eval;
                    return beta;
                }
                if (eval > alpha)
                {
                    transpositionTable[index].flags = 1;
                    transpositionTable[index].move = move;
                    alpha = eval;
                }
            }

            transpositionTable[index].eval = alpha;
            return alpha;
        }

        if (moves.Length == 1) return moves[0];
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -Search(depth, 0, -2000000000, 2000000000);
            board.UndoMove(move);
            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }
        }

        return bestMove;
    }
}