namespace auto_Bot_341;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_341 : IChessBot
{
    /*
    * I have kind of given up of on this
    * I can't reach a good enough depth, I am probably doing something very wrong
    * Moreover this often crash / lose on time
    * 
    * Search : 
    * Negamax (form of minimax) featuring 
    *   - alpha-beta pruning, 
    *   - moves ordering, 
    *   - iterative deepening,
    *   - quiescence search
    * Depth adapting on time left, but it sometime crashes...
    * On a 1 minute match, average depth is ~6 (+ quiescence search)
    * 
    * Eval :
    * Piece-square table, favouring:
    *   - occupying the center (pawns, knights and to a lesser extent bishop)
    *   - advanced pawns (especially in endgame)
    *   - rook on the seventh rank
    *   - king in castled position (opening and mid-game)
    *   - centralizing king (endgame)
    * Mobility (clumsy)
    * In quiescence search: ultra simple material evaluation
    * 
    * Does not know about: king safety, pawn structure, trapped pieces, ...
    * Is unable to mate with king + rook, two bishops, bishop + knight
    *
    */



    // Piece values: null, pawn(early game), knight, bishop, rook, queen, king
    readonly static int[] pieceValues = { 0, 70, 260, 300, 500, 900, 0 };

    /*
     * The following array encodes for the piece-square tables (PST)
     * Each entry describe 16 values (2 rows) while using only 1 tokens 
     * The decoded values can range from 0 to 12 
     * The unit is decipawns (for now)
     */
    static readonly ulong[] encodedPST = {
        // Pawn (endgame): push pawns
        /*  0 0 0 0 0 0 0 0
            C C C C C C C C
            8 8 8 8 8 8 8 8
            5 5 5 5 5 5 5 5
            3 3 3 3 3 3 3 3
            0 0 0 0 0 0 0 0
            0 0 0 0 0 0 0 0
            0 0 0 0 0 0 0 0 */
        665416608367449120, 277256920696924280, 203932680, 0,

        // Pawn (early and mid-game): take the center (d4,e4),
        // push toward promotion, while avoiding pushing a2,b2,f2,g2,h2,
        // favour capture toward the center
        /*  0 0 0 0 0 0 0 0
            C C C C C C C C
            7 8 A A A A 8 7
            3 3 6 9 9 6 3 3
            0 0 4 8 8 1 0 0
            1 1 3 5 5 2 1 1
            1 2 2 2 2 3 2 1
            0 0 0 0 0 0 0 0 */
        665416608367449120, 167413732942090096, 55854895836880816, 73577895,

        // Knight: incentive to centralize
        /*  0 1 2 2 2 2 1 0
            1 2 3 3 3 3 2 1
            2 3 4 4 4 4 3 2
            2 3 4 5 5 4 3 2
            2 3 4 5 5 4 3 2
            2 3 4 4 4 4 3 2
            1 2 3 3 3 3 2 1
            0 1 2 2 2 2 1 0 */
        60044977447651924, 115521451859744322, 115496361614258562, 4593593485008724,

        // Bishop: small incentive to centralize
        /*  0 0 0 0 0 0 0 0
            0 1 1 1 1 1 1 0
            0 1 2 2 2 2 1 0
            0 1 2 3 3 2 1 0
            0 1 2 3 3 2 1 0
            0 1 2 2 2 2 1 0
            0 1 1 1 1 1 1 0
            0 0 0 0 0 0 0 0 */
        4265490200799282, 4618683662547682, 4593593417061922, 5229042,

        // Rook: seventh rank slightly better
        /*  0 0 0 0 0 0 0 0
            1 1 1 1 1 1 1 1
            0 0 0 0 0 0 0 0
            0 0 0 0 0 0 0 0
            0 0 0 0 0 0 0 0
            0 0 0 0 0 0 0 0
            0 0 0 0 0 0 0 0
            0 0 0 0 0 0 0 0 */
        55451384030620760, 0, 0, 0, 

        // Queen: nothing, bot is already way too eager to put her in danger
        0, 0, 0, 0,

        // King (early and mid-game): castle and stay behind pawns
        /*  0 0 0 0 0 0 0 0
            0 0 0 0 0 0 0 0
            0 0 0 0 0 0 0 0
            1 1 1 1 1 1 1 1
            2 2 2 2 2 2 2 2
            4 4 4 4 4 4 4 4
            A A 5 5 5 5 A A
            B C 8 5 6 5 C B */
        0, 55451384030620760, 221805536258438160, 611957704401785830,

        // King (endgame): centralize
        // Less penalty for going up to help promoting pawns
        /*  0 1 2 2 2 2 1 0
            1 4 5 6 6 5 4 1
            2 5 7 8 8 7 5 2
            2 5 7 8 8 7 5 2
            2 5 7 8 8 7 5 2
            2 4 6 7 7 6 4 2
            1 3 4 5 5 4 3 1
            0 1 2 2 2 2 1 0 */
        68601048094767006, 124380535482834568, 120115045282035286, 4593593490268524
    };

