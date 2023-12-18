namespace auto_Bot_254;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

//https://seblague.github.io/chess-coding-challenge/documentation/#piece-list-class     Documentation
//https://www.youtube.com/watch?v=iScy18pVR58                                           Video
//https://github.com/SebLague/Chess-Challenge                                           Github

public class Bot_254 : IChessBot
{

    TranspositionTable transpositionTable = new TranspositionTable(100000);
    public Move Think(Board board, Timer timer)
    {

        //Initiate the movelist, best move and movescore
        Move[] Moves = board.GetLegalMoves();
        Move BestMove = Moves[0];

        //Reorder the movelist for optimal guesses
        Array.Sort(EvaluateMoves(Moves, board).ToArray(), Moves);
        Array.Reverse(Moves);

        //Itterative deepening
        for (int totalDepth = 3; totalDepth < 15; totalDepth++)
        {

            int BestMoveScore = -100000;
            //Debug.Write(totalDepth);

            //place the best move in front of the Moves array
            Move[] BestMoveFirstMoves = new Move[Moves.Length + 1];
            Array.Copy(Moves, 0, BestMoveFirstMoves, 1, Moves.Length);
            BestMoveFirstMoves[0] = BestMove;

            //MiniMax/NegaMax
            foreach (Move move in BestMoveFirstMoves)
            {

                //Return the best move if too much time has passed
                if (timer.MillisecondsElapsedThisTurn > 1300 || timer.MillisecondsElapsedThisTurn * 11 > timer.MillisecondsRemaining)
                {
                    if (BestMove.IsCapture || BestMove.MovePieceType == PieceType.Pawn)
                    {
                        TranspositionTable transpositionTable = new TranspositionTable(100000);
                    }
                    return BestMove;
                }

                //Make the move
                board.MakeMove(move);
                int MoveScore = -Minimax(board, totalDepth, -100000, 100000, false, timer);
                board.UndoMove(move);

                //If the move was better than the previous, change it
                if (MoveScore > BestMoveScore)
                {
                    BestMoveScore = MoveScore;
                    BestMove = move;
                }
            }

        }
        //If depth 15 has passed (it won't) submit the best move
        return BestMove;
    }

    int EvaluateBoard(Board board)
    {

        int boardScore = 0;

        PieceList[] Pieces = board.GetAllPieceLists();

        foreach (PieceList PieceT in Pieces)
        {
            foreach (Piece piece in PieceT)
            {
                boardScore += EvaluatePiece(piece, 20);
            }
        }

        boardScore = boardScore * (board.IsWhiteToMove ? 1 : -1);
        return boardScore;
    }

    List<int> EvaluateMoves(Move[] Moves, Board board)
    {

        List<int> MoveScoreGuessList = new List<int> { };

        foreach (Move move in Moves)
        {
            int MoveScoreGuess = 0;

            if (move.CapturePieceType != PieceType.None)
            {
                MoveScoreGuess += 10 * EvaluatePieceType(move.CapturePieceType) - EvaluatePieceType(move.MovePieceType);
            }

            else if (move.IsPromotion)
            {
                MoveScoreGuess += EvaluatePieceType(move.PromotionPieceType);
            }

            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                MoveScoreGuess -= EvaluatePieceType(move.MovePieceType);
            }

            MoveScoreGuessList.Add(MoveScoreGuess);
        }

