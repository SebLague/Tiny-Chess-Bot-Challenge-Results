namespace auto_Bot_437;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Math;

public class Bot_437 : IChessBot
{
    // right now funktions are seperated. before submision, everything will be compacted into the think function if possible.
    //---this section is variables designated to zobrist hashing and the transportition table---

    const ulong boardHashLen = (1 << 20);
    (ulong key, float boardVal, int depth, Move bestMove, int bound)[] boardHashes = new (ulong, float, int, Move, int)[boardHashLen]; //dict <zobrist key, tuple<total_board_value, depth_iteration, bestMove>>

    //right now this funktion is not needed as it seems board has a funktion to get the zobrist key but it might need to be reintruduced if the api funktion is to slow
    //ulong hashBoard(Board board)
    //{
    //    PieceList[] PL = board.GetAllPieceLists();
    //    Piece[] PA = new Piece[28];
    //    int PAI = 0;
    //    foreach (PieceList PL2 in PL)
    //    {
    //        for(int i=0;i<PL2.Count;i++)
    //        {
    //            PA[PAI] = PL2.GetPiece(i);
    //            PAI++;
    //        }
    //    }
    //    return 0;
    //}
    //---end---

    bool weAreWhite;
    int[] pieceSqareValues;
    // how much each piece is worth
    int[] pieceValues = {
            100, // Pawn
            300, // Knight
            320, // Bishop
            500, // Rook
            900, // Queen
            2000 }; // King

    public bool IsEndgameNoFunction = false;


    float infinity = 1000000; // should work aslong as it's bigger than: 900 + 500 * 2 + 320 * 2 + 300 * 2 + 100 * 8 + 50 * 16 = 4740 (king not included because both colors always has a king

    // debug variables (variables only used for debuging)
    //int searchedMoves = 0; //#DEBUG
    //int foundCheckMates = 0; //#DEBUG
    //int addedZobristKeys = 0; //#DEBUG
    //int usedZobristKeys = 0; //#DEBUG
    // -----------------------------

    Move bestMove;

    int phase = 0;
    int[] phaseValues = { 0, 0, 1, 1, 2, 4, 0 };
    int qd = -5; // quince search depth
    public void updatePhase(Board board)
    {
        phase = board.GetAllPieceLists()
            .SelectMany(x => x)
            .Sum(
                p => phaseValues[(int)p.PieceType]);

    }
    public Move Think(Board board, Timer timer)
    {
        //DivertedConsole.Write(getPieceValue(PieceType.Pawn, new Square(1, 1), false));

        pieceSqareValues = toPieceArray(new[] { 4747474776866575, 4649555643514953, 4047465140464645, 3747424147474747, 0022383326366858, 3465586645525363, 4449525141455150, 3932444717413138, 3949243740524244, 4358605946495362, 4651515547525252, 4952524738474341, 5759576255576465, 4653555841444955, 3740444735404343, 3543424542444852, 3947565141364648, 4443495040404343, 4540454543484447, 3745514847424550, 2954524356474245, 4554484343424440, 3347403643434134, 4849452943585132, 4747474799979386, 7476726757545149, 5150474549494648, 5150505047474747, 3137443940454047, 4142505043485454, 4246525541474752, 3542454639334143, 4341444545464944, 4845474747505150, 4648515344475050, 4342454741454146, 5150535251515151, 4949494949485148, 4849504946474647, 4646474845484847, 4554545543535759, 4249506148545460, 4255536143405249, 4141394338394135, 2637424244525152, 5052545245545455, 4246535442475153, 4044495132384144 }); // use https://onlinestringtools.com/split-string to split into 16 long parts
        updatePhase(board);
        //DivertedConsole.Write("Phase: " + phase); //#Debug

        weAreWhite = board.IsWhiteToMove;
        // DivertedConsole.Write("---calculate new move--- " + (weAreWhite ? "W" : "B")); //#DEBUG
        bestMove = Move.NullMove;
        for (int depth = 1; depth <= 30; depth++)
        {
            miniMax(board, depth, weAreWhite ? 1 : -1, -infinity + 10, infinity - 10, getPieceValues(board) * (weAreWhite ? 1 : -1), 0, timer);

            //}
            if (timer.MillisecondsRemaining < 0)
                qd = 0;
        }




        //if (boardHashes.Count > 9500)
        //{ //#DEBUG
        //    DivertedConsole.Write("flushing bordhashes buffer"); //#DEBUG
        //} //#DEBUG

        //DivertedConsole.Write("dececion took: " + timer.MillisecondsElapsedThisTurn + " ms this turn"); //#DEBUG
        //foreach (ulong i in boardHashes.Keys) if (boardHashes[i].Item2 < boardHashCounter - maxSearchDepth) boardHashes.Remove(i); 
        //DivertedConsole.Write("------ " + (weAreWhite ? "W" : "B")); //#DEBUG

        return bestMove == Move.NullMove ? board.GetLegalMoves()[0] : bestMove;
        //DivertedConsole.Write(isPieceProtectedAfterMove(board, moves[0]));

    }

