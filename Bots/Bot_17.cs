namespace auto_Bot_17;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

/* < THE COPYCAT >
 *
 * This bot always tries to replicate the opponent's move as closely as possible.
 * That's it really. It's admittedly not a very good bot as far as chess bots goes
 * but that wasn't my intention. I just find it fun to look at :)
 * 
 * Thanks for everything! <3
 * 
 * - castur_
 */

public class Bot_17 : IChessBot
{
    Random random = new Random();
    ulong opponentBitboardOld = 0b0;
    bool isWhite;

    public Move Think(Board board, Timer timer)
    {
        Move[] legalMoves = board.GetLegalMoves();

        // New game
        if (opponentBitboardOld == 0b0)
        {
            // Initialize the opponent's bitboard. We don't know if white or black is
            // at the top of the board, so we instead see where the bot's pieces are
            opponentBitboardOld = (legalMoves[0].StartSquare.Index < 16) ?
                0b1111111111111111000000000000000000000000000000000000000000000000 :
                0b0000000000000000000000000000000000000000000000001111111111111111;

            // The bot can't copy the opponents move if its the first to move,
            // so we just pick a random valid move if that's the case
            if (isWhite = board.GetPiece(legalMoves[0].StartSquare).IsWhite)
            {
                return legalMoves[random.Next(legalMoves.Length)];
            }
        }

        // Get the new state of the opponent's pieces
        ulong opponentBitboardNew = isWhite ? board.BlackPiecesBitboard : board.WhitePiecesBitboard;

        // Using some bit magic we can figure out what move the opponent last made
        ulong lastMoveMaskStart = opponentBitboardOld & ~opponentBitboardNew;
        ulong lastMoveMaskTarget = ~opponentBitboardOld & opponentBitboardNew;

        // Get the indices from the bit masks
        int lastMoveIndexStart = (int)Math.Log2(lastMoveMaskStart);
        int lastMoveIndexTarget = (int)Math.Log2(lastMoveMaskTarget);

        // Reflect the move horizontally about the centre of the board
        // (I left the expression unsimplified for the sake of clarity)
        int newMoveIndexStart = lastMoveIndexStart + 8 * (7 - 2 * (lastMoveIndexStart / 8));
        int newMoveIndexTarget = lastMoveIndexTarget + 8 * (7 - 2 * (lastMoveIndexTarget / 8));

        // We now have a target to aim for, but we don't yet know if that move is valid.
        // We thus make use of a spiral search algorithm to find the move(s) that
        // lands the closest to said target. If multiple moves are equally close we then 
        // choose the one with a starting square closest to that of the opponent's reflected move

        List<Move> targets = new List<Move>();

        int x = newMoveIndexTarget % 8;
        int y = newMoveIndexTarget / 8;
        int dir = 1;

        // This is a naïve approach but hey, it works right? Why make it more complicated than it needs to
        for (int steps = 1; steps < 16; ++steps)
        {
            for (int i = 0; i < steps; ++i)
            {
                if (x < 0 || x >= 8 || y < 0 || y >= 8)
                {
                    continue;
                }
                foreach (Move move in legalMoves)
                {
                    if (move.TargetSquare.Index == (8 * y + x))
                    {
                        targets.Add(move);
                    }
                }
                if (targets.Count > 0)
                {
                    goto targets_found;
                }
                x += dir;
            }
            for (int i = 0; i < steps; ++i)
            {
                if (x < 0 || x >= 8 || y < 0 || y >= 8)
                {
                    continue;
                }
                foreach (Move move in legalMoves)
                {
                    if (move.TargetSquare.Index == (8 * y + x))
                    {
                        targets.Add(move);
                    }
                }
                if (targets.Count > 0)
                {
                    goto targets_found;
                }
                y += dir;
            }
            dir *= -1;
        }
    targets_found:

        Move result = Move.NullMove;

        // Here we determine which of the potential moves is the closest
        // to the opponent's move by simply taking the euclidean distance
        int minDist = int.MaxValue;
        foreach (Move target in targets)
        {
            int x1 = target.StartSquare.Index % 8;
            int y1 = target.StartSquare.Index / 8;
            int x2 = newMoveIndexStart % 8;
            int y2 = newMoveIndexStart / 8;
            int dx = x2 - x1;
            int dy = y2 - y1;
            int dist = dx * dx + dy * dy;
            if (dist < minDist)
            {
                minDist = dist;
                result = target;
            }
        }

        // If we for some reason didn't find a valid move we just randomize it
        // (This should never happen but you can never be too safe lol)
        if (result.Equals(Move.NullMove))
        {
            result = legalMoves[random.Next(legalMoves.Length)];
        }

        // "Make" the move in case we capture any pieces, then update the bitboard 
        board.MakeMove(result);
        opponentBitboardOld = isWhite ? board.BlackPiecesBitboard : board.WhitePiecesBitboard;

        // We have now computed the objectively best move to make :)
        return result;
    }
}