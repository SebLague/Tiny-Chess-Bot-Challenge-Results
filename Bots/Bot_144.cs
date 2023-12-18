namespace auto_Bot_144;
using ChessChallenge.API;
using System;
using System.Collections.Generic;


public class Bot_144 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        List<int> list = new List<int>(); //Stores all moves with the same score
        bool tied = false;
        System.Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        Move move = new();
        int score = new();
        int newscore = new();
        score = 0;

        for (int i = 0; i < allMoves.Length; i++)
        {
            move = allMoves[i];
            //Mate in One takes ultimate priority
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                score = 225;
                list.Clear();
                list.Add(i);
                tied = false;
                break;
            }
            //Cause stalemate
            if (MoveIsStalemate(board, move))
            {
                if (board.IsInsufficientMaterial())
                {
                    moveToPlay = move;
                    score = 200;
                    list.Clear();
                    list.Add(i);
                    tied = false;
                }
                break;
            }
            //High priority Castling
            if (move.IsCastles)
            {
                newscore = 45;
                if (newscore > score)
                {
                    moveToPlay = move;
                    score = newscore;
                    list.Clear();
                    list.Add(i);
                    tied = false;
                }
            }
            //Avoid moves leading to a direct Checkmate
            Board whiteBoard = Board.CreateBoardFromFEN(board.GetFenString());
            whiteBoard.MakeMove(move);
            Move[] whiteMoves = whiteBoard.GetLegalMoves();
            bool dropMove = false;
            foreach (Move moveW in whiteMoves)
            {
                if (MoveIsCheckmate(whiteBoard, moveW) || (MoveIsStalemate(whiteBoard, moveW)))
                {
                    dropMove = true;
                }
            }
            whiteBoard.UndoMove(move);
            if (dropMove)
            {
                break;
            }
            newscore = 0;
            //Promotion choices
            if (move.IsPromotion)
            {
                if (Convert.ToInt32(move.PromotionPieceType) == 5)
                {
                    newscore = 205;
                    if (newscore > score)
                    {
                        moveToPlay = move;
                        score = newscore;
                        list.Clear();
                        list.Add(i);
                        tied = false;
                    }
                }
            }
            //Judge whether to take a capture
            if (move.IsCapture)
            {
                newscore += (Convert.ToInt32(move.CapturePieceType) + 1 - Convert.ToInt32(move.MovePieceType)) * 4;
                if (Convert.ToInt32(move.MovePieceType) == 6 && (board.HasKingsideCastleRight(board.IsWhiteToMove) || board.HasQueensideCastleRight(board.IsWhiteToMove)))
                {
                    newscore -= 30;
                }
                if (board.SquareIsAttackedByOpponent(move.TargetSquare) == false)
                {
                    newscore = (Convert.ToInt32(move.CapturePieceType)) * 4;
                }
                int piecesAttacked = PiecesUnderAttack(board);
                Board whiteBoard2 = Board.CreateBoardFromFEN(board.GetFenString());
                whiteBoard2.MakeMove(move);
                whiteBoard2.ForceSkipTurn();
                int piecesNowAttacked = PiecesUnderAttack(whiteBoard2);
                whiteBoard2.UndoMove(move);
                whiteBoard2.UndoSkipTurn();
                if (piecesAttacked > piecesNowAttacked)
                {
                    newscore = newscore * 4;
                }
                if (newscore > score)
                {
                    moveToPlay = move;
                    score = newscore;
                    list.Clear();
                    list.Add(i);
                    tied = false;
                }
            }

            //Take moves that enable more moves
            //newscore = 0;
            if (move.IsCapture == false)
            {
                //Board newBoard = Board.CreateBoardFromFEN(board.GetFenString());
                board.MakeMove(move);
                board.ForceSkipTurn();
                Move[] moveTest = board.GetLegalMoves();
                board.UndoSkipTurn();
                board.UndoMove(move);
                foreach (Move moveB in moveTest)
                {
                    if (Convert.ToInt32(moveB.MovePieceType) > 1)
                    {
                        newscore += 1;
                    }
                }
                foreach (Move moveA in allMoves)
                {
                    if (Convert.ToInt32(moveA.MovePieceType) > 1)
                    {
                        newscore -= 1;
                    }
                }
                //newscore = moveTest.Length - allMoves.Length;
                if (board.SquareIsAttackedByOpponent(move.TargetSquare))
                {
                    newscore -= 15;
                }
                if (Convert.ToInt32(move.MovePieceType) == 6)
                {
                    newscore = newscore / 4;
                }
                //Hesitate to move the queen so fast
                if (Convert.ToInt32(move.MovePieceType) == 5)
                {
                    newscore -= 5;
                }
                int piecesAttackedPlayer = PiecesUnderAttack(board);
                board.ForceSkipTurn();
                int piecesAttackedEnemy = PiecesUnderAttack(board);
                board.UndoSkipTurn();

                Board whiteBoard2 = Board.CreateBoardFromFEN(board.GetFenString());
                whiteBoard2.MakeMove(move);
                int piecesNowAttackedEnemy = PiecesUnderAttack(whiteBoard2);
                whiteBoard2.ForceSkipTurn();
                int piecesNowAttackedPlayer = PiecesUnderAttack(whiteBoard2);
                whiteBoard2.UndoMove(move);
                whiteBoard2.UndoSkipTurn();

                if (piecesAttackedPlayer > piecesNowAttackedPlayer)
                {
                    newscore += piecesAttackedPlayer * 4;
                }
                if (piecesAttackedPlayer < piecesNowAttackedPlayer)
                {
                    newscore -= 10;
                }
                if (piecesAttackedEnemy > piecesNowAttackedEnemy)
                {
                    newscore -= 3;
                }
                if (piecesAttackedEnemy < piecesNowAttackedEnemy)
                {
                    newscore += 5;
                }

                if (Convert.ToInt32(move.MovePieceType) == 6 && (board.HasKingsideCastleRight(board.IsWhiteToMove) || board.HasQueensideCastleRight(board.IsWhiteToMove)))
                {
                    newscore -= 20;
                }
                //newscore -= rng.Next(5);
            }

            //Debug
            //if (newscore > 0)
            //{
            //DivertedConsole.Write(move + "   Score: " + newscore);
            //}

            if (newscore > score)
            {
                moveToPlay = move;
                score = newscore;
                list.Clear();
                list.Add(i);
                tied = false;
                newscore = 0;
            }
            if (newscore == score)
            {
                list.Add(i);
                tied = true;
            }
        }
        //Returns move
        if (tied)
        {
            int[] terms = list.ToArray();
            moveToPlay = allMoves[terms[rng.Next(terms.Length)]];
        }
        //DivertedConsole.Write("Caissa chose: " + moveToPlay);
        return moveToPlay;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        Board newBoard = Board.CreateBoardFromFEN(board.GetFenString());
        newBoard.MakeMove(move);
        bool isMate = newBoard.IsInCheckmate();
        newBoard.UndoMove(move);
        return isMate;
    }

    // Test if this move gives stalemate
    bool MoveIsStalemate(Board board, Move move)
    {
        Board newBoard = Board.CreateBoardFromFEN(board.GetFenString());
        newBoard.MakeMove(move);
        bool isMate = newBoard.IsInStalemate();
        newBoard.UndoMove(move);
        return isMate;
    }
    int PiecesUnderAttack(Board board)
    {
        int result = 0;
        for (int i = 1; i < 6; i++)
        {
            PieceList myUnits = board.GetPieceList((PieceType)Enum.ToObject(typeof(PieceType), i), board.IsWhiteToMove);
            foreach (Piece unit in myUnits)
            {
                if (board.SquareIsAttackedByOpponent(unit.Square))
                {
                    result += i * 4;
                }
            }
        }
        return result;
    }
}
//}