namespace auto_Bot_401;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// #8: Blindfold bot - an attempt at making a good bot that doesn't use recursive seaching
//                   - looks only its first move ahead, everything else it calculates from that position.
// Advanatges: Very fast, good at defending, doesn't assume opponent's moves
// Disadvantages: Poor positioning, doesn't plan ahead, weak to check forks
// Has a terrible weakness involving sliders (it cannot see discovered attacks)

public class Bot_401 : IChessBot
{

    bool isBotWhite;

    ulong totalPlayerAttackBitboard; // All of the player(bot)'s attacks on the opponent's pieces. Doesn't count how many times a piece is attacked.
    ulong totalOpponentAttackBitboard;

    // On each tile, there's a list of all the squares that are attacking it, divided into player & opponent-occupied 
    public List<int>[] playerAttackInfo = new List<int>[64];
    public List<int>[] opponentAttackInfo = new List<int>[64];

    public Bot_401()
    {
        for (int i = 0; i < 64; i++)
        {
            playerAttackInfo[i] = new();
            opponentAttackInfo[i] = new();
        }
    }

    public Move Think(Board board, Timer timer)
    {
        isBotWhite = board.IsWhiteToMove;


        /* The idea:
        1. Am i being attacked?
        Look at own net (and the lack of it), does the attack leave me in loss?
        If so, add a good avoiding move to the list

        2. Can i capture?
        Look at opponent's net & their attacked pieces, does the attack leave me in gain?
        If so, add a good attacking move to the list

        3.Do i have good moves? Use them! (preferrably the best one)
        If i don't, proceed

        4. Is it endgame?
        If so, try to approach the enemy king and checkmate

        5.Can i attack the opponent's weak point?
        Look at their free or poorly protected pieces, make a safe attack
        */

        InitBitboardsAndNets(board);

        Move[] moves = board.GetLegalMoves();


        Move bestMove = moves[0];
        int bestScore = int.MinValue;


        foreach (Move move in moves)
        {
            bool safe = false;


            bool winningEndgame = false;

            int startingScore = EvaluateMaterialPoints(board, isBotWhite) - EvaluateMaterialPoints(board, !isBotWhite);


            if (EvaluateMaterialPoints(board, !isBotWhite) < 9) winningEndgame = true;

            board.MakeMove(move);

            InitBitboardsAndNets(board);

            // 1. Looks at all attacked positions and iterates over the attack info tables, checking if at any point the opponent will be at an advantage
            int score = 0;
            List<int> targetList = rateAllExchanges(board, totalOpponentAttackBitboard, false);
            if (targetList.Count > 0) score += targetList.Min();

            if (score >= 0) safe = true;
            // 2. Adds the captured piece points to the list
            score += EvaluateMaterialPoints(board, isBotWhite) - EvaluateMaterialPoints(board, !isBotWhite) - startingScore;


            // 3. If the bot is about to lose material it's generally a bad move
            score *= 1000;


            ulong playerPiecesBitboard = board.WhitePiecesBitboard;
            if (!isBotWhite) playerPiecesBitboard = board.BlackPiecesBitboard;

            Piece playedPiece = board.GetPiece(move.TargetSquare);

            // 4.
            // If the bot is in a winning endgame, it will switch tactics and try to move towards enemy king
            if (winningEndgame && score <= 0)
            {

                score += 100;

                if (board.IsRepeatedPosition()) score -= 150;

                if (playedPiece.IsPawn) score += 20;

                // Try to approach the king if the pieces are far away
                Square kingSquare = board.GetKingSquare(!isBotWhite);

                while (playerPiecesBitboard != 0)
                {
                    Square square = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref playerPiecesBitboard));

                    int distance = Math.Max(Math.Abs(kingSquare.File - square.File), Math.Abs(kingSquare.Rank - square.Rank)); // Manhattan distance with diagonals
                    score -= distance * distance * 2;
                }

            }

            // 5.
            // Make a safe attack
            else
            {
                // Important: the safe check has to be here to ensure the attacking piece is also not being attacked negatively.
                if (safe)
                {
                    targetList = rateAllExchanges(board, totalPlayerAttackBitboard, true);
                    if (targetList.Count > 0) score += targetList.Max();
                }

                if (playedPiece.IsPawn) score += 1;

            }



            if (board.IsDraw())
            {
                if (winningEndgame) score -= 100000;
                else score += 1000;
            }
            else if (board.IsInCheckmate()) score = int.MaxValue;