    private float miniMax(Board board, int depth, int currentPlayer, float min, float max, float prevBase, int ply, Timer timer)
    {
        //bool isMaximizingPlayer = currentPlayer > 0; // could also be called isWhite
        if (board.IsRepeatedPosition()) return 0;

        Move bMove = Move.NullMove;
        float bMoveMat = -infinity;

        if (depth < 1)
        {

            bMoveMat = prevBase;

            if (max <= min) return prevBase;
            min = Max(min, prevBase);
        }

        var moves = board.GetLegalMoves(depth <= 0);
        if (moves.Length == 0) // if there are no legal moves we can do
            return depth > 0
                ? board.IsInCheck()
                    ? -infinity // checkmate
                    : 0 // stalemate
                : prevBase; // no more capturing moves

        ulong key = board.ZobristKey;
        var result = boardHashes[key % boardHashLen];
        bool foundTable = result.key == key;

        if (ply > 0 && foundTable && result.depth >= depth &&
            (result.bound == 1
            || result.bound == 0 && result.boardVal >= max
            || result.bound == 2 && result.boardVal <= min
            )) return result.boardVal;
        var storedBestMove = result.bestMove.RawValue; // this automaticly happens when we do move == otherMove, but it's slighty faster do to only calculating it once. can be removed if needed, token wise
        List<(Move move, float Base)> sortedMoves = moves.Select(m => (m, evaluateBase(m, currentPlayer > 0))).ToList();
        // if(depth < 1) sortedMoves.Add(new (Move.NullMove, prevBase));

        sortedMoves = sortedMoves.OrderByDescending(
            item => foundTable && storedBestMove == item.move.RawValue && result.depth > qd
            ? infinity
            : item.Base
            - (item.move.IsCapture
                ? pieceValues[(int)item.move.MovePieceType - 1] / 3
                : 0)).ToList();
        // if it's a capture it subtracks the attackers value thereby creating MVV-LVA (Most Valuable Victim - Least Valuable Aggressor)

        float origMin = min;
        // Iterate through sortedMoves and evaluate potential moves
        foreach (var (move, Base) in sortedMoves)
        {


            float v = 0;
            board.MakeMove(move);

            float newBase = move.IsEnPassant || move.IsCastles ? getPieceValues(board) * currentPlayer : (prevBase + Base); // if it is enPassent we recalculate the move

            v =
                depth > qd ? //if
                    -miniMax(board, depth - 1, -currentPlayer, -max, -min, -newBase, ply + 1, timer) : //if the depth is bigger than qd (q search depth) use minimax (we swap max and min because the player has changed)
                    newBase;

            board.UndoMove(move);




            if (v > bMoveMat)
            {
                // improve best move and the best moves result
                bMove = move;
                bMoveMat = v;
                if (ply < 1) bestMove = bMove; // if it's root we want to asign global best move to local best move
                // ^ has to be here if we are using simom
                // alpha beta
                min = Max(min, v);
                if (max <= min) break;
            }

        }


        boardHashes[key % boardHashLen] = (key, bMoveMat, depth, bMove, bMoveMat >= max ? 0 : bMoveMat > origMin ? 2 : 1);


        return bMoveMat;
    }
    float getPieceValues(Board board) =>
        board.GetAllPieceLists().SelectMany(x => x).Sum(p =>
            getPieceValue(p.PieceType, p.Square, p.IsWhite) * (p.IsWhite ? 1 : -1));

