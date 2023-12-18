namespace auto_Bot_631;
using ChessChallenge.API;
using System;

public class Bot_631 : IChessBot
{
    int expectedMoves = 60;
    int Infinity = int.MaxValue;
    int[] PIECE_VALUES = { 100, 320, 330, 500, 900, 0 };
    Board position;
    Move bestPreviousIteration;
    Move bestMove;
    int bestEval;
    int evaluation = 0;
    // values from PeSTO's Evaluation Function on https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    ulong[] PstEndCompressed =
    {
        9020838262468278, 9022211711708381, 9023931042901230, 9022900652619502, 9022694627620597, 9022969237088015, 9020494261130500, 9018019419662575,
        15285925041790708, 15111171949865233, 14582239613829390, 13739392681005841, 14196309686121745, 13667443252540198, 14828597460221207, 15600728157323531,
        12330507846932746, 12541887615208721, 12016184691921687, 11382797140124431, 10995081717899028, 10888979917719341, 11908638707689260, 11977496160768781,
        10149557141899000, 9869458165083414, 9483736147439896, 9202260764941083, 8955970831676186, 9166320612823329, 9623510351508250, 9621723511671043,
        9480984949808366, 9341073303681276, 8920374010062613, 8780255801270040, 8779635712999195, 8744520462779671, 9130653457207049, 8988403334262517,
        9163981196484845, 9270909911288573, 8813651988193035, 9061042373922069, 9025514805531415, 8848698248405776, 8988266164524295, 8741837045107447,
        9479334606263525, 9304924037436149, 9305612710167812, 9376325857566989, 9482085800468750, 9023450267898628, 9093612047808763, 8775507981484271,
        9022829913554635, 9021319967000798, 9023242233566443, 9023794404240117, 9023312830002916, 9023586766274802, 9021389223876840, 9020425534287573,
    };
    ulong[] PstMidCompressed =
    {
        9013345831209151, 9018710382215447, 9022478408104720, 9021453660723441, 9029014416946888, 9018154443757790, 9023796023285506, 9017471821306637,
        12467874328072477, 13736716384973567, 11176017966790380, 12369813388788473, 11418947765264632, 13462325294953212, 10221577977870554, 8636623247011043,
        8810487671023351, 9275244612017944, 9942167940894466, 10120013546655984, 11317587191675628, 11004022239949062, 9909456673857302, 8324161818751722,
        8531625640053487, 9483391604214508, 9237240322318580, 9767346531590373, 9836614018793186, 9451784951964391, 9624198081281266, 8217096864531164,
        8073953653419727, 8954733739494655, 8850005260627685, 9447935175421145, 9624888904383698, 9237239379130580, 9378114041415391, 8144667340569293,
        8108451633685746, 8883471910831346, 8884915022195434, 8673671350451410, 9131686533920468, 9131550839014626, 10187630548229361, 8601515224599269,
        7791380505148161, 8986001372475655, 8320315934512888, 8215379148932288, 8496992506220757, 9870490569940720, 10360874362075913, 8249464127357704,
        9017605888540401, 9023382352682276, 9020838259322636, 9022555310920906, 9023655896015624, 9022900113624804, 9023514953499416, 9023242494385422,
    };
    int[] gamePhaseWeights = { 0, 1, 1, 2, 4, 0 };
    int[,] PstMid = new int[6, 64];
    int[,] PstEnd = new int[6, 64];
    int MobilityWeight = 1;
    int nMoves = 0;
    bool searchTimedOut = false;
    Timer turnTimer;
    long allocatedTime;
    int gamePhase;

