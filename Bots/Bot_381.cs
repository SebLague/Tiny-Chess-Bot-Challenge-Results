namespace auto_Bot_381;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Main bot class that does the thinking. NAME= Scouty (https://en.wikipedia.org/wiki/Principal_variation_search)
/// Use Negascout algo with Transposition Table, Quienscence, Score ordering, and Pesto Piece Square Table compressed
/// Null move pruning and Killer move ordering try also.
/// I begin without any help, inspired by rustic chess engine, and then took idea from the discord like the compressed pst etc.
/// Thank you for this Challenge Seb, it was fun developping it.
/// </summary>
public class Bot_381 : IChessBot

{

    //win vs mybotv1, oB3
    // From Sidhant-Roymoulik -> todo test others packed PST
    decimal[] pestoCompressed = {
        63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m, 936945638387574698250991104m, 75531285965747665584902616832m,
        77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m, 2475894077091727551177487608m,
        2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m,
        77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m, 5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m, 5619082524459738931006868492m, 649197923531967450704711664m,
        75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m, 76772801025035254361275759599m,
        75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m,
        73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m, 3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m,
        68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m,
    };
    //PST uncompressed
    int[][] pestoTable = new int[64][];
    //Pieces values
    short[] pvm = { 82, 337, 365, 477, 1025, 0, // Middlegame
                    94, 281, 297, 512, 936, 0}; //Endgame

    Board board;
    Move bestmoveRoot = Move.NullMove;
    Timer timer;
    //Transposition Table
    MyMove[] tt = new MyMove[0x400000]; //Max size TT ~4000000
    //List of killerMoves (removed to same token)
    MyMove[] killerMoves; //keep only 1 instead of 2 because save time and token.
    //bool doNull = true;
    int MVA_OFFSET = int.MaxValue - 256;

    public Bot_381() =>
        pestoTable = pestoCompressed.Select(packedTable =>
        {
            int pieceType = 0;
            //Uncompress PESTO PST
            return decimal.GetBits(packedTable).Take(3)
                .SelectMany(c => BitConverter.GetBytes(c)
                    .Select((byte square) => (int)((sbyte)square * 1.461) + pvm[pieceType++]))
                .ToArray();
        }).ToArray();

    public Move Think(Board _board, Timer _timer)
    {
        board = _board;
        timer = _timer;
        killerMoves = new MyMove[6000];//Max ply
        //count = 0;
        for (int depth = 1; ;)
        {
            applyNegascoutOnmoves(depth++, -100000, 100000, board.IsWhiteToMove ? 1 : -1, 0);
            //DivertedConsole.Write("Depth=" + depth + " move n°" + board.PlyCount + " checks : " + count + " -> " + bestmoveRoot + " = " + bestScore);
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 40)
                break;
        }
        return bestmoveRoot.IsNull ? board.GetLegalMoves()[0] : bestmoveRoot;
    }

    //Test with minimax, negamaw -> negascout was the most quick and strong.
    //inspired by https://rustic-chess.org/search/ordering/how.html
    int applyNegascoutOnmoves(int depth, int alpha, int beta, int color, int ply)
    {
        bool quiencense = depth <= 0,
            dopv = false,
            incheck = board.IsInCheck(),
            notroot = ply > 0;
        int alphaOrigin = alpha;
        ulong zKey = board.ZobristKey;
        if (!notroot && board.IsRepeatedPosition()) return 0;
        if (incheck) depth++;

        MyMove ttMove = tt[zKey & 0x3FFFFF];
        if (!notroot && ttMove != null && ttMove.key == zKey && ttMove.depth >= depth)
        {
            switch (ttMove.flag)
            {
                case 0:
                    alpha = Math.Max(alpha, ttMove.value);
                    break;
                case 1:
                    beta = Math.Min(beta, ttMove.value);
                    break;
                case 2:
                    return ttMove.value;
            }
            if (alpha >= beta) return ttMove.value;
        }
        int bestMoveValue = -200000;
        if (quiencense)
        {
            bestMoveValue = evaluateBoard();
            if (bestMoveValue > beta) return bestMoveValue;
            alpha = Math.Max(alpha, bestMoveValue);
        }
        /* Remove null move pruning to save token
         * else
        {
            if (doNull && !incheck && depth >=2)
            {
                doNull = false;
                board.TrySkipTurn();
                int nullScore = -applyNegascoutOnmoves(depth - 3 - depth / 6, -beta, 1 - beta, -color, ply + 1);
                board.UndoSkipTurn();
                doNull = true;
                if (nullScore >= beta) return nullScore;
            }
        }*/

        var moves = new List<MyMove>();
        //Score moves
        foreach (Move m in board.GetLegalMoves(quiencense))
        {
            int value = 0;
            MyMove move = new MyMove(m);
            if (move.equals(ttMove)) //TT ordering
                value = MVA_OFFSET + 60;
            else if (m.IsCapture) //Capture ordering
                value = MVA_OFFSET + (0xABCDEF0 >> (int)m.MovePieceType * 4 & 0xF) + 10 * (int)m.CapturePieceType; //MVV_LVA
            else if (move.Equals(killerMoves[board.PlyCount]))
                value = MVA_OFFSET - 10; //10 = killerValue*/
            move.sortScore = value;
            moves.Add(move);
        }

        MyMove bestMove = new MyMove(Move.NullMove);

        for (int i = 0; i < moves.Count; i++)
        {
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 40) return 100000;

            //PickMove -> put the best sort score at the first index -> best to order inside the loop to use full capacity of move ordering
            for (int j = i + 1; j < moves.Count; j++)
                if (moves[j].sortScore > moves[i].sortScore)
                    (moves[i], moves[j]) = (moves[j], moves[i]); //swap
            MyMove currentMove = moves[i];
            board.MakeMove(currentMove.move);
            int currentScore = 0; //DRAW score
            if (!board.IsDraw())
            {
                if (!dopv) currentScore = -applyNegascoutOnmoves(depth - 1, -beta, -alpha, -color, ply + 1);
                else
                {
                    currentScore = -applyNegascoutOnmoves(depth - 1, -alpha - 1, -alpha, -color, ply + 1);
                    if (currentScore > alpha && currentScore < beta) currentScore = -applyNegascoutOnmoves(depth - 1, -beta, -alpha, -color, ply + 1);
                }
            }
            board.UndoMove(currentMove.move);
            if (currentScore > bestMoveValue)
            {
                bestMoveValue = currentScore;
                bestMove = currentMove;
                if (!notroot) bestmoveRoot = bestMove.move;
            }
            if (bestMoveValue > alpha)
            {
                alpha = bestMoveValue;
                dopv = true;
            }
            if (alpha >= beta)
            {
                if (!currentMove.move.IsCapture) //&& !currentMove.Equals(killerMoves[board.PlyCount, 0]))
                {
                    //killerMoves[board.PlyCount, 1] = killerMoves[board.PlyCount, 0];
                    killerMoves[board.PlyCount] = currentMove;
                }
                break;
            }
        }
        if (!quiencense && moves.Count == 0)
        {
            if (board.IsInCheck())
                return ply - 100000; //we are checkmate
            return 0; //stalemate
        }
        if (bestMoveValue >= beta) bestMove.flag = 0;//lowerbound
        else bestMove.flag = (bestMoveValue <= alphaOrigin) ? 1 : 2; //upperbound : exact
        bestMove.value = bestMoveValue;
        bestMove.depth = depth;
        bestMove.key = zKey;
        tt[zKey & 0x3FFFFF] = bestMove;
        return bestMoveValue;
    }
    int evaluateBoard()
    {
        //count++;
        int gamePhase = 0, mgValue = 0, egValue = 0;
        foreach (bool whitePiece in new[] { true, false })
        {
            for (int i = 0; i < 6; i++)
            {
                ulong bitboard = board.GetPieceBitboard((PieceType)i + 1, whitePiece);
                while (bitboard != 0)
                {
                    gamePhase += 0x042110 >> i * 4 & 0xF;
                    int j = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard) ^ (whitePiece ? 56 : 0); ;
                    mgValue += pestoTable[j][i];
                    egValue += pestoTable[j][i + 6];
                }
            }
            mgValue = -mgValue;
            egValue = -egValue;
        }
        return (board.IsWhiteToMove ? 1 : -1) * (mgValue * gamePhase + egValue * (24 - gamePhase)) / 24;
    }
    class MyMove
    {
        public Move move;
        public ulong key;
        public int depth, value, flag, sortScore;
        public MyMove(Move _move) => move = _move;
        // override object.Equals
        public bool equals(MyMove obj) => obj != null && move.Equals(obj.move);
    }
}



