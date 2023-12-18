namespace auto_Bot_140;
using ChessChallenge.API;
using System;
using System.Linq;


public class Bot_140 : IChessBot
{

    int[] eg_king_table = {
        -50,-40,-30,-20,-20,-30,-40,-50,
        -30,-20,-10,  0,  0,-10,-20,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-30,  0,  0,  0,  0,-30,-30,
        -50,-30,-30,-30,-30,-30,-30,-50
    };

    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int[] passedPawnBonuses = { 0, 600, 400, 250, 150, 75, 75 };

    bool isWhite;

    public Move Think(Board board, Timer timer)
    {
        isWhite = board.IsWhiteToMove;

        Move[] allMoves = board.GetLegalMoves();

        Move moveToPlay = allMoves[new Random().Next(allMoves.Length)];

        var rank = 0;

        foreach (Move move in allMoves)
        {
            int tmp = EvaluateMove(board, move);

            if (tmp > rank)
            {
                rank = tmp;
                moveToPlay = move;
            }
        }
        return moveToPlay;
    }


    int EvaluateMove(Board board, Move move)
    {
        int score = 0;
        int figureCount = BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard);

        board.MakeMove(move);
        score = board.GetPieceList(PieceType.Pawn, isWhite).Count * pieceValues[(int)PieceType.Pawn] +
                board.GetPieceList(PieceType.Queen, isWhite).Count * pieceValues[(int)PieceType.Queen] +
                board.GetPieceList(PieceType.Knight, isWhite).Count * pieceValues[(int)PieceType.Knight] +
                board.GetPieceList(PieceType.Bishop, isWhite).Count * pieceValues[(int)PieceType.Bishop] +
                board.GetPieceList(PieceType.Rook, isWhite).Count * pieceValues[(int)PieceType.Rook];

        if (board.IsInCheckmate())
        {
            board.UndoMove(move);
            return int.MaxValue;
        }

        if (board.IsDraw() || board.IsFiftyMoveDraw() || board.IsInStalemate() || board.IsInsufficientMaterial() || board.IsRepeatedPosition())
        {
            board.UndoMove(move);
            return 0;
        }
        board.UndoMove(move);

        //Entwicklung fördern, auf in den Kampf mit den Zentrumsbauern, wer will schon ewig leben...
        if (figureCount == 16 && move.MovePieceType == PieceType.Pawn && (move.StartSquare.File >= 2 || move.StartSquare.File <= 5))
        {
            score += 200;
        }

        if (move.MovePieceType == PieceType.Pawn && move.IsPromotion && move.PromotionPieceType == PieceType.Queen)
        {
            score += 8000;
        }

        //Im Mittelspiel und noch Bauern, dann Bauern hochbewegen wenn nicht in Gefahr
        if (figureCount >= 16 && move.MovePieceType == PieceType.Pawn && !FigureIsInDanger(board, move, false))
        {
            score += 700;
        }

        //Im Endspiel bauern hoch
        if (figureCount <= 8)
        {
            if (move.MovePieceType == PieceType.Pawn && !FigureIsInDanger(board, move, false))
            {
                score += 700;
            }

            if (move.MovePieceType == PieceType.King)
            {
                //Im Endspiel König zum Gegner ziehen
                //In Richtung Gegner-König stürmen....
                Square enemyKingSquare = board.GetKingSquare(!isWhite);

                if (enemyKingSquare.File > move.StartSquare.File && (move.StartSquare.File < move.TargetSquare.File) || enemyKingSquare.Rank > move.StartSquare.Rank && (move.StartSquare.Rank < move.TargetSquare.Rank))
                {
                    score += 300;
                }
            }
        }

        //Königin in Sicherheit bringen
        if (move.MovePieceType == PieceType.Queen && FigureIsInDanger(board, move, true) && !FigureIsInDanger(board, move, false))
        {
            score += 7000;
        }

        //Schlecht wenn man die Königin in Gefahr bringt
        if (move.MovePieceType == PieceType.Queen && FigureIsInDanger(board, move, false))
        {
            return -1000;
        }

        //Schach setzen vermutlich eine gute Idee wenn keine Gefahr....
        board.MakeMove(move);
        bool isInCheck = board.IsInCheck();
        board.UndoMove(move);
        if (isInCheck && !FigureIsInDanger(board, move, false))
        {
            score += 500;
        }

        //Das grosse Fressen
        if (pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType] > pieceValues[(int)move.MovePieceType])
        {
            score += 7000;
        }
        //Das kleine Fressen
        if (pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType] == pieceValues[(int)move.MovePieceType])
        {
            score += 300;
        }
        //Fressen ohne Konsequenzen
        if (move.IsCapture && !FigureIsInDanger(board, move, false))
        {
            score += 5000 + pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType];
        }
        //Schlecht wenn man sich in Gefahr begibt
        if (FigureIsInDanger(board, move, false))
        {
            score -= 1000;
        }
        //Noch viel schlechter wenns dazu nix zu Fressen gibt
        if (FigureIsInDanger(board, move, false) && !move.IsCapture)
        {
            score -= 3000;
        }

        //Schauen, ob Figur in Gefahr ist und durch Zug Gefahr gebannt, das wäre gut
        if (FigureIsInDanger(board, move, true) && !FigureIsInDanger(board, move, false))
        {
            score += 1000 + pieceValues[(int)move.MovePieceType];
        }

        //Wenn Schach ist, die billigste Figur zur Verteidigung nehmen
        if (board.IsInCheck())
        {
            score -= pieceValues[(int)move.MovePieceType] * 10;
        }

        //Random Seed für gleichgute Züge
        score += new Random().Next(15);

        if (move.MovePieceType == PieceType.Pawn)
        {
            foreach (Piece p in board.GetPieceList(PieceType.Pawn, isWhite))
            {
                if (!((board.GetPieceBitboard(PieceType.Pawn, isWhite) & (ulong)(0x0101010101010101) << (new Square(p.Square.Index).File)) == 0))
                {
                    score += -400;
                }
            }
        }

        score += move.IsCastles ? 200 : 0;
        foreach (Move oldMove in board.GameMoveHistory)
        {
            if ((move.TargetSquare.Name == oldMove.StartSquare.Name) && (move.StartSquare.Name == oldMove.TargetSquare.Name))
            {
                score += -500;
                break;
            }
        }

        score += (isWhite ? Enumerable.Reverse(eg_king_table).ToArray()[move.TargetSquare.Index] : eg_king_table[move.TargetSquare.Index]);

        return score;
    }





    bool FigureIsInDanger(Board board, Move move, bool before)
    {

        if (before)
        {
            return board.SquareIsAttackedByOpponent(move.StartSquare);
        }

        board.MakeMove(move);
        board.ForceSkipTurn();
        bool isAttacked = board.SquareIsAttackedByOpponent(move.TargetSquare);

        board.UndoSkipTurn();
        board.UndoMove(move);

        return isAttacked;

    }
}