namespace auto_Bot_128;
using ChessChallenge.API;
using System;

public class Bot_128 : IChessBot
{
    //King is not in this list so as to cause exception when unintentionally qeurying it's value
    static float[] pieceValues = { 0.0f, 1.0f, 3.0f, 3.3f, 5.0f, 9.0f };

    private int moveIndex = -1;

    private int[] positionIndices = { -80, -70, -60, -50, -40, -35, -30, -25, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 80 };

    private ulong[] idealPositions =
    {
        868082074056920076,
        940704799824678413,
        939855994094750477,
        868082091303898124,
        940423363653668109,
        1012764636003438094,
        1591483802437686806,
        868082074056920076,
        868082074056920076,
        1012762419733073422,
        1012762419733073422,
        1157442765409226768,
        1302123111085380114,
        1591483802437686806,
        1663823975275763479,
        868082074056920076,
        217305304961254403,
        290495426158397444,
        436020196764683526,
        435739825611410438,
        436021300588121350,
        435738721787972614,
        290495421846652932,
        217305304961254403,
        579286540304910856,
        724248360957775114,
        724532043581361674,
        723969093627939850,
        724249469092957450,
        723967994116246538,
        723966885981064202,
        579286540304910856,
        868082078368664588,
        796024480018992139,
        796024480018992139,
        796024480018992139,
        796024480018992139,
        796024480018992139,
        940704825695145485,
        868082074056920076,
        579286544616655368,
        723966885981129738,
        723967989804502282,
        796025583842429964,
        796025583842429963,
        723967989804502026,
        723966885981064202,
        579286544616655368,
        1158003499092283920,
        1157437246292037648,
        722836570780469258,
        578156216480826888,
        433475875116418054,
        289078108240413444,
        144680345676153346,
        282578800148736,
        217868254914676227,
        434329134945797894,
        506672623565932551,
        579013904539191560,
        651354077377268233,
        723689834921593610,
        796025583842429963,
        579286540304910856,
    };

    private int getPosRating(Board board, int pieceType, int x, int y)
    {
        if (!board.IsWhiteToMove)
            y = 7 - y;

        return positionIndices[(idealPositions[pieceType * 8 + y] >> (x * 8)) & 0xFF];
    }

    private float getMoveRating(Board board, Move move, int maxDepth)
    {
        float rating = 0.0f;
        board.MakeMove(move);
        foreach (Move crntMove in board.GetLegalMoves(true))
        {
            float crntRating = pieceValues[(int)crntMove.CapturePieceType];
            if (maxDepth != 0/* && (maxDepth >= 8 || crntRating > 0.0f)*/)
                crntRating += getMoveRating(board, crntMove, maxDepth - 1);
            rating = Math.Min(rating, -crntRating);
        }
        rating += pieceValues[(int)move.CapturePieceType];
        //rating *= 4.0f; //Material importance
        if (board.IsInCheck())
            rating += 0.4f;
        if (board.IsInCheckmate())
            rating += 1024.0f;
        board.UndoMove(move);

        return rating;
    }

    public Move Think(Board board, Timer timer)
    {
        moveIndex++;
        Random rng = new();

        Move[] moves = board.GetLegalMoves();
        Move? bestMove = null;
        Move? secondBestMove = null;
        float moveRating = -128.0f;
        foreach (Move move in moves)
        {
            //Square coords
            int x = move.TargetSquare.Index % 8, y = move.TargetSquare.Index / 8;

            float crntMoveRaiting = 0.0f;
            //Capturing
            if (move.IsCapture)
                crntMoveRaiting += (float)move.CapturePieceType;
            //Avoid too many king moves
            if (move.MovePieceType == PieceType.King)
                crntMoveRaiting -= 0.1f;
            //Promotion
            if (move.IsPromotion)
                crntMoveRaiting += (float)move.PromotionPieceType * 0.5f;
            //Promote castle in the early game
            if (move.IsCastles)
                crntMoveRaiting += 3.0f / (1.0f + moveIndex);
            //Pawns in the early and late game
            //if (move.MovePieceType == PieceType.Pawn)
            //    crntMoveRaiting += (float)Math.Pow(moveIndex - 40, 2) / 800.0f;
            //Figures in the very early game
            //else
            //    crntMoveRaiting += 2.0f / (1.0f + moveIndex);
            //Taking the center
            //if (moveIndex < 8)
            //    crntMoveRaiting += 1.0f / (1.0f + Math.Abs(x - 3.5f + y - 3.5f));
            //Ideal positions
            int otherPosRatingOffset = 0;
            if (move.MovePieceType == PieceType.Pawn)
                otherPosRatingOffset = -1;
            if (move.MovePieceType == PieceType.King)
                otherPosRatingOffset = 1;
            float gameProgressMultiplier = Math.Clamp((moveIndex - 4) / 30.0f, 0.0f, 1.0f);
            float posRating = getPosRating(board, (int)move.MovePieceType, x, y) * (1.0f - gameProgressMultiplier) + getPosRating(board, (int)move.MovePieceType + otherPosRatingOffset, x, y) * gameProgressMultiplier;
            //DivertedConsole.Write(move.StartSquare.ToString() + move.TargetSquare.ToString());
            //DivertedConsole.Write("Pos: ");
            //DivertedConsole.Write((posRating - 100) / 100.0f * 2.0f);
            crntMoveRaiting += (posRating - 100) / 100.0f * 2.0f;
            //float a = getMoveRating(board, move, 8);
            //DivertedConsole.Write("Mat: ");
            //DivertedConsole.Write(a);
            crntMoveRaiting += getMoveRating(board, move, 8);
            if (crntMoveRaiting > moveRating)
            {
                moveRating = crntMoveRaiting;
                secondBestMove = bestMove;
                bestMove = move;
            }
        }
        if (board.IsRepeatedPosition())
            bestMove = secondBestMove;
        if (bestMove != null)
            return bestMove.Value;

        return moves[rng.Next(moves.Length)];
    }
}