//OLD TRY (custom PST compression)


//int[] gamephaseInc = { 0, 1, 1, 2, 4, 0 };// -> 0x042110

/*int getPSTValue(int phase, bool whitePiece, int i, int j)
{
    ulong pst = (bestPositions[whitePiece ? 63 - j : j] >> (4 * i + 24 * phase)) & 0xF;
    int sign = (pst & 8) == 8 ? -1 : 1;
    return (whitePiece ? 1 : -1) * (int)(
        sign * ((0x23281E140F0A0500 >> ((int)(pst & 7) * 8)) & 0xff) //pst value
        + (0x5A32201F0A >> i * 8 & 0xff) * 10); //piece value
}*/

/*int[][] MVV_LVA = { -> 
    new int[]{15, 14, 13, 12, 11, 10 }, // victim P, attacker P, N, B, R, Q, K
    new int[]{25, 24, 23, 22 ,21, 20 }, // victim N, attacker P, N, B, R, Q, K
    new int[]{35, 34, 33, 32, 31, 30 }, // victim B, attacker P, N, B, R, Q, K
    new int[]{45, 44, 43, 42, 41, 40 }, // victim R, attacker P, N, B, R, Q, K
    new int[]{55, 54, 53, 52, 51, 50 }, // victim Q, attacker P, N, B, R, Q, K
 //value = MVA_OFFSET + MVV_LVA[(int)move.move.CapturePieceType - 1][(int)move.move.MovePieceType - 1] -10;
};*/


