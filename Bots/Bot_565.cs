namespace auto_Bot_565;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_565 : IChessBot
{
    Timer myTimer;
    Board myBoard;
    int TIMEOUT_LIMIT;
    /*
        Bookkeeping, 
        we need to keep tabs on material changes so we don't waste time counting it every eval cycle
    */
    int[] materialCost = new int[] { 100, 320, 350, 500, 900, 0 };
    double material = 0;
    double game_progress = 0;

    /*
        Transposition Table
    */
    public struct TranspositionEntry
    {
        public int depth;
        public SeekResult result;
        public ulong zobrist;
        public int flagtype;

        public TranspositionEntry(int _depth, SeekResult _result, ulong _zobrist, int _flagtype)
        {
            depth = _depth;
            result = _result;
            zobrist = _zobrist;
            flagtype = _flagtype;
        }
    }

    //static ulong ttLength = 6250000;
    TranspositionEntry[] TranspositionTable = new TranspositionEntry[6250000]; //200MB
    double[] MoveOrderArray = new double[32767];//Hopefully not enough to get me over the memory limit. 
    /*
        Logic
    */
    double MoveOrder(Move m) => -MoveOrderArray[m.TargetSquare.Index + 64 * m.StartSquare.Index];
    public Bot_565()
    {
        PST = new byte[6, 2][];
        for (int i = 0; i < 6; i++)
        {
            PST[i, 0] = Decompress_PST(i, 0);
            PST[i, 1] = Decompress_PST(i, 1);
        }
    }

    byte[] Decompress_PST(int piece, int opening)
    {
        var pst = new byte[64];
        for (int i = 0; i < 64; i++)
        {
            pst[i] = (byte)((compressed_PST[piece, opening, 0] >> i & 1) << 2
            | (compressed_PST[piece, opening, 1] >> i & 1) << 1
            | (compressed_PST[piece, opening, 2] >> i & 1));
        }
        return pst;
    }

    void UpdatedMaterial(Move move, double scale, bool undo = true)
    {
        if (undo)
        {
            myBoard.UndoMove(move);
        }
        else
        {
            myBoard.MakeMove(move);
        }
        if (move.IsCapture)
        {
            material += materialCost[(int)move.CapturePieceType - 1] * scale;
            game_progress += undo ? -1 : 1;
        }
        if (move.IsPromotion)
        {
            material += materialCost[(int)move.PromotionPieceType - 1] * scale;
        }
    }

    public Move Think(Board board, Timer timer)
    {
        myBoard = board;
        myTimer = timer;
        material = 0;
        game_progress = 32;

        TIMEOUT_LIMIT = Math.Clamp(timer.MillisecondsRemaining / 20, 10, 1000);

        var pieces = myBoard.GetAllPieceLists();
        //First, count initial material
        for (int i = 0; i < 12; i++)
        {
            var count = pieces[i].Count;
            game_progress -= count;
            material += count *
            materialCost[i % 6] * Math.Sign(5.5 - i);
        }
        var moveScale = myBoard.IsWhiteToMove ? 1 : -1;
        SeekResult bestMove = Seek(1, moveScale, 1);
        int d = 1;
        while (true)
        {
            try
            {
                bestMove = Seek(d += 2, moveScale);
            }
            catch (TimeoutException)
            {
                return bestMove.move;
            }
        }
    }

    public struct SeekResult
    {
        public double score;
        public Move move;
    }


    public SeekResult Seek(int depth, int color, double alpha = Double.MinValue, double beta = Double.MaxValue)
    {
        //evals++;
        if (myTimer.MillisecondsElapsedThisTurn > TIMEOUT_LIMIT) throw new TimeoutException();

        bool draw = myBoard.IsDraw();
        bool checkmate = myBoard.IsInCheckmate();
        if (depth == 0 || draw || checkmate)
        {

            ///Calculate PST 
            double eval = material;
            for (int i = 0; i < 12; i++)
            {
                PieceList pl = myBoard.GetPieceList((PieceType)((i % 6) + 1), i < 6);
                for (int j = 0; j < pl.Count; j++)
                {
                    Piece p = pl.GetPiece(j);
                    var isWhite = p.IsWhite;
                    var square = p.Square.Index;
                    int piece = (int)p.PieceType - 1;
                    int index = isWhite ? square : 2 * (square % 8) + 56 - square;
                    eval += (isWhite ? 1 : -1) * (8 * PST[piece, game_progress < 20 ? 0 : 1][index]
                    //Passed pawns
                    + 20 * (p.PieceType == PieceType.Pawn && ((ulong)(7 * (0x0101010101010101 << p.Square.File) >> 1) & myBoard.GetPieceBitboard(PieceType.Pawn, !isWhite)) == 0 ? p.Square.Rank - (isWhite ? 0 : 8) : 0));
                }
            }
            ///Return leaf node
            return new()
            {
                score = checkmate ? -10000000 : draw ? 0 :
                eval * color,
            };
        }

        double alphaOrig = alpha;
        var zobrist = myBoard.ZobristKey;
        var ttEntry = TranspositionTable[zobrist % 6250000];
        int flag = ttEntry.flagtype;
        double score = ttEntry.result.score;

        if (ttEntry.zobrist == zobrist && ttEntry.depth >= depth)
        {
            alpha = flag == 2 ? Math.Max(alpha, score) : alpha;
            beta = flag == 3 ? Math.Min(beta, score) : beta;
            if (flag == 1 || alpha >= beta) return ttEntry.result;
        }

        SeekResult bestMove = new()
        {
            score = double.MinValue
        };
        var moves = myBoard.GetLegalMoves().OrderBy(MoveOrder);

        foreach (Move move in moves)
        {
            try
            {
                UpdatedMaterial(move, color, false);
                SeekResult v = Seek(depth - (myBoard.IsInCheck() ? 0 : 1), -color, -beta, -alpha);
                v.score = -v.score;
                var vScore = v.score;
                v.move = move;
                MoveOrderArray[move.TargetSquare.Index + 64 * move.StartSquare.Index] = vScore;
                UpdatedMaterial(move, -color, true);
                bestMove = vScore > bestMove.score ? v : bestMove;
                alpha = Math.Max(alpha, vScore);
                if (alpha >= beta) break;
            }
            catch (TimeoutException ex)
            {
                throw ex;
            }
        }

        TranspositionTable[zobrist % 6250000] = new(
            depth,
            bestMove,
            zobrist,
            bestMove.score < alphaOrig ? 2 : bestMove.score > beta ? 3 : 1
        );
        return bestMove;
    }


    /*
    Piece-Square Tables, compressed form.
    Ulong[Piece, start game or End game, bit
    The entire board has been given two 3 bit numbers per square per piece represented in three 64-bit integer bitmaps, 
    forming a simple PST for both the opening and the endgame. The constructor of the bot will use this to form the 
    proper PST that the evaluation code will use. 
*/
    ulong[,,] compressed_PST = new ulong[6, 2, 3]{
        //Pawn opening
        {{
            0b0000000011111111111111110001100000011000000000000000000000000000,
            0b0000000011111111000000001110011100001000111001111110011100000000,
            0b0000000011111111111111111110011111111111000000000000000000000000
        },
        //Pawn closing
        {
            0b1111111111111111111111111111111100000000000000000000000000000000,
            0b1111111111111111111111110000000011111111111111110000000000000000,
            0b1111111111111111000000001111111111111111000000000000000000000000
        }}, 
        //Knight opening
        {{
            0b0000000000000000000000000000000000000000001001000000000000000000,
            0b0000000000000000000000000101101000000000000000000001100000000000,
            0b0000000000000000111111111010010111111111110110110000000001000010
        },
        //Knight closing
        {
            0b0000000000000000001111000011110000111100001111000000000000000000,
            0b0000000001111110010000100101101001011010010000100111111000000000,
            0b0000000000000000001111000010010000100100101111010000000000000000
        }}, 
        //Bishop opening
        {{
            0b0000000000000000000000000000000000000000000000000000000000000000,
            0b1000000101000010001001000001100000011000001001000101101000000000,
            0b0000000000000000010110100110011001100110010110100011110000100100
        },
        //Bishop closing
        {
            0b0000000001111110011111100111111001111110011111100111111000000000,
            0b0000000000000000000000000000000000000000000000000000000000000000,
            0b0000000000000000000000000000000000000000000000000000000000000000
        }}, 
        //Rook opening
        {{
            0b0000000000000000000000000000000000000000000000000000000000111000,
            0b0000000000000000000110000001100011111111111111110000000010000001,
            0b0000000000000000000000000010010011111111000000000000000010111001
        },
        //Rook closing
        {
            0b1111111111111111111111111111111111111111111111111111111111111111,
            0b1111111111111111111111111111111111111111111111111111111111111111,
            0b1111111111111111111111111111111111111111111111111111111111111111
        }}, 
        //Queen opening
        {{
            0b0000000000000000000000001111111111111111111111110000000000000000,
            0b0000000011111111111111110000000000000000000000000111111100001000,
            0b1111111100000000111111111111111111111111000000000111111100000000
        },
        //Queen closing
        {
            0b1111111011111111111111111111111111111111111111111111111101111110,
            0b0000000101111110011111100111111001111110011111100111111010000001,
            0b1111111111111111111111111111111111111111111111111111111111111111
        }}, 
        //King opening
        {{
            0b0000000000000000000000000000000000000000000000000000000001010100,
            0b0000000000000000000000000000000000000000000000000000000011000111,
            0b0000000000000000000000000000000000000000000000000000000011010111
        },
        //King closing 
        {
            0b0000000000000000000000000001100000011000000000000000000000000000,
            0b0000000001111110011111100110011001100110011111100111111000000000,
            0b1111111110000001101111011011110110111101101111011000000111111111
        }
        },
    };

    //PST[piece][opening or ending, square];
    byte[,][] PST;
}