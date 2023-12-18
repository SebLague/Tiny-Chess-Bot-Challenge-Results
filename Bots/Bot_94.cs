namespace auto_Bot_94;
using ChessChallenge.API;


public class Bot_94 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int ImWhite = -1;

    public Move Think(Board board, Timer timer)
    {
        int depth = 3;
        if (timer.MillisecondsRemaining < 2000) { depth = 2; }
        if (timer.MillisecondsRemaining < 500) { depth = 1; }
        Move[] allMoves = board.GetLegalMoves();
        if (board.IsWhiteToMove) { ImWhite = 1; }
        if (timer.MillisecondsRemaining < 15) { return allMoves[0]; } //panic mode
        int bestEval = -100;
        Move BestMove = allMoves[0];
        foreach (Move move in allMoves)
        {
            int moveEval = 100;
            if (MoveIsCheckmate(board, move))
            {
                return move; //always mate in 1
            }


            if (!allowsCheckmate(board, move))
            { //don't get mated in 1 (if possible)
                board.MakeMove(move);
                if (!board.IsInCheckmate() && !board.IsDraw())
                {
                    for (int i = 0; i < 5; i += 1)
                    { // this for loop iterates over the different simulated opponents
                        int responseBotEval = -100;
                        Move response1 = getMiniBotMove(i, board);
                        board.MakeMove(response1);
                        if (!board.IsInCheckmate() && !board.IsDraw() && depth > 1)
                        {
                            Move[] allMoves2 = board.GetLegalMoves();
                            foreach (Move move2 in allMoves2)
                            {
                                int move2Eval = -100;
                                board.MakeMove(move2);
                                if (board.IsInCheckmate()) { responseBotEval = 100; board.UndoMove(move2); break; }
                                if (!board.IsInCheckmate() && !board.IsDraw())
                                {
                                    Move response2 = getMiniBotMove(i, board);
                                    board.MakeMove(response2);
                                    if (board.IsInCheckmate()) { move2Eval = -100; }
                                    if (!board.IsInCheckmate() && !board.IsDraw() && depth > 2)
                                    {
                                        Move[] allMoves3 = board.GetLegalMoves();
                                        foreach (Move move3 in allMoves3)
                                        {
                                            int move3Eval = -100;
                                            board.MakeMove(move3);
                                            if (board.IsInCheckmate())
                                            {
                                                move2Eval = 100;
                                                board.UndoMove(move3);
                                                break;
                                            }
                                            if (!board.IsInCheckmate() && !board.IsDraw())
                                            {
                                                Move response3 = getMiniBotMove(i, board);
                                                board.MakeMove(response3);
                                                if (board.IsDraw()) { move3Eval = 0; }
                                                else { move3Eval = eval(board); }
                                                board.UndoMove(response3);
                                            }
                                            if (move3Eval > move2Eval) { move2Eval = move3Eval; }
                                            board.UndoMove(move3);
                                            if (move2Eval == 100) { break; }
                                        }
                                    }
                                    else if (board.IsDraw()) { move2Eval = 0; }
                                    else { move2Eval = eval(board); }
                                    board.UndoMove(response2);
                                }
                                if (move2Eval > responseBotEval) { responseBotEval = move2Eval; }
                                board.UndoMove(move2);
                                if (responseBotEval == 100) { break; }
                            }
                        }
                        else if (board.IsDraw() && responseBotEval > 0) { responseBotEval = 0; }
                        board.UndoMove(response1);
                        if (responseBotEval < moveEval) { moveEval = responseBotEval; }
                    }
                }
                if (moveEval > bestEval) { bestEval = moveEval; BestMove = move; }
                board.UndoMove(move);
                if (moveEval == 100) { break; }
            }
        }
        return BestMove;
    }

    bool allowsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool badMove = false;
        Move[] allOpponentsResponses = board.GetLegalMoves();
        foreach (Move response in allOpponentsResponses)
        {
            if (MoveIsCheckmate(board, response))
            {
                badMove = true;
                break;
            }

        }
        board.UndoMove(move);
        return badMove;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    int eval(Board board)
    {
        PieceList[] pieceList = board.GetAllPieceLists();
        int[] values = { 1, 3, 3, 5, 9, 0, -1, -3, -3, -5, -9, 0 };
        int eval = 0;
        for (int i = 0; i < 12; i += 1)
        {
            eval += pieceList[i].Count * values[i] * ImWhite;
        }
        return eval;
    }

    bool canBeCaptured(Board board, Move move)
    {
        board.MakeMove(move);
        Move[] allMoves = board.GetLegalMoves();
        bool captured = false;
        foreach (Move response in allMoves)
        {
            if (move.TargetSquare == response.TargetSquare)
            {
                captured = true;
                break;
            }
        }
        board.UndoMove(move);
        return captured;
    }

    Move Greedy_Bot(Board board)
    { //always takes the most valuable piece
        Move[] allMoves = board.GetLegalMoves();
        Move moveToPlay = allMoves[0];
        int highestValueCapture = 0;
        foreach (Move move in allMoves)
        {
            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }

        return moveToPlay;
    }

    Move Defensive_Bot(Board board)
    {
        Move[] allMoves = board.GetLegalMoves();
        Move moveToPlay = allMoves[0];
        int lowestPossibleDifferentCaptures = 16;
        foreach (Move move in allMoves)
        {
            // Find move that allows opponent the least capture
            board.MakeMove(move);
            Move[] allOpponentsResponsesThatAreCaptures = board.GetLegalMoves(true);
            if (allOpponentsResponsesThatAreCaptures.Length < lowestPossibleDifferentCaptures)
            {
                lowestPossibleDifferentCaptures = allOpponentsResponsesThatAreCaptures.Length;
                moveToPlay = move;
            }
            board.UndoMove(move);
        }
        return moveToPlay;
    }

    Move Checkers_Bot(Board board)
    { //always takes the first check it finds
        Move[] allMoves = board.GetLegalMoves();
        Move moveToPlay = allMoves[0];
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            if (board.IsInCheck())
            {
                board.UndoMove(move);
                return move;
            }
            board.UndoMove(move);
        }
        return moveToPlay;

    }
    Move Pawn_Pusher_Bot(Board board)
    { // always moves a pawn
        Move[] allMoves = board.GetLegalMoves();
        Move moveToPlay = allMoves[0];
        foreach (Move move in allMoves)
        {
            if (move.MovePieceType == (PieceType)1)
            {
                moveToPlay = move;
                break;
            }
        }
        return moveToPlay;
    }
    Move Intuitive_Bot(Board board)
    { // always moves the first move that comes to mind
        Move[] allMoves = board.GetLegalMoves();
        Move moveToPlay = allMoves[0];
        return moveToPlay;
    }
    Move getMiniBotMove(int bot_choice, Board board)
    {
        if (bot_choice == 0)
        {
            return Greedy_Bot(board);
        }
        else if (bot_choice == 1)
        {
            return Defensive_Bot(board);
        }
        else if (bot_choice == 2)
        {
            return Checkers_Bot(board);
        }
        else if (bot_choice == 3)
        {
            return Pawn_Pusher_Bot(board);
        }
        else
        {
            return Intuitive_Bot(board);
        }
    }
}