        return MoveScoreGuessList;
    }

    //Gives a score for a specific piece
    int EvaluatePieceType(PieceType piece)
    {
        int[] PieceWeights = { 0, 100, 320, 330, 500, 900, 10000 };
        return PieceWeights[(int)piece];
    }

    int EvaluatePiece(Piece piece, int PiecesOnBoard)
    {

        //Start with the optimal simple weight of the piecetype
        //int[] PieceWeights = {0, 100, 320, 330, 500, 900, 20000};
        int PieceType = (int)piece.PieceType;

        //Matrix of pieceweights
        //x gives a standard bonus for the type of piece
        //y gives a type-specific bonus weight for how far that piece wants to be to the centre
        //z gives a type-specific bonus weight for the rank of the piece 
        int[,] PieceWeights = {{0, 0, 0},
        {125, 0, 10},       //pawn
        {345, 18, 0},       //knight
        {355, 8, 0},       //bishop
        {510, 0, 1},       //rook
        {910, 6, 0},       //queen
        {20000, -5, -10}};    //king

        int[] ManHatDistanceMap = { 0, 1, 2, 3, 3, 2, 1, 0 };

        //Get the rank of the piece
        int Rank = piece.Square.Rank - 4;
        int ManHatDis = ManHatDistanceMap[Rank + 4] + ManHatDistanceMap[piece.Square.File];

        //Calculates the piecescore by multiplying the weights with the values
        //only the position and distance from centre are inverted for white/black, as the rank is already inverted for black/white
        return (piece.IsWhite ? 1 : -1) * ((PieceWeights[PieceType, 0]) + PieceWeights[PieceType, 1] * ManHatDis) + PieceWeights[PieceType, 2] * Rank;
    }

    //Minimax/NegaMax search, implemented such that it switches to only search for captures once depth has become zero
    int Minimax(Board board, int depth, int alpha, int beta, bool CapturesOnly, Timer timer)
    {

        //Stop evaluating if time passed
        if (timer.MillisecondsElapsedThisTurn > 1300 || timer.MillisecondsElapsedThisTurn * 11 > timer.MillisecondsRemaining)
        {
            return alpha;
        }

        if (board.IsDraw())
        {
            return 0;
        }


        //Check if board is already in transpositiontable. If so, return that
        if (transpositionTable.TryRetrieve(board.ZobristKey, depth, out TranspositionTableEntry entry))
        {
            return entry.Score;
        }


        //Switch from NotCapturesOnly to CapturesOnly
        if (depth == 3)
        {
            return Minimax(board, 2, alpha, beta, true, timer);
        }

        if (depth == 0)
        {
            return EvaluateBoard(board);
        }

        //Get all the legal moves (Captures only depending on the bool)
        Move[] Moves = board.GetLegalMoves(CapturesOnly);

        //Check if amount of moves is zero
        //  If in CaptureOnlyMode, evaluate board
        //  If not, check for checkmate or stalemate
        if (Moves.Length == 0)
        {
            if (CapturesOnly)
            {
                return EvaluateBoard(board);
            }
            if (board.IsInCheck())
            {
                return -100000;
            }
        }

        //Sort the movelist
        Array.Sort(EvaluateMoves(Moves, board).ToArray(), Moves);
        Array.Reverse(Moves);


        //Generate all possible moves
        foreach (Move move in Moves)
        {
            board.MakeMove(move);
            int MoveScore = -Minimax(board, depth - 1, -beta, -alpha, CapturesOnly, timer);
            board.UndoMove(move);

            //snip
            if (MoveScore >= beta)
            {
                return beta;
            }

            alpha = Math.Max(alpha, MoveScore);
        }

        //Store the new evaluation in the table
        transpositionTable.Store(board.ZobristKey, depth, alpha);

        return alpha;
    }


    //This is just ChatGPT helping me out writing the TranspositionTable (I never wrote in c# before so this is close to magic for me)
    public class TranspositionTableEntry
    {
        public int Depth { get; set; }
        public int Score { get; set; }

    }

    public class TranspositionTable
    {
        private Dictionary<ulong, TranspositionTableEntry> table;
        private LinkedList<ulong> accessOrder;
        private int sizeLimit;

        public TranspositionTable(int sizeLimit)
        {
            table = new Dictionary<ulong, TranspositionTableEntry>();
            accessOrder = new LinkedList<ulong>();
            this.sizeLimit = sizeLimit;
        }

        public void Store(ulong hash, int depth, int score)
        {
            if (table.Count >= sizeLimit)
            {
                // Remove the least-recently-used entry from the table
                ulong lruKey = accessOrder.First.Value;
                table.Remove(lruKey);
                accessOrder.RemoveFirst();
            }

            // Store the new entry in the table
            table[hash] = new TranspositionTableEntry { Depth = depth, Score = score };

            // Update the access order by moving the new entry to the end of the list
            accessOrder.AddLast(hash);
        }

        public bool TryRetrieve(ulong hash, int depth, out TranspositionTableEntry entry)
        {
            if (table.TryGetValue(hash, out entry) && entry.Depth >= depth)
            {
                // Update the access order by moving the retrieved entry to the end of the list
                accessOrder.Remove(hash);
                accessOrder.AddLast(hash);
                return true;
            }

            return false;
        }
    }
}

