namespace auto_Bot_574;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// Authors: Tyler Branscombe & Austin Daniel
// This bot is optimized to maximize the number of en passants in a game
// To accomplish this it prioritizes a few different actions:
// It aims to push any pawns which are not on their starting squares to a "target" position: the 5th rank (or 4th if playing as black), this allows it to en passant the opponent if they move an adjacent pawn forward 2
// It keeps any pawns on their starting square on that square until the opponent reaches the 4th rank (5th for black) in which case if moves forward, setting up an en passant for the opponent
// It will place high value pieces behind any pawns in their "target position" to try and entice the opponents pawns on the adjacent files to  move forward 2 spaces to attack it, setting up for an en passant
//
// In addition to prioritizing en passants, this bot also plays an animation in the console to celebrate both en passant-ing the opponent, and getting en passant-ed by the opponent
// It then parses the variable name and logs each frame to the console (***this animation was removed due to the update rules which ban nameof()***)
//
// NOTE: to see the animation use this code: https://pastebin.com/Rk4qQxQB
public class Bot_574 : IChessBot
{

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    // This value is used to store the square of a pawn which has just been placed in a spot where it could be take via en passant
    Square square = new Square();
    // En passant counters for self and opponent
    int enPassantedCount = 0;
    int enPassantCount = 0;