/*
void scoreMoves(List<MyMove> moves, MyMove ttMove)
{
    for (int i = 0; i < moves.Count; i++)
    {
        int value = 0;
        MyMove move = moves[i];
        if (move.equals(ttMove)) //TT ordering
            value = MVA_OFFSET + 60;
        else if (move.move.IsCapture) //Capture ordering
            value = MVA_OFFSET + (0xABCDEF0 >> (int)move.move.MovePieceType * 4 & 0xF) + 10 * (int)move.move.CapturePieceType; //MVV_LVA
        else //Killer move ordering
             //for (int n = 0; n < 2; n++)
            if (move.Equals(killerMoves[board.PlyCount]))
                value = MVA_OFFSET - 10; //10 = killerValue
        move.sortScore = value;
    }
}*/

//r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1 -> -> (with quiencese)
//Depth=9 move n°0 checks : 16 299 258 Nb positions saved : 3039928 -> Move: 'e2a6' = -47 -> 42.7
//Depth=8 move n°0 checks : 3 392 607 Nb positions saved : 1327587 -> Move: 'e2a6' = 5 -> 17.2s (19 without init) -> 12.1
//Depth=8 move n°0 checks : 3 396 320 Nb positions saved : 1329521 -> Move: 'e2a6' = 5 (with one killermove) -> 11.7
//Depth=8 move n°0 checks : 3 518 421 Nb positions saved : 1388626 -> Move: 'e2a6' = -41 -> 11.8 (with pesto) 
//Depth=8 move n°0 checks : 6 875 033 -> Move: 'e2a6' = -82 -> 23s (with check depth and others)
//Depth=7 move n°0 checks : 1 662 195 -> Move: 'e2a6' = -36 -> 5.4 -> 3.1 -> 4.6
//Depth=6 move n°0 checks : 326 165 -> Move: 'e2a6' = -30 -> 1s
//Depth=5 Nb move checks : 107 362 Nb positions saved : 6107 -> Move: 'd5e6' = 315 -> 0.4s
//Depth=4 Nb move checks : 317 510 Nb positions saved : 68992 -> Move: 'd5e6' = 315 -> 1.4s
//Depth=3 Nb move checks : 5 906 Nb positions saved : 1796 -> Move: 'e2a6' = 50 -> 0.1s
//Depth=2 Nb move checks : 4 004 Nb positions saved : 138 -> Move: 'e2a6' = 390 -> 0.1s
//Depth=1 Nb move checks : 166 Nb positions saved : 49 -> Move: 'e2a6' = 70 -> 0.1