            // The furthest bit of searching the bot does - if an opponent's move is a check or a mate it's probably bad for the bot
            else
            {
                foreach (Move responseMove in board.GetLegalMoves())
                {
                    board.MakeMove(responseMove);
                    if (board.IsInCheck()) score -= 2;
                    if (board.IsInCheckmate()) { score = int.MinValue; board.UndoMove(responseMove); break; }
                    else board.UndoMove(responseMove);
                }

            }

            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        return bestMove;
    }


    // Calculates the maximal net loss of a piece exchange chain (or maximal net gain if the player is attacking) for all attacked pieces on a bitboard
    public List<int> rateAllExchanges(Board board, ulong targetLocationBitboard, bool playerIsAttacking)
    {

        List<int> minNetGains = new();

        while (targetLocationBitboard != 0)
        {
            int minNetGain = 0; // The attacker can back out at any time if it's bad for them

            int index = BitboardHelper.ClearAndGetIndexOfLSB(ref targetLocationBitboard);
            // The piece about to be attacked, by value
            int attackedPieceValue = buildAttackChain(board, new List<int>() { index })[0];

            // The pieces protecting and attacking it, by value
            List<int> playerChain = buildAttackChain(board, playerAttackInfo[index]);
            List<int> opponentChain = buildAttackChain(board, opponentAttackInfo[index]);

            // Swap the lists and get the negative value (highest net gain) if the player is attacking
            if (playerIsAttacking)
                (opponentChain, playerChain) = (playerChain, opponentChain);


            // The attacked piece gets captured first, the others will go by lowest value first
            playerChain.Insert(0, attackedPieceValue);

            int netGain = 0;

            while (playerChain.Count != 0 && opponentChain.Count != 0)
            {
                netGain -= playerChain[0]; // Get captured - If it's a free piece, there won't be a response capture
                playerChain.RemoveAt(0);
                if (playerChain.Count != 0)
                {
                    netGain += opponentChain[0]; // Capture back
                    opponentChain.RemoveAt(0);
                }
                if (netGain < minNetGain) minNetGain = netGain;
            }
            if (playerIsAttacking) minNetGain = -minNetGain;
            minNetGains.Add(minNetGain);

        }

        return minNetGains;

    }

    // Generates a sorted list of all the piece values attacking a square
    public List<int> buildAttackChain(Board board, List<int> squareAttackInfo)
    {
        List<int> attackChain = new();
        foreach (int square in squareAttackInfo)
        {
            Piece piece = board.GetPiece(new Square(square));
            int value;
            if (piece.IsPawn) value = 1;
            else if (piece.IsBishop || piece.IsKnight) value = 3;
            else if (piece.IsRook) value = 5;
            else if (piece.IsQueen) value = 9;
            else value = 9000;
            attackChain.Add(value);
        }
        attackChain.Sort();
        return attackChain;
    }


    // Calculates the material points of either black or white
    public int EvaluateMaterialPoints(Board board, bool isWhite)
    {
        return board.GetPieceList(PieceType.Pawn, isWhite).Count * 1
              + board.GetPieceList(PieceType.Bishop, isWhite).Count * 3
              + board.GetPieceList(PieceType.Knight, isWhite).Count * 3
              + board.GetPieceList(PieceType.Rook, isWhite).Count * 5
              + board.GetPieceList(PieceType.Queen, isWhite).Count * 9;

    }

    public ulong GetPieceAttacks(Piece piece, Board board)
    {

        if (piece.IsPawn)
            return BitboardHelper.GetPawnAttacks(piece.Square, piece.IsWhite);

        else if (piece.IsKnight)
            return BitboardHelper.GetKnightAttacks(piece.Square);


        else if (piece.IsKing)
            return BitboardHelper.GetKingAttacks(piece.Square);

        else
            return BitboardHelper.GetSliderAttacks(piece.PieceType, piece.Square, board);

    }

    public void InitBitboardsAndNets(Board board)
    {

        totalPlayerAttackBitboard = 0;
        totalOpponentAttackBitboard = 0;


        for (int i = 0; i < 64; i++)
        {
            playerAttackInfo[i].Clear();
            opponentAttackInfo[i].Clear();
        }

        ulong ownPieceBitboard = board.WhitePiecesBitboard;
        ulong opponentPieceBitboard = board.BlackPiecesBitboard;

        if (!isBotWhite)
            (ownPieceBitboard, opponentPieceBitboard) = (opponentPieceBitboard, ownPieceBitboard);


        // Finds all the pieces that are attacked or protected, and adds the attacker's square to the info list
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {

            foreach (Piece piece in pieceList)
            {
                ulong targetBitboard = GetPieceAttacks(piece, board);

                if (piece.IsWhite == isBotWhite) totalPlayerAttackBitboard |= targetBitboard & opponentPieceBitboard;


                else totalOpponentAttackBitboard |= targetBitboard & ownPieceBitboard;

                while (targetBitboard != 0)
                {
                    int index = BitboardHelper.ClearAndGetIndexOfLSB(ref targetBitboard);

                    if (piece.IsWhite == isBotWhite) playerAttackInfo[index].Add(piece.Square.Index);
                    else opponentAttackInfo[index].Add(piece.Square.Index);
                }



            }

        }
    }
}
