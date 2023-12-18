namespace auto_Bot_137;
using ChessChallenge.API;

using System;

using System.Linq;


public class Bot_137 : IChessBot
{
    public int[] KingBitBoard =
    {
       -63,-63,-63,-63,-63,  -63,-63,-63,-30,-30,-30,-30,-30,-30,-30,-30,-20,-20,-20,-20,-20,-20,-20,-20,20,-20,-20,-20,-20,-20,-20,-20,-9,-9,-9,-9,-9,-9,-9,-9,-9,-9,-9,-9,-9,-9,-9,-9,0,0,0,0,0,0,0,0,90,80,70,50,50,70,80,90
    };
    public bool IsBlunder(Board board, Move move)
    {
        // Threshold value; you might need to adjust it based on your evaluation scale.
        const double BLUNDER_THRESHOLD = 50;
        bool m = board.IsWhiteToMove;
        double initialEval = GetCurrentEval(board, move, m);

        foreach (Move move2 in board.GetLegalMoves(true))
        {
            board.MakeMove(move2);
            if (move2.TargetSquare.Name == move.TargetSquare.Name) // This is a blunder
            {
                if (initialEval - GetCurrentEval(board, move2, board.IsWhiteToMove) > BLUNDER_THRESHOLD)
                {
                    board.UndoMove(move2);
                    return true;
                }
            }
            board.UndoMove(move2);
        }
        return false;
    }
    static T[] CloneArray<T>(T[] originalArray)
    {
        T[] clonedArray = new T[originalArray.Length];
        Array.Copy(originalArray, clonedArray, originalArray.Length);
        return clonedArray;
    }

    PieceType k = PieceType.King;
    PieceType n = PieceType.Knight;
    PieceType q = PieceType.Queen;
    PieceType r = PieceType.Rook;
    PieceType b = PieceType.Bishop;
    PieceType p = PieceType.Pawn;
    public double GetCurrentEval(Board board, Move move, bool isWhite = false)
    {
        double p2 = isWhite ? 1 : -1;

        // heheheh No nead for PieceType.Pawn when you have just p!
        // Now we need to count the pieces and return the difference!
        double WhitePieceAddUp = board.GetPieceList(p, true).Count * 99;
        WhitePieceAddUp += board.GetPieceList(n, true).Count * 300;
        WhitePieceAddUp += board.GetPieceList(b, true).Count * 350;
        WhitePieceAddUp += board.GetPieceList(r, true).Count * 600;
        WhitePieceAddUp += board.GetPieceList(q, true).Count * 900;
        double blackPieceAddUP = board.GetPieceList(p, false).Count * 99;
        blackPieceAddUP += board.GetPieceList(n, false).Count * 300;
        blackPieceAddUP += board.GetPieceList(b, false).Count * 350;
        blackPieceAddUP += board.GetPieceList(r, false).Count * 600;
        blackPieceAddUP += board.GetPieceList(q, false).Count * 900;
        double eval = (WhitePieceAddUp - blackPieceAddUP) * p2 * 2;

        if (board.IsInCheckmate())
        {
            eval += double.PositiveInfinity;
        }
        if (board.IsInCheck())
        {
            eval += 99;
        }


        int[] tempArray = CloneArray(KingBitBoard);
        if (!isWhite)
        {
            tempArray.Reverse();
        }

        eval += tempArray[board.GetKingSquare(isWhite).Index];




        return eval;
    }

    public Move Search(Move[] moves, Board board, int depth = 4)
    {
        Move selectedMove = moves[0];
        double bestEval = double.NegativeInfinity;
        int perspective = board.IsWhiteToMove ? 1 : -1;

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            double eval;

            if (!IsBlunder(board, move))
            {
                eval = -MiniMax(board, depth - 1, double.NegativeInfinity, double.PositiveInfinity, perspective * -1);
            }
            else
            {
                eval = double.NegativeInfinity;
            }

            if (board.IsRepeatedPosition())
            {
                eval = double.NegativeInfinity;
            }

            if (move.MovePieceType == r)
            {
                eval -= 25;
            }
            //  bool i = board.IsWhiteToMove;
            // int r2 = move.TargetSquare.Rank;
            // if (move.MovePieceType !=k)
            /// {
            //    if (i && !(r2 == 1))
            //    {
            //        eval += 99;
            //    }
            //    else
            ///    {
            ///         if(!i && !(r2== 8))
            //        {
            //            eval += 99;
            //         }
            // else
            //  {
            //     eval -= 100;
            // }
            ///    }
            ///  }


            board.UndoMove(move);
            if (eval > bestEval)
            {
                bestEval = eval;
                selectedMove = move;
            }
        }