/* With iterative : 
Depth=2 move n°0 checks : 1890 -> Move: 'e2a6' = 23
Depth=3 move n°0 checks : 3732 -> Move: 'e2a6' = 23
Depth=4 move n°0 checks : 9007 -> Move: 'e2a6' = -10
Depth=5 move n°0 checks : 37855 -> Move: 'd5e6' = 2
Depth=6 move n°0 checks : 205721 -> Move: 'e2a6' = -27
Depth=7 move n°0 checks : 642303 -> Move: 'e2a6' = 100000
2s
With null move pruning
Depth=2 move n°0 checks : 1890 -> Move: 'e2a6' = 23
Depth=3 move n°0 checks : 3733 -> Move: 'e2a6' = 23
Depth=4 move n°0 checks : 9057 -> Move: 'e2a6' = -10
Depth=5 move n°0 checks : 44412 -> Move: 'd5e6' = 2
Depth=6 move n°0 checks : 229299 -> Move: 'e2a6' = -27
Depth=7 move n°0 checks : 518857 -> Move: 'e2a6' = -30
Depth=8 move n°0 checks : 527904 -> Move: 'e2a6' = 100000
1.6s
*/

// 1- 1 -6 contre X, 2-2-12, 3-2-6, 2-4-8
//best = 537 token -> 853 -> 959
//int[] positionValues = { -50, -40, -30, -20, -10, -5, 0, 5, 10, 15, 20, 25, 30, 40, 50 };
//int[] positionValues = { 0, 5, 10, 15, 20, 30, 40, 50 };
//TODO make symetric, in ulong and modify bestPos to include -1/+1
// ->  positionValues = 0x23281E140F0A0500
//int[] pieceValues = { 100, 310, 320, 500, 900, 0 }; 
//               King|Queen|Rook|Bishop|Knight|Pawn piece value /10 coded on 8 bits
//->  pieceValue = 0x5A32201F0A;

