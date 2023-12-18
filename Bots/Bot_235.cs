namespace auto_Bot_235;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_235 : IChessBot
{
    Board board;
    bool amIWhite;
    int forwardIsUp = 1;
    int turnCount = 0;
    const int CHECK_BONUS = 950; //-piecevalue.
    const int RANK_FILE_MULTIPLIER = 1;
    double DISREGARD_MOVES_STD_DEV_MOD = 2.5;

    bool checkDraws = false;
    Timer timer;

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 500 };
    int[] pieceBonusMoveValues = { 0, 17, 16, 10, 10, 18, -5 };

    struct TransitionTableEntry
    {
        public TransitionTableEntry(ulong _key, int _moveScore, Move _bestMove, int _depth)
        {
            key = _key;
            moveScore = _moveScore;
            move = _bestMove;
            depth = _depth;
        }

        public ulong key;
        public int moveScore;
        public Move move;
        public int depth;
    }

    const int zobEntries = (1 << 20);
    TransitionTableEntry[] tTable = new TransitionTableEntry[zobEntries];

    public Move Think(Board _b, Timer _timer)
    {
        //DivertedConsole.Write("Turn: " + turnCount);
        timer = _timer;

        if (timer.MillisecondsRemaining < 3000)
        {
            DISREGARD_MOVES_STD_DEV_MOD = 2.4 - (1.0 - ((double)timer.MillisecondsRemaining / 3000.0));
            //DivertedConsole.Write("Considering less moves..." + DISREGARD_MOVES_STD_DEV_MOD.ToString());
        }

        //Beginning of the game we prefer to move pawns.
        if (turnCount > 2)
            pieceBonusMoveValues = new int[] { 0, 10, 13, 12, 10, 14, -5 };

        board = _b;

        amIWhite = board.IsWhiteToMove;
        //Weird logic to get the bot to know what side of the board it is playing on.
        forwardIsUp = (board.GetPieceBitboard(PieceType.Rook, amIWhite) & 0x0000000000000001) > 0 ? 1 : -1;

        //Should do an actual opener here...

        //Then try and wing it.
        Move bestMove = new Move();
        int bestMoveValue = DepthSearch(3, true, out bestMove, true);

        //DivertedConsole.Write("Trying move \"" + bestMove.MovePieceType + " to: " + bestMove.TargetSquare.Name + "\" with score: " + bestMoveValue );
        //EvaluateMove(board, bestMove, true);

        ++turnCount;

        checkDraws = IsCheckOrCheckmate(bestMove) > 0;

        //System.Threading.Thread.Sleep(100);
        return bestMove;
    }

    int DepthSearch(int _depth, bool isMyMove, out Move _bestMove, bool _levelOne = false)
    {
        Move[] moves = board.GetLegalMoves();
        int[] moveScores = new int[moves.Length];
        int myMoveMultiplier = (isMyMove ? 1 : -1);
        ulong safeKey = board.ZobristKey % zobEntries;

        //Zobrist key logic that I don't understand very well.
        if (tTable[safeKey].key == board.ZobristKey && _depth < 3 && _depth >= tTable[safeKey].depth)
        {
            //DivertedConsole.Write("Zobrist key found!");
            _bestMove = tTable[safeKey].move;
            return tTable[safeKey].moveScore;
        }

        for (int i = 0; i < moves.Count(); ++i)
        {
            moveScores[i] += EvaluateMove(board, moves[i]) * myMoveMultiplier;
        }

        //Null move check.
        if (moves.Count() == 0)
        {
            _bestMove = new Move();
            return 0;
        }

        //Standard deviation squared.
        double average = moveScores.Average();
        double stdDeviation = Math.Sqrt(moveScores.Average(v => Math.Pow(v - average, 2)));

        if (_depth > 0)
        {
            for (int i = 0; i < moves.Count(); ++i)
            {
                //Try to consider moves that are better than X stdDeviations from average
                if ((moveScores[i] < (average - DISREGARD_MOVES_STD_DEV_MOD * stdDeviation) && isMyMove) ||
                    (moveScores[i] > (average + DISREGARD_MOVES_STD_DEV_MOD * stdDeviation) && isMyMove == false))
                {
                    moveScores[i] += -9999 * myMoveMultiplier;
                    continue;
                }
                //string moveString = moves[i].ToString();
                board.MakeMove(moves[i]);

                //Add the best / worst move from the rest of our depth search. Opponent's score should be inverted.
                int bestMoveScore = DepthSearch(_depth - 1, !isMyMove, out _bestMove);
                moveScores[i] += bestMoveScore;
                board.UndoMove(moves[i]);
            }
        }

        //Sort array so good moves are at the front
        Array.Sort(moveScores, moves);

        //TODO: Delete this logging.
        //if (_levelOne)
        //{
        //    for (int i = 0; i < moves.Count(); ++i)
        //        DivertedConsole.Write("Move: " + moves[i] + "\t" + moveScores[i]);
        //}

        _bestMove = isMyMove ? moves[moves.Count() - 1] : moves[0];
        int bestScore = isMyMove ? moveScores[moveScores.Count() - 1] : moveScores[0];

        tTable[safeKey] = new TransitionTableEntry(board.ZobristKey, bestScore, _bestMove, _depth);

        return bestScore;
    }

    int EvaluateMove(Board _b, Move _move, bool _log = false)
    {
        int moveScore = turnCount < 3 ? pieceBonusMoveValues[(int)_move.MovePieceType] : 0;

        //if (_log) DivertedConsole.Write("Move score piece bonus: " + moveScore);

        moveScore += IsCheckOrCheckmate(_move);

        //if (_log) DivertedConsole.Write("With check bonus: " + moveScore);

        Piece capturedPiece = _b.GetPiece(_move.TargetSquare);
        if (capturedPiece.IsNull == false && pieceValues[(int)capturedPiece.PieceType] > moveScore)
        {
            moveScore += pieceValues[(int)capturedPiece.PieceType] - (pieceValues[(int)_move.MovePieceType] / 10);
        }

        //if (_log) DivertedConsole.Write("With capture bonus: " + moveScore);

        //File bonus
        moveScore += (3 - Math.Abs(_move.TargetSquare.File - 3 - (_move.TargetSquare.File % 2))) * RANK_FILE_MULTIPLIER;
        //Rank bonus
        moveScore += _move.TargetSquare.Rank * forwardIsUp * RANK_FILE_MULTIPLIER; //Multiplying this by two makes the bot very aggressive.
        //if (_log) DivertedConsole.Write("With rank/file bonus: " + moveScore);
        //Penalty for repeated...
        moveScore += Convert.ToInt32(WillThisCauseRepeated(_move)) * -350;
        //if (_log) DivertedConsole.Write("repeated penalty: " + moveScore);
        if ((_move.StartSquare.Rank == 1 && forwardIsUp == 1) || (_move.StartSquare.Rank == 8 && forwardIsUp == -1))
            moveScore += 15; //"Development" bonus.

        return moveScore;
    }

    int Bound(int _i)
    {
        return Math.Clamp(_i, 0, 7);
    }

    bool WillThisCauseRepeated(Move _move)
    {
        board.MakeMove(_move);
        bool isRepeated = board.IsRepeatedPosition();
        board.UndoMove(_move);
        return isRepeated;
    }

    // Test if this move gives checkmate
    int IsCheckOrCheckmate(Move move)
    {
        int moveScore = 0;
        board.MakeMove(move);
        moveScore += Convert.ToInt32(board.IsInCheckmate()) * 99999;
        moveScore += Math.Max(Convert.ToInt32(board.IsInCheck()) * ((CHECK_BONUS - pieceValues[(int)move.MovePieceType]) / 50), 0);
        if (checkDraws) //Only check draws if moveScore 
            moveScore += Convert.ToInt32(board.IsDraw()) * -999999;
        board.UndoMove(move);
        return moveScore;
    }
}