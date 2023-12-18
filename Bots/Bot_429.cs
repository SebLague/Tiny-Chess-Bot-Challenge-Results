namespace auto_Bot_429;
// Les moutons

// Les Moutons is a wonderful bot that bullies humans to defeat.
// However it may lose against stronger bots.
// As a french bot, he is very stubborn and plays in a very unique style.
// Sometimes he practices the "french grève" and sacrifices his pieces.

using ChessChallenge.API;
using System;

public class Bot_429 : IChessBot
{
    private bool isWhite;
    private int oriDepth;
    private Move bestMoveFound;
    private int[] piecesValues = { 0, 71, 293, 300, 456, 905, 100000 }, nmove = { 0, 0 };
    private float coefinale = 1;

    public Move Think(Board board, Timer timer)
    {
        Poseval(board, true);
        isWhite = board.IsWhiteToMove;
        // A complicated function that determines the depth
        oriDepth = (int)(3 + Math.Pow(4 * (1.01 - coefinale), (timer.MillisecondsRemaining / timer.GameStartTimeMilliseconds)));
        bestMoveFound = Move.NullMove;
        nmove = new int[2] { 0, 0 };
        // Time to launch the incredible algorithm MeaniMax !!!
        MeaniMax(board, oriDepth, -9999999, 9999999);
        return bestMoveFound;
    }

