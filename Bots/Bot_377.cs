namespace auto_Bot_377;
//////////////////////
//Nathaniel's Chessbot
//
//This monstrosity hypothetically gives each board state a score based on how many pieces are within both teams theatened areas.
//There's definetly some kinks that I would love to come back and iron out in a future version.
//
///////////////////////
//Allowed Namespaces
using ChessChallenge.API;
using System;
//using System.Numerics;
using System.Collections.Generic;
//using System.Linq;

public class Bot_377 : IChessBot
{
    //Control Maps
    public ByteBoard controlMap = new ByteBoard();
    public ByteBoard whiteControlMap = new ByteBoard();
    public ByteBoard blackControlMap = new ByteBoard();

    Dictionary<string, float> positionValue = new Dictionary<string, float>();

    //Values given to various board conditions
    float[] pieceControlValues = { 0, 100, 300, 300, 500, 900, 40 };
    float[] emptyControlValues = { 1, 2, 3, 4, 4, 3, 2, 1 };
    float pawnRankMod = 20;

    //Game states that are referenced often
    float turnTime;
    Timer _timer;
    bool playerIsWhite;
    int maxSearchWidth;

    public Move Think(Board board, Timer timer)
    {
        playerIsWhite = board.IsWhiteToMove;
        _timer = timer;


        //Number of pieces remaining is used to determine how much time to spend searching. It needs to be searching more in the late game
        float percentOfPiecesRemaining = 0;
        foreach (PieceList piece in board.GetAllPieceLists())
        {
            percentOfPiecesRemaining += piece.Count;
        }
        percentOfPiecesRemaining /= 32;
        float percentOfPiecesCaptured = 1 - percentOfPiecesRemaining;


        turnTime = timer.MillisecondsRemaining / ((40 * percentOfPiecesRemaining) + 20);
        if (timer.MillisecondsRemaining < 2000) turnTime = 0;
        maxSearchWidth = 10 + (int)(10 * percentOfPiecesCaptured);


        return MoveSort(board, 3 + (int)(3 * percentOfPiecesCaptured), out float notUsed);
    }

    #region Search & Evaluate


    public Move MoveSort(Board board, int turnsAhead, out float score)
    {
        Dictionary<Move, float> moveValues = new Dictionary<Move, float>();
        Move[] moves = board.GetLegalMoves();
        moveValues.Add(Move.NullMove, board.IsWhiteToMove ? float.MinValue : float.MaxValue);//Just to compare against. Saves some brain capacity

        //Get Initial Value for each move
        foreach (Move move in moves)
        {

            #region Evaluate Move

            board.MakeMove(move);

            string fen = board.GetFenString();

            float moveScore = 0;
            if (board.IsInCheckmate()) { moveScore = board.IsWhiteToMove ? int.MinValue : int.MaxValue; }//Swapped because move will be undone.
            else if (board.IsDraw()) moveScore = 0;
            else if (move.IsPromotion && move.PromotionPieceType != PieceType.Queen) moveScore = board.IsWhiteToMove ? int.MaxValue : int.MinValue;
            else if (positionValue.ContainsKey(fen)) moveScore = positionValue[fen];
            else
            {
                #region Get Control Score
                #region Build Control Maps

                PieceList[] pieces = board.GetAllPieceLists();
                controlMap = new ByteBoard();
                whiteControlMap = new ByteBoard();
                blackControlMap = new ByteBoard();
                foreach (PieceList list in pieces)
                {
                    foreach (Piece piece in list)
                    {
                        if (list.IsWhitePieceList) whiteControlMap.AddBitBoard((BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite)));
                        else blackControlMap.AddBitBoard(BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite), -1);
                    }
                }

                //The player who's turn it is is given an extra point of control
                if (board.IsWhiteToMove) whiteControlMap.AddBitBoard(whiteControlMap.MapOfControlledSquares());
                else blackControlMap.AddBitBoard(blackControlMap.MapOfControlledSquares(), -1);

                controlMap = whiteControlMap + blackControlMap;

                #endregion

                ByteBoard playerToMoveControlMap = board.IsWhiteToMove ? whiteControlMap : blackControlMap;

                LoopBoard((int i, int j) =>
                {
                    Piece piece = board.GetPiece(new Square(i, j));
                    ByteBoard enemyControlMap = piece.IsWhite ? blackControlMap : whiteControlMap;
                    if (enemyControlMap.byteBoard[piece.Square.File, piece.Square.Rank] != 0) moveScore += pieceControlValues[(int)piece.PieceType] * controlMap.SquareSign(piece.Square);

                    moveScore += (emptyControlValues[i] + emptyControlValues[j]) * controlMap.SquareSign(piece.Square);

                    if (piece.IsPawn)
                    {
                        if (piece.IsWhite) moveScore += piece.Square.Rank * pawnRankMod;
                        else moveScore -= (9 - piece.Square.Rank) * pawnRankMod;
                    }
                });

                #region Get Material Score

                foreach (PieceList piece in board.GetAllPieceLists())
                {
                    moveScore += piece.Count * pieceControlValues[(int)piece.TypeOfPieceInList] * (piece.IsWhitePieceList ? 3 : -3);//An actual material gain is given 3 times as much value as controlling a piece
                }

                #endregion

                #endregion
            }


