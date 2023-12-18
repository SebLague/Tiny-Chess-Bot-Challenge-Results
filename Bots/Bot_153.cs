namespace auto_Bot_153;
using ChessChallenge.API;
using System.Collections.Generic;

// A chess bot that gives each possible move a score based on various factors
// and then picks the move with the highest score
// (or a random move from any move that has the highest score if there are move than one).

// The bot plays a decent opening, regularly takes trades and does a good job of
// moving out of immediate danger.

// Limitations:
// Due to the limited token count the bot does have some weaknesses.
// It can only look one move ahead so it can't use any stratergies
// that require more than one move. This makes it bad at going for checkmate
// as well as defending against checkmate (it can still see checkmate in one move).
// The bot will sometimes prioritise moving a piece out of danger over taking
// the piece that is putting it in danger.
public class Bot_153 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    private int[] pieceValues = { 0, 1, 3, 3, 5, 9, 10 };

    private delegate bool IsMoveTypeDelegate(Board board);

    // Defining variables outside of function so I don't have to pass
    // them into functions (lowering token count)
    private Board myBoard;
    private Move myMove;
    private Square mySquare;
    private bool isWhiteToMove;

    // I switched to hard coding the score values in to lower the token count
    //int startingScore = 0;
    //int pushingCentrePawnsScore = 3; // only if they haven't moved yet
    //int developingPiecesScore = 3; // knights and bishops only
    //int castlingScore = 4;
    //int advancingPiecesScore = 2; // knights and bishops only
    //int bestCaptureScore = 5;
    //int capturingAttackerScore = 2; // multiplied by attacked piece value
    //int promotingToQueenScore = 4;
    //int movePieceInDangerScore = 2; // multiplied by piece value
    //int checkingScore = 3;
    //int checkMatingScore = 1000;
    //int drawingScore = -4;
    //int loseScore = -1000;
    //int unsafeMoveForThisPieceScore = -3; // multiplied by piece value
    //int unsafeMoveForAnotherPieceScore = -3; // multiplied by piece put in danger value

    public Move Think(Board board, Timer timer)
    {
        myBoard = board;
        var moves = myBoard.GetLegalMoves();
        List<int> moveScores = new(),
            bestCapturesMoveIndexes = new(),
            posOfPiecesInDanger = new();

        isWhiteToMove = myBoard.IsWhiteToMove;
        ulong myPieceBitBoard = myBoard.WhitePiecesBitboard;
        if (!isWhiteToMove)
            myPieceBitBoard = myBoard.BlackPiecesBitboard;

        int bestCapturesValue = -20;

        // Determine which pieces (if any) are in danger
        for (int i = 0; i < 64; i++)
        {
            mySquare = new(i);
            if (BitboardHelper.SquareIsSet(myPieceBitBoard, mySquare)
                && !PieceIsSafe(myBoard.GetPiece(mySquare).PieceType))
                posOfPiecesInDanger.Add(i);
        }

        // Determine the score of each move
        for (int i = 0; i < moves.Length; i++)
        {
            myMove = moves[i];
            Square myStartSquare = myMove.StartSquare,
                myTargetSquare = myMove.TargetSquare;
            PieceType myMovePieceType = myMove.MovePieceType;
            int myMovePieceValue = pieceValues[(int)myMovePieceType],
                myMoveScore = 0, // startingScore
                myStartSquareFile = myStartSquare.File,
                myStartSquareRank = myStartSquare.Rank;

            // Pushing centre pawns if they haven't moved yet
            if (myMovePieceType == PieceType.Pawn
                && ((myStartSquareRank == 1 && isWhiteToMove) || (myStartSquareRank == 6 && !isWhiteToMove))
                && (myStartSquareFile == 3 || myStartSquareFile == 4))
                myMoveScore += 3; // pushingCentrePawnsScore
            if (myMovePieceType == PieceType.Knight || myMovePieceType == PieceType.Bishop)
            {
                // Developing pieces
                if ((myStartSquareRank == 0 && isWhiteToMove)
                    || (myStartSquareRank == 7 && !isWhiteToMove))
                    myMoveScore += 3; // developingPiecesScore
                // Advancing pieces
                else if ((myTargetSquare.Rank > myStartSquareRank && isWhiteToMove)
                    || (myTargetSquare.Rank < myStartSquareRank && !isWhiteToMove))
                    myMoveScore += 2; // advancingPiecesScore
            }
            // Castling
            if (myMove.IsCastles)
                myMoveScore += 4; // castlingScore
            // Capturing pieces that gives the highest value lead
            // (capture most valuable piece with least valuable attacker)
            if (myMove.IsCapture)
            {
                int capturePieceValue = pieceValues[(int)myBoard.GetPiece(myTargetSquare).PieceType],
                    captureValue = capturePieceValue - myMovePieceValue;
                mySquare = myTargetSquare;
                // If capture piece is undefended
                if (CalculateSquareAttackerValues(!isWhiteToMove).Count == 0)
                    captureValue = capturePieceValue;
                if (captureValue == bestCapturesValue)
                    bestCapturesMoveIndexes.Add(i);
                if (captureValue > bestCapturesValue)
                {
                    bestCapturesMoveIndexes.Clear();
                    bestCapturesMoveIndexes.Add(i);
                    bestCapturesValue = captureValue;
                }

                // Removing danger by taking a piece
                for (int j = 0; j < posOfPiecesInDanger.Count; j++)
                {
                    mySquare = new(posOfPiecesInDanger[j]);
                    myBoard.MakeMove(myMove);
                    if (PieceIsSafe(myMovePieceType) && !(mySquare == myStartSquare))
                        myMoveScore += 2 * myMovePieceValue; // capturingAttackerScore
                    myBoard.UndoMove(myMove);
                }
            }
            // Promoting to queen
            if (myMove.IsPromotion && myMove.PromotionPieceType == PieceType.Queen)
                myMoveScore += 4; // promotingToQueenScore
            // Check
            if (MoveIsOfType(myBoard => myBoard.IsInCheck()))
                myMoveScore += 3; // checkingScore
            // Checkmate
            if (MoveIsOfType(myBoard => myBoard.IsInCheckmate()))
                myMoveScore += 1000; // checkMatingScore
            // Draw
            if (MoveIsOfType(myBoard => myBoard.IsDraw()))
                myMoveScore += -4; // drawingScore
            // Moving a piece that is in danger
            foreach (int pos in posOfPiecesInDanger)
            {
                if (myStartSquare.Index == pos)
                    myMoveScore += 2 * myMovePieceValue; // movePieceInDangerScore
            }

            // Reduce score of move if it leads to checkmate in one move
            myBoard.MakeMove(myMove);
            var opponentMoves = myBoard.GetLegalMoves();
            // It would be better to pass the move into the variable
            // instead of using temp but that requires more tokens
            Move temp = myMove;
            for (int j = 0; j < opponentMoves.Length; j++)
            {
                myMove = opponentMoves[j];
                if (MoveIsOfType(myBoard => myBoard.IsInCheckmate()))
                    myMoveScore += -1000; // LoseScore
            }
            myMove = temp;
            // Reduce score of move if it is unsafe
            // Get new bitboard since a piece has moved
            myPieceBitBoard = myBoard.WhitePiecesBitboard;
            if (!isWhiteToMove)
                myPieceBitBoard = myBoard.BlackPiecesBitboard;
            for (int j = 0; j < 64; j++)
            {
                // Determine if j is the position of a piece in danger
                // This is used to check if the move puts a piece in
                // danger that wasn't before
                bool isInPosOfPieceInDanger = false;
                foreach (int pos in posOfPiecesInDanger)
                {
                    if (pos == j)
                        isInPosOfPieceInDanger = true;
                }
                mySquare = new(j);
                PieceType currentPieceType = myBoard.GetPiece(mySquare).PieceType;
                if (BitboardHelper.SquareIsSet(myPieceBitBoard, mySquare)
                    && !PieceIsSafe(currentPieceType))
                {
                    // If move puts moving piece in danger
                    // and not (capturing a piece and the value of the captured piece is equal
                    // or greater than of the caputuring piece), it is unsafe
                    if (mySquare == myTargetSquare)
                    {
                        if (!(myMove.IsCapture && pieceValues[(int)myMove.CapturePieceType] >= myMovePieceValue))
                            myMoveScore += -3 * myMovePieceValue; // unsafeMoveForThisPieceScore
                    }
                    // If move puts another piece in danger
                    else if (!isInPosOfPieceInDanger)
                    {
                        myMoveScore += -3 * (int)currentPieceType; // unsafeMoveForAnotherPieceScore
                    }
                }
            }
            myBoard.UndoMove(myMove);

            moveScores.Add(myMoveScore);
        }

        // Add score for best captures
        if (bestCapturesValue >= 0)
        {
            foreach (int index in bestCapturesMoveIndexes)
            {
                moveScores[index] += 5; // bestCaptureScore
            }
        }

        // Find all moves with the highest score and put them in an array
        int bestMovesScore = moveScores[0];
        foreach (int moveScore in moveScores)
        {
            if (moveScore > bestMovesScore)
                bestMovesScore = moveScore;
        }
        List<Move> bestMoves = new();
        for (int i = 0; i < moveScores.Count; i++)
        {
            if (moveScores[i] == bestMovesScore)
                bestMoves.Add(moves[i]);
        }

        // Select random move out of best moves
        System.Random rng = new();
        return bestMoves[rng.Next(bestMoves.Count)];
    }

    // Test if move is of type (e.g. isDraw)
    private bool MoveIsOfType(IsMoveTypeDelegate isMoveType)
    {
        myBoard.MakeMove(myMove);
        bool isType = isMoveType(myBoard);
        myBoard.UndoMove(myMove);
        return isType;
    }

    // Checks if a piece is currently on a safe square
    private bool PieceIsSafe(PieceType pieceType)
    {
        // Get all attackers and all defenders of the square
        List<int> attackerValues = CalculateSquareAttackerValues(!isWhiteToMove),
            defenderValues = CalculateSquareAttackerValues(isWhiteToMove);

        // If there is an attacker weaker that the piece,
        // the piece isn't safe
        if (attackerValues.Count > 0 && attackerValues[0] < pieceValues[(int)pieceType])
            return false;

        // Go through both lists to find first attacker with
        // a different value to the corresponding defender
        for (int i = 0; i < attackerValues.Count; i++)
        {
            // If we run out of defenders but still have at least
            // one more attacker, the piece isn't safe
            if (i > defenderValues.Count - 1)
                return false;
            // If the attacker is weaker, piece isn't safe
            if (attackerValues[i] < defenderValues[i])
                return false;
            // If defender is weaker, piece is safe
            if (attackerValues[i] > defenderValues[i])
                return true;
        }

        // If we run out of attackers to check, the piece is safe
        return true;
    }

    // Calculate the values of pieces attacking a certain square
    // and return in a sorted list
    private List<int> CalculateSquareAttackerValues(bool isWhite)
    {
        List<int> attackers = new();

        ulong[] attackingPieces =
        {
            0,
            BitboardHelper.GetPawnAttacks(mySquare, !isWhite),
            BitboardHelper.GetKnightAttacks(mySquare),
            BitboardHelper.GetSliderAttacks(PieceType.Bishop, mySquare, myBoard),
            BitboardHelper.GetSliderAttacks(PieceType.Rook, mySquare, myBoard),
            BitboardHelper.GetSliderAttacks(PieceType.Queen, mySquare, myBoard),
            BitboardHelper.GetKingAttacks(mySquare)
        };

        for (int i = 0; i < 64; i++)
        {
            Square currentSquare = new(i);
            if (myBoard.GetPiece(currentSquare).IsWhite == isWhite)
            {
                for (int j = 1; j < attackingPieces.Length; j++)
                {
                    if (BitboardHelper.SquareIsSet(attackingPieces[j], currentSquare)
                        && myBoard.GetPiece(currentSquare).PieceType == (PieceType)j)
                        attackers.Add(pieceValues[j]);
                }
            }
        }

        attackers.Sort();

        return attackers;
    }
}