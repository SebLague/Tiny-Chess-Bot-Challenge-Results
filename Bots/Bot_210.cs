namespace auto_Bot_210;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// Edited and Authored by Dane Smith by way of Sebastion Lague tournament for mini chess bots.
// yoda3
// discord name yodanater
// danems11@gmail.com

public class Bot_210 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 320, 500, 900, 10000 };
    //int[] pawnRankVal = { 0, 0, 1, 3, 10, 20, 50, 0};
    int[] pawnVal = { 0,  0,  0,  0,
                      -5, 0, 0,-20,
                      0, -5,-10,  0,
                      2,  2, 20, 20,
                      5,  5, 10, 25,
                     10, 15, 25, 30,
                     50, 50, 50, 50,
                      0,  0,  0,  0};
    int[] bk_rank_file = { -10, 2, 5, 8, 8, 5, 2, -10 };
    //int cur_move = 0;
    int max_depth = 20;
    int max_milli_turn = 1500;
    //Hashtable t = new Hashtable();
    //Dictionary<string, string> t = new Dictionary<string, string>();
    // int time_panic1 = 20000;
    // int time_panic2 = 10000;
    // int time_panic3 = 5000;

    // int panic_turn1 = 500;
    // int panic_turn2 = 250;
    // int panic_turn3 = 90;
    //Dictionary<String, int> t = new Dictionary<String, int>;
    //int moves_evaluated = 0;
    //int ply=0;
    // int final_alpha = 0;
    // int final_beta = 0;
    Move[] killerMoves = new Move[2];//{Move.NullMove, Move.NullMove};

    public Move Think(Board board, Timer timer)
    {
        //moves_evaluated = 0;
        Move[] moves = board.GetLegalMoves();
        //bool IsWhite = board.IsWhiteToMove; // True is White, False is True
        if (timer.MillisecondsRemaining < 20000)
        {
            max_milli_turn = 500;
        }
        if (timer.MillisecondsRemaining < 10000)
        {
            max_milli_turn = 250;
        }
        if (timer.MillisecondsRemaining < 5000)
        {
            max_milli_turn = 90;
        }

        Move best_move = Move.NullMove;
        //int depth=0;
        for (int i = 1; i < max_depth; i++)
        {
            //ply = 0;
            //(Move cur_move, int move_eval) = MiniMaxTry(timer, board, i, -10000000, 10000000, board.IsWhiteToMove, true, false);
            Move cur_move = MinMaxMove(timer, board, i, board.IsWhiteToMove);
            //DivertedConsole.Write("Depth: "+i+" turn timer: "+timer.MillisecondsElapsedThisTurn+ " moves evaluated: "+moves_evaluated);
            //DivertedConsole.Write(move_eval);
            if (i == 1 || timer.MillisecondsElapsedThisTurn < max_milli_turn)
            {
                best_move = cur_move;
                if (i == 4 && timer.MillisecondsElapsedThisTurn > 500)
                {
                    break;
                }
            }
            else
            {

                break;
            }
            //DivertedConsole.Write(i);
            //depth=i;
        }
        //DivertedConsole.Write(depth+ " depth");//, final beta: "+final_beta + " final alpha: "+final_alpha);

        return best_move;
    }

    Move MinMaxMove(Timer timer, Board board, int depth, bool IsWhite)
    {
        // Move[] original_moves = board.GetLegalMoves();
        // Move[] capture_moves = board.GetLegalMoves(true);
        // // add killermoves to capture list
        // // if (original_moves.Contains(killerMoves[0]) && original_moves.Contains(killerMoves[1])){
        // //     Move[] killers = killerMoves.Except(capture_moves);
        // //     capture_moves = capture_moves.Concat(killers);
        // // }
        // // subtract interesting moves from original moves
        // Move[] uncaptures = original_moves.Except(capture_moves).ToArray();
        // // now order of moves goes captures, killers, then others
        // Move[] moves = capture_moves.Concat(uncaptures).ToArray();
        int best_eval = -1000000;

        Move[] moves = OrderMoves(board);
        Move best_move = moves[0];
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return move;
            }
            int move_eval = MinMax(timer, board, depth - 1, -1000000, 1000000, IsWhite, false, true);
            board.UndoMove(move);
            if (move_eval > best_eval)
            {
                best_eval = move_eval;
                best_move = move;
                //DivertedConsole.Write("Best score: "+best_eval + " best move: "+best_move);
            }
        }
        //DivertedConsole.Write("Best score: "+best_eval + " best move: "+best_move);
        return best_move;
    }

    Move[] OrderMoves(Board b)
    {
        List<Tuple<Move, int>> mlist = new List<Tuple<Move, int>>();
        foreach (Move move in b.GetLegalMoves())
        {
            if (move.IsPromotion)
            {
                mlist.Add(Tuple.Create(move, 5));
            }
            else if (move.IsCapture)
            {
                mlist.Add(Tuple.Create(move, Array.IndexOf(Enum.GetValues(move.CapturePieceType.GetType()), move.CapturePieceType)));
            }
            else if (killerMoves.Contains(move))
            {
                //DivertedConsole.Write("Killer moved used!");
                mlist.Add(Tuple.Create(move, 0));
            }
            else
            {
                mlist.Add(Tuple.Create(move, -1));
            }
        }
        return mlist.OrderByDescending(t => t.Item2).Select(t => t.Item1).ToArray();
    }

    int MinMax(Timer timer, Board board, int depth, int alpha, int beta, bool IsWhite, bool maximizing, bool doNull)
    {
        if (board.IsInCheckmate())
        {
            if (maximizing) { return -1000000 + depth; }
            else { return 1000000 - depth; }
        }
        if (board.IsDraw())
        {
            return 0;
        }
        if (depth == 0 || timer.MillisecondsElapsedThisTurn > max_milli_turn)
        {
            return Evaluate(board, IsWhite);
        }

        // if (board.IsInCheck()){
        //     depth+=1;
        // }

        // null move hueristic, check at lower depth if null move produces a cutoff, then return the cutoff 
        // for faster search.
        // if (doNull && !board.IsInCheck() && depth>=4){
        //     board.TrySkipTurn();
        //     int null_eval = MinMax(timer, board, depth-4, alpha, beta, IsWhite, !maximizing, false);
        //     board.UndoSkipTurn();
        //     if (null_eval >= beta && maximizing){
        //         return beta;
        //     }
        //     else if(null_eval <= alpha && !maximizing){
        //         return alpha;
        //     }
        // }
        int best_eval = 0;
        // Move[] original_moves = board.GetLegalMoves();
        // Move[] capture_moves = board.GetLegalMoves(true);
        // // add killermoves to capture list

        // // subtract interesting moves from original moves
        // Move[] uncaptures = original_moves.Except(capture_moves).ToArray();

        // // if (uncaptures.Contains(killerMoves[0])){
        // //     int index = uncaptures.IndexOf(killerMoves[0]);
        // //     uncaptures.RemoveAt(index);
        // //     uncaptures.Insert(0,killerMoves[0]);

        // //     //Move[] killers = killerMoves.Except(capture_moves);
        // //     //capture_moves = capture_moves.Concat(killers);
        // // }
        // // now order of moves goes captures, killers, then others
        // Move[] moves = capture_moves.Concat(uncaptures).ToArray();
        if (maximizing)
        {
            best_eval = -1000000;
            foreach (Move move in OrderMoves(board))
            {
                //if (move in board.GetLegalMoves())
                board.MakeMove(move);
                //if (board.IsInCheckmate()){return 1000000;}
                //if (board.IsInCheck()){depth++;}
                // String fen = board.GetFenString()+depth.ToString();
                // int eval = 0;
                // if(t.ContainsKey(fen)){
                //     eval = int.Parse(t[fen]);
                //    // DivertedConsole.Write(fen);
                // }
                // else{
                //     eval = MinMax(timer, board, depth-1, alpha, beta, IsWhite, false, true);
                //     t.Add(fen, eval.ToString());
                // }
                // if (board.IsInCheck()){
                //     depth+=1;
                // }

                // best_eval = Math.Max(best_eval,eval);
                best_eval = Math.Max(best_eval, MinMax(timer, board, depth - 1, alpha, beta, IsWhite, false, true));
                board.UndoMove(move);

                if (best_eval >= beta)
                {
                    if (best_eval > alpha && !move.IsCapture)
                    {
                        //DivertedConsole.Write("Max killer hit!");
                        killerMoves[1] = killerMoves[0];
                        killerMoves[0] = move;
                    }
                    //DivertedConsole.Write("hit beta break!");
                    return best_eval;
                }
                alpha = Math.Max(alpha, best_eval);
                //final_alpha = alpha;


            }
        }
        else
        {
            best_eval = 1000000;
            foreach (Move move in OrderMoves(board))
            {
                board.MakeMove(move);
                //if (board.IsInCheck()){depth++;}
                // String fen = board.GetFenString()+depth.ToString();
                // int eval = 0;
                // if(t.ContainsKey(fen)){
                //     eval = int.Parse(t[fen]);
                //     //DivertedConsole.Write(fen);
                // }
                // else{
                //     eval = MinMax(timer, board, depth-1, alpha, beta, IsWhite, true, true);
                //     t.Add(fen, eval.ToString());
                // }
                // best_eval = Math.Min(best_eval,eval);
                best_eval = Math.Min(best_eval, MinMax(timer, board, depth - 1, alpha, beta, IsWhite, true, true));
                board.UndoMove(move);

                if (best_eval <= alpha)
                {
                    if (best_eval < beta && !move.IsCapture)
                    {
                        //DivertedConsole.Write("Min killer hit!");
                        killerMoves[1] = killerMoves[0];
                        killerMoves[0] = move;
                    }
                    // //DivertedConsole.Write("hit alpha break!");
                    return best_eval;
                }
                beta = Math.Min(beta, best_eval);
                //final_beta = beta;

            }
        }
        //final_alpha = alpha;
        //final_beta = beta;
        return best_eval;

    }

    // (Move, int) MiniMaxTry(Timer timer, Board board, int depth, int alpha, int beta, bool IsWhite, bool maximizing, bool doNull){

    //     //DivertedConsole.Write(timer.MillisecondsElapsedThisTurn);
    //     //DivertedConsole.Write(depth);
    //     Move[] moves = board.GetLegalMoves();
    //     //Random rng = new();
    //     //Move best_move=Move.NullMove; 
    //     if (moves.Length==0){
    //         return (Move.NullMove, Evaluate(board, IsWhite));
    //     }
    //     Move best_move = moves[0];
    //     if(depth==0 || timer.MillisecondsElapsedThisTurn > max_milli_turn){
    //         return (moves[0], Evaluate(board, IsWhite));
    //     }
    //     if (board.IsInCheck()){
    //         depth+=1;
    //     }

    //     // if (doNull && !board.IsInCheck() && depth>=3){
    //     //     board.TrySkipTurn();
    //     //     (Move _mnull, int null_eval) = MiniMaxTry(timer, board, depth-3, -beta, -beta+1, IsWhite, false, true);
    //     //     board.UndoSkipTurn();
    //     //     if (null_eval >= beta){
    //     //         //beta = null_eval;
    //     //         //DivertedConsole.Write("-null eval >= beta hit!");
    //     //         return (_mnull, null_eval);
    //     //     }
    //     // }

    //     //board.MakeMove(moveToPlay);
    //     int best_eval = 0;
    //     //Move best_move; // set to random move
    //     //board.UndoMove(moveToPlay);
    //     //int best_eval = current_eval;
    //     // then find better one
    //     int move_eval =0;
    //     if (maximizing){
    //         best_eval = -1000000;
    //         foreach (Move move in moves)
    //         {
    //             //moves_evaluated+=1;
    //             board.MakeMove(move);
    //             if (board.IsInCheckmate()){
    //                 board.UndoMove(move);
    //                 return (move, 1000000);
    //             }
    //             if (board.IsInCheck()){
    //                 depth+=1;
    //             }
    //             if (board.IsDraw()){
    //                 //board.UndoMove(move);
    //                //return (move, 0);
    //                move_eval=0;
    //             }else{
    //                 (Move _m, move_eval) = MiniMaxTry(timer, board, depth-1, alpha, beta, IsWhite, false, true);
    //             }
    //             //int this_move_eval = Evaluate(board, IsWhite);
    //             //(Move _m, int move_eval) = MiniMaxTry(timer, board, depth-1, alpha, beta, IsWhite, false);
    //             if (move_eval > best_eval){
    //                 //DivertedConsole.Write("best eval: "+move_eval.ToString()+" move: "+move);
    //                 best_eval = move_eval;
    //                 best_move = move;//MiniMaxTry(board, depth+1, !IsWhite);
    //             }
    //             board.UndoMove(move);
    //             alpha = Math.Max(alpha, best_eval);
    //             if (best_eval > beta){
    //                 //DivertedConsole.Write("Beta working");
    //                 break;
    //             }

    //         }            
    //     }
    //     else{
    //         best_eval = 1000000;
    //         foreach (Move move in moves)
    //         {
    //             //moves_evaluated+=1;

    //             board.MakeMove(move);
    //             bool isMate = board.IsInCheckmate();
    //             if (isMate){
    //                 board.UndoMove(move);
    //                 return (move, -1000000);
    //             }
    //             // if (board.IsInCheck()){
    //             //     depth+=1;
    //             // }
    //             if (board.IsDraw()){
    //                 //board.UndoMove(move);
    //                //return (move, 0);
    //                move_eval=0;
    //             }else{
    //                 (Move _m, move_eval) = MiniMaxTry(timer, board, depth-1, alpha, beta, IsWhite, true, true);
    //             }
    //             //int this_move_eval = Evaluate(board, IsWhite);
    //             //(Move _m, int move_eval) = MiniMaxTry(timer, board, depth-1, alpha, beta, IsWhite, true);
    //             if (move_eval < best_eval){
    //                 best_eval = move_eval;
    //                 best_move = move;//MiniMaxTry(board, depth+1, !IsWhite);
    //             }
    //             board.UndoMove(move);
    //             beta = Math.Min(beta, best_eval);
    //             if (best_eval < alpha){
    //                 //DivertedConsole.Write("Alpha working: "+alpha.ToString());
    //                 break;
    //             }

    //         }   
    //     }
    //     final_alpha = alpha;
    //     final_beta = beta;
    //     return (best_move, best_eval);
    // }

    int CenterBonus(Piece p)
    {
        return bk_rank_file[p.Square.Rank] + bk_rank_file[p.Square.File];
    }
    // int SpaceBonus(Move[] moves){
    //     int val = 0;
    //     foreach (Move m in moves){
    //         val+=1;
    //         if(m.IsCapture){
    //             val+=4;
    //         }
    //         if(m.IsPromotion){
    //             val+=300;
    //         }
    //         if(m.IsCastles){
    //             val+=90;
    //         }
    //     }
    //     return val;
    // }

    // int KingCloseness(Board board, Piece p, bool white){
    //     //Square king_square = ; // get opposite color king square;
    //     //int distance = Math.Abs(board.GetKingSquare(!white).File - p.Square.File)+Math.Abs(board.GetKingSquare(!white).Rank - p.Square.Rank);
    //     return 14-Math.Abs(board.GetKingSquare(!white).File - p.Square.File)+Math.Abs(board.GetKingSquare(!white).Rank - p.Square.Rank); // max distance is 14

    // }

    int Evaluate(Board board, bool white)
    {
        // if (board.IsDraw()){
        //     return 0;
        // }
        int Evaluation = 0;
        //Move[] moves = board.GetLegalMoves();
        //PieceList[] pLs = board.GetAllPieceLists();
        //DivertedConsole.Write(Convert.ToString((long)board.WhitePiecesBitboard, 2).Split()[0]);
        // int whitesum = 0;
        // foreach (char c in Convert.ToString((long)board.WhitePiecesBitboard, 2)){
        //     whitesum += Math.Clamp(Convert.ToInt32(c), 0,1);
        // }
        // int whitesum = 10-BitboardHelper.GetNumberOfSetBits(board.WhitePiecesBitboard);
        // int blacksum = 10-BitboardHelper.GetNumberOfSetBits(board.BlackPiecesBitboard);
        //DivertedConsole.Write("WhiteSum: " +whitesum.ToString());
        foreach (PieceList pList in board.GetAllPieceLists())
        {
            foreach (Piece p in pList)
            {
                if (p.IsWhite)
                { // white pieces evaluation
                    if (p.IsPawn)
                    {
                        Evaluation += pieceValues[1];
                        //int ind = p.Square.Rank*4+ Math.Abs(3*(p.Square.File/4)-p.Square.File%4);
                        //DivertedConsole.Write("Pawn ind: " +ind.ToString() + " file: "+ p.Square.File + " rank: "+ p.Square.Rank);
                        Evaluation += pawnVal[p.Square.Rank * 4 + Math.Abs(3 * (p.Square.File / 4) - p.Square.File % 4)];
                        //Evaluation += CenterBonus(p);
                        //Evaluation += SpaceBonus(moves, p);
                    }
                    if (p.IsKnight)
                    {
                        Evaluation += pieceValues[2] + CenterBonus(p);
                        //Evaluation += CenterBonus(p);
                        //Evaluation += SpaceBonus(moves, p);
                        //Evaluation += KingCloseness(board, p, white)+blacksum*2;
                    }
                    if (p.IsBishop)
                    {
                        Evaluation += pieceValues[3] + CenterBonus(p) / 2;
                        //Evaluation += ;
                        //Evaluation += SpaceBonus(moves, p);
                    }
                    if (p.IsRook)
                    {
                        Evaluation += pieceValues[4];
                        if (p.Square.Rank == 7)
                            Evaluation += 10;
                        //Evaluation += SpaceBonus(moves, p)*2;
                    }
                    if (p.IsQueen)
                    {
                        Evaluation += pieceValues[5] + CenterBonus(p) / 2;
                        //Evaluation += CenterBonus(p)/2;
                        //Evaluation += SpaceBonus(moves, p)/2;
                        //Evaluation += KingCloseness(board, p, white)+blacksum*2;
                    }
                    if (p.IsKing)
                    {
                        Evaluation += pieceValues[6];
                        //Evaluation += KingCloseness(board, p, white)+blacksum;
                    }
                    //Evaluation += SpaceBonus(moves);
                }
                else
                { // black pieces evaluation
                    if (p.IsPawn)
                    {
                        Evaluation -= pieceValues[1];
                        //int ind = p.Square.Rank*4+ Math.Abs(3*(p.Square.File/4)-p.Square.File%4);
                        Evaluation -= pawnVal[32 - (p.Square.Rank * 4 + Math.Abs(3 * (p.Square.File / 4) - p.Square.File % 4))];
                        //Evaluation -= pawnRankVal[7-p.Square.Rank];
                        //Evaluation -= CenterBonus(p);
                        //Evaluation -= SpaceBonus(moves, p);
                    }
                    if (p.IsKnight)
                    {
                        Evaluation -= pieceValues[2] + CenterBonus(p);
                        //Evaluation -= CenterBonus(p);
                        //Evaluation -= SpaceBonus(moves, p);
                        //Evaluation -= KingCloseness(board, p, !white)-whitesum*2;
                    }
                    if (p.IsBishop)
                    {
                        Evaluation -= pieceValues[3] + CenterBonus(p) / 2;
                        //Evaluation -= CenterBonus(p)/2;
                        //Evaluation -= SpaceBonus(moves, p);
                    }
                    if (p.IsRook)
                    {
                        Evaluation -= pieceValues[4];
                        if (p.Square.Rank == 2)
                            Evaluation -= 10;
                        //Evaluation -= SpaceBonus(moves, p)*2;
                    }
                    if (p.IsQueen)
                    {
                        Evaluation -= pieceValues[5] + CenterBonus(p) / 2;
                        //Evaluation -= CenterBonus(p)/2;
                        //Evaluation -= SpaceBonus(moves, p)/2;
                        //Evaluation -= KingCloseness(board, p, !white)-whitesum*2;
                    }
                    if (p.IsKing)
                    {
                        Evaluation -= pieceValues[6];
                        //Evaluation -= KingCloseness(board, p, !white)-whitesum;
                    }
                    //Evaluation -= SpaceBonus(moves);
                }
                //DivertedConsole.Write(p);
            }
        }
        if (white)
            return Evaluation;
        else
            return -Evaluation;
    }
}