    // 1024 == 2 * 8 * 64 == #colours * #pieceTypes * #squares
    // Access value: PST[isWhite*512 + pieceType*64 + squareIndex]
    // TODO: I have only 6 real piece types, should I use smaller array (2*7*64 or 2*6*64) ?
    readonly int[] PST = new int[1024];

    static readonly QuiescenceMoveComparer quiescenceMoveComparer = new();
    //static readonly LeafMoveComparer leafMoveComparer = new();

    Node root = new();

    public Move Think(Board board, Timer timer)
    {
        // Initial material evaluation
        int myMaterial = 0, enemyMaterial = 0;
        foreach (PieceList pl in board.GetAllPieceLists())
        {
            int a = pl.Count * pieceValues[(int)pl.TypeOfPieceInList];
            if (pl.IsWhitePieceList == board.IsWhiteToMove)
                myMaterial += a;
            else enemyMaterial += a;
        }

        // Switch to endgame PST
        if (enemyMaterial < 1800)
        {
            // Pawn PST for endgame
            DecodePSTLine(0, 1, 100);

            // King PST for endgame
            // Note : the king base value goes from 0 to 10000 here
            // this is so the present decoding is not triggered twice, without using more tokens
            DecodePSTLine(7, 6, 10000);
        }

        root = root.Children == null
            ? new() { RawScore = myMaterial - enemyMaterial }
            : root.Children.Values.FirstOrDefault(n => n.EntryMove.Equals(board.GameMoveHistory.Last()));

        int timeStopThinkingTrigger = timer.MillisecondsRemaining * 199 / 200;
        root.FirstEval(board, PST, -9999, 9999);

        if (root.Children.Count >= 2)
            // Negamax with iterative deepening
            for (int depth = root.Depth; timer.MillisecondsRemaining >= timeStopThinkingTrigger;)
                root.EvalDepth(board, PST, ++depth);

        root = root.Children.Values[0];

        return root.EntryMove;
    }

    public Bot_341()
    {
        // Decoding the piece-square tables
        for (int p = 1; p < 7;)
            DecodePSTLine(p, p, pieceValues[p++]);
    }

    void DecodePSTLine(int line, int pieceType, int baseValue)
    {
        // I optimized this code to minimize token count
        // So it doesn't make a lot of sense anymore
        ulong l2 = 0;
        for (int i = 0, n = pieceType * 64; i < 64;
            PST[n] = PST[568 + n++ - i++ / 8 * 16] = baseValue + 10 * (int)(l2 % 13),
            l2 /= 13)
            if (i % 16 == 0) l2 = encodedPST[4 * line + i / 16];
    }

    internal class Node
    {

        public Move EntryMove;

        // Children: null by default
        public SortedList<double, Node>? Children; //= null;

        public int Depth,
            RawScore,   // Pure material / PST eval, that is transmitted from root to leaves, 
            Score,      // Full evaluation with mobility and QS result, transmitted from leaves to root
            childrenKeyComplement; // Used to have unique keys in children list

        public void EvalDepth(Board board, int[] PST, int depthToReach, int alpha = -9999, int beta = 9999)
        {
            // If checkmate was reached in a previous iteration there is no point going further
            if (Score > 8000 || Score < -8000) return;

            // Stalemate / repetition / 50 moves rules
            if (board.IsDraw())
            {
                Score = 0;
                return;
            }

            FirstEval(board, PST, alpha, beta);

            // Mate
            if (Children.Count == 0)
                Score = Depth - 9999;

            // Recursion needed
            else if (Depth < depthToReach)
            {
                Score = -99999;

                foreach (double key in new List<double>(Children.Keys))
                {
                    Node child = Children[key];

                    board.MakeMove(child.EntryMove);
                    child.EvalDepth(board, PST, depthToReach, -beta, -alpha);
                    board.UndoMove(child.EntryMove);

                    int moveScore = -child.Score;

                    Children.Remove(key);
                    Children.Add(0.0001 * childrenKeyComplement++ - moveScore, child);

                    if (moveScore > Score)
                    {
                        Score = moveScore;
                        if (moveScore >= beta) break;             // Pruning
                        if (moveScore > alpha) alpha = moveScore;
                    }
                }
            }
        }

        public void FirstEval(Board board, int[] PST, int alpha, int beta)
        {
            // Checking if we have a leaf;
            if (Children != null) return;

            var moves = board.GetLegalMoves();

            int mePSTStart = board.IsWhiteToMove ? 512 : 0,
                blackPSTStart = 512 - mePSTStart;

            Children = new(moves.Length);

            bool notPruned = true;
            Score = -99999;
            foreach (Move move in moves)
            {
                int piecePSTStart = mePSTStart + (int)move.MovePieceType * 64,
                    targetSquareIndex = move.TargetSquare.Index,
                    // Move effect on evaluation: 
                    moveScore = RawScore
                        // Piece left this square
                        - PST[piecePSTStart + move.StartSquare.Index]
                        // Promotion
                        + (move.IsPromotion ? PST[mePSTStart + (int)move.PromotionPieceType * 64 + targetSquareIndex]
                        // Piece reached that square
                        : PST[piecePSTStart + move.TargetSquare.Index])
                        // Value of pieces captured (note: I am overlooking exact enemy pawn position in en passant)
                        + (move.IsCapture ? PST[blackPSTStart + (int)move.CapturePieceType * 64 + targetSquareIndex] : 0),
                    // Mobility
                    moveScoreWithComplement = moveScore - 400 / (moves.Length + 3);

                if (moveScore - RawScore >= 70 && notPruned)
                {
                    board.MakeMove(move);
                    moveScoreWithComplement = -QuiescenceSearch(board, -moveScoreWithComplement, -beta, -alpha);
                    board.UndoMove(move);
                }

                Node child = new() { EntryMove = move, Depth = Depth + 1, RawScore = -moveScore, Score = -moveScoreWithComplement };
                Children.Add(0.0001 * childrenKeyComplement++ - moveScoreWithComplement, child);

                if (moveScoreWithComplement > Score)
                {
                    Score = moveScoreWithComplement;
                    if (Score >= beta) notPruned = false;
                    if (Score > alpha) alpha = Score;
                }
            }

        }

        public int QuiescenceSearch(Board board, int preScore, int alpha, int beta)
        {
            // Quiescence search consist here in a very rapid evaluation of chains of captures

            if (preScore >= beta) return preScore; // Can return beta instead

            if (preScore > alpha) alpha = preScore;

            var captures = board.GetLegalMoves(true);
            Array.Sort(captures, quiescenceMoveComparer);

            foreach (Move capture in captures)
            {
                // Ultra simplified eval: no piece-square tables, no mobility
                // Promotion is assumed queen
                int moveScore = preScore
                    + pieceValues[(int)capture.CapturePieceType]
                    + (capture.IsPromotion ? 680 : 0)
                    ;

                board.MakeMove(capture);
                moveScore = -QuiescenceSearch(board, -moveScore, -beta, -alpha);
                board.UndoMove(capture);

                /*
                if (alpha > 4000 && alpha < 7000 || alpha < -4000 && alpha > -7000 ||
                    beta > 4000 && beta < 7000 || beta < -4000 && beta > -7000)
                {
                    DivertedConsole.Write("Absurd result in quiescence search");
                    DivertedConsole.Write(board.CreateDiagram());
                    return 0;
                }
                */

                // Pruning
                if (moveScore >= beta)
                    return moveScore;

                if (moveScore > alpha)
                    alpha = moveScore;
            }

            return alpha;
        }
    }

    public class QuiescenceMoveComparer : Comparer<Move>
    {
        // Compares by piece captured, then piece moving
        // It is reversed so Sort() goes from best to worse
        public override int Compare(Move x, Move y)
        {
            if (x.IsPromotion)
                return y.IsPromotion
                    ? y.PromotionPieceType - x.PromotionPieceType
                    : x.PromotionPieceType == PieceType.Queen ? -1 : 1;
            if (y.IsPromotion)
                return y.PromotionPieceType == PieceType.Queen ? 1 : -1;

            int r = y.CapturePieceType - x.CapturePieceType;
            return r == 0 ? x.MovePieceType - y.MovePieceType : r;
        }
    }
}


