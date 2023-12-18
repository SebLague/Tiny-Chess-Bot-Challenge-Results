namespace auto_Bot_293;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;
using PieceList = ChessChallenge.API.PieceList;

public class Bot_293 : IChessBot
{
    public static Board b;
    public static Timer t;
    public static int turn_limit_ms = 1000;
    //public static int positions_searched = 0;
    //public static int max_depth = 0;
    public static QueueMove library = new QueueMove();

    public struct TotalEval
    {
        public int eval = 0;
        public bool mate = false;
        public int mate_in = -1;
        public bool draw = false;

        public TotalEval()
        {
        }

        public TotalEval(bool d)
        {
            draw = d;
        }

        public TotalEval(int e)
        {
            eval = e;
        }

        public TotalEval(int e, int m)
        {
            eval = e;
            mate = true;
            mate_in = m;
        }

        public void MateIn(int m)
        {
            mate = true;
            mate_in = m;
        }

        public TotalEval ParentEval()
        {
            TotalEval new_eval = this;
            new_eval.eval *= -1;
            if (new_eval.mate)
            {
                new_eval.mate_in++;
            }
            return new_eval;
        }

        /*public string String()
        {
            string str = "";
            if (mate)
            {
                str = "[Mate in " + mate_in + "]";
            } else if (draw)
            {
                str = "[Draw]";
            } else
            {
                str = "[" + eval + "]";
            }
            return str;
        }*/
    }

    public Move Think(Board board, Timer timer)
    {
        b = board;
        t = timer;
        if (b.GameMoveHistory.Length < 2)
        {
            library = new QueueMove();
        }
        Move move = EvalQueue();
        //DivertedConsole.Write("Searched " + positions_searched + " positions to maximum depth " + max_depth + ".");
        return move;
    }

    public class QueueMove
    {
        public Move move = Move.NullMove;
        public bool has_parent = false;
        public QueueMove parent;
        public bool has_children = false;
        public List<QueueMove> children = new List<QueueMove>();
        public TotalEval eval = new TotalEval(0);
        //public int depth = 0;

        public int CompareQueues(QueueMove a, QueueMove b)
        {
            TotalEval x = a.eval;
            TotalEval y = b.eval;
            bool x_good_mate = x.mate && x.mate_in % 2 == 1;
            bool y_good_mate = y.mate && y.mate_in % 2 == 1;
            if (x_good_mate && y_good_mate)
            {
                return x.mate_in - y.mate_in;
            }
            else if (x_good_mate)
            {
                return -1;
            }
            else if (y_good_mate)
            {
                return 1;
            }
            else if (x.mate && y.mate)
            {
                return y.mate_in - x.mate_in;
            }
            else if (x.mate)
            {
                return 1;
            }
            else if (y.mate)
            {
                return -1;
            }
            else if (x.draw)
            {
                return 1;
            }
            else if (y.draw)
            {
                return -1;
            }
            return y.eval - x.eval;
        }

        public QueueMove()
        {
            //positions_searched++;
        }

        public QueueMove(TotalEval e)
        {
            //positions_searched++;
            eval = e;
        }

        public QueueMove(Move m)
        {
            move = m;
            eval = Eval(m);
            //positions_searched++;
        }

        public QueueMove(Move m, QueueMove p)
        {
            move = m;
            eval = Eval(m);
            parent = p;
            has_parent = true;
            //positions_searched++;
            //depth = p.depth+1;
            //max_depth = Math.Max(depth, max_depth);
            b.MakeMove(m);
            b.UndoMove(m);
        }

        public bool Investigate()
        {
            if (eval.mate && eval.mate_in < 3)
            {
                return false;
            }
            if (!has_children)
            {
                if (!this.MakeChildren())
                {
                    return false;
                }
            }
            else
            {
                bool investigated = false;
                foreach (QueueMove m in children)
                {
                    if (m.Investigate())
                    {
                        investigated = true;
                        children.Sort(CompareQueues);
                        break;
                    }
                }
                if (!investigated)
                {
                    return false;
                }
            }

            //DivertedConsole.Write("Available moves: " + children.Count);
            // TODO This solved the problem but really maybe there is another thing you should do perhaps???
            eval = children[0].eval.ParentEval();
            return true;
        }

        public bool MakeChildren()
        {
            has_children = true;
            MakeParentMoves();
            b.MakeMove(move);

            Span<Move> legal_moves = stackalloc Move[218];
            b.GetLegalMovesNonAlloc(ref legal_moves);

            foreach (Move m in legal_moves)
            {
                children.Add(new QueueMove(m, this));
            }
            children.Sort(CompareQueues);

            b.UndoMove(move);
            UndoParentMoves();

            if (legal_moves.Length == 0)
            {
                has_children = false;
                return false;
            }
            return true;
        }

        public void MakeParentMoves()
        {
            if (has_parent)
            {
                parent.MakeParentMoves();
                b.MakeMove(parent.move);
            }
        }

        public void UndoParentMoves()
        {
            if (has_parent)
            {
                b.UndoMove(parent.move);
                parent.UndoParentMoves();
            }
        }
    }