            board.UndoMove(move);
            positionValue[fen] = moveScore;
            moveValues.Add(move, moveScore);

            #endregion
        }

        List<Move> checkedMoves = new List<Move>
        {
            Move.NullMove
        };

        if (turnsAhead > 0)
        {
            #region Evaluate Future Turns

            for (int i = 0; i < maxSearchWidth; i++)
            {
                if (_timer.MillisecondsElapsedThisTurn >= turnTime && board.IsWhiteToMove != playerIsWhite) break; //Exit early if there's no time, but make sure all searches end on the same color so we can compare apples to apples

                Move moveToCheck = HighestValueUncheckedMove(ref moveValues, ref checkedMoves, board);
                board.MakeMove(moveToCheck);

                float newScore;
                MoveSort(board, turnsAhead - 1, out newScore);
                moveValues[moveToCheck] = newScore;
                positionValue[board.GetFenString()] = newScore;

                board.UndoMove(moveToCheck);
            }

            #endregion

            checkedMoves = new List<Move> { Move.NullMove }; //Refresh checked moves so I can use the same function to get the final result
        }

        Move result = HighestValueUncheckedMove(ref moveValues, ref checkedMoves, board);
        score = moveValues[result];
        return result;
    }


    #endregion

    #region ByteBoard

    public class ByteBoard
    {
        public int[,] byteBoard = new int[8, 8];
        public void AddBitBoard(ulong bitBoard, int value = 1)
        {
            LoopBoard((int i, int j) =>
            {
                byteBoard[i, j] += BitboardHelper.SquareIsSet(bitBoard, new Square(i, j)) ? value : 0;
            });
        }
        public static ByteBoard operator +(ByteBoard left, ByteBoard right)
        {
            ByteBoard result = new ByteBoard();
            LoopBoard((int i, int j) =>
            {
                result.byteBoard[i, j] = (left.byteBoard[i, j] + right.byteBoard[i, j]);
            });
            return result;
        }
        public int SquareSign(Square square)
        {
            return Math.Clamp(byteBoard[square.File, square.Rank], -1, 1);
        }

        public ulong MapOfControlledSquares()
        {
            ulong result = 0;
            LoopBoard((int i, int j) =>
            {
                if (byteBoard[i, j] != 0) BitboardHelper.SetSquare(ref result, new Square(i, j));
            });
            return result;
        }

    }

    #endregion

    #region Helper Functions

    public static void LoopBoard(Action<int, int> action)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                action(i, j);
            }
        }
    }
    public static Move HighestValueUncheckedMove(ref Dictionary<Move, float> moveValues, ref List<Move> checkedMoves, Board board)
    {
        Move highestValueUncheckedMove = Move.NullMove;
        if (checkedMoves.Count < moveValues.Keys.Count)
        {
            foreach (Move move in moveValues.Keys)
            {
                if (!checkedMoves.Contains(move) && (
                    (board.IsWhiteToMove && moveValues[move] >= moveValues[highestValueUncheckedMove]) ||
                    (!board.IsWhiteToMove && moveValues[move] <= moveValues[highestValueUncheckedMove])))
                {
                    highestValueUncheckedMove = move;
                }
            }
        }
        return highestValueUncheckedMove;
    }

    #endregion

    #region Debug

    //public void DEBUG_DisplayControlMaps(Board board)
    //{
    //    BuildControlMap(board);
    //    DivertedConsole.Write();
    //    DivertedConsole.Write();
    //    DivertedConsole.Write("Control Map: ");
    //    for (int i = 7; i >= 0; i--)
    //    {
    //        for (int j = 0; j < 8; j++)
    //        {
    //            DivertedConsole.Write($" {controlMap.byteBoard[j, i],2} ");
    //        }
    //        DivertedConsole.Write();
    //    }
    //    DivertedConsole.Write($"Control Score: {GetControlScore(board)}");
    //    DivertedConsole.Write();

    //}

    #endregion
}