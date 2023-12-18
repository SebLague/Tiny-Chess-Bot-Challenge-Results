namespace auto_Bot_457;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_457 : IChessBot
{
    readonly int[] pieceWeight = { 0, 1, 3, 3, 5, 9, 10 };

    public Move Think(Board board, Timer timer)
    {
        Move[] legalMoves = board.GetLegalMoves();
        int numberOfLegalMoves = legalMoves.Length;
        int tryNum = 0;
        int overallRating = 0; //Variable updated with the best current move
        //Default move is random
        Random rng = new();
        Move moveToPlay = legalMoves[rng.Next(0, legalMoves.Length)];

        //Tells on move, which player has which pieces & associates the map of the pieces
        ulong botPiecesBitBoard = board.BlackPiecesBitboard;
        ulong ennemyPiecesBitBoard = board.WhitePiecesBitboard;
        if (board.IsWhiteToMove)
        {
            botPiecesBitBoard = board.WhitePiecesBitboard;
            ennemyPiecesBitBoard = board.BlackPiecesBitboard;
        }

        //Test every move
        foreach (Move move in legalMoves)
        {
            int moveRating = 0;
            bool oppHasMate = false;

            //Look for checkmate
            if (MoveIsCheckmate(board, move))
            {
                moveRating = 1000000000;
                moveToPlay = move;
                overallRating = moveRating;
                break;
            }

            // Check if opponent has checkmate
            board.MakeMove(move);
            Move[] availableEnnemyMoves = board.GetLegalMoves();
            foreach (Move ennemyMove in availableEnnemyMoves)
            {
                if (MoveIsCheckmate(board, ennemyMove))
                {
                    moveRating = -1; //Negative value only reachable with opponent checkmate
                    oppHasMate = true;
                    break;
                }
            }
            board.UndoMove(move);

            //Check if piece is hanging check for each piece if attacked by inferior value piece or undefended

            //Special moves and captures
            if (!oppHasMate)
            {
                //Special moves
                if (move.IsEnPassant)
                { //En passant is cool
                    moveRating += 10;
                }
                else if (move.IsPromotion)
                {
                    moveRating += pieceWeight[(int)move.PromotionPieceType] * 100;
                }
                else if (move.IsCastles)
                {
                    moveRating += 50;
                }
                else if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
                {

                    board.MakeMove(move);
                    if (board.IsInCheck())
                    {
                        moveRating += 99; //Inferior as pawn capture (see next)
                    }
                    board.UndoMove(move);
                }


                //Attack benefits: squares and pieces under attack
                int[] totalBoardDefense = new int[64];
                board.MakeMove(move);
                GetSquareDefenders(board, move.TargetSquare, totalBoardDefense, ennemyPiecesBitBoard);
                board.UndoMove(move);
                //Increase rating with number of attacked squares; and their corresponding pieces (1 is empty square)
                for (int i = 0; i < 7; i++)
                {
                    moveRating += 1 + totalBoardDefense.Count(f => f == pieceWeight[i]);
                }


                //Analyze attacker and target
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int capturedPieceValue = pieceWeight[(int)capturedPiece.PieceType] * 100;
                Piece capturingPiece = board.GetPiece(move.StartSquare);
                int capturingPieceValue = pieceWeight[(int)capturingPiece.PieceType] * 100;

                //Defense and attack ratios on the specific square
                //Creates array of the square's attackers (ennemy's square defenders) and their type
                int[] squareAttacker = new int[64];
                GetSquareDefenders(board, move.TargetSquare, squareAttacker, ennemyPiecesBitBoard);
                //Creates array of the square's defenders and their type
                board.MakeMove(move);
                botPiecesBitBoard = board.WhitePiecesBitboard;
                if (board.IsWhiteToMove)
                {
                    botPiecesBitBoard = board.BlackPiecesBitboard;
                }
                int[] squareDefender = new int[64];
                GetSquareDefenders(board, move.TargetSquare, squareDefender, botPiecesBitBoard);
                board.UndoMove(move);
                //Sets pieces in order of importance is array<
                Array.Sort(squareAttacker);
                Array.Sort(squareDefender);

                //Compares each attacker/defender, if bot defense position is winning, rating increases
                int defenseBenefits = 0, leastEnnemyAttacker = 0;
                for (int i = 1; i < 6; i++)
                {
                    //Using pieceWeight values, from 1(pawn), to 5(queen)
                    //Counts pieces from each side, compares
                    int nbOfSquareDefendersOfTypei = squareDefender.Count(f => f == i);
                    int nbOfSquareAttackersOfTypei = squareAttacker.Count(f => f == i);
                    //Rating increases and move is played if position is beneficial
                    if (nbOfSquareDefendersOfTypei > nbOfSquareAttackersOfTypei)
                    {
                        //The weaker the defenders, the better the rating
                        defenseBenefits += (nbOfSquareDefendersOfTypei - nbOfSquareAttackersOfTypei) * (11 - pieceWeight[i]);//Defended by: pawn=10; knight/bishop=8; rook=6; queen=2; king=1
                        //States the least strong attacker
                        if (nbOfSquareAttackersOfTypei != 0 && i > leastEnnemyAttacker)
                        {
                            leastEnnemyAttacker = i;
                        }

                    }//Breaks if ennemy defense is stronger
                    else if (nbOfSquareDefendersOfTypei < nbOfSquareAttackersOfTypei)
                    {
                        break;
                    }//Continues if defense by piece is equal
                }
                //Works for all cases; if move is not a capture: points are defense points only
                //If the move is an equal or favorable capture for bot BUT bot has losing or equal defense
                //Free capture case
                if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
                {
                    moveRating += capturedPieceValue + defenseBenefits;
                }
                //Exchange capture case with attacker < attacked
                else if (capturingPieceValue < capturedPieceValue)
                {
                    moveRating += capturedPieceValue - capturingPieceValue + defenseBenefits;
                }
                //Attacker and attacked are equal; defense situation is benficial & attacker will not be eaten by inferior piece
                else if (capturingPieceValue == capturedPieceValue && defenseBenefits != 0 && capturingPieceValue < leastEnnemyAttacker)
                {
                    moveRating += defenseBenefits;
                }
            }
            //Choose best move
            if (overallRating < moveRating)
            {
                overallRating = moveRating;
                moveToPlay = move;
            }
            tryNum++;
        }
        return moveToPlay;
    }

    public int[] GetSquareDefenders(Board board, Square square, int[] squareDefender, ulong pieceBitBoard)
    {
        int squareIdx = square.Index;
        //Using bot's or ennemy's bitboard for finder: prevent crossing uses btw ClearAndGetIndexOfLSB & GetPieceAttacks
        ulong finderBitboard = pieceBitBoard;
        int numberOfPlayerPieces = BitboardHelper.GetNumberOfSetBits(finderBitboard);//Using bot's bitboard
        //Checks each player's piece
        for (int i = 0; i < numberOfPlayerPieces; i++)
        {
            int positionOfPlayerPiece = BitboardHelper.ClearAndGetIndexOfLSB(ref finderBitboard);
            //Test player's defense from square: determine square and the piece on it
            Square squareFromDefense = new(positionOfPlayerPiece);
            PieceType defendingPiece = board.GetPiece(squareFromDefense).PieceType;
            ulong defender = BitboardHelper.GetPieceAttacks(defendingPiece, squareFromDefense, pieceBitBoard, !board.IsWhiteToMove);//Move has been done => turn changes : ennemy move; if ennemy is white, out is false; if ennemy is not white out is true
            //Checks how many positions the piece defends
            int numberOfBotDefenses = BitboardHelper.GetNumberOfSetBits(defender);
            //Checks the positions it defends
            for (int j = 0; j < numberOfBotDefenses; j++)
            {
                int defendedSquare = BitboardHelper.ClearAndGetIndexOfLSB(ref defender);
                //Assigns correct piece value to defending position on array only if the target square is attacked
                if (defendedSquare == squareIdx)
                {
                    squareDefender[positionOfPlayerPiece] = (int)defendingPiece;
                }
            }
        }
        //For whole board attack checker
        for (int j = 0; j < 64; j++)
        {
            Square test = new(j);
            if (board.SquareIsAttackedByOpponent(test))
            {
                squareDefender[j] = 1 + pieceWeight[(int)board.GetPiece(test).PieceType];
            }
        }
        return squareDefender;
    }

    public bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

}