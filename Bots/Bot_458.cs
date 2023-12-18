namespace auto_Bot_458;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_458 : IChessBot
{
    readonly int[] pieceWeight = { 0, 1, 3, 3, 5, 9, 10 };
    public Move Think(Board board, Timer timer)
    {

        //Stating the bot's color
        bool botColor;
        if (board.PlyCount % 2 == 0) //White plays after even moves
        {
            botColor = true;
        }
        else
        {
            botColor = false;
        }

        //Creating array of legal moves to choose from
        Move[] legalMoves = board.GetLegalMoves();

        //Variable replaced after each position test with the best current position
        int overallRating = 0;

        //Default move is random
        Random rng = new();
        Move moveToPlay = legalMoves[rng.Next(0, legalMoves.Length)];

        //Testing every legal move
        foreach (Move move in legalMoves)
        {
            int moveRating = 0;
            bool oppHasMate = false;

            //Maps of both player's pieces
            ulong botPiecesBitBoardAfterMove;
            ulong ennemyPiecesBitBoardAfterMove;
            ulong allPiecesBitBoardAfterMove;

            board.MakeMove(move);
            if (botColor)
            {
                botPiecesBitBoardAfterMove = board.WhitePiecesBitboard;
                ennemyPiecesBitBoardAfterMove = board.BlackPiecesBitboard;
            }
            else
            {
                botPiecesBitBoardAfterMove = board.BlackPiecesBitboard;
                ennemyPiecesBitBoardAfterMove = board.WhitePiecesBitboard;
            }
            allPiecesBitBoardAfterMove = board.AllPiecesBitboard;
            board.UndoMove(move);

            //Creating a BitBoard Array { {botPiecesBitBoard}, {ennemyPiecesBitBoard}, {allPiecesBitBoard}}
            ulong[] allBitBoards = { botPiecesBitBoardAfterMove, ennemyPiecesBitBoardAfterMove, allPiecesBitBoardAfterMove };

            //I. a. Bot has checkmate
            if (MoveIsCheckmate(board, move))
            {
                moveRating = 1000000000;
                moveToPlay = move;
                overallRating = moveRating;
                break;
            }
            //I. b. Bot has check
            board.MakeMove(move);
            if (board.IsInCheck())
            {
                moveRating += 40;
            }

            //II. a. Opponent has checkmate on next move
            Move[] availableEnnemyMoves = board.GetLegalMoves();
            foreach (Move ennemyMove in availableEnnemyMoves)
            {
                if (MoveIsCheckmate(board, ennemyMove))
                {
                    moveRating = -1; //Negative value only reachable with opponent checkmate
                    oppHasMate = true;
                    continue;
                }
            }
            board.UndoMove(move);

            //III. Preventing draw/stalemate... if bot is winning
            if (!oppHasMate)
            {
                //IV. Bot's attack
                //IV. a. Hanging bot's pieces?
                board.MakeMove(move);
                bool hangingPiece = BotHasHangingPiece(board, move, allBitBoards);
                if (hangingPiece)
                {
                    moveRating = 0;
                    board.UndoMove(move);
                    continue;
                }
                else
                {
                    board.UndoMove(move);
                    //IV. b. Attack/defense benefits: squares and pieces under attack
                    //Defense rating of a move
                    board.MakeMove(move);
                    int[] defenseBenefits = SquareDefense(board, move.TargetSquare, allBitBoards);
                    moveRating += defenseBenefits[0] / 2;

                    //IV. c. Capture/ promotion benefits
                    //Analyzing attacker and target or promotion
                    board.UndoMove(move);
                    Piece capturedPiece = board.GetPiece(move.TargetSquare);
                    int capturedPieceValue = pieceWeight[(int)capturedPiece.PieceType] * 100;
                    Piece capturingPiece = board.GetPiece(move.StartSquare);
                    int capturingPieceValue = pieceWeight[(int)capturingPiece.PieceType] * 100;
                    board.MakeMove(move);
                    if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
                    {
                        if (move.IsPromotion)
                        {
                            moveRating += 1 + pieceWeight[(int)move.PromotionPieceType] * 100 - 100 + capturedPieceValue; //-100 : losing 1 pawn
                        }
                        else if (capturedPieceValue != 0)
                        {
                            moveRating += 1 + capturingPieceValue - capturedPieceValue;
                        }
                    }
                    else if (defenseBenefits[0] > 0 && capturedPieceValue != 0)
                    {
                        if (defenseBenefits[1] > (int)capturingPiece.PieceType || defenseBenefits[1] == 0)
                        {
                            moveRating += capturedPieceValue - capturingPieceValue;
                        }
                    }
                    board.UndoMove(move);
                    //V. Bot's position after move
                    int[] rankAndFilesRatings = { 0, 1, 2, 3, 3, 2, 1, 0 };
                    moveRating += rankAndFilesRatings[move.TargetSquare.File];
                    moveRating += rankAndFilesRatings[move.TargetSquare.Rank];
                    if (botColor)
                    {
                        moveRating += move.TargetSquare.Rank;
                    }
                    else
                    {
                        moveRating += 8 - move.TargetSquare.Rank;
                    }

                    //VI. Special moves
                    if (move.IsEnPassant) //En passant is cool
                    {
                        moveRating += 19;
                    }
                    else if (move.IsCastles)
                    {
                        moveRating += 35;
                    }

                    //VII. King move is bad
                    if ((int)move.MovePieceType == 6)
                    {
                        moveRating = 0;
                    }
                }
            }

            //Choosing best move 
            if (overallRating <= moveRating)
            {
                overallRating = moveRating;
                moveToPlay = move;
            }
        }
        return moveToPlay;
    }

    //Returns the attackers of a square, their position (on the array) and their type
    public int[] GetSquareAttackers(Board board, Square square, int[] squareAttackers, ulong[] allBitBoards, bool botIsAttacking) //Board; square is the observed square; squareAttackers is the array to write in the attackers; allPiecesBitboard gather players' and combined BitBoards, botToMove: bot is attacking;
    {
        int playerAllBitBoard;
        bool pawnDirection;
        if (botIsAttacking)
        {
            playerAllBitBoard = 0;
            pawnDirection = !board.IsWhiteToMove;
        }
        else
        {
            playerAllBitBoard = 1;
            pawnDirection = board.IsWhiteToMove;
        }

        int squareIdx = square.Index;

        //Using whoever's attacking BitBoard to go through their pieces
        ulong attackerBitBoard = allBitBoards[playerAllBitBoard];
        int numberOfAttackerPieces = BitboardHelper.GetNumberOfSetBits(attackerBitBoard);

        //Checks attacker's pieces
        for (int i = 0; i < numberOfAttackerPieces; i++)
        {
            int positionOfAttackerPiece = BitboardHelper.ClearAndGetIndexOfLSB(ref attackerBitBoard);
            //Test attacker's attack from square: determine the square and the piece on it
            Square squareFromAttack = new(positionOfAttackerPiece);
            PieceType attackingPiece = board.GetPiece(squareFromAttack).PieceType;
            ulong attackedSquares = BitboardHelper.GetPieceAttacks(attackingPiece, squareFromAttack, allBitBoards[2], pawnDirection);
            //Checks how many positions the piece attacks
            int numberOfAttackedSquares = BitboardHelper.GetNumberOfSetBits(attackedSquares);
            //Checks the positions it attacks
            for (int j = 0; j < numberOfAttackedSquares; j++)
            {
                int attackedSquare = BitboardHelper.ClearAndGetIndexOfLSB(ref attackedSquares);
                //Assigns correct PieceType (0-6) to defending position on array only if the target square is attacked
                if (attackedSquare == squareIdx)
                {
                    squareAttackers[positionOfAttackerPiece] = (int)attackingPiece;
                }
            }
        }
        return squareAttackers;
    }

    //Returning a rating of the defense of a position, positive or negative or null (win/loss/equal)
    public int[] SquareDefense(Board board, Square square, ulong[] allBitBoards)//Attacker: bot == true
    {
        //Creates array of the bot's attack on the square
        int[] botAttacks = new int[64];
        GetSquareAttackers(board, square, botAttacks, allBitBoards, true);

        //Creating an array of the ennemy's attack on the square
        int[] ennemyAttacks = new int[64];
        GetSquareAttackers(board, square, ennemyAttacks, allBitBoards, false);

        //Comparing both side's defense
        int[] defenseBenefits = { 0, 0 };//1st value is rating, 2nd is weakest opp
        for (int i = 1; i <= 6; i++)
        {
            //Using pieceWeight values, from 1(pawn), to 5(queen)
            //Counting pieces from each side, comparing
            int nbOfBotAttackersOfTypei = botAttacks.Count(f => f == i);
            int nbOfEnnemyAttackersOfTypei = ennemyAttacks.Count(f => f == i);

            //Noting down the weakest attacker
            if (nbOfEnnemyAttackersOfTypei != 0 && (defenseBenefits[1] == 0 || defenseBenefits[1] > i))
            {
                defenseBenefits[1] = i;
            }

            //The weaker the defenders, the better the rating
            defenseBenefits[0] += (nbOfBotAttackersOfTypei - nbOfEnnemyAttackersOfTypei) * (11 - pieceWeight[i]);//Defended by: pawn=10; knight/bishop=8; rook=6; queen=2; king=1
            if (defenseBenefits[0] < 0)
            {
                break;
            }
        }
        if (defenseBenefits[0] == 0) //Bot has just played and will win
        {
            defenseBenefits[0] += 1;
        }
        return defenseBenefits;
    }

    //Catching hanging pieces => losing defense or weaker opponent
    public bool BotHasHangingPiece(Board board, Move move, ulong[] allBitBoards)
    {
        ulong botPiecesBitBoard = allBitBoards[0];
        bool hangingPiece = false;
        //Testing every square with a bot's piece
        int numberOfBotPieces = BitboardHelper.GetNumberOfSetBits(botPiecesBitBoard);

        for (int i = 0; i < numberOfBotPieces; i++)
        {
            int positionOfBotPiece = BitboardHelper.ClearAndGetIndexOfLSB(ref botPiecesBitBoard);
            Square testedSquare = new(positionOfBotPiece);
            PieceType testedPiece = board.GetPiece(testedSquare).PieceType;

            board.UndoMove(move);
            if (board.SquareIsAttackedByOpponent(testedSquare))
            {
                board.MakeMove(move);
                int[] pieceDefense = SquareDefense(board, testedSquare, allBitBoards);

                if (((int)testedPiece > pieceDefense[1] && pieceDefense[1] != 0) || pieceDefense[0] < 0)
                {
                    hangingPiece = true;
                }
            }
            else
            {
                board.MakeMove(move);
            }
        }

        return hangingPiece;
    }

    public bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}