    // getPieceValue
    // gets the value of one piece depending on its type and its position on the board
    // pieceType: the type of the piece that should be avaluated
    // s: the sqare the piece is standing on (only used to calculate piece sqare tables)
    // isWhite: if the piece is white. used to flip the board if necessary 
    private float getPieceValue(PieceType pieceType, Square s, bool IsWhite)
    {
        int pieceTypeIndex = (int)pieceType - 1;
        if (pieceTypeIndex < 0) return 0;

        int flatPos = s.Index;
        if (IsWhite) flatPos ^= 0b111000; // flip the board if we are white
        if (s.File > 3) flatPos ^= 0b000111; // mirror the board to use less bbs
        flatPos = (flatPos & 0b111000) >> 1 | (flatPos & 0b000011) + pieceTypeIndex * 32; // shift the ranks down beacouse we only have 4 files instead of 8

        //int flatPos = s.Index;
        //int rank = (((flatPos & 0b111000) ^ (IsWhite ? 0b111000 : 0) >> 1); // flip the y axis

        //if (s.File > 3) flatPos ^= 0b000111;
        //flatPos = rank | (flatPos & 0b000011);

        //DivertedConsole.Write(flatPos);


        //int flatPos =
        //    (s.File > 3 ? 7 - s.File : s.File) // this mirrors the table to use less BBS
        //    + (IsWhite ? 7 - s.Rank : s.Rank) * 4 // flip the table if it is white
        //    + pieceTypeIndex * 32; // choose the correct table depending on what type of piece
        return pieceValues[pieceTypeIndex] + (pieceSqareValues[flatPos] * phase + pieceSqareValues[flatPos + 192] * (24 - phase)) / 24 * 3.5f - 167;
    } //#DEBUG

    int[] toPieceArray(long[] arr) => Array.ConvertAll(arr, element => Enumerable.Range(0, 8).Select(i => int.Parse(element.ToString("D16").Substring(i * 2, 2)))).SelectMany(x => x).ToArray();




    //left in the code for now even tho it's unused might be used in the future
    //public bool isPieceProtectedAfterMove(Board board, Move move) => !board.SquareIsAttackedByOpponent(move.TargetSquare); //#DEBUG

    float evaluateBase(Move move, bool isWhite)
    {
        //if (move.IsEnPassant || move.IsCastles) // beause it is a "special" move we just return 0. this is for some reason better than returning below
        //    return 0;
        return
            -getPieceValue(move.MovePieceType, move.StartSquare, isWhite)  // remove the old piece 
            + getPieceValue(move.IsPromotion ? move.PromotionPieceType : move.MovePieceType, move.TargetSquare, isWhite) // add the new piece (move piece type if it is't promotion. if it is use the promotion piece type)
            + getPieceValue(move.CapturePieceType, move.TargetSquare, !isWhite); // remove the captured piece (plus beacuse we capture the oponements piece wich is good for the current player)

    }

    //float evaluateTop(Board board, int currentPlayer) => board.IsInCheckmate() ? 1000000000000 * currentPlayer* maxSearchDepth : 0;

    //ulong prevSeed = 0;
    //ulong smallRandomNumberGenerator(ulong seed = 0, int maxSizeRange = 100)
    //{
    //    if (seed == 0) seed = prevSeed;
    //    prevSeed = (ulong)Abs(Cos(seed * 10) * maxSizeRange);
    //    return prevSeed;
    //}
}