// List of all prefered possition : index of value above coded in an 6*4 bit int for each case
// King|Queen|Rook|Bishop|Knight|Pawn
// square piece from https://github.com/lhartikk/simple-chess-ai/blob/master/script.js 
/*ulong[] bestPositions =
                   {0x236306, 0x146416, 0x146426, 0x56426, 0x56426, 0x146426, 0x146416, 0x236306,
                    0x24741e, 0x16863e, 0x16866e, 0x6866e, 0x6866e, 0x16866e, 0x16863e, 0x24741e,
                    0x245428, 0x166668, 0x17678a, 0x7689c, 0x7689c, 0x17678a, 0x166668, 0x245428,
                    0x255427, 0x166777, 0x176798, 0x768ab, 0x768ab, 0x176798, 0x166777, 0x255427,
                    0x365426, 0x266666, 0x276896, 0x1768aa, 0x1768aa, 0x276896, 0x266666, 0x355426,
                    0x445427, 0x376875, 0x376884, 0x376896, 0x376896, 0x376884, 0x366875, 0x445427,
                    0xa45417, 0xa66738, 0x676668, 0x666673, 0x666673, 0x666668, 0xa66738, 0xa45417,
                    0xa36306, 0xc46416, 0x846426, 0x657426, 0x657426, 0x846426, 0xc46416, 0xa36306
};*/
/* With EG = MG
ulong[] bestPositions = {
0xdc0cf0dc0cf0, 0xea0ae0ea0ae0, 0xea0ad0ea0ad0, 0xf90ad0f90ad0, 0xf90ad0f90ad0, 0xea0ad0ea0ad0, 0xea0ae0ea0ae0, 0xdc0cf0dc0cf0,
0xda1ae7da1ae7, 0xe020c7e020c7, 0xe02007e02007, 0xf02007f02007, 0xf02007f02007, 0xe02007e02007, 0xe020c7e020c7, 0xda1ae7da1ae7,
0xda9ad2da9ad2, 0xe00002e00002, 0xe10124e10124, 0xf10235f10235, 0xf10235f10235, 0xe10124e10124, 0xe00002e00002, 0xda9ad2da9ad2,
0xd99ad1d99ad1, 0xe00111e00111, 0xe10132e10132, 0xf10244f10244, 0xf10244f10244, 0xe10132e10132, 0xe00111e00111, 0xd99ad1d99ad1,
0xc09ad0c09ad0, 0xd00000d00000, 0xd10230d10230, 0xe10244e10244, 0xe10244e10244, 0xd10230d10230, 0xd00000d00000, 0xc99ad0c99ad0,
0xaa9ad1aa9ad1, 0xc10219c10219, 0xc1022ac1022a, 0xc10230c10230, 0xc10230c10230, 0xc1022ac1022a, 0xc00219c00219, 0xaa9ad1aa9ad1,
0x4a9ae14a9ae1, 0x4001c24001c2, 0x010002010002, 0x000001c00001c, 0x00001c00001c, 0x000002000002, 0x4001c24001c2, 0x4a9ae14a9ae1,
0x4c0cf04c0cf0, 0x5a0ae05a0ae0, 0x2a0ad02a0ad0, 0x0091ad0091ad0, 0x091ad0091ad0, 0x2a0ad02a0ad0, 0x5a0ae05a0ae0, 0x4c0cf04c0cf0
};*/

/* PST with simplified tb and king EG
  ulong[] bestPositions = {
    0xfc0cf0dc0cf0, 0xea0ae0ea0ae0, 0xda0ad0ea0ad0, 0xc90ad0f90ad0, 0xc90ad0f90ad0, 0xda0ad0ea0ad0, 0xea0ae0ea0ae0, 0xfc0cf0dc0cf0,
    0xda1ae7da1ae7, 0xc020c7e020c7, 0xa02007e02007, 0x2007f02007, 0x2007f02007, 0xa02007e02007, 0xc020c7e020c7, 0xda1ae7da1ae7,
    0xda9ad2da9ad2, 0xa00002e00002, 0x410124e10124, 0x510235f10235, 0x510235f10235, 0x410124e10124, 0xa00002e00002, 0xda9ad2da9ad2,
    0xd99ad1d99ad1, 0xa00111e00111, 0x510132e10132, 0x610244f10244, 0x610244f10244, 0x510132e10132, 0xa00111e00111, 0xd99ad1d99ad1,
    0xd09ad0c09ad0, 0xa00000d00000, 0x510230d10230, 0x610244e10244, 0x610244e10244, 0x510230d10230, 0xa00000d00000, 0xd99ad0c99ad0,
    0xda9ad1aa9ad1, 0xa10219c10219, 0x41022ac1022a, 0x510230c10230, 0x510230c10230, 0x41022ac1022a, 0xa00219c00219, 0xda9ad1aa9ad1,
    0xda9ae14a9ae1, 0xd001c24001c2, 0x10002010002, 0x1c00001c, 0x1c00001c, 0x2000002, 0xd001c24001c2, 0xda9ae14a9ae1,
    0xfc0cf04c0cf0, 0xda0ae05a0ae0, 0xda0ad02a0ad0, 0xd91ad0091ad0, 0xd91ad0091ad0, 0xda0ad02a0ad0, 0xda0ae05a0ae0, 0xfc0cf04c0cf0
    };*/