    public Bot_631()
    {
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 64; j++)
            {
                PstMid[i, j] = (int)((PstMidCompressed[j] >> (9 * (5 - i))) & 0b111111111) - 256;
                PstEnd[i, j] = (int)((PstEndCompressed[j] >> (9 * (5 - i))) & 0b111111111) - 256;
            }
        }
    }

    int TranslateSquare(int index, bool flip)
    {
        int id = index + 7 - 2 * (index % 8);
        return flip ? index : 63 - id;
    }

    void InitialEvaluation()
    {
        evaluation = 0;
        CalculateGamePhase();
        PieceList[] allPieces = position.GetAllPieceLists();
        for (int i = 0; i < 12; i++)
        {
            int v = allPieces[i].Count * PieceValue(i % 6);
            foreach (Piece piece in allPieces[i])
            {
                v += PieceSquareValue(i % 6, TranslateSquare(piece.Square.Index, i > 5));
            }
            evaluation += (i < 6 == position.IsWhiteToMove) ? v : -v;
        }
    }

    int PieceSquareValue(int pieceType, int square)
    {
        return (gamePhase * PstMid[pieceType, square] + (24 - gamePhase) * PstEnd[pieceType, square]) / 24;
    }

    int PieceValue(int pieceType)
    {
        return PIECE_VALUES[pieceType];
    }

    int Evaluate()
    {
        if (position.IsDraw()) return 0;
        if (position.IsInCheckmate()) return -Infinity;
        return evaluation + MobilityWeight * position.GetLegalMoves().Length;
    }

    void CalculateGamePhase()
    {
        gamePhase = 0;
        PieceList[] allPieces = position.GetAllPieceLists();
        for (int i = 0; i < 12; i++)
        {
            gamePhase += allPieces[i].Count * gamePhaseWeights[i % 6];
        }
        gamePhase = Math.Min(gamePhase, 24);
    }

    void UpdateEvaluation(Move move, bool flip)
    {
        evaluation = -evaluation;
        CalculateGamePhase();
        int pieceType = (int)move.MovePieceType - 1;
        int eval = 0;
        int targetSquare = TranslateSquare(move.TargetSquare.Index, flip);
        eval += PieceSquareValue(pieceType, TranslateSquare(move.StartSquare.Index, flip));
        eval -= PieceSquareValue(pieceType, targetSquare);
        if (move.IsCapture)
        {
            int capturedPieceType = (int)move.CapturePieceType - 1;
            eval -= PieceSquareValue(capturedPieceType, TranslateSquare(move.TargetSquare.Index, !flip));
            eval -= PieceValue(capturedPieceType);
        }
        if (move.IsPromotion)
        {
            int promotionType = (int)move.PromotionPieceType - 1;
            eval -= PieceValue(promotionType);
            eval += PieceValue(pieceType);
        }
        evaluation += eval;
    }

    int GuessMoveEval(Move move)
    {
        if (move == bestPreviousIteration) return Infinity;
        int score = 0;
        if (move.IsPromotion) score += 500;
        if (move.IsCapture) score += PieceValue((int)move.CapturePieceType - 1);
        if (position.SquareIsAttackedByOpponent(move.TargetSquare)) score -= 150;
        return score;
    }

    void OrderMoves(Move[] moves)
    {
        Array.Sort(moves, (x, y) => { return GuessMoveEval(y) - GuessMoveEval(x); });
    }

    int Search(int depth, int alpha, int beta, bool updateMove, bool capturesOnly)
    {
        if (turnTimer.MillisecondsElapsedThisTurn > allocatedTime && !capturesOnly && bestPreviousIteration != Move.NullMove)
        {
            searchTimedOut = true;
            return 0;
        }
        if (position.IsDraw() || position.IsInCheckmate()) return Evaluate();
        int eval = -Infinity;
        if (capturesOnly)
        {
            eval = Evaluate();
            if (eval >= beta) return eval;
        }
        Move[] moves = position.GetLegalMoves(capturesOnly);
        if (depth == 0) capturesOnly = true;
        OrderMoves(moves);
        foreach (Move move in moves)
        {
            UpdateEvaluation(move, !position.IsWhiteToMove);
            position.MakeMove(move);
            int moveEval = -Search(depth - 1, -beta, -alpha, false, capturesOnly);
            eval = Math.Max(eval, moveEval);
            if (updateMove && moveEval >= bestEval && !searchTimedOut)
            {
                bestEval = moveEval;
                bestMove = move;
            }
            position.UndoMove(move);
            UpdateEvaluation(move, !position.IsWhiteToMove);
            if (eval > beta) break;
            alpha = Math.Max(alpha, eval);
        }
        return eval;
    }

    public Move Think(Board board, Timer timer)
    {
        bestMove = Move.NullMove;
        bestPreviousIteration = Move.NullMove;
        if (expectedMoves - nMoves < 6) expectedMoves += 10;
        allocatedTime = timer.MillisecondsRemaining / (expectedMoves - nMoves) * 3 / 4;
        turnTimer = timer;
        nMoves++;
        int depth = 1;
        do
        {
            searchTimedOut = false;
            position = board;
            InitialEvaluation();
            bestEval = -Infinity;
            Search(depth++, -Infinity, Infinity, true, false);
            if (!searchTimedOut) bestPreviousIteration = bestMove;
        } while (5 * turnTimer.MillisecondsElapsedThisTurn < allocatedTime);
        return bestPreviousIteration;
    }

}