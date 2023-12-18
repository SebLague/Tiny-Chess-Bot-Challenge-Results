namespace auto_Bot_290;
using ChessChallenge.API;
using System;
public class Bot_290 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        bool color = board.IsWhiteToMove;
        int[] pieceValues = { 0, 100, 290, 300, 500, 900, 10000 };
        Move[] moves = board.GetLegalMoves();
        Random rng = new();
        Move topMove = moves[rng.Next(moves.Length)];
        int bestCapture = -10000000;
        int rank1 = 0;
        if (!color)
        {
            rank1 = 7;
        }
        int layer = 0;
        foreach (Move move in moves)
        {
            int moveVal = 0;
            //avoids moving into mates in 1
            if (OppMi1(board, move))
            {
                continue;
            }
            //finds mate in 3 or less
            if (MateInSoon(board, move))
            {
                topMove = move;
                break;
            }
            //encourages taking pieces; values pieces based on pieceValues array
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
            moveVal += capturedPieceValue;
            //discourages moving into attacked squares
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                if (IsProtected(board, move))
                {
                    moveVal += OppCapVal(board, move);
                }
                moveVal -= pieceValues[(int)move.MovePieceType];
            }
            //encourages restricting opponents moves in endgame
            if (GetTotVal(board, !color) <= 800)
            {
                board.MakeMove(move);
                Move[] mmoves = board.GetLegalMoves();
                int val = 0;
                foreach (Move mmove in mmoves)
                {
                    int valTemp = DistFromCenter(board, !color);
                    if (valTemp < val)
                    {
                        val = valTemp;
                    }
                }
                moveVal += 25 * val;
                board.UndoMove(move);
            }
            //encourages moving out of attacked squares
            if (board.SquareIsAttackedByOpponent(move.StartSquare))
            {
                if (IsProtected(board, move))
                {
                    int topCap = OppCapVal(board, move);
                    moveVal -= topCap;
                }
                moveVal += pieceValues[(int)move.MovePieceType];
            }
            //encourages taking the center
            if ((move.TargetSquare.Index == 27 || move.TargetSquare.Index == 28 || move.TargetSquare.Index == 36 || move.TargetSquare.Index == 35) && !(move.StartSquare.Index == 27 || move.StartSquare.Index == 28 || move.StartSquare.Index == 36 || move.StartSquare.Index == 35))
            {
                moveVal += 90;
            }
            //encourages promoting to queens
            if (move.IsPromotion && move.PromotionPieceType == PieceType.Queen)
            {
                moveVal += 800;
            }
            if (board.GameMoveHistory.Length < 20)
            {
                //discourages moving the king forward at the start of the game
                if (move.TargetSquare.Rank != rank1 && move.MovePieceType == PieceType.King)
                {
                    moveVal -= 200;
                }
            }
            //encourages developing
            if (move.StartSquare.Rank == rank1 && move.TargetSquare.Rank != rank1 && (move.MovePieceType == PieceType.Knight || move.MovePieceType == PieceType.Bishop))
            {
                moveVal += 200;
            }
            else if (move.MovePieceType == PieceType.Pawn && (move.TargetSquare.File == 3 || move.TargetSquare.File == 4))
            {
                moveVal += 100;
            }
            //discourages moving knights to the edges
            if (move.MovePieceType == PieceType.Knight && (move.TargetSquare.File == 0 || move.TargetSquare.File == 7))
            {
                moveVal -= 150;
            }
            //encourages castling
            if (move.IsCastles)
            {
                moveVal += 150;
            }
            //encourages bot to push pawns if they're not being attacked
            if (move.MovePieceType == PieceType.Pawn)
            {
                //encourages pushing center pawns over outer pawns
                if (move.TargetSquare.File == 0 || move.TargetSquare.File == 7)
                {
                    moveVal -= 50;
                }
                if (move.TargetSquare.File == 1 || move.TargetSquare.File == 6)
                {
                    moveVal -= 30;
                }
                if (move.TargetSquare.File == 2 || move.TargetSquare.File == 5)
                {
                    moveVal -= 10;
                }
                moveVal += 90;
            }
            //checks move strength against current best move
            if (moveVal > bestCapture)
            {
                topMove = move;
                bestCapture = moveVal;
            }
        }
        return topMove;
        int DistFromCenter(Board board, bool color)
        {
            int dist = (int)Math.Round(Math.Abs(board.GetKingSquare(color).File - 3.5) + Math.Abs(board.GetKingSquare(color).Rank - 3.5));
            return dist;
        }
        bool MateInSoon(Board board, Move move)
        {
            board.MakeMove(move);
            Move[] moves = board.GetLegalMoves();
            foreach (Move mmove in moves)
            {
                board.MakeMove(mmove);
                Move[] mmoves = board.GetLegalMoves();
                foreach (Move mmmove in mmoves)
                {
                    board.MakeMove(mmmove);
                    Move[] mmmoves = board.GetLegalMoves();
                    foreach (Move mmmmove in mmmoves)
                    {
                        if (!OppMi1(board, mmmmove))
                        {
                            board.UndoMove(mmmove);
                            board.UndoMove(mmove);
                            board.UndoMove(move);
                            return false;
                        }
                    }
                    board.UndoMove(mmmove);
                }
                board.UndoMove(mmove);
            }
            board.UndoMove(move);
            return true;
        }
        bool OppMi1(Board board, Move move)
        {
            board.MakeMove(move);
            Move[] moves = board.GetLegalMoves();
            foreach (Move mmove in moves)
            {
                if (MoveIsCheckmate(board, mmove))
                {
                    board.UndoMove(move);
                    return true;
                }
            }
            board.UndoMove(move);
            return false;
        }
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }
        bool IsProtected(Board board, Move move)
        {
            Move[] moves = board.GetLegalMoves();
            foreach (Move sec in moves)
            {
                if (sec.TargetSquare.Index == move.TargetSquare.Index && sec.StartSquare.Index != move.StartSquare.Index)
                {
                    return true;
                }
            }
            return false;
        }
        int OppCapVal(Board board, Move move)
        {
            int moveInd = move.TargetSquare.Index;
            board.MakeMove(move);
            Move[] oppmoves = board.GetLegalMoves();
            int topCap = 1000;
            foreach (Move take in oppmoves)
            {
                if (take.TargetSquare.Index == moveInd)
                {
                    int temp1 = pieceValues[(int)take.MovePieceType];
                    if (temp1 < topCap)
                    {
                        topCap = temp1;
                    }
                }
            }
            board.UndoMove(move);
            return topCap;
        }
        int GetTotVal(Board board, bool white)
        {
            int TotVal = 0;
            int num = 1;
            while (num <= 5)
            {
                int Size = 0;
                PieceList list = board.GetPieceList((PieceType)num, white);
                Size = list.Count;
                TotVal += Size * pieceValues[num];
                num += 1;
            }
            return TotVal;
        }
    }
}