/*PESTO reworked
 * ulong[] bestPositions = {
    0xfa3bf0fd5df0, 0xe42ce04061f0, 0xc44ab0355fd0, 0xc53ad0b27ef0, 0xa529d0f77d70, 0x342ad0d62ef0, 0x122bf00651b0, 0xb41cf0366af0,
    0xab2ad75c5df7, 0x3439a70e53e7, 0x3531d7c97c77, 0x362a07907b67, 0x3799a7ab7547, 0x641bd7977777, 0x4529c7e55415, 0x201bf7d76fba,
    0x2c10c7ab9bf9, 0x311ac74b4671, 0x421027015665, 0x371027b26675, 0x471007c53577, 0x6591a7176777, 0x6490c7477674, 0x3291e7c7306c,
    0xa119b5bdc9ab, 0x441214cda133, 0x443243ab1441, 0x560241db5774, 0x570340d04664, 0x560221d35672, 0x570123b0a143, 0x1600c3e0c04c,
    0xcc19c3fae9bd, 0x9511920dd310, 0x442339daa339, 0x471449ea0532, 0x559139f02553, 0x45923ae99241, 0x26a911d11242, 0xa4aac0f9c1ad,
    0xcb9ac1bbf0cd, 0x9d0991b0d3a9, 0x239209cab329, 0x410230f0b32a, 0x429320e91341, 0x33a199d00531, 0x12a9c0b39445, 0xa1bbcad1d2ba,
    0xdc9be30ee1de, 0xac9cc21ab3f0, 0x1d09a2a2c3ac, 0x3b0092f0a09c, 0x3ba103e2010b, 0x1caac0b32444, 0x9eabc02995b6, 0xbd9de920f0cc,
    0xfdacd0b0cdf0, 0xdd0af06cb9c0, 0xcc1cc02a0bf0, 0xae09b0f23cd0, 0xd99ac02b3bb0, 0xbdbbc0dd1ad0, 0xcc19f04deec0, 0xeecbf03fdcc0
};*/
/*ulong[] bestPositions = {
        0xfa3bf0dc0cf0, 0xe42ce0ea0ae0, 0xc44ab0ea0ad0, 0xc53ad0f90ad0, 0xa529d0f90ad0, 0x342ad0ea0ad0, 0x122bf0ea0ae0, 0xb41cf0dc0cf0,
        0xab2ad7da1ae7, 0x3439a7e020c7, 0x3531d7e02007, 0x362a07f02007, 0x3799a7f02007, 0x641bd7e02007, 0x4529c7e020c7, 0x201bf7da1ae7,
        0x2c10c7da9ad2, 0x311ac7e00002, 0x421027e10124, 0x371027f10235, 0x471007f10235, 0x6591a7e10124, 0x6490c7e00002, 0x3291e7da9ad2,
        0xa119b5d99ad1, 0x441214e00111, 0x443243e10132, 0x560241f10244, 0x570340f10244, 0x560221e10132, 0x570123e00111, 0x1600c3d99ad1,
        0xcc19c3c09ad0, 0x951192d00000, 0x442339d10230, 0x471449e10244, 0x559139e10244, 0x45923ad10230, 0x26a911d00000, 0xa4aac0c99ad0,
        0xcb9ac1aa9ad1, 0x9d0991c10219, 0x239209c1022a, 0x410230c10230, 0x429320c10230, 0x33a199c1022a, 0x12a9c0c00219, 0xa1bbcaaa9ad1,
        0xdc9be34a9ae1, 0xac9cc24001c2, 0x1d09a2010002, 0x3b009200001c, 0x3ba10300001c, 0x1caac0000002, 0x9eabc04001c2, 0xbd9de94a9ae1,
        0xfdacd04c0cf0, 0xdd0af05a0ae0, 0xcc1cc02a0ad0, 0xae09b0091ad0, 0xd99ac0091ad0, 0xbdbbc02a0ad0, 0xcc19f05a0ae0, 0xeecbf04c0cf0,
            };*/