        return selectedMove;
    }

    private double MiniMax(Board board, int depth, double alpha, double beta, int perspective)
    {
        if (depth == 0 || board.IsInCheckmate())
        {
            Move[] move = board.GetLegalMoves();
            if (board.IsInCheckmate())
            {
                return double.NegativeInfinity;
            }
            if (board.IsInStalemate())
            {
                return 0;
            }
            if (board.IsInsufficientMaterial())
            {
                return 0;
            }
            return GetCurrentEval(board, move[0], board.IsWhiteToMove);
        }

        Move[] legalMoves = board.GetLegalMoves();

        foreach (Move move in legalMoves)
        {
            double t = move.TargetSquare.File;

            board.MakeMove(move);
            double eval = -MiniMax(board, depth - 1, -beta, -alpha, perspective * -1);
            PieceType mP = move.MovePieceType;
            if (mP == p && (t == 3 || t == 4))
            {

                eval += 99;

            }
            if (move.CapturePieceType > mP)
            {
                eval += 999;
            }

            if (move.IsPromotion)
            {
                if (move.PromotionPieceType == q)
                {
                    eval += 900;
                }
            }

            if (mP == n && (t == 0 || t == 7))
            {

                // Not a good move most likely
                eval -= 99;

            }



            Move[] movesForGood = board.GetLegalMoves(true);

            int numberOfPiecesTargeted = 0;




            foreach (Move move2 in movesForGood)
            {
                board.MakeMove(move2);

                if (move2.MovePieceType < move2.CapturePieceType)
                {
                    numberOfPiecesTargeted += 1;
                }

                board.UndoMove(move2);
            }

            if (numberOfPiecesTargeted > 2)
            {
                eval += numberOfPiecesTargeted * 400;
            }




            board.UndoMove(move);

            ;
            if (eval >= beta)
            {
                return beta;
            }


            if (eval > alpha)
            {
                alpha = eval;
            }
        }

        return alpha;
    }




    /* An error occurred while bot was thinking.
    System.NullReferenceException: Object reference not set to an instance of an object.
    at ChessChallenge.Chess.Board.MovePiece(Int32 piece, Int32 startSquare, Int32 targetSquare) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\Framework\Chess\Board\Board.cs:line 113
    at ChessChallenge.Chess.Board.UndoMove(Move move, Boolean inSearch) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\Framework\Chess\Board\Board.cs:line 325
    at ChessChallenge.API.Board.UndoMove(Move move) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\API\Board.cs:line 98
    at MyBot.Search(Move[] moves, Board Board, Int32 depth) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 79
    at MyBot.Think(Board board, Timer timer) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 98
    at ChessChallenge.Application.ChallengeController.GetBotMove() in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\Framework\Application\Core\ChallengeController.cs:line 150
    Illegal move: Null in position: rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1
    Game Over: BlackIllegalMove
    */
    /*An error occurred while bot was thinking.
    System.IndexOutOfRangeException: Index was outside the bounds of the array.
       at MyBot.MiniMax(Board board, Int32 depth, Double alpha, Double beta, Int32 perspective) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 110
       at MyBot.MiniMax(Board board, Int32 depth, Double alpha, Double beta, Int32 perspective) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 123
       at MyBot.MiniMax(Board board, Int32 depth, Double alpha, Double beta, Int32 perspective) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 123
       at MyBot.MiniMax(Board board, Int32 depth, Double alpha, Double beta, Int32 perspective) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 123
       at MyBot.Search(Move[] moves, Board board, Int32 depth) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 93
       at MyBot.Think(Board board, Timer timer) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 153
       at ChessChallenge.Application.ChallengeController.GetBotMove() in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\Framework\Application\Core\ChallengeController.cs:line 150
    Illegal move: Null in position: rn1qkbnr/ppp1pppp/8/8/2pP2B1/8/bP3P1P/2B2KNR w kq - 0 9
    Game Over: WhiteIllegalMove
    Unhandled exception. System.IndexOutOfRangeException: Index was outside the bounds of the array.
       at MyBot.MiniMax(Board board, Int32 depth, Double alpha, Double beta, Int32 perspective) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 110
       at MyBot.MiniMax(Board board, Int32 depth, Double alpha, Double beta, Int32 perspective) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 123
       at MyBot.MiniMax(Board board, Int32 depth, Double alpha, Double beta, Int32 perspective) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 123
       at MyBot.MiniMax(Board board, Int32 depth, Double alpha, Double beta, Int32 perspective) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 123
       at MyBot.Search(Move[] moves, Board board, Int32 depth) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 93
       at MyBot.Think(Board board, Timer timer) in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\My Bot\MyBot.cs:line 153
       at ChessChallenge.Application.ChallengeController.GetBotMove() in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\Framework\Application\Core\ChallengeController.cs:line 150
    --- End of stack trace from previous location ---
       at ChessChallenge.Application.ChallengeController.Update() in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\Framework\Application\Core\ChallengeController.cs:line 378
       at ChessChallenge.Application.Program.Main() in C:\Users\Username\Downloads\Chess-Challenge-main\Chess-Challenge-main\Chess-Challenge\src\Framework\Application\Core\Program.cs:line 40*/

    Move generateOkMove(Move[] moves, Board board)
    {
        while (true)
        {
            Random m = new Random();
            Move e = moves[m.Next(moves.Length)];

            board.MakeMove(e);
            if (!IsBlunder(board, e))
            {
                board.UndoMove(e);
                return e;
            }
            board.UndoMove(e);
        }
    }
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        if (board.GameMoveHistory.Length < 7 || timer.MillisecondsRemaining <= 10000)
        {

            return generateOkMove(moves, board);
        }
        return Search(moves, board, 5);
    }
}