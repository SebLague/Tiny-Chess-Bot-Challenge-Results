namespace auto_Bot_111;
using ChessChallenge.API;
using System;


public class Bot_111 : IChessBot
{

    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    bool isWhite;

    public Move Think(Board board, Timer timer)
    {
        isWhite = board.IsWhiteToMove;

        Move[] allMoves = board.GetLegalMoves();

        Move moveToPlay = Move.NullMove;

        var rank = 0;

        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                board.UndoMove(move);

                return move;
            }
            board.UndoMove(move);

            int tmp = EvaluateMove(board, move);
            if (tmp > rank)
            {
                rank = tmp;
                moveToPlay = move;
            }
        }

        if (moveToPlay != Move.NullMove)
        {
            //DivertedConsole.Write("Gewählter Zug: " + moveToPlay.StartSquare.Name + moveToPlay.TargetSquare.Name + " Score: " + rank);
            //DivertedConsole.Write("--------------------------------------------------------------------------------");
            return moveToPlay;
        }
        //DivertedConsole.Write("Kein Zug gefunden - nehme den erstbesten....");
        //DivertedConsole.Write("--------------------------------------------------------------------------------");
        return allMoves[new Random().Next(allMoves.Length)];
    }


    int EvaluateMove(Board board, Move move)
    {
        int score = 0;

        board.MakeMove(move);
        if (board.IsDraw() || board.IsFiftyMoveDraw() || board.IsInStalemate() || board.IsInsufficientMaterial() || board.IsRepeatedPosition())
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => GameIsFinished");
            board.UndoMove(move);
            return 0;
        }
        board.UndoMove(move);

        board.MakeMove(move);
        score = GetBoardCount(board);
        board.UndoMove(move);

        //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Board-Count: " + score);


        if (board.IsInCheckmate())
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Checkmate");
            return int.MaxValue;
        }


        //Entwicklung fördern, auf in den Kampf mit den Zentrumsbauern, wer will schon ewig leben...
        if (GetFigureCount(board) == 16 && move.MovePieceType == PieceType.Pawn && (move.StartSquare.File >= 2 || move.StartSquare.File <= 5))
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Entwicklung fördern");
            score += 200;
        }

        if (move.MovePieceType == PieceType.Pawn && move.IsPromotion && move.PromotionPieceType == PieceType.Queen)
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => MoveIsPawnBeforePromotion");
            score += 8000;
        }

        //Im Mittelspiel und noch Bauern, dann Bauern hochbewegen wenn nicht in Gefahr
        if (GetFigureCount(board) == 16 && move.MovePieceType == PieceType.Pawn && !FigureIsInDanger(board, move, false))
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Mittelspiel Bauern bewegen");
            score += 700;
        }

        //Im Endspiel bauern hoch
        if (GetFigureCount(board) <= 8)
        {
            if (move.MovePieceType == PieceType.Pawn && !FigureIsInDanger(board, move, false))
            {
                //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Endspiel Bauern hoch");
                score += 700;
            }
            else if (move.MovePieceType == PieceType.King)
            {
                //Im Endspiel König zum Gegner ziehen
                //In Richtung Gegner-König stürmen....
                Square enemyKingSquare = board.GetKingSquare(!isWhite);
                if (enemyKingSquare.File > move.StartSquare.File && (move.StartSquare.File < move.TargetSquare.File))
                {
                    score += 300;

                }
                else if (enemyKingSquare.Rank > move.StartSquare.Rank && (move.StartSquare.Rank < move.TargetSquare.Rank))
                {
                    score += 300;
                }
            }
        }

        //Königin in Sicherheit bringen
        if (move.MovePieceType == PieceType.Queen && FigureIsInDanger(board, move, true) && !FigureIsInDanger(board, move, false))
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Königin in Gefahr");
            score += 7000;
        }

        //Schlecht wenn man die Königin in Gefahr bringt
        if (move.MovePieceType == PieceType.Queen && FigureIsInDanger(board, move, false))
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Königin in Gefahr");
            return -1000;
        }

        //Schach setzen vermutlich eine gute Idee wenn keine Gefahr....
        if (MoveIsCheck(board, move) && !FigureIsInDanger(board, move, false))
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Schach - belohnen");
            score += 500;
        }


        //Das grosse Fressen
        if (pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType] > pieceValues[(int)move.MovePieceType])
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Guter Tausch - machen");
            score += 7000;
        }
        //Das kleine Fressen
        if (pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType] == pieceValues[(int)move.MovePieceType])
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Tauschen");
            score += 300;
        }
        //Fressen ohne Konsequenzen
        if (move.IsCapture && !FigureIsInDanger(board, move, false))
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Gefahrloses nehmen - machen");
            score += 5000 + pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType];
        }
        //Schlecht wenn man sich in Gefahr begibt
        if (FigureIsInDanger(board, move, false))
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Figur in Gefahr, tendentiell schlecht");
            score -= 1000;
        }
        //Noch viel schlechter wenns dazu nix zu Fressen gibt
        if (FigureIsInDanger(board, move, false) && !move.IsCapture)
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Figur in Gefahr, und kein Angriff - schlecht");
            score -= 3000;
        }

        //Schauen, ob Figur in Gefahr ist und durch Zug Gefahr gebannt, das wäre gut
        if (FigureIsInDanger(board, move, true) && !FigureIsInDanger(board, move, false))
        {
            //DivertedConsole.Write("Move: " + move.StartSquare.Name + move.TargetSquare.Name + " => Figur in Gefahr und kann gerettet werden - gut");
            score += 1000 + pieceValues[(int)move.MovePieceType];
        }

        //Wenn Schach ist, die billigste Figur zur Verteidigung nehmen
        if (board.IsInCheck())
        {
            score -= pieceValues[(int)move.MovePieceType] * 10;
        }

        //Random Seed für gleichgute Züge
        score += new Random().Next(150);

        return score;
    }

    int GetBoardCount(Board board)
    {
        int score = 0;


        PieceList pawns = board.GetPieceList(PieceType.Pawn, isWhite);
        score = score + (pawns.Count * pieceValues[(int)PieceType.Pawn]);

        PieceList queens = board.GetPieceList(PieceType.Queen, isWhite);
        score = score + (queens.Count * pieceValues[(int)PieceType.Queen]);

        PieceList knights = board.GetPieceList(PieceType.Knight, isWhite);
        score = score + (knights.Count * pieceValues[(int)PieceType.Knight]);

        PieceList bishops = board.GetPieceList(PieceType.Bishop, isWhite);
        score = score + (bishops.Count * pieceValues[(int)PieceType.Bishop]);

        PieceList rooks = board.GetPieceList(PieceType.Rook, isWhite);
        score = score + (rooks.Count * pieceValues[(int)PieceType.Rook]);


        return score;
    }

    int GetFigureCount(Board board)
    {
        PieceList pawns = board.GetPieceList(PieceType.Pawn, isWhite);
        PieceList queens = board.GetPieceList(PieceType.Queen, isWhite);
        PieceList knights = board.GetPieceList(PieceType.Knight, isWhite);
        PieceList bishops = board.GetPieceList(PieceType.Bishop, isWhite);
        PieceList rooks = board.GetPieceList(PieceType.Rook, isWhite);

        return pawns.Count + queens.Count + knights.Count + bishops.Count + rooks.Count;
    }

    //bool MoveIsCheckmate(Board board, Move move)
    //{
    //    board.MakeMove(move);
    //    bool isMate = board.IsInCheckmate();
    //    board.UndoMove(move);
    //    return isMate;
    //}

    bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isInCheck = board.IsInCheck();
        board.UndoMove(move);
        return isInCheck;
    }

    //SquareIsUnderAttachByOpponent funktioniert nicht, daher eigene Lösung...
    bool FigureIsInDanger(Board board, Move move, bool before)
    {
        if (before)
        {
            board.ForceSkipTurn();
            Move[] opponentMoves = board.GetLegalMoves(true);
            board.UndoSkipTurn();

            foreach (Move opponentMove in opponentMoves)
            {
                if (opponentMove.TargetSquare.Name == move.StartSquare.Name)
                {
                    return true;
                }
            }
            return false;
        }
        else
        {
            board.MakeMove(move);
            Move[] opponentMoves = board.GetLegalMoves(true);
            board.UndoMove(move);

            foreach (Move opponentMove in opponentMoves)
            {
                if (opponentMove.TargetSquare.Name == move.TargetSquare.Name)
                {
                    return true;
                }
            }
            return false;
        }

    }
}