    public SheepEval MeaniMax(Board board, int depth, float alpha, float beta)
    {
        if (depth == 0) return Poseval(board);

        Move[] moves = board.GetLegalMoves();

        if (moves.Length == 0 && board.IsInCheckmate()) return new SheepEval(-50000 + (oriDepth - depth), -50000 + (oriDepth - depth));
        if (board.IsDraw()) return new SheepEval(0, 0);

        // A revolutionnary algorithm that allows to play the best move on average
        float moyenneOfPosition, bestMoyenneAtRoot = -99999999;
        if (isWhite == board.IsWhiteToMove) moyenneOfPosition = 0;
        else moyenneOfPosition = -99999999;
        bool isBestMoveCapture = false;
        nmove[board.IsWhiteToMove ? 1 : 0] = moves.Length;
        Move alphaMateMove = Move.NullMove;

        int i = 0;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            SheepEval sheep = MeaniMax(board, depth - 1, -beta, -alpha);
            float evaluation = -sheep.eval, moyenne = -sheep.moyenne;
            board.UndoMove(move);

            if (isWhite == board.IsWhiteToMove) moyenneOfPosition += (moyenne - moyenneOfPosition) / (i + 1);
            else moyenneOfPosition = Math.Max(alpha, moyenneOfPosition);

            if (alpha >= beta)
            {
                if (isBestMoveCapture) return new SheepEval(beta, beta);
                else return new SheepEval(moyenneOfPosition, beta);
            }

            // Find the best move and the average best move to determine which one to use
            if (evaluation > alpha)
            {
                alpha = evaluation;
                isBestMoveCapture = move.IsCapture;

                if (depth == oriDepth && alpha > 49900) alphaMateMove = move;
            }

            if (depth == oriDepth)
            {
                if (move.IsCastles) moyenne += 20;

                // Found new best average move
                if (moyenne > bestMoyenneAtRoot)
                {
                    bestMoyenneAtRoot = moyenne;
                    bestMoveFound = move;
                }
                if (alphaMateMove != Move.NullMove) bestMoveFound = alphaMateMove;
            }
            i++;
        }
        // Use the classic best move (Minimax)
        if (isBestMoveCapture) return new SheepEval(alpha, alpha);
        // Else use the average best move (Meanimax)
        else return new SheepEval(moyenneOfPosition, alpha);
    }

    // Structure that contains minimax output and meanimax output
    // Saves a lot of tokens (or not...)
    public class SheepEval
    {
        public float moyenne, eval;

        public SheepEval(float moy, float beval)
        {
            this.moyenne = moy;
            this.eval = beval;
        }
    }

    private long[] piecegrid = {
      9565974397469357, //pawn
      1233245635783589, //knight
      3444467545664588, //bishop
      4533464446445644, //rook
      3446466647885688  //queen
    };

    int casindex(int x, int y)
    {
        return (int)(18 - 4 * Math.Abs(x - 3.5f) - Math.Abs(y - 3.5f));
    }

    // Very readable evaluation function
    // Any position will be evaluated with accuracy
    public SheepEval Poseval(Board board, bool calcCoef = false)
    {
        float whiteMaterial = 0, blackMaterial = 0;
        int whitePoscore = 0, blackPoscore = 0, j = 0;
        //Now let's detemrmine the material & positional score of each player
        for (int i = 0; i < 5; i++)
        {
            j = board.GetAllPieceLists()[i].Count;
            whiteMaterial += j * piecesValues[i + 1];
            for (int k = 0; k < j; k++)
            {
                whitePoscore += piecegrid[i].ToString()[casindex(board.GetAllPieceLists()[i].GetPiece(k).Square.File, board.GetAllPieceLists()[i].GetPiece(k).Square.Rank)] - '0';
            }
            j = board.GetAllPieceLists()[i + 6].Count;
            blackMaterial += j * piecesValues[i + 1];
            for (int k = 0; k < j; k++)
            {
                blackPoscore += piecegrid[i].ToString()[casindex(board.GetAllPieceLists()[i + 6].GetPiece(k).Square.File, board.GetAllPieceLists()[i + 6].GetPiece(k).Square.Rank)] - '0';
            }
        }
        // An incredible evaluation function which evaluates the position differently if its opening/middlegame or endgame
        // Very effective
        float eval;
        eval = ((whiteMaterial - blackMaterial) / (whiteMaterial + blackMaterial) * 1000) + (whiteMaterial - blackMaterial) * 10 + nmove[1] - nmove[0] + 3 * coefinale * (whitePoscore - blackPoscore);
        if (coefinale < 0.5) eval += getEndGameKingEval((board.IsWhiteToMove ? whiteMaterial : blackMaterial), (board.IsWhiteToMove ? blackMaterial : whiteMaterial), board.GetPiece(board.GetKingSquare(board.IsWhiteToMove)), board.GetPiece(board.GetKingSquare(!board.IsWhiteToMove))) / (coefinale + 0.5f);

        if (calcCoef) coefinale = (blackMaterial + whiteMaterial) / 7142;

        if (!board.IsWhiteToMove) eval *= -1;
        return new SheepEval(eval, eval);
    }

    // Very sophisticated endgame eval.
    float getEndGameKingEval(float mMaterial, float nMaterial, Piece mKing, Piece nKing)
    {
        // Like Magnus Carlsen, this AI plays endgames perfectly
        if (mMaterial > nMaterial + 150) return (1 - coefinale) * ((4.701f * getDistanceFromCenter(nKing.Square.File, nKing.Square.Rank) + 1.599f * (14.02f - getMDistance(mKing, nKing))));
        return -getDistanceFromCenter(mKing.Square.File, mKing.Square.Rank) * (1 - coefinale);
    }

    float getDistanceFromCenter(int i, int j)
    {
        if (i < 4)
        {
            if (j < 4) return Math.Abs(3 - i) + Math.Abs(3 - j);
            else return Math.Abs(3 - i) + Math.Abs(4 - j);
        }
        else
        {
            if (j < 4) return Math.Abs(4 - i) + Math.Abs(3 - j);
            else return Math.Abs(4 - i) + Math.Abs(4 - j);
        }
    }

    float getMDistance(Piece wRoi, Piece bRoi) => Math.Abs(bRoi.Square.File - wRoi.Square.File) + Math.Abs(bRoi.Square.Rank - wRoi.Square.Rank);
}
