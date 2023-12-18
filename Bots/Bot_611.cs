namespace auto_Bot_611;
// Resources used
// https://github.com/AnshGaikwad/Chess-World/blob/master/play.py
// https://github.com/jw1912/Chess-Challenge/blob/nn/Chess-Challenge/src/My%20Bot/MyBot.cs#L164
// https://web.archive.org/web/20071030084528/http://www.brucemo.com/compchess/programming/alphabeta.htm

using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_611 : IChessBot
{

    public Move Think(Board board, Timer timer)
    {
        Move bestMove = default;
        int iterDepth = 3;

        //while (iterDepth < 64 && timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 30)
        Search(-30000, 30000, iterDepth++);


        return bestMove;

        void Search(int alpha, int beta, int depth)
        {
            int bestValue = -30000;
            var moves = board.GetLegalMoves(false);
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                var boardValue = -AlphBet(-beta, -alpha, depth - 1);
                if (boardValue > bestValue)
                {
                    bestValue = boardValue;
                    bestMove = move;
                }
                if (boardValue > alpha)
                {
                    alpha = boardValue;
                }
                board.UndoMove(move);
            }

            int AlphBet(int alpha, int beta, int depth)
            {
                int bestscore = -9999;
                var moves1 = board.GetLegalMoves(false);
                if (depth == 0)
                {
                    return quiesce(alpha, beta);
                }
                foreach (Move move in moves1)
                {
                    board.MakeMove(move);
                    int val = -AlphBet(-beta, -alpha, depth - 1);
                    board.UndoMove(move);
                    if (val >= beta)
                    {
                        return val;
                    }
                    if (val > bestscore)
                    {
                        bestscore = val;
                    }
                    if (val > alpha)
                    {
                        alpha = val;
                    }
                }
                return alpha;
            }

            int quiesce(int alpha, int beta)
            {
                int standPat = Evaluate();
                if (standPat >= beta)
                    return beta;
                if (alpha < standPat)
                    alpha = standPat;
                var moves2 = board.GetLegalMoves(false);
                foreach (Move move in moves2)
                {
                    if (move.IsCapture)
                    {
                        board.MakeMove(move);
                        int score = -quiesce(-beta, -alpha);
                        board.UndoMove(move);
                        if (score >= beta)
                            return beta;
                        if (score > alpha)
                            alpha = score;
                    }
                }
                return alpha;
            }
        }

        // Evaluation Feed Forward NN
        int Evaluate()
        {
            float[] x0 = refine();
            List<float> x = new List<float>(x0);
            for (int i = 0; i < W.Length; i++)
            {
                x = MV_multiply(W[i], x).ToList();
                for (int j = 0; j < x.Count; j++)
                {
                    x[j] += B[i][j];
                }
                x = Relu(x);
            }
            //int result = (int)(-(float)Math.Log((1/(x[0]))-1)*1000);
            //return result;
            return (int)(x[0] * 1000);
        }

        float[] refine()
        {
            float[] magicNum = {1799006.86904004F, 1263811.01656626F, 1471359.55383204F, 1845698.98262769F,
 1338375.48563436F, 1967142.81661152F,  983261.48628797F,  572963.14041666F,
  529013.76797665F,  342594.49390919F,  331331.47627541F,  218989.83534127F};
            float[] totBoard = new float[12];
            foreach (bool stm in new[] { true, false })
            {
                int num = 0;
                for (var p = PieceType.Pawn; p <= PieceType.King; p++)
                {
                    float mask = board.GetPieceBitboard(p, stm);
                    if (stm)
                        totBoard[num] = mask;
                    else
                        totBoard[num + 6] = mask;

                    num++;
                }
            }
            for (int iter = 0; iter <= 11; iter++)
            {
                totBoard[iter] += 1;
                Math.Log(totBoard[iter]);
                totBoard[iter] /= magicNum[iter];
                totBoard[iter] *= 1000;
            }
            return totBoard;
        }

        List<float> Relu(List<float> X)
        {
            for (int i = 0; i < X.Count; i++)
            {
                if (X[i] < 0)
                    X[i] = 0;
            }
            return X;
        }
    }

    float[] MV_multiply(float[,] matrix, List<float> vector)
    {
        float[] U = new float[matrix.GetLength(1)];
        for (int i = 0; i < matrix.GetLength(1); i++)
        {
            for (int j = 0; j < matrix.GetLength(0); j++)
            {
                U[i] += matrix[j, i] * vector[j];
            }
        }
        return U;
    }

    float[][] B = new float[][] {
            new float[] {0.03723057F,-0.02762258F,-0.01822989F,0.01881527F,0.0F,
0.01681741F,0.05372256F},

            new float[] {-0.02900909F,-0.11783109F,0.05222551F,-0.01532215F,0.08988103F},

            new float[] {0.11955099F}
        };


    readonly float[][,] W = new float[][,] {
        new float[,] {{-0.2245362F,0.26698455F,-0.06180186F,0.48695573F,-0.2463555F,
0.14795984F,-0.7198302F},
{-0.2327547F,1.4093838F,0.36108053F,1.3187697F,-0.29626F,
0.05212346F,-2.403266F},
{-0.94918567F,1.924019F,-0.51017267F,0.46424535F,-0.0486629F,
0.40468052F,-1.2982361F},
{-0.14670518F,-0.5582919F,0.5218005F,0.36282298F,-0.3485757F,
1.1722072F,1.5048163F},
{0.44167754F,0.2916649F,0.25444445F,0.247489F,-0.33844364F,
-1.3355412F,-0.64011896F},
{-0.90532476F,-0.6576307F,-1.077615F,-0.86914754F,-0.24946654F,
-0.26641902F,0.36790362F},
{2.3710692F,0.67924076F,2.1382835F,1.3556714F,-0.38678256F,
1.4025216F,2.3730643F},
{0.13229287F,-1.3165954F,0.47243595F,1.2844214F,-0.2680156F,
0.07807291F,-0.42814243F},
{0.7724434F,1.55683F,-0.27742192F,-0.64641577F,-0.28080624F,
-0.39437747F,0.561956F},
{0.717485F,-0.12144568F,-0.06022454F,0.32228944F,-0.04870766F,
-0.15342246F,0.39988837F},
{0.21623155F,-0.11056086F,0.8012865F,-0.7402706F,-0.11873627F,
1.8311926F,0.39109844F},
{-0.43093172F,-0.30519754F,-0.83965504F,0.2904027F,-0.54140484F,
1.3032936F,0.58138674F}},

            new float[,] {{0.7534533F,0.36069393F,0.01789098F,-0.3618458F,0.8589776F},
{0.61125696F,-0.36628985F,1.6377976F,0.5661947F,1.0410044F},
{-0.22709699F,-0.7106891F,0.3642272F,-0.11433335F,1.4423318F},
{0.7330322F,-0.7759369F,0.846702F,1.3073138F,0.8674749F},
{0.19072425F,0.55867F,-0.5703055F,-0.4375305F,0.4746259F},
{-1.6324086F,-0.15931149F,0.54522127F,-0.17149477F,0.1086554F},
{-0.5892155F,0.30308014F,0.79505754F,-1.1514558F,0.38477027F}},

            new float[,] {{-1.6760415F},
{-0.44252953F},
{0.46493134F},
{1.2749492F},
{1.0145607F}}
};


}