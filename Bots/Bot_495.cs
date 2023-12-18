namespace auto_Bot_495;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_495 : IChessBot
{
    private int MaxDepth { get; set; } = 4;
    private Move bestMove;
    private Board board;
    private readonly int[] pieceValues = { 100, 300, 300, 500, 900, 999999999 };

    // таблицы приклов
    private readonly ulong[,] _packedEvaluationTablesWhite =
    {
        { 271536506641564,  17570711268508,  31864362429440, 265025671457948,  17570795154588, 266061010234524,  50556060101788, 271536506641564 },
        { 270539956473374, 270539990693406, 270565760497182, 270565760497152, 270565760497152, 270565760497177, 270539990693401, 270539956473369 },
        { 259553463687173, 259562054552074, 259592118352906, 259592119981096, 259592119981586, 259592118352901, 259562054552079, 259553463687176 },
        { 248511354427394, 248532829865218, 248554306666506, 248554305355866, 248554305355821, 248554306666506, 248532829865218, 248511354427394 },
        { 204530805430278, 204552280868102, 204573755834374, 204573755836422, 204573755839494, 204573755834374, 204552280868102, 204530805430278 },
        { 193535773038597, 193557248018949, 193557248212997, 193557248212997, 193557248212997, 193557248212997, 193557248018949, 193535773038597 },
        { 182540572872202, 182562047718922, 182562047718912, 182562047718922, 182562047718922, 182562047718922, 182562047718922, 182540572872202 },
        { 172580460141638, 171545372716102, 171545372716102, 171545372716102, 171545372716102, 171545372716102, 171545372716102, 172580460141638 }
    };

    private readonly ulong[,] _packedEvaluationTablesBlack =
    {
        { 172580460141638, 171545372716102, 171545372716102, 171545372716102, 171545372716102, 171545372716102, 171545372716102, 172580460141638 },
        { 182540572872202, 182562047718922, 182562047718912, 182562047718922, 182562047718922, 182562047718922, 182562047718922, 182540572872202 },
        { 193535773038597, 193557248018949, 193557248212997, 193557248212997, 193557248212997, 193557248212997, 193557248018949, 193535773038597 },
        { 204530805430278, 204552280868102, 204573755834374, 204573755836422, 204573755839494, 204573755834374, 204552280868102, 204530805430278 },
        { 248511354427394, 248532829865218, 248554306666506, 248554305355866, 248554305355821, 248554306666506, 248532829865218, 248511354427394 },
        { 259553463687173, 259562054552074, 259592118352906, 259592119981071, 259592119981586, 259592118352901, 259562054552079, 259553463687176 },
        { 270539956473374, 270539990693406, 270565760497182, 270565760497152, 270565760497152, 270565760497177, 270539990693401, 270539956473369 },
        { 271536506641564,  17570711268508,  31864362429440, 265025671457948,  17570795154588, 266061010234524,  50556060101788, 271536506641564 }
    };

    //распаковка таблиц приколов
    public int EvaluationTables(PieceType type, bool isWhite, int file, int rank)
    {
        int pieceIndex = (int)type;
        ulong packedDataWhite = _packedEvaluationTablesWhite[rank, file];
        ulong packedDataBlack = _packedEvaluationTablesBlack[rank, file];
        int shiftAmount = 8 * (pieceIndex - 1);
        sbyte unpackedDataWhite = unchecked((sbyte)((packedDataWhite >> shiftAmount) & 0xFF));
        sbyte unpackedDataBlack = unchecked((sbyte)((packedDataBlack >> shiftAmount) & 0xFF));

        return isWhite ? unpackedDataWhite : unpackedDataBlack;
    }

    //поиск тихих ходов
    public int QuiescenceSearch(int alpha, int beta, int color)
    {
        int standPat = EvaluateBoard();

        if (standPat >= beta)
            return beta;

        if (alpha < standPat)
            alpha = standPat;

        foreach (Move move in board.GetLegalMoves())
        {
            if (move.IsCapture)
            {
                board.MakeMove(move);

                int score = -QuiescenceSearch(-beta, -alpha, -color);

                board.UndoMove(move);

                if (score >= beta)
                    return beta;

                if (score > alpha)
                    alpha = score;
            }
        }

        return alpha;
    }

    //смешной метод в три строки, типо СЦЖ НЦН (самая ценная жертва, наименее ценный нападающий)
    public int MVVLVA(Move move)
    {
        Piece capturedPiece = board.GetPiece(move.TargetSquare);
        int pieceValue = pieceValues[(int)capturedPiece.PieceType];
        return pieceValue * pieceValue - pieceValue;
    }

    //это база
    public int Search(int depth, int alpha, int beta, int color)
    {
        //мат не мат
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
            return board.IsDraw() ? 0 : (board.IsInCheckmate() ? -999999 + (MaxDepth - depth) : QuiescenceSearch(alpha, beta, color));

        //MVV LVA сортировка
        var legalMoves = board.GetLegalMoves();
        Array.Sort(legalMoves, (move1, move2) => MVVLVA(move2).CompareTo(MVVLVA(move1)));

        //переменные
        int bestEval = -999999;
        int origAlpha = alpha;

        //глядим на список всех возможных ходов
        for (int i = 0; i < legalMoves.Length; i++)
        {
            var move = legalMoves[i];

            //делаем ход
            board.MakeMove(move);
            int eval;

            //редукция
            if (i >= 2 && depth > 1)
            {
                int reducedDepth = depth - 1;
                eval = -Search(reducedDepth, -alpha - 1, -alpha, -color);

                //на условиях повторно вызываем сёрч
                if (eval > alpha && eval < beta)
                {
                    eval = -Search(depth - 1, -beta, -alpha, -color);
                }
            }
            //без условий вызываем сёрч
            else
            {
                eval = -Search(depth - 1, -beta, -alpha, -color);
            }

            //отменяем
            board.UndoMove(move);

            //обновляем лучшую оценку и альфу
            if (eval > bestEval)
            {
                bestEval = eval;
                if (depth == MaxDepth) bestMove = move; //сохраняем лучший ход
                alpha = Math.Max(alpha, eval);
                if (alpha >= beta) break; //отсечение
            }

            //прерываем поиск, если глубина меньше или равна 3 и оценка значительно хуже альфы
            if (depth <= 3 && eval + 2000 < alpha)
            {
                break;
            }
        }

        //лучший ход готов
        return bestEval;
    }

    //веса для разных стадий игры
    private int[] stageWeights = { };

    public int[] CalculateStageWeights()
    {
        int totalPieceCount = board.GetAllPieceLists().Sum(pList => pList.Count);
        if (totalPieceCount == 32) return new int[] { 5, 5, 5, 5, 0 };
        if (totalPieceCount >= 30) return new int[] { 5, 5, 9, 5, 0 };
        else return new int[] { 5, 5, 5, 5, 5 };
    }

    //применение весов
    public int EvaluateBoard()
    {
        int color = board.IsWhiteToMove ? 1 : -1;

        int squareBonus = board.GetAllPieceLists()
            .SelectMany(pList => pList)
            .Sum(piece => EvaluationTables(piece.PieceType, piece.IsWhite, piece.Square.File, piece.Square.Rank));

        //прикол вычисляет материальное значение на шахматной доске, учитывая количество и ценность различных типов фигур
        int materialValue = Enumerable.Range(0, 5)
            .Select(i => (board.GetAllPieceLists()[i].Count - board.GetAllPieceLists()[i + 6].Count) * pieceValues[i])
            .Sum();

        if (board.IsWhiteToMove)
        {
            //собсна применение весов для белых
            int totalEvaluation = (materialValue * color * stageWeights[0]) +
                (board.GetLegalMoves().Length * color * stageWeights[1]) +
                (squareBonus * color * stageWeights[2]) + (board.IsInCheck() ? 100 : 0 * color * stageWeights[3]) + (Attacks() * color * stageWeights[4]);

            return totalEvaluation;
        }
        else
        {
            //собсна применение весов для черных
            int totalEvaluation = (materialValue * color * stageWeights[0]) +
                (board.GetLegalMoves().Length * stageWeights[1]) +
                (squareBonus * stageWeights[2]) + (board.IsInCheck() ? 100 : 0 * color * stageWeights[3]) + (Attacks() * color * stageWeights[4]);

            return totalEvaluation;
        }
    }

    //атаки, защиты
    private int Attacks()
    {
        return board.GetLegalMoves()
            .Where(move => move.IsCapture)
            .Sum(move => (board.SquareIsAttackedByOpponent(move.TargetSquare) ? 20 : 10));
    }

    //конец
    public Move Think(Board board, Timer timer)
    {
        this.board = board;

        MaxDepth = board.GetAllPieceLists().Sum(pl => pl.Count) <= 12 ? 5 : MaxDepth;

        stageWeights = CalculateStageWeights();

        bool isInCheck = board.IsInCheck();

        if (isInCheck)
        {
            MaxDepth++;
        }

        Search(MaxDepth, -999999999, 999999999, board.IsWhiteToMove ? 1 : -1);

        if (isInCheck)
        {
            MaxDepth--;
        }

        return bestMove;
    }
}