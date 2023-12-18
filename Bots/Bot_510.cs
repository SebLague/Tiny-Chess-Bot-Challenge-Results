namespace auto_Bot_510;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_510 : IChessBot
{
    public Bot_510()
    {
        // values taken and modified from https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
        // and clamped from -100 to +155, shifted 28 values down, so you need to add 28 to get original value back
        ulong[] compressed =
        {
            0xe4e4e4e4e4e4e4e4, 0xce0afcd5cdd0e3c1, 0xd805e7e7dae0e0ca, 0xcbeeeaf5f0dfe2c9, 0xcdf5f0fbf9eaf1d6, 0xd0fd1c2503feebde, 0xd906622843216a46, 0xe4e4e4e4e4e4e4e4,
            0xe4e4e4e4e4e4e4e4, 0xdde6e4f1eeececf1, 0xdce3dfe4e5deebe8, 0xe3e7dcdddde1edf1, 0xf5f5e8e2e9f1fc04, 0x3836191c27394842, 0x7f7f68776a7f7f7f, 0xe4e4e4e4e4e4e4e4,
            0xcdd1c8d3c3aacf80, 0xd1d6f6e3e1d8afc7, 0xd4fdf5f7eef0dbcd, 0xdcf9f700f1f4e8d7, 0xfaf6290919f7f5db, 0x102d6538250920b5, 0xd3eb22fb082cbb9b, 0x80d58321b3c28b80,
            0xa4b2d2ced5cdb1c7, 0xb8cdd0e2dfdad0ba, 0xced0e1eef3e3e1cd, 0xd2e8f5f4fdf4ded2, 0xd2eceffafafae7d3, 0xbbd1dbe3edeed0cc, 0xb0cccbdbe2cbdccb, 0x81a5c9c5c8d7beaa,
            0xcfbdd8d7cfd6e1c3, 0xe505f9ebe4f4f3e8, 0xeef6fff2f3f3f3e4, 0xe8eef006fef1f1de, 0xe2eb090916f7e9e0, 0xe20916070c0f09d4, 0xb5f61f02d7d2f4ca, 0xdcebbacbbf92e8c7,
            0xd3dfd4dbdfcddbcd, 0xc9d5dbe8e3ddd2d6, 0xd5dde7f1eeece1d8, 0xdbe1eeebf7f1e7de, 0xe6e7eef2edf0ede1, 0xe8e4eae2e3e4dce6, 0xd6e0d7e1d8ebe0dc, 0xccd3dbdddcd9cfd6,
            0xcabfebf4f5e5d7d1, 0x9ddeefe3dbd0d4b8, 0xc3dfe4e7d3d4cbb7, 0xcdeaddede3d8cac0, 0xd0dc07fcfeebd9cc, 0xf42111f508fef7df, 0x10fe2734221e04ff, 0x0f03ed2317040e04,
            0xd0e8d7dfe3e7e6db, 0xe1d9dbdbe6e4dede, 0xd4dcd8dde3dfe4e0, 0xd9dcdedfe8ece9e7, 0xe6e3e5e6e5f1e7e8, 0xe1dfe1e8e9ebebeb, 0xe7ece7e1eff1f1ef, 0xe9ecf0f0f3f6eef1,
            0xb2c5cbd5eedbd2e3, 0xe5e1f3ece6efdcc1, 0xe9f2e6dfe2d9e6d6, 0xe1e7e0e2dadbcadb, 0xe5e2f5e3d4d4c9c9, 0x1d131c01ecebd3d7, 0x1a001dd4e5dfbdcc, 0x110f101ff001e4c8,
            0xbbd0c4dfb9cec8c3, 0xc4c0cdd4d4c6cdce, 0xe9eef5edeaf3c9d4, 0xfb0b060313f700d2, 0x081d0c1d11fcfae7, 0xedf7071315edead0, 0xe402fd1e0d04f8d3, 0xf8eef7fffffafadb,
            0xf2fcc8ecaef008d5, 0xecedd4b9a4dcebe5, 0xc9d5c6b8b6ced6d6, 0xb1c3b8b6bdc9e3b3, 0xc0d6cbc6c9d8d0d3, 0xcefaead0d4e6fcdb, 0xc7bee0dcddd0e301, 0xf1e6c2acd5f4fba3,
            0xb9ccd6c8d9cfc2af, 0xd3dfe8f2f1e8d9c9, 0xdbebf4fbf9efe1d1, 0xd9edfbfffcf9e0d2, 0xe7fe05fefffcfadc, 0xf11011f8f3fbf5ee, 0xeffb0af5f5f2f5d8, 0xd3e8f3d9d2d2c19a
        };

        m_SquareBonus = compressed.SelectMany(BitConverter.GetBytes).ToArray();
    }

    // saving some brain-space when we only need to type 'NullMove' instead of 'Move.NullMove'
    readonly Move c_NullMove = Move.NullMove;

    // out transposition table
    // 1. Zobrist Key
    // 2. best move
    // 3. evaluation
    // 4. depth
    // 5. flag
    private (ulong, Move, int, int, int)[] m_TT = new (ulong, Move, int, int, int)[0x400000];

    // Will store the best move after each search-iteration
    // Move[] m_KillerMoves; // maybe a bug, but it didn't improve anything, in fact, it made it slightly worse... :(
    private Move m_BestMove;
    private Board m_Board;
    private Timer m_Timer;
    private bool m_HasFoundABestMoveThisSearch;

    private int
        // specifies the maximum number of milliseconds the 
        m_ForceBreakMilliseconds,
        // // specifies the recommended number of milliseconds to spend in the 
        // iterative deepening loop
        m_RecommendedThinkTimeThisTurn;

    // from https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    private int[] m_PieceValues = { 82, 94, 337, 281, 365, 297, 477, 512, 1025, 936, 0, 0 };

    // Bonus Square
    private static byte[] m_SquareBonus;

    public Move Think(Board board, Timer timer)
    {
        m_BestMove = c_NullMove;
        m_Board = board;
        m_ForceBreakMilliseconds = timer.MillisecondsRemaining / 8; // maximal think-time is one eighth of the remaining time, one sixth caused "too many" time-outs
        m_RecommendedThinkTimeThisTurn = timer.MillisecondsRemaining / 175 + 40; // works somewhat ok for games between 1 to 3 minutes
        m_HasFoundABestMoveThisSearch = false;
        m_Timer = timer;

        // give my bot some extra time to think when it has 10+ more seconds that the
        // opponent
        if (timer.MillisecondsRemaining - timer.OpponentMillisecondsRemaining >= 10_000)
            m_RecommendedThinkTimeThisTurn += 300;

        // force the bot to play a better opening by 
        // comparing the zobrist key with the known zobrist keys of various positions
        // This trick boosted the performance eminent, since it didn't blunder
        // openings any more so hard
        int openingIndex = openingBookIndex(m_Board.ZobristKey);
        if (openingIndex >= 0)
            return m_Board.GetLegalMoves()[openingIndex];

        // iterative deepening with aspiration window
        // See https://web.archive.org/web/20071031095918/http://www.brucemo.com/compchess/programming/aspiration.htm for aspiration windows
        for (int alpha = -0x3fffffff, beta = 0x3fffffff, depth = 2; timer.MillisecondsElapsedThisTurn < m_RecommendedThinkTimeThisTurn || !m_HasFoundABestMoveThisSearch;)
        {
            int val = search(alpha, beta, depth, 0, 0);

            // We fell outside the window, so try again
            // with a full-width window (and the same depth).
            if (val <= alpha || val >= beta)
            {
                alpha = -0x3fffffff;
                beta = 0x3fffffff;
                m_HasFoundABestMoveThisSearch = false;
                continue;
            }

            // found a forced mate or timeout
            if (m_HasFoundABestMoveThisSearch && m_Timer.MillisecondsElapsedThisTurn > m_ForceBreakMilliseconds || Math.Abs(val) >= 120000000)
                break;

            alpha = val - 62; // 60;
            beta = val + 62; // 60;
            depth++;
            m_HasFoundABestMoveThisSearch = true;
        }

        return m_BestMove;
    }

    private int search(int alpha, int beta, int depth, int depthFromRoot, int numExtensions)
    {
        Move bestMove = depthFromRoot == 0 ? m_BestMove : c_NullMove;
        int evalType = 2; // c_TTEntryUpperBound
        ulong zobristKey = m_Board.ZobristKey;

        if (m_Board.IsInCheckmate())
            return -130000000 - depth;

        // Draw detection
        if (m_Board.IsRepeatedPosition() && depthFromRoot > 0 || m_Board.IsDraw())
            return 0; // TODO: maybe play for a win, not a draw...

        if (depth <= 0)
        {
            // since only these lines + movegen changed, we can save some tokens
            // by not implementing a quiesce search function
            int score = eval();
            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }
        else if (!m_Board.IsInCheck())
            // razoring: https://www.chessprogramming.org/Razoring
            if (depth <= 3 && eval() + 500 <= alpha)
                depth--;

        // lookup transposition
        ref var ttEntry = ref m_TT[zobristKey & 0x3fffff];
        if (ttEntry.Item1 == zobristKey)
        {
            bestMove = ttEntry.Item2;

            // TODO: test if `depthFromRoot > 0` is actually required, or if it just slows us down...
            if (ttEntry.Item4 >= depth && depthFromRoot > 0)
            {
                int score = correctMateScoreForTT(ttEntry.Item3, -depth);

                if (ttEntry.Item5 == 0) // 0 = c_TTEntryExact
                    return score;

                if (ttEntry.Item5 == 2 && score <= alpha) // 2 = c_TTEntryUpperBound
                    return score;

                if (ttEntry.Item5 == 1 && score >= beta) // 1 = c_TTEntryLowerBound
                    return score;
            }
        }

        Span<Move> legalMoves = stackalloc Move[218];
        m_Board.GetLegalMovesNonAlloc(ref legalMoves, depth <= 0 && !m_Board.IsInCheck());

        // moveordering
        {
            Span<int> moveScores = stackalloc int[legalMoves.Length];
            int currentMoveScoreIndex = 0;
            foreach (Move move in legalMoves)
            {
                // MVV/LVA move ordering: https://web.archive.org/web/20071027170528/http://www.brucemo.com/compchess/programming/quiescent.htm#MVVLVA
                moveScores[currentMoveScoreIndex++] = (move == bestMove) ? -10_000_000 :
                    move.IsCapture ?
                    (int)move.MovePieceType - (int)move.CapturePieceType * 1_000_000 :
                    0;
            }

            moveScores.Sort(legalMoves);
        }

        foreach (Move move in legalMoves)
        {
            m_Board.MakeMove(move);
            //int extensionDepth = numExtensions < 14 && (m_Board.IsInCheck() || move.MovePieceType == PieceType.Pawn && (move.TargetSquare.Rank == 6 || move.TargetSquare.Rank == 1))  ? 1 : 0; // no improvement at all
            int extensionDepth = numExtensions < 16 && m_Board.IsInCheck() ? 1 : 0;
            int score = -search(-beta, -alpha, depth - 1 + extensionDepth, depthFromRoot + 1, numExtensions + extensionDepth);
            m_Board.UndoMove(move);

            // make sure to also break here when time is up
            if (m_HasFoundABestMoveThisSearch && m_Timer.MillisecondsElapsedThisTurn > m_ForceBreakMilliseconds)
                return 0;

            if (score >= beta)
            {
                if (depth >= 0)
                    ttEntry = (zobristKey, bestMove, correctMateScoreForTT(score, depth), depth, 1); // 1 = c_TTEntryLowerBound

                return beta;
            }

            if (score > alpha)
            {
                alpha = score;
                evalType = 0; // 0 = exact
                bestMove = move;

                if (depthFromRoot == 0)
                    m_BestMove = move;
            }
        }

        if (depth >= 0)
            ttEntry = (zobristKey, bestMove, correctMateScoreForTT(alpha, depth), depth, evalType);

        return alpha;
    }

    // inspired by https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    private int eval()
    {
        // if pieces are black, the result will be negated, so we are calculating
        // score = piecesWhite + (-piecesBlack);
        int
            mgScore = 0,
            egScore = 0,
            gamePhase = 0;

        ulong pieces = m_Board.AllPiecesBitboard;
        while (pieces != 0)
        {

            Piece piece = m_Board.GetPiece(new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref pieces)));
            bool isWhite = piece.IsWhite;

            int index = isWhite ? piece.Square.Index : (0x38 ^ piece.Square.Index);

            // we multiply by 128, since we have 2 * 64 lookup-values (middle- and end-game)
            index += ((int)piece.PieceType - 1) * 128;

            int mg = (sbyte)m_SquareBonus[index] + m_PieceValues[index >> 6] + 28;
            int eg = (sbyte)m_SquareBonus[index + 64] + m_PieceValues[index / 64 + 1] + 28;

            mgScore += isWhite ? mg : -mg;
            egScore += isWhite ? eg : -eg;

            // inspirted by gamphase: https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
            gamePhase += 0x42110 >> (int)piece.PieceType * 4 & 0xf;
        }

        // tapered eval
        if (gamePhase > 24) gamePhase = 24; // in case of early promotion
        int score = (mgScore * gamePhase + egScore * (24 - gamePhase)) / (m_Board.IsWhiteToMove ? 24 : -24);

        return score + m_Board.GetLegalMoves().Length + 16;
    }

    private int openingBookIndex(UInt64 key) => key switch
    {
        // startup-position
        0xb792d82d18345f3a or // play d4
        // d3
        0x6d05b94e51c068bb or // play d5
        // e3
        0x01ce91d895cde705 or // play d5
        // Nc3
        0x756d13714aeac83c or // play d5
        // d4
        0xc13102b0dafb9dde => 15, // play d5

        // e4 c5, nf3 nc6, nc3
        17764639934957879951 or // play c6

        // e4
        15607329186585411972 => 14, // play c5

        // d4 d5
        0x85a0f477f3d92136 => 22, // play queen's gambit

        // d4 d5, e4
        0x1f039fb96ee153db => 16, // play e6 (queens gambit declined) (current version doesn't handle this so good...)

        // d4 d5, c4 c6, nf3
        0xe363569f7c6d79af => 17, // play nf6

        // d4 Nf6 (inaccurate opening by black, white will take the center with c4
        0x1fd2cbbf4ec0ce5b => 23, // play c4

        // don't remember
        3226510044126492241 => 13,

        // engine-specific, don't remember
        10741390688593562366 => 27,

        // c4 e6, nf3 d5
        322568147724310267 => 25, // play c4

        // d4 c6, nc3 d4
        786530213024198710 => 24, // play d4

        // engine specific
        //10964647065783687628 => 9,

        // rnbqkbnr/pp3ppp/4p3/2p5/2pPP3/2N5/PP3PPP/R1BQKBNR w KQkq c6 0 5
        //8919887365738648167 => 33,

        // otherwise return -1 to indicate that no opening has been found
        _ => -1
    };

    // Thanks to Sebastian for the idea
    // (https://github.com/SebLague/Chess-AI/blob/d0832f8f1d32ddfb95525d1f1e5b772a367f272e/Assets/Scripts/Core/TranspositionTable.cs#L88)
    // for storing, use normal depth value
    // for retrieving, use negated depth value
    private static int correctMateScoreForTT(int score, int depth)
    {
        if (score >= 120000000)
            score -= depth;

        if (score <= -120000000)
            score += depth;

        return score;
    }
}