namespace auto_Bot_71;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
// had a problim withe a systhem Timer variabel... needet to spesify eventhough i don't use the Timer at all
using Timer = ChessChallenge.API.Timer;

public class Bot_71 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        //get legal moves
        Move[] moves_A = board.GetLegalMoves();
        if (moves_A.Length == 1)
        {
            //return, if only 1 legal move
            return moves_A[0];
        }
        else
        {
            //convirt ligel moves to List (I prevere Lists over Arrays)
            List<Move> moves = new();
            foreach (Move move in moves_A)
            {
                moves.Add(move);
            }

            //checkmate ?
            List<Move> checkmate_moves = new();
            foreach (Move move in moves)
            {
                board.MakeMove(move);

                if (board.IsInCheckmate())
                {
                    checkmate_moves.Add(move);
                }

                board.UndoMove(move);
            }

            if (checkmate_moves.Count > 0)
            {
                //checkmate !
                return Rando(checkmate_moves);
            }
            else
            {
                //no checkmate possibel
                //opponent checkmate ?
                List<Move> checkmate_save_moves = new();
                List<Move> check_save_moves = new();
                foreach (Move move in moves)
                {
                    board.MakeMove(move);
                    bool mate_save = true;
                    bool save = true;
                    Move[] opponent_moves = board.GetLegalMoves();
                    foreach (Move opponent_move in opponent_moves)
                    {
                        board.MakeMove(opponent_move);

                        if (board.IsInCheckmate())
                        {
                            mate_save = false;
                            save = false;
                            board.UndoMove(opponent_move);
                            break;
                        }
                        else if (board.IsInCheck())
                        {
                            save = false;
                        }

                        board.UndoMove(opponent_move);
                    }

                    if (mate_save)
                    {
                        checkmate_save_moves.Add(move);
                    }
                    if (save)
                    {
                        check_save_moves.Add(move);
                    }

                    board.UndoMove(move);
                }

                if (check_save_moves.Count == 1)
                {
                    return check_save_moves[0];
                }
                else if (check_save_moves.Count > 0)
                {
                    return Save(board, check_save_moves);
                }
                else if (checkmate_save_moves.Count == 1)
                {
                    //return, if only 1 checkmate save move
                    return checkmate_save_moves[0];
                }
                else if (checkmate_save_moves.Count > 0)
                {
                    return Save(board, checkmate_save_moves);
                }
                else
                {
                    return Save(board, moves);
                }
            }
        }
    }

    private static Move Save(Board board, List<Move> moves)
    {
        //creating a Dictionary that sores how many peaces can acess the same squeare
        Dictionary<Square, int> heat = new();
        foreach (Move move in moves)
        {
            if (heat.ContainsKey(move.TargetSquare))
            {
                heat[move.TargetSquare] += 1;
            }
            else
            {
                heat[move.TargetSquare] = 1;
            }
        }

        //creating a List on "save" moves
        List<Move> save_moves = new();
        foreach (Move move in moves)
        {
            if (heat[move.TargetSquare] > 1)
            {

                //Moves to squares that can be accest by at least one other peace
                save_moves.Add(move);
            }
            else
            {

                //Moves to squares that can not be accest by the opponint in the next move
                board.MakeMove(move);

                Move[] opponent_moves = board.GetLegalMoves();
                bool save = true;
                foreach (Move opponent_move in opponent_moves)
                {
                    if (opponent_move.TargetSquare.Name == move.TargetSquare.Name)
                    {
                        save = false;
                        break;
                    }
                }

                if (save)
                {
                    save_moves.Add(move);
                }

                board.UndoMove(move);
            }
        }
        if (save_moves.Count == 1)
        {
            //return, if only 1 "save" move
            return save_moves[0];
        }
        else if (save_moves.Count > 0)
        {
            return Check_him(board, save_moves);
        }
        else
        {
            return Check_him(board, moves);
        }
    }

    private static Move Check_him(Board board, List<Move> moves)
    {
        List<Move> checkhim_moves = new();
        foreach (Move move in moves)
        {
            board.MakeMove(move);

            if (board.IsInCheck())
            {
                checkhim_moves.Add(move);
            }

            board.UndoMove(move);
        }

        if (checkhim_moves.Count == 1)
        {
            //return, if only 1 Chech him move
            return checkhim_moves[0];
        }
        else if (checkhim_moves.Count > 0)
        {
            return Captur(board, checkhim_moves);
        }
        else
        {
            return Captur(board, moves);
        }
    }

    private static Move Captur(Board board, List<Move> moves)
    {
        List<Move> captur_move = new();
        foreach (Move move in moves)
        {
            if (move.IsCapture || move.IsPromotion)
            {
                captur_move.Add(move);
            }
        }

        if (captur_move.Count == 1)
        {
            // retorn if only 1 possible captur/promotion move
            return captur_move[0];
        }
        else if (captur_move.Count > 0)
        {
            return Drow(board, captur_move, true);
        }
        else
        {
            return Drow(board, moves);
        }
    }

    private static Move Drow(Board board, List<Move> moves, bool balance_flag = false)
    {
        List<Move> drow_save_moves = new();
        foreach (Move move in moves)
        {
            board.MakeMove(move);

            if (!board.IsDraw())
            {
                drow_save_moves.Add(move);
            }

            board.UndoMove(move);
        }

        if (drow_save_moves.Count == 1)
        {
            return drow_save_moves[0];
        }
        else if (drow_save_moves.Count > 0)
        {
            return Rando(drow_save_moves, balance_flag);
        }
        else
        {
            return Rando(moves, balance_flag);
        }
    }

    private static Move Rando(List<Move> moves, bool balance_flag = false)
    {
        if (balance_flag)
        {
            return Balance(moves);
        }

        Random rand = new();
        return moves[rand.Next(0, moves.Count)];
    }

    private static Move Balance(List<Move> moves)
    {
        Dictionary<Move, int> values = new();
        foreach (Move move in moves)
        {
            if (move.IsCapture)
            {
                values[move] = pice_id(move.CapturePieceType);
            }
            else if (move.IsPromotion)
            {
                values[move] = pice_id(move.CapturePieceType);
            }
            else
            {
                values[move] = 0;
            }
        }

        int hi = 0;
        foreach (KeyValuePair<Move, int> pair in values)
        {
            if (pair.Value == 5)
            {
                hi = 5;
                break;
            }
            else if (pair.Value > hi)
            {
                hi = pair.Value;
            }
        }

        List<Move> hivalue_moves = new();
        foreach (KeyValuePair<Move, int> pair in values)
        {
            if (pair.Value == hi)
            {
                hivalue_moves.Add(pair.Key);
            }
        }

        return Rando(hivalue_moves);
    }

    private static int pice_id(PieceType piece)
    {
        if (piece == PieceType.None) return 0;
        else if (piece == PieceType.Pawn) return 1;
        else if (piece == PieceType.Knight) return 2;
        else if (piece == PieceType.Bishop) return 3;
        else if (piece == PieceType.Rook) return 4;
        else if (piece == PieceType.Queen) return 5;
        else if (piece == PieceType.King) return 6;
        else return 0;
    }
}