    public Move Think(Board board, Timer timer)
    {

        Move[] allMoves = board.GetLegalMoves();
        Random rng = new();
        // DivertedConsole.Write(square.Name);

        // Identify if the opponent just played an en passant, if so play animation
        // If the square name is not a1, indicating that the square class variable has been updated
        if (square.Name != "a1")
        {
            // Set up a ulong where all positions are 1 except the position of the square holding the pawn which can be en passanted
            int squareIndex = (7 - square.Rank) * 8 + (7 - square.File);
            ulong bitboardRepresentation = 0b1000000000000000000000000000000000000000000000000000000000000000;
            bitboardRepresentation = bitboardRepresentation >> squareIndex;
            bitboardRepresentation = ~bitboardRepresentation;
            // Take the the NOT of an OR between the current board and the map just made:
            // The will return all 0s unless the space where the en passant-able pawn was is now empty, which can only happen if that pawn was enpassanted
            bitboardRepresentation = ~(board.AllPiecesBitboard | bitboardRepresentation);
            // If it does not contain all 0s then an en passant happened by the opponent, so play the appropriate animation
            if (bitboardRepresentation > 0)
            {
                enPassantedCount++;
                AnimateFrames(enPassantCount, enPassantedCount);

            }
        }
        // Reset the calss variable
        square = new Square();

        // Make a list of all available non pawn moves
        List<Move> nonPawnMovesArrayList = new List<Move>();
        foreach (Move m in allMoves)
        {
            if (m.MovePieceType == PieceType.Pawn || board.GetPiece(m.TargetSquare).IsPawn)
            {
                continue;
            }
            else
            {
                nonPawnMovesArrayList.Add(m);
            }
        }
        Move[] nonPawnMoves = nonPawnMovesArrayList.ToArray();
        // If there are only pawn moves just use all moves
        if (nonPawnMoves.Length == 0)
        {
            nonPawnMoves = allMoves;
        }

        // Define/instantiate Move variables
        Move moveToPlay = nonPawnMoves[rng.Next(nonPawnMoves.Length)];
        Move move2Play = new Move();
        Move nextEnPassantMove = new Move();
        Move checkmateMove = new Move();
        Move pushPawnToTarget = new Move();
        Move putHighValuePieceBehindInPositionPawn = new Move();

        // Instantiate highest value piece variables
        int highestValueCapture = 0;
        int highestValue2 = 0;

        // Logic for checking pawn locations dependent on piece color
        int pieceListIndex;
        int targetRank;
        int startingRank;
        if (board.IsWhiteToMove)
        {
            pieceListIndex = 0;
            targetRank = 4;
            startingRank = 1;
        }
        else
        {
            pieceListIndex = 6;
            targetRank = 3;
            startingRank = 6;
        }


        // Create boolean arrays which store whether pawns are on their starting square, their target square, or neither
        bool[] pawnsOnStartingSquare = new bool[8];
        bool[] pawnsInPosition = new bool[8];
        foreach (Piece Piece in board.GetAllPieceLists()[pieceListIndex])
        {
            // Populate pawn on starting square array
            if (Piece.Square.Rank == startingRank)
            {
                pawnsOnStartingSquare[Piece.Square.File] = true;
            }
            // Populate pawns in position array
            if (Piece.Square.Rank == targetRank)
            {
                pawnsInPosition[Piece.Square.File] = true;
            }
        }
        // Create int arrays identifying the indexes of pawns which we want to move or which are in the target position
        bool[] pawnsToMove = pawnsOnStartingSquare.Zip(pawnsInPosition, (a, b) => a && b).ToArray();
        int[] pawnIndexesToMove = pawnsToMove.Select((value, index) => new { value, index })
                                                .Where(item => item.value)
                                                .Select(item => item.index)
                                                .ToArray();
        int[] pawnIndexesInPosition = pawnsInPosition.Select((value, index) => new { value, index })
                                                .Where(item => item.value)
                                                .Select(item => item.index)
                                                .ToArray();
        // If all pawns are on the starting squares, set the B and G pawns as pawns to push
        int[] numbers = { 1, 6 };
        if (pawnIndexesToMove.Length == 0)
        {
            pawnIndexesToMove = numbers;
        }


        // If no move is assigned in code below, but a capture is possible, assign the highest possible capture to moveToPlay
        foreach (Move move in nonPawnMoves)
        {
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }


        // For each possible move check for these scenarios, and store the move if it is in that scenario:
        // 1. Is this move an en passant?
        // 2. Does this move set the opponent up for an en passant?
        // 3. Is this move a checkmate?
        // 4. Is this move a pawn push that we ant to do?
        // 5. Does this move put the highest value piece behind an in position pawn?
        foreach (Move move in allMoves)
        {
            // If en passant is possible, store it
            if (move.IsEnPassant)
            {
                enPassantCount++;
                AnimateFrames(enPassantCount, enPassantedCount);
                return move;
            }
            // If we can set up an en passant for our opponent, store it
            if (nextEnPassant(board, move))
            {
                nextEnPassantMove = move;
            }
            // If no en passants are possible for us or our opponent on the next turn, and this move is check mate, store it
            if (MoveIsCheckmate(board, move))
            {
                checkmateMove = move;
            }

            // This section pushes all pawns which are not on their starting square and not at their target rank yet, to their target rank
            // If the pawn on the file of this move is not on it's starting square and not to the target rank
            if (pawnIndexesToMove.Contains(move.TargetSquare.File))
            {
                // If this move is a pawn move
                if (move.MovePieceType == PieceType.Pawn)
                {
                    // If this move pushes the pawn to the 4th or 5th rank, store it
                    if (move.TargetSquare.Rank <= 4 && move.TargetSquare.Rank >= 3)
                    {
                        pushPawnToTarget = move;
                    }
                }
            }

            // This section pushes the most powerful piece that can move behind any pawns in position to that position
            // If the pawn on the file of this move is on its target rank
            if (pawnIndexesInPosition.Contains(move.TargetSquare.File))
            {
                // If the target rank of this move is behind the in position pawn
                if ((move.TargetSquare.Rank == 3 && board.IsWhiteToMove) || (move.TargetSquare.Rank == 4 && !board.IsWhiteToMove))
                {
                    Piece piece = board.GetPiece(move.StartSquare);
                    int value = pieceValues[(int)piece.PieceType];
                    // If this is the highest rank piece which can move to behind an in position pawn, store it
                    if (highestValue2 < value)
                    {
                        putHighValuePieceBehindInPositionPawn = move;
                        highestValue2 = value;
                    }
                }
            }
        }// End for each

        // Now that we have analyzed all possible moves, check the move variables one at a time in priority order and return it if populated

        if (!nextEnPassantMove.IsNull)
        { // If we had no en passants but we could set one up for the opponent, do that
          // Set the square variable to flag the next call of this function to check if the most recent move by the opponent was an en passant
            square = nextEnPassantMove.TargetSquare;
            return nextEnPassantMove;

        }
        else if (!checkmateMove.IsNull)
        { // If we had no en passants and couldn't set up any for the opponent, and we have a checkmate, do it
            return checkmateMove;
        }
        else if (!pushPawnToTarget.IsNull)
        { // If we could do none of the above, but can push a pawn towards it's target square, do that
            return pushPawnToTarget;
        }
        else if (!putHighValuePieceBehindInPositionPawn.IsNull)
        { // If we could do none of the above but can place a piece behind a pawn in its target position, do that
            return putHighValuePieceBehindInPositionPawn;
        }
        // If none of these scenarios happen, play moveTo Play which is either a random move, or the highest value capture available
        return moveToPlay;

    }// End Think method



    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    // Identify if a move sets up the opponent for an en passant
    bool nextEnPassant(Board board, Move move)
    {
        board.MakeMove(move);
        foreach (Move m in board.GetLegalMoves())
        {
            if (m.IsEnPassant)
            {
                board.UndoMove(move);
                return true;
            }
        }
        board.UndoMove(move);
        return false;
    }



    // Diplays image on pawn and en passant counts any time an en passant occurs
    static void AnimateFrames(int enPassantCount, int enPassantedCount)
    {
        DivertedConsole.Write(@"
   __   
  /  \  
  \__/    -EN PASSANT!
 /____\ 
  |  |  
  |__|  
 (====) 
 }===={ 
(______)");
        DivertedConsole.Write("En Passant Count:" + enPassantCount + "\nEn Passant-ed Count:" + enPassantedCount + "\nThanks Sebastion! <3");
    }



}