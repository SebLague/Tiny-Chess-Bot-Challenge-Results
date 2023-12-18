namespace auto_Bot_275;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_275 : IChessBot
{

    public Dictionary<ulong, double> cache = new Dictionary<ulong, double>();

    public Move Think(Board board, Timer timer)
    {
        var a = Search(board, true, 10000, debug: false);
        return a.move!.Value;
    }



    Random random = new Random();

    //
    // Strategy constants
    //

    // https://en.wikipedia.org/wiki/Chess_piece_relative_value
    Dictionary<PieceType, double> materialValues = new Dictionary<PieceType, double>()
    {
        { PieceType.King, 0 }, // This shouldn't interfere with the scores
        { PieceType.Pawn, 1 },
        { PieceType.Knight, 3 },
        { PieceType.Bishop, 3 },
        { PieceType.Rook, 5 },
        { PieceType.Queen, 9 }
    };

    const double SC_MaterialWeight = 300.0; // how much do we value material advantage?
    const double SC_Draw = -999999999; // really big negative for draws/repetitions/stalemates. Will avoid unless the alternative is a loss
    const double SC_Check = 50; // putting the opponent in check is good and should be investigated // Not used
    const double SC_PieceCost = 0; // extra incentive to just remove enemy pieces
    const double SC_BoardCenterControl = 90;
    const double SC_Supression = 150; // incentive to box in opposing king // Not used
    const double SC_Threatened = 0.1; // penalty for having your pieces targeted
    const double SC_KingProx = 1; // incentive to move towards the enemy king
    const double SC_PawnAdvance = 5; // move the pawns forward
    const double SC_HangingPenalty = 0.1; // a piece that is left hanging loses this much of its value
    const double SC_HangingAndThreatenedPenalty = 0.9; // a piece that is left hanging and is threatened loses this much of its value

    // How much do I like this board?
    // This is a potential next turn for me.
    public double Score(Board board, bool myTurn)
    {
        if (board.IsInCheckmate()) return myTurn ? double.NegativeInfinity : double.PositiveInfinity;

        double score = random.NextDouble();
        if (board.IsDraw() || board.IsFiftyMoveDraw())
        {
            score += SC_Draw * (myTurn ? 1 : -1);
        }

        // check check
        //if (board.IsInCheck()) score += SC_Check * (myTurn ? -1 : 1);

        var b_board = board.AllPiecesBitboard;
        ulong b_op_threat = 0, b_my_threat = 0;
        if (myTurn) b_op_threat = threatmap(board);
        else b_my_threat = threatmap(board);
        board.ForceSkipTurn();
        if (myTurn) b_my_threat = threatmap(board);
        else b_op_threat = threatmap(board);
        board.UndoSkipTurn();/////////////////////////THE BOARD WILL NO LONGER SAY IT IS IN CHECK, EVEN IF IT IS

        bool myTeam = board.IsWhiteToMove;
        if (!myTurn) myTeam = !myTeam;

        // Control the center of the board
        score -= bitboard(b_op_threat, 3, 3) * SC_BoardCenterControl;
        score -= bitboard(b_op_threat, 3, 4) * SC_BoardCenterControl;
        score -= bitboard(b_op_threat, 4, 3) * SC_BoardCenterControl;
        score -= bitboard(b_op_threat, 4, 4) * SC_BoardCenterControl;
        score += bitboard(b_my_threat, 3, 3) * SC_BoardCenterControl;
        score += bitboard(b_my_threat, 3, 4) * SC_BoardCenterControl;
        score += bitboard(b_my_threat, 4, 3) * SC_BoardCenterControl;
        score += bitboard(b_my_threat, 4, 4) * SC_BoardCenterControl;

        // Piece counting metrics
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                if (piece.IsKing) continue;
                var mine = isMine(piece, myTeam);
                var r = piece.Square.Rank;
                var f = piece.Square.File;

                score += mine * SC_PieceCost;

                // compute the material advantage
                var pieceValue = materialValues[piece.PieceType] * mine * SC_MaterialWeight;
                score += pieceValue;

                // don't leave pieces hanging, and threaten enemy pieces
                if (mine == 1)
                {
                    if (bitboard(b_op_threat, r, f) == 1) // penalty for having pieces threatened, (mostly) cancelled by defending
                    {
                        if (bitboard(b_my_threat, r, f) == 1) score -= pieceValue * SC_Threatened;
                        else score -= pieceValue * SC_HangingAndThreatenedPenalty;
                    }
                    if (bitboard(b_my_threat, r, f) == 1) score -= pieceValue * SC_HangingPenalty; // penalty for leaving a piece hanging

                }
                else
                {
                    if (bitboard(b_my_threat, r, f) == 1) // penalty for having pieces threatened, (mostly) cancelled by defending
                    {
                        if (bitboard(b_op_threat, r, f) == 1) score -= pieceValue * SC_Threatened;
                        else score -= pieceValue * SC_HangingAndThreatenedPenalty;
                    }
                    if (bitboard(b_op_threat, r, f) == 1) score -= pieceValue * SC_HangingPenalty; // penalty for leaving a piece hanging
                }

                // piece progression
                if (piece.IsPawn)
                {
                    if (piece.IsWhite) // higher score for higher file
                    {
                        score += f * SC_PawnAdvance * mine;
                    }
                    else // higher score for lower file
                    {
                        score += (8 - f) * SC_PawnAdvance * mine;
                    }
                }
                else
                {
                    // distance to king
                    var kpos = board.GetKingSquare(mine != 1);
                    var _x = kpos.Rank - r;
                    var _y = kpos.File - f;
                    score -= _x * _x + _y * _y * SC_KingProx * mine;
                }
            }

        }
        return score;
    }

    int bitboard(ulong board, int rank, int file) // check a rank and file in a bitboard
    {
        if (rank > 7 || rank < 0 || file > 7 || file < 0) return 0;
        return ((board >> rank + file * 8) & 1) > 0 ? 1 : 0;
    }
    int isMine(Piece piece, bool myTeam) // returns 1 if its my piece, -1 if its my opponent's
    {
        return piece.IsWhite == myTeam ? 1 : -1;
    }

    ulong threatmap(Board board)
    {
        ulong map = 0;
        for (int r = 0; r < 8; r++)
        {
            for (int f = 0; f < 8; f++)
            {
                if (board.SquareIsAttackedByOpponent(new Square(r, f))) map |= ((ulong)1) << r + f * 8;
            }
        }
        return map;
    }

    HashSet<ulong> visited = new HashSet<ulong>();

    SearchResult Search(Board board, bool myTurn, int budget, bool debug = false)
    {
        var moves = board.GetLegalMoves();

        // out of budget for this path, score this board
        if (moves.Count() == 0 || budget <= 0) return new SearchResult(null, Score(board, myTurn), 0);

        if (visited.Contains(board.ZobristKey))
        {
            return new SearchResult(null, SC_Draw * (myTurn ? 1 : -1), 0);
        }

        visited.Add(board.ZobristKey);

        var each = budget / moves.Length;

        double bestScore = 0;
        Move? bestMove = null;
        int bestDepth = 0;

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            var result = Search(board, !myTurn, each);
            var score = result.score;
            if (bestMove == null || (myTurn && score > bestScore) || (!myTurn && score < bestScore) || (score == bestScore && result.depth < bestDepth))
            {
                bestMove = move;
                bestScore = score;
                bestDepth = result.depth;
            }

            board.UndoMove(move);

            //if (debug) DivertedConsole.Write("Best: {0} {1} | This: {2} {3}", bestMove.ToString(), bestScore, move.ToString(), score);
        }

        visited.Remove(board.ZobristKey);
        return new SearchResult(bestMove, bestScore, bestDepth + 1);
    }

    struct SearchResult
    {
        public Move? move;
        public double score;
        public int depth;

        public SearchResult(Move? move, double score, int depth) : this()
        {
            this.move = move;
            this.score = score;
            this.depth = depth;
        }
    }
}