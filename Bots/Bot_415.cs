namespace auto_Bot_415;
using ChessChallenge.API;
using System;
using System.Collections.Generic;


public class Bot_415 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    public ulong bitboard;
    public bool mycolourIsWhite;
    public int jeseraisprotegeValue = 74;
    public int nbrPieceSuisSeulProtectionValue = 78;
    public int jemedeveloppeValue = 29;
    public int jemetscheckValue = -8;
    public int jemenaceValue = -37;
    public Move Think(Board board, Timer timer)
    {
        if (board.IsWhiteToMove == true)
        {
            mycolourIsWhite = true;
        }
        if (board.IsWhiteToMove == false)
        {
            mycolourIsWhite = false;
        }

        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];

        int highestScoredValue = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            //Check checkmate in two
            if (MoveIsCheckmateInTwo(board, move))
            {
                moveToPlay = move;
                break;
            }


            // Find capture value for each move
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            // Find create value (for promotion only)
            int createPieceValue = 0;
            if (move.IsPromotion == true)
            {
                createPieceValue = pieceValues[(int)move.PromotionPieceType];
            }

            // Find highest sum (peut être moduler par hyperfacteur pour renforcement learning)
            int ScoredValue = createPieceValue + capturedPieceValue;

            if (jeseraisprotege(board, move))
            {
                ScoredValue = ScoredValue + jeseraisprotegeValue;
            }

            if (nbrPieceSuisSeulProtection(board, move) != 0)
            {
                ScoredValue = ScoredValue - nbrPieceSuisSeulProtection(board, move) * nbrPieceSuisSeulProtectionValue;
            }
            if (jemedeveloppe(board, move))
            {
                ScoredValue = ScoredValue + jemedeveloppeValue;
            }

            //else if(jemedeveloppe(board, move) & move.MovePieceType != PieceType.King)
            //{
            //    ScoredValue = ScoredValue + jemedeveloppeValue/2;
            //}
            if (jemetscheck(board, move))
            {
                ScoredValue += jemetscheckValue;
            }

            if (jemenace(board, move))
            {
                ScoredValue += jemenaceValue;
            }

            if (ScoredValue > highestScoredValue)
            {
                moveToPlay = move;
                highestScoredValue = ScoredValue;
            }

        }
        return moveToPlay;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool MoveIsCheckmateInTwo(Board board, Move move)
    {
        bool isMateInTwo = false;
        board.MakeMove(move);

        Move[] allMovesOfEnnemy = board.GetLegalMoves();

        int compteurDeToutLesCoupsEnnemy = 0;
        foreach (Move moveOfEnnemy in allMovesOfEnnemy)
        {
            board.MakeMove(moveOfEnnemy);

            Move[] allMovesInTwo = board.GetLegalMoves();
            foreach (Move moveInTwo in allMovesInTwo)
            {
                if (MoveIsCheckmate(board, moveInTwo))
                {
                    compteurDeToutLesCoupsEnnemy++;
                    break;
                    // car on prendra quoi qu'il arrive s'il y a mat en 2 le coup qui fera mat au prochain
                }
            }
            board.UndoMove(moveOfEnnemy);
        }
        if (compteurDeToutLesCoupsEnnemy == allMovesOfEnnemy.Length && allMovesOfEnnemy.Length != 0) // on empeche les pats
        {
            isMateInTwo = true;
        }
        board.UndoMove(move);
        return isMateInTwo;
    }
    bool jeseraisprotege(Board board, Move move)
    {

        bool squareattaque = board.SquareIsAttackedByOpponent(move.TargetSquare);

        board.MakeMove(move);
        bool squaredefendu = board.SquareIsAttackedByOpponent(move.TargetSquare);
        board.UndoMove(move);
        if (squareattaque == false)
        {
            return true;
        }
        else
        {
            return (squaredefendu & squareattaque);
        }
    }

    int nbrPieceSuisSeulProtection(Board board, Move move)
    {
        int pieceseuldefenseur = 0;
        bitboard = BitboardHelper.GetPieceAttacks(move.MovePieceType, move.StartSquare, board, board.IsWhiteToMove);
        List<int> indexPieceEnVue = new List<int>();
        while (BitboardHelper.GetNumberOfSetBits(bitboard) != 0)
        {
            indexPieceEnVue.Add(BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard));
        } //j'ai l'index de toute les pièces que peux attaquer avec ma pièce

        List<Piece> pieceProtege = new List<Piece>();

        foreach (int i in indexPieceEnVue)
        {
            Square test = new Square(i);
            if (board.GetPiece(test).IsWhite & mycolourIsWhite)
            {
                pieceProtege.Add(board.GetPiece(test));
            }
            else if (!(board.GetPiece(test).IsWhite) & !mycolourIsWhite)
            {
                pieceProtege.Add(board.GetPiece(test));
            }
        }

        //j'ai la liste des pièce que je protège

        List<Square> positionPieceProtege = new List<Square>();
        foreach (Piece pieceprotege in pieceProtege)
        {
            positionPieceProtege.Add(pieceprotege.Square);
        }
        //j'obtient la liste des position de mes pièces protéger

        board.MakeMove(move);

        foreach (Square i in positionPieceProtege)
        {
            if (!board.SquareIsAttackedByOpponent(i))
            {
                pieceseuldefenseur++;
            };
        }

        board.UndoMove(move);
        //je fais le move et vois si elle sont encore protégée


        return pieceseuldefenseur;
    }
    bool jemedeveloppe(Board board, Move move)
    {
        if (((move.TargetSquare.Index > move.StartSquare.Index + move.StartSquare.Index % 8) & mycolourIsWhite) | ((move.TargetSquare.Index < move.StartSquare.Index - move.StartSquare.Index % 8) & !mycolourIsWhite))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    bool jemetscheck(Board board, Move move)
    {
        bool check = false;
        board.MakeMove(move);
        if (board.IsInCheck())
        {
            check = true;
        }
        board.UndoMove(move);
        return check;
    }

    bool jemenace(Board board, Move move)
    {
        board.MakeMove(move);
        bitboard = BitboardHelper.GetPieceAttacks(move.MovePieceType, move.StartSquare, board, board.IsWhiteToMove);
        List<int> indexPieceEnVue = new List<int>();
        while (BitboardHelper.GetNumberOfSetBits(bitboard) != 0)
        {
            indexPieceEnVue.Add(BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard));
        } //j'ai l'index de toute les pièces que peux attaquer avec ma pièce

        List<Piece> pieceAttaque = new List<Piece>();

        foreach (int i in indexPieceEnVue)
        {
            Square test = new Square(i);
            if (board.GetPiece(test).IsWhite & !mycolourIsWhite)
            {
                pieceAttaque.Add(board.GetPiece(test));
            }
            else if (!(board.GetPiece(test).IsWhite) & mycolourIsWhite)
            {
                pieceAttaque.Add(board.GetPiece(test));
            }
        }
        board.UndoMove(move);

        bitboard = BitboardHelper.GetPieceAttacks(move.MovePieceType, move.StartSquare, board, board.IsWhiteToMove);
        List<int> indexPieceEnVue2 = new List<int>();
        while (BitboardHelper.GetNumberOfSetBits(bitboard) != 0)
        {
            indexPieceEnVue2.Add(BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard));
        } //j'ai l'index de toute les pièces que peux attaquer avec ma pièce

        int nbrattaqueapres = pieceAttaque.Count;
        pieceAttaque.Clear();

        foreach (int i in indexPieceEnVue2)
        {
            Square test = new Square(i);
            if (board.GetPiece(test).IsWhite & !mycolourIsWhite)
            {
                pieceAttaque.Add(board.GetPiece(test));
            }
            else if (!(board.GetPiece(test).IsWhite) & mycolourIsWhite)
            {
                pieceAttaque.Add(board.GetPiece(test));
            }
        }

        if (pieceAttaque.Count < nbrattaqueapres)
        {
            return true;
        }

        return false;
    }
}