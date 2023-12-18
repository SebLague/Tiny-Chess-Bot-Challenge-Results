namespace auto_Bot_91;
using ChessChallenge.API;


public class Bot_91 : IChessBot
{
    bool letsFlagHim = false;
    bool pleaseDraw = false;
    int ImWhite = -1;
    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        if (board.IsWhiteToMove) { ImWhite = 1; }
        if (timer.MillisecondsRemaining < 5) { return allMoves[0]; } //panic mode
        if (timer.MillisecondsRemaining < 10 || eval(board) < -9) { pleaseDraw = true; }
        if (timer.OpponentMillisecondsRemaining < 10) { letsFlagHim = true; }
        Move flaggingMove = allMoves[0];
        int maxOpponentsMoveOption = 0;
        bool anyCheckmateFound = false;
        int earliestFoundCheckmate = 100;
        Move CandidateMatingMove = allMoves[0];
        Move CandidateNonLoosingMove = allMoves[0];
        bool existsPawnPush = false;
        int bestEval = -100;
        foreach (Move move in allMoves)
        {
            bool leadsToCheckmate = false;
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }



            if (pleaseDraw && (board.IsDraw() || board.IsRepeatedPosition()))
            {
                return move;
            }

            if (earliestFoundCheckmate > 2 && !allowsCheckmate(board, move))
            {

                board.MakeMove(move);
                Move[] allOpponentsResponses = board.GetLegalMoves();
                int moveEval = 100;
                foreach (Move response in allOpponentsResponses)
                {
                    board.MakeMove(response);
                    int evalResponse = eval(board);
                    if (evalResponse < moveEval)
                    {
                        moveEval = evalResponse;
                    }
                    board.UndoMove(response);
                }
                if (moveEval > bestEval && !board.IsRepeatedPosition())
                {
                    bestEval = moveEval;
                    CandidateNonLoosingMove = move;
                }
                if (moveEval == bestEval && !existsPawnPush && move.MovePieceType == (PieceType)1)
                { // amongs otherwise equal moves prefer pawn pushes
                    existsPawnPush = true;
                    CandidateNonLoosingMove = move;
                }
                board.UndoMove(move);

                if (allOpponentsResponses.Length > maxOpponentsMoveOption && !allowsCheckmateOrBadCaptureOrIsCheck(board, move))
                {
                    flaggingMove = move;
                    maxOpponentsMoveOption = allOpponentsResponses.Length;
                }
                if (letsFlagHim) { break; } //If our goal is just to flag our opponent no need to look for good moves
                if (!allowsCheckmateOrBadCaptureOrIsCheck(board, move))
                {
                    board.MakeMove(move);
                    board.ForceSkipTurn();
                    Move[] allSecondMoves = board.GetLegalMoves();
                    foreach (Move move2 in allSecondMoves)
                    {
                        if (MoveIsCheckmate(board, move2))
                        {
                            leadsToCheckmate = true;
                            earliestFoundCheckmate = 2;
                            break;
                        }

                        if (timer.MillisecondsRemaining > 100 && earliestFoundCheckmate > 3 && !allowsCheckmateOrBadCaptureOrIsCheck(board, move2))
                        {
                            board.MakeMove(move2);
                            board.ForceSkipTurn();
                            Move[] allThirdMoves = board.GetLegalMoves();
                            foreach (Move move3 in allThirdMoves)
                            {
                                if (MoveIsCheckmate(board, move3))
                                {
                                    leadsToCheckmate = true;
                                    earliestFoundCheckmate = 3;
                                    break;
                                }

                                if (timer.MillisecondsRemaining > 3000 && earliestFoundCheckmate > 4 && !allowsCheckmateOrBadCaptureOrIsCheck(board, move3))
                                {
                                    board.MakeMove(move3);
                                    board.ForceSkipTurn();
                                    Move[] allFourthMoves = board.GetLegalMoves();
                                    foreach (Move move4 in allFourthMoves)
                                    {
                                        if (MoveIsCheckmate(board, move4))
                                        {
                                            leadsToCheckmate = true;
                                            earliestFoundCheckmate = 4;
                                            break;
                                        }
                                    }
                                    board.UndoSkipTurn();
                                    board.UndoMove(move3);
                                    if (leadsToCheckmate) { break; }
                                }
                            }
                            board.UndoSkipTurn();
                            board.UndoMove(move2);
                            if (leadsToCheckmate) { break; }
                        }
                    }
                    board.UndoSkipTurn();
                    board.UndoMove(move);
                }
            }
            if (leadsToCheckmate)
            {
                CandidateMatingMove = move;
                anyCheckmateFound = true;
            }
        }
        if (letsFlagHim)
        {
            return flaggingMove;
        }
        if (anyCheckmateFound) { return CandidateMatingMove; }
        return CandidateNonLoosingMove;






        bool allowsCheckmateOrBadCaptureOrIsCheck(Board board, Move move)
        {
            board.MakeMove(move);
            bool badMove = false;
            if (board.IsInCheck()) { board.UndoMove(move); return true; }
            Move[] allOpponentsResponses = board.GetLegalMoves();
            foreach (Move response in allOpponentsResponses)
            {
                if (MoveIsCheckmate(board, response) || response.CapturePieceType == (PieceType)5 || move.TargetSquare == response.TargetSquare)
                {
                    badMove = true;
                    break;
                }

            }
            board.UndoMove(move);
            return badMove;
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


}