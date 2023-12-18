namespace auto_Bot_491;
using ChessChallenge.API;
using System;

public class Bot_491 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 100, 300, 305, 500, 900, 10000 };

    int searchedPositions = 0;

    public Move Think(Board board, Timer timer)
    {
        searchedPositions = 0; //reset to 0

        float bestEval = -100000 * SignMove(board); // lower bound eval

        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better than the initial bestEval is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            //search and evaluate this move till given depth
            board.MakeMove(move);
            float eval = SearchAllMoves(board, 2);
            board.UndoMove(move);

            // If the move is better -> update move
            if (BetterforTurnplayer(board, eval, bestEval))
            {
                moveToPlay = move;
                bestEval = eval;
                //DivertedConsole.Write("non random " + moveToPlay);
                //DivertedConsole.Write("Eval: " + bestEval);
            }

            // Avoid repetition/draws with randomness
            if (MoveIsDraw(board, move))
            {
                moveToPlay = allMoves[rng.Next(allMoves.Length)];
                //DivertedConsole.Write("Try to avoid Repetition/Draw");
            }

        }

        DivertedConsole.Write("Moves searched: " + searchedPositions);
        DivertedConsole.Write("Evaluation: " + bestEval / 100);

        return moveToPlay;
    }

    // Test if this move gives checkmate
    static bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    // Test if this move results in a draw
    static bool MoveIsDraw(Board board, Move move)
    {
        board.MakeMove(move);
        bool isDraw = board.IsDraw();
        board.UndoMove(move);
        return isDraw;
    }

    // Test if this move gives check
    static bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }

    // Is the moveEval better than bestEval?
    static bool BetterforTurnplayer(Board board, float moveEval, float bestEval)
    {
        return (moveEval > bestEval && board.IsWhiteToMove) || (moveEval < bestEval && !board.IsWhiteToMove);
    }

    // Searches and evaluates all moves rekursively, till given depth
    // Very time expensive!
    // Option of only searching captures

    // Muss noch auf Endlosschleifen usw. geprüft werden!
    float SearchAllMoves(Board board, int depth, bool capturesOnly = false)
    {
        // safety
        if (depth > 4)
        {
            DivertedConsole.Write("Fail: Search depth exceeds limit!");
            return 0;
        }

        // lower bound eval
        float bestEval = -100000 * SignMove(board);

        if (depth > 0 && !board.IsInCheckmate() && !board.IsDraw())
        {
            depth -= 1;

            Move[] moves = board.GetLegalMoves(capturesOnly);

            //searches and evaluates those moves
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                float eval = SearchAllMoves(board, depth);
                board.UndoMove(move);

                if (BetterforTurnplayer(board, eval, bestEval))
                {
                    bestEval = eval;
                }
            }
        }
        else
        {
            searchedPositions++;
            bestEval = Evaluate(board); //evaluate endposition
        }

        return bestEval;
    }

    // My simple evaluation function
    public float Evaluate(Board board)
    {
        float eval = 0;

        // check for checkmate and Draw
        if (board.IsInCheckmate())
        {
            return -10000 * SignMove(board);
        }
        if (board.IsDraw())
        {
            // 1 centi-pawn "punishment" if the opponent can draw
            // or reward if you can draw
            return -1 * SignMove(board);
        }


        PieceList[] allPieceLists = board.GetAllPieceLists();
        foreach (PieceList plist in allPieceLists)
        {
            int pvalue = pieceValues[(int)plist.TypeOfPieceInList];

            int coulourSign = -1;
            if (plist.IsWhitePieceList)
            {
                coulourSign = 1;
            }

            foreach (Piece piece in plist)
            {
                // Counts piecevalue
                eval += pvalue * coulourSign;
            }

        }
        return eval;
    }

    //Plus if White is to move, Minus if Black is to move
    static int SignMove(Board b)
    {
        int sign = -1;
        if (b.IsWhiteToMove)
        {
            sign = 1;
        }
        return sign;
    }
}