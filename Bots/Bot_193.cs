namespace auto_Bot_193;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_193 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        //when there is enough time left and it is midgame search deeper
        int depth = 3;

        //mit 30000 114 15 15, 1 timeout
        if (board.PlyCount > 8 && timer.MillisecondsRemaining > 20000)
        {
            depth = 4;
        }
        else if (timer.MillisecondsRemaining < 5000)
        {
            depth = 2;
        }

        String result = minmax(board, depth, board.IsWhiteToMove, int.MinValue, int.MaxValue).Split("//")[0];
        Move ret = new Move(result, board);


        //if its a promotion make it a queen promotion
        if (ret.MovePieceType == PieceType.Pawn)
        {
            if (ret.IsPromotion)
            {
                return new Move(result + "q", board);
            }
        }
        return ret;
    }

    public String minmax(Board board, int depth, bool isMaximising, int alpha, int beta)
    {
        int score = isMaximising ? int.MinValue : int.MaxValue;
        Move[] allmoves = randomizeMoves(board.GetLegalMoves());
        Move[] capturemoves = board.GetLegalMoves(true);
        Move[] moves = capturemoves.Union(allmoves).ToArray();

        if (moves.Length > 0)
        {
            Move bestmove = moves[0];

            if (depth > 0)
            {
                foreach (Move move in moves)
                {
                    board.MakeMove(move);
                    String res = minmax(board, depth - 1, !isMaximising, alpha, beta);
                    int thisscore = int.Parse(res.Split("//")[1]);
                    thisscore += pawnPushing(board, move);
                    board.UndoMove(move);

                    if ((thisscore > score && isMaximising) || (thisscore < score && !isMaximising))
                    {
                        score = thisscore;
                        bestmove = move;
                    }

                    if (isMaximising)
                    {
                        alpha = Math.Max(alpha, score);
                        if (beta <= alpha)
                        {
                            break;
                        }
                    }
                    else
                    {
                        beta = Math.Min(beta, score);
                        if (beta <= alpha)
                        {
                            break;
                        }
                    }

                }
            }
            else
            {
                foreach (Move move in moves)
                {
                    board.MakeMove(move);
                    int thisscore = evaluatePosition(board);
                    thisscore += pawnPushing(board, move);

                    if ((thisscore > score && isMaximising) || (thisscore < score && !isMaximising))
                    {
                        score = thisscore;
                        bestmove = move;
                    }
                    board.UndoMove(move);
                }
            }


            return (bestmove.ToString().Split(" ")[1].Replace("'", "") + "//" + score.ToString());
        }
        else
        {
            if (board.IsDraw() || board.IsInStalemate())
            {
                return "draw//0";
            }

            //differentiate between us having checkmate and being checkmated
            if (board.SquareIsAttackedByOpponent(board.GetKingSquare(true)))
            {
                if (board.IsWhiteToMove)
                {
                    return "whiteischeckmated//-10000";
                }
            }
            return "blackischeckmated//10000";
        }
    }

    public int pawnPushing(Board board, Move move)
    {
        if (board.PlyCount > 95)
        {
            if (move.MovePieceType == PieceType.Pawn)
            {
                if (board.IsWhiteToMove)
                {
                    return -5;
                }
                else
                {
                    return +5;
                }
            }
        }
        return 0;
    }

    public int evaluatePosition(Board board)
    {
        int rankeval = 0;
        if (board.PlyCount > 75)
        {
            Square enemyKingSquare = board.GetKingSquare(!board.IsWhiteToMove);

            switch (enemyKingSquare.Rank)
            {
                case 0: rankeval += 3; break;
                case 1: rankeval += 2; break;
                case 2: rankeval += 1; break;
                case 5: rankeval += 1; break;
                case 6: rankeval += 2; break;
                case 7: rankeval += 3; break;
            }

            switch (enemyKingSquare.File)
            {
                case 0: rankeval += 3; break;
                case 1: rankeval += 2; break;
                case 2: rankeval += 1; break;
                case 5: rankeval += 1; break;
                case 6: rankeval += 2; break;
                case 7: rankeval += 3; break;
            }

            Square myKingSquare = board.GetKingSquare(board.IsWhiteToMove);
            rankeval += 14 - (Math.Abs(enemyKingSquare.File - myKingSquare.File) + Math.Abs(enemyKingSquare.Rank - myKingSquare.Rank));
        }

        if (board.IsInStalemate())
        {
            return 0;
        }

        if (board.IsDraw())
        {
            return 0;
        }

        String fenstring = board.GetFenString();
        fenstring = fenstring.Split(' ')[0];

        int blackscore = 0;
        int whitescore = 0;

        if (board.IsWhiteToMove)
        {
            whitescore += rankeval;
        }
        else
        {
            blackscore += rankeval;
        }



        foreach (char c in fenstring.ToCharArray())
        {
            if (c != '/')
            {
                if (char.IsLower(c))
                {
                    switch (c)
                    {
                        case 'r': blackscore += 525; break;
                        case 'n': blackscore += 350; break;
                        case 'b': blackscore += 350; break;
                        case 'q': blackscore += 1000; break;
                        case 'p': blackscore += 100; break;
                        case 'k': blackscore += 10000; break;
                    }
                }
                else if (char.IsUpper(c))
                {
                    switch (c)
                    {
                        case 'R': whitescore += 525; break;
                        case 'N': whitescore += 350; break;
                        case 'B': whitescore += 350; break;
                        case 'Q': whitescore += 1000; break;
                        case 'P': whitescore += 100; break;
                        case 'K': whitescore += 10000; break;
                    }
                }
            }
        }

        int eval = whitescore - blackscore;
        int checkeval = 0;
        if (board.IsInCheck())
        {
            checkeval += 3;
        }

        if (board.IsInCheckmate())
        {
            checkeval += 101;
        }

        if (board.IsWhiteToMove)
        {
            eval += checkeval;
        }
        else
        {
            eval -= checkeval;
        }

        return eval;
    }

    public Move[] randomizeMoves(Move[] moves)
    {
        for (int i = 0; i < moves.Length; i++)
        {
            Random rndm = new Random();
            int j = rndm.Next(i, moves.Length);
            Move temp = moves[i];
            moves[i] = moves[j];
            moves[j] = temp;
        }
        return moves;
    }
}