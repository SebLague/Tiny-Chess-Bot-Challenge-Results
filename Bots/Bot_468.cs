namespace auto_Bot_468;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_468 : IChessBot
{
    int[] PiecesValue = new int[]
        {
        0,   // None
        100,   // Pawn
        300, // Knight
        310, // Bishop
        500,   // Rook
        900,  // Queen
        0    // King
        };

    private Dictionary<ulong, TranspositionEntry> m_cashedEvaluations = new Dictionary<ulong, TranspositionEntry>();
    private Move m_lastMoves;
    private HashSet<Move>[] m_killerMoves;
    Move[] bestMove = new Move[6];

    public Move Think(Board board, Timer timer)
    {
        //Calculer max depth en fonction du temps
        return IterativeDeepening(board, timer, timer.MillisecondsRemaining / (16 - Math.Min(board.PlyCount, 8)), 4);
    }

    private Move IterativeDeepening(Board board, Timer timer, int timetoThink, int maxDepth)
    {
        List<Move> moves = board.GetLegalMoves().ToList();
        //MoveOrder
        bestMove[0] = moves[0];

        //KillerMoves
        m_killerMoves = new HashSet<Move>[maxDepth + 1]; // Need Extension pb
        for (int x = 0; x < m_killerMoves.Length; x++) m_killerMoves[x] = new HashSet<Move>();//A optimiser

        for (int i = 1; i <= maxDepth; i++)
        {
            int bestEvaluation = int.MinValue;
            moves.Sort((Move m1, Move m2) => Math.Sign(CompareMove(m2, 0) - CompareMove(m1, 0)));//Pb d'override avec depth 1

            foreach (Move m in moves) // Possible de factoriser avec la fonction search ?
            {
                board.MakeMove(m);
                m_lastMoves = m;

                int evaluation = -Search(board, i, int.MinValue, int.MaxValue, i);

                board.UndoMove(m);

                if (evaluation > bestEvaluation)
                {
                    bestMove[0] = m;
                    m_killerMoves[0].Add(m);
                    bestEvaluation = evaluation;
                }

                if (timer.MillisecondsElapsedThisTurn > timetoThink || bestEvaluation >= int.MaxValue) break; // peut être pas necessaire
            }
            if (timer.MillisecondsElapsedThisTurn > timetoThink || bestEvaluation >= int.MaxValue) break;
        }

        //int size = (((Marshal.SizeOf<ulong>() + Marshal.SizeOf<TranspositionEntry>()) * m_cashedEvaluations.Count) + Marshal.SizeOf<Move>() * m_killerMoves.Count()) / 1000000;
        //DivertedConsole.Write("Dictionary : " + size + "mb");

        return bestMove[0];
    }

    //NegaMax
    private int Search(Board board, int depth, int alpha, int beta, int maxDepth)
    {

        if (depth <= 0 || board.IsInCheckmate() || board.IsDraw())
        {
            return Evaluate(board);
        }

        int currentDepth = maxDepth - depth + 1;
        List<Move> moves = board.GetLegalMoves().ToList();
        //Move Order
        moves.Sort((Move m1, Move m2) => Math.Sign(CompareMove(m2, currentDepth) - CompareMove(m1, currentDepth)));//A optimiser

        int max = int.MinValue;

        int b = beta;

        foreach (Move m in moves) //Tester en for
        {
            board.MakeMove(m);
            m_lastMoves = m;

            ulong ZobristPos = board.ZobristKey;
            int evaluation;

            TranspositionEntry entry;
            if (m_cashedEvaluations.TryGetValue(ZobristPos, out entry) && entry.depth >= maxDepth)
            {
                evaluation = entry.evaluation;
            }
            else
            {
                evaluation = -Search(board, depth - 1, -b, -alpha, maxDepth);

                //Negascout
                if (evaluation > alpha && evaluation < beta && moves.IndexOf(m) > 0 && depth > 1) // Un peu plus lent pour l'instant
                {
                    evaluation = -Search(board, depth - 1, -beta, -alpha, maxDepth);
                }
            }

            board.UndoMove(m);

            if (evaluation > max) bestMove[currentDepth] = m;

            max = Math.Max(max, evaluation);
            alpha = Math.Max(alpha, max);

            //if (depth <= 2 && !board.IsInCheck() && alpha > -100000 && beta < 100000 && evaluation < alpha - 1600) //Futility Pruning
            //    break;

            if (alpha >= beta)
            {
                if (!m.IsCapture && !m.IsPromotion) m_killerMoves[currentDepth - 1].Add(m);
                break;
            }

            b = alpha + 1;
        }
        return max;
    }

    private int Evaluate(Board currentPosition)
    {
        if (currentPosition.IsInCheckmate())
            return int.MinValue + 1;

        //PB avec NegaMax
        //if (currentPosition.IsDraw())
        //    return -100000;

        float evaluation = 0;
        int materialValue = 0;

        foreach (PieceList pl in currentPosition.GetAllPieceLists())
        {
            foreach (Piece p in pl)
            {
                evaluation += PiecesValue[(int)p.PieceType] * IsThisColorPlaying(p, currentPosition) * 4;
                materialValue += PiecesValue[(int)p.PieceType];
            }
        }

        float gamePhase = Math.Max(0, 1 - ((materialValue - 700) / 3220)); //0 is opening / 1 is endgame

        //+ de captures en fin de partie
        evaluation *= 1 + (gamePhase * 8f);

        foreach (PieceList pl in currentPosition.GetAllPieceLists()) // GetAttacks (GetSliderAttacks)
        {
            foreach (Piece p in pl)
            {
                float currentValueToAdd = 0;
                float distToCenterFile = Math.Abs(p.Square.File - 3.5f);
                float distToCenter = -Math.Min(distToCenterFile, Math.Abs(p.Square.Rank - 3.5f));
                float distToSixthRank = Math.Abs(p.Square.Rank - (p.IsWhite ? 6 : 1));

                if (p.IsKnight || p.IsBishop || p.IsQueen)
                {
                    currentValueToAdd += distToCenter * (p.IsKnight ? 14 : 10);
                    //Possibly faster solution
                    //evaluation += (4 - Math.Min(Math.Abs(p.Square.File - 3), Math.Abs(p.Square.File - 4)) + Math.Min(Math.Abs(p.Square.Rank - 3), Math.Abs(p.Square.Rank - 4))) * 10 * IsThisColorPlaying(p, currentPosition);
                }
                else if (p.IsPawn)
                {
                    currentValueToAdd += ((4 - distToSixthRank) * 8 - distToCenterFile);
                }
                else if (p.IsRook) // Negatif de positionnement == positif de perdre la pièce
                {
                    currentValueToAdd += (distToSixthRank == 0 ? 50 : 0) + distToCenterFile * -5;
                }
                else if (p.IsKing)
                {
                    currentValueToAdd += distToCenter * 30 * (gamePhase * 2 - 1);
                }

                evaluation += currentValueToAdd * IsThisColorPlaying(p, currentPosition);
            }
        }

        evaluation += currentPosition.GetLegalMoves().Length * -1f;

        if (currentPosition.IsInCheck())
            evaluation -= 40;

        //if (m_lastMoves.MovePieceType == m_preLastMoves.MovePieceType && m_lastMoves.MovePieceType != PieceType.Pawn)
        //    evaluation -= 10;

        if (m_lastMoves.IsCastles) //Peut être à enlever pour gagner de la place
            evaluation += 80;
        //else if(!currentPosition.HasKingsideCastleRight(currentPosition.IsWhiteToMove) && !currentPosition.HasQueensideCastleRight(currentPosition.IsWhiteToMove))//A optimiser
        //    evaluation -= 10 * (1-gamestate);

        //if (m_lastMoves.IsPromotion)
        //    evaluation += 80;

        return (int)evaluation;
    }

    private float CompareMove(Move m1, int depthCount)
    {
        float v1 = 0;

        if (m1 == bestMove[depthCount]) return int.MaxValue;

        if (m_killerMoves[depthCount].Contains(m1))
        {
            v1 += 50;
        }

        if (m1.IsPromotion) v1 += 5;
        if (m1.PromotionPieceType == PieceType.Queen) v1 += 900;
        if (m1.IsCapture) v1 += (PiecesValue[(int)m1.CapturePieceType] - (PiecesValue[(int)m1.MovePieceType] / 10)); //board.SquareIsAttackedByOpponent(m1.TargetSquare) Check recapture
        if (m1.IsCastles) v1 += 10;
        if (m1.IsEnPassant) v1 += 1;

        return v1;
    }

    private int IsThisColorPlaying(Piece p, Board b)
    {
        return p.IsWhite == b.IsWhiteToMove ? 1 : -1;
    }

    //private bool NeedExtension(Board b)
    //{
    //    return false; //Timeout
    //    if (b.IsInCheck()) return true;
    //    else if (m_lastMoves.MovePieceType == PieceType.Pawn && (m_lastMoves.TargetSquare.Rank == 6 || m_lastMoves.TargetSquare.Rank == 1)) return true;
    //    //else if (m_lastMoves.IsCapture) return true;

    //    return false;
    //}
}

public struct TranspositionEntry
{
    public int evaluation;
    public int depth;

    public TranspositionEntry(int evaluation, int depth)
    {
        this.evaluation = evaluation;
        this.depth = depth;
    }
}