    public static Move EvalQueue()
    {
        int time_remaining_turn_start = t.MillisecondsRemaining;
        if (library.has_children)
        {
            foreach (QueueMove m in library.children)
            {
                if (b.GameMoveHistory[b.GameMoveHistory.Length - 1] == m.move)
                {
                    //DivertedConsole.Write("Opponent chose " + m.move);
                    library = m;
                    library.has_parent = false;
                    library.move = Move.NullMove;
                    break;
                }
            }
            if (library.eval.mate)
            {
                /*foreach (QueueMove m in library.children)
                {
                    DivertedConsole.Write("Initial evaluation of " + m.move + " at turn start: " + m.eval.String());
                }*/
            }
        }
        else
        {
            library.Investigate();
        }
        /*foreach(QueueMove m in library.children)
        {
            DivertedConsole.Write("Initial evaluation of " + m.move + ": " + m.eval.String());
        }*/
        while (t.MillisecondsElapsedThisTurn < turn_limit_ms
            && 20 * t.MillisecondsRemaining > 19 * time_remaining_turn_start
            && !(library.eval.mate && library.eval.mate_in % 2 == 0)
            //&& !(library.children.Count > 1 && new QueueMove().CompareQueues(library.children[1], new QueueMove(new TotalEval(true))) >= 0)
            && library.Investigate())
        {
        }

        /*bool first = true;
        foreach (QueueMove m in library.children)
        {
            DivertedConsole.Write("Final evaluation of " + m.move + ": " + m.eval.String());
            if (!first)
            {
                continue;
            }
            first = false;
            if (m.eval.mate)
            {
                foreach(QueueMove m2 in m.children)
                {
                    DivertedConsole.Write("Opponent will respond with " + m2.move + ": " + m2.eval.String());
                }
                DivertedConsole.Write("The full line is:");
                QueueMove m_recur = m;
                while (m_recur.has_children)
                {
                    DivertedConsole.Write("\t" + m_recur.move + ": " + m_recur.eval.String());
                    m_recur = m_recur.children[0];
                }
            } else
            {
                if (m.children.Count > 0)
                {
                    DivertedConsole.Write("Opponent will respond with " + m.children[0].move + ": " + m.children[0].eval.String());
                }
            }
            if (!library.eval.mate)
            {
                break;
            }
        }*/
        //DivertedConsole.Write("Done evaluating");
        Move best_move = library.children[0].move;
        library = library.children[0];
        library.has_parent = false;
        library.move = Move.NullMove;
        return best_move;
    }

    public static int Eval()
    {
        // Evaluation (after moving):
        // +(your available moves*)-(opponent available moves)
        // +(material advantage)
        int eval = 0;
        eval -= MoveHeuristic();
        if (b.TrySkipTurn())
        {
            eval += MoveHeuristic();
        }
        else
        {
            //eval += SwapSides().GetLegalMoves().Length;
            b.ForceSkipTurn();
            eval += MoveHeuristic();
        }
        b.UndoSkipTurn();
        eval -= 10 * material_adv();
        return eval;
    }

    public static int MoveHeuristic()
    {
        //int CAP_MOD = 2;
        System.Span<Move> legal_moves = stackalloc Move[256];
        b.GetLegalMovesNonAlloc(ref legal_moves);
        //System.Span<Move> legal_caps = stackalloc Move[256];
        //b.GetLegalMovesNonAlloc(ref legal_caps, true);
        return legal_moves.Length;
    }

    public static TotalEval Eval(Move m)
    {
        b.MakeMove(m);
        TotalEval eval = new TotalEval();
        if (b.IsInCheckmate())
        {
            eval.MateIn(1);
        }
        if (b.IsDraw())
        {
            eval.draw = true;
        }
        eval.eval = Eval();
        b.UndoMove(m);
        return eval;
    }

    public static int material_adv()
    {
        int[] material_value = new int[] { 1, 3, 3, 5, 9, 0 };
        //int[] white_pawn_value = new int[] { 0, 1, 1, 2, 3, 4, 6, 0 };
        //int[] black_pawn_value = new int[] { 0, 6, 4, 3, 2, 1, 1, 0 };
        int adv = 0;
        int mult = b.IsWhiteToMove ? 1 : -1;
        PieceList[] piece_lists = b.GetAllPieceLists();
        for (int i = 0; i < piece_lists.Length; i++)
        {
            PieceList piece_list = piece_lists[i];

            /*if (i%6 == 0)
            {
                foreach(Piece p in piece_list)
                {
                    adv += i < 6 ? mult * white_pawn_value[p.Square.Rank] : -mult * black_pawn_value[p.Square.Rank];
                }
            } else
            {
                adv += mult * material_value[i % 6] * piece_list.Count * (i < 6 ? 1 : -1);
            }*/
            adv += mult * material_value[i % 6] * piece_list.Count * (i < 6 ? 1 : -1);
        }
        return adv;
    }

    /*public static Board SwapSides()
    {
        string new_fen;
        if (b.IsWhiteToMove)
        {
            new_fen = Regex.Replace(b.GetFenString(), "(?<=^[^ ]* )w", "b");
        } else
        {
            new_fen = Regex.Replace(b.GetFenString(), "(?<=^[^ ]* )b", "w");
        }
        return Board.CreateBoardFromFEN(new_fen);
    }*/
}