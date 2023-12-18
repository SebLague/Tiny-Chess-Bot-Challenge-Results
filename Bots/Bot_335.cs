namespace auto_Bot_335;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>
/// Der wilde Kaiser 
/// </summary>
public class Bot_335 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 1337 },
          fieldValues = { 3, 4, 4, 5, 5, 4, 4, 3,
                      4, 5, 6, 6, 6, 6, 5, 4,
                      4, 6, 7, 7, 7, 7, 6, 4,
                      5, 6, 7, 8, 8, 7, 6, 5,
                      5, 6, 7, 8, 8, 7, 6, 5,
                      4, 6, 7, 7, 7, 7, 6, 4,
                      4, 5, 6, 6, 6, 6, 5, 4,
                      3, 4, 4, 5, 5, 4, 4, 3},
        //                    +10 +12 +16 +24  +40  +72
        pawnMoveBonuses = { 0, 10, 22, 38, 62, 102, 174 };

    int depth = 6, earlyQueenMoveMalus = 800, millisecondsRemaining, nextMovesCount = 3;
    double timeImportanceFactor, highestScore, timeFactorPercent;

    Board theBoard;
    List<Piece> piecesToDevelop;
    Piece pieceToMove;
    Move moveToPlay, secondMoveToPlay;

    //public Bot_335 ()
    //{
    //}

    public Move Think(Board board, Timer timer)
    {
        //What pieces can i possible move?
        theBoard = board;
        var allMoves = theBoard.GetLegalMoves();
        var badTradeMoves = new List<Move>();
        bool firstMove = piecesToDevelop == null;
        int piecesLeft = BitOperations.PopCount(theBoard.AllPiecesBitboard);

        highestScore = -13333333337;
        moveToPlay = allMoves[0];
        secondMoveToPlay = moveToPlay;

        if (firstMove)
        {
            //First move things
            piecesToDevelop = (
              from pieceList in theBoard.GetAllPieceLists()
              from piece in pieceList
              where piece.PieceType == PieceType.Knight ||
                    piece.PieceType == PieceType.Bishop
              select piece).ToList();

            //On the first move, I have to calculate how many steps I can plan ahead based on the given time...
            //for desmos.com: f\left(x\right)\ =10\cdot\left(\frac{1}{\left(0.8+2^{-0.1x}\right)}\right)-5
            //depth = (int)Math.Max(10 * (1 / (0.8 + Math.Pow(2, -0.1 * (timer.GameStartTimeMilliseconds - 30)))) - 5, 2);
            //... Or to save capacity I just do 6 (set in implementation)
        }
        else
        {
            //Hm... Okay, how far i can calculate my next moves?
            #region Detail
            //// Proportion of time remaining
            //double timeOutInPercent = 100 - (double)timer.MillisecondsRemaining / timer.GameStartTimeMilliseconds * 100.0 * timeImportanceFactor;

            //// Proportion of remaining pieces
            //double remainingPiecesRatio = BitOperations.PopCount(theBoard.AllPiecesBitboard) / 32 * 100.0;

            //// Factor x
            //double x = (timeOutInPercent + remainingPiecesRatio) / 2;

            //// And multiplied by the performance score
            ////x *= performanceScore; < --no longer relevant

            //// Application of the formula for depth
            ////for desmos.com: f\left(x\right)=\ 10\cdot 1.00276^{-3x}
            //depth = (int)Math.Round(10 * Math.Pow(1.00276, -3 * x));
            #endregion

            millisecondsRemaining = timer.MillisecondsRemaining;
            depth = (int)Math.Round(10 * Math.Pow(1.00276, -3 * (100 - 100.0 * millisecondsRemaining / timer.GameStartTimeMilliseconds * timeImportanceFactor + 100.0 * piecesLeft / 32.0) / 2));

            //No need to make it too complicated at the beginning
            if (depth > 6 && earlyQueenMoveMalus > 0) depth = 6;
            //Ensure never to lose because of time
            if (millisecondsRemaining < 10000 && depth > millisecondsRemaining / 1000) depth = millisecondsRemaining / 1000;
            //Dangerous because of the time, but in the last moves I may not only look at the best three moves
            if (piecesLeft < 8) nextMovesCount = 4;
        }

        //Let's check all of my moves!
        foreach (Move move in allMoves)
        {
            // Always play checkmate in one (powerd by EvilBot `-´)
            if (MoveIsCheckmate(move))
            {
                moveToPlay = move;
                break;
            }

            //First i have to check if this move is not obviously a blunder
            if (GetTradeScore(move) < 0)
            {
                badTradeMoves.Add(move);
                continue;
            }

            EvaluateAndSetBestMove(move);
        }

        //Did I found a move that does not lead to material loss?
        //Then I just have to look at these again more closely
        if (highestScore == -13333333337) foreach (var btm in badTradeMoves) EvaluateAndSetBestMove(btm);

        // Do I whant to draw the match?
        var ula = theBoard.GameRepetitionHistory;
        //Actually, the position doesn't have to be played 3 times in a row, but otherwise it would cost too many tokens. 
        if (ula.Length > 6 && ula[6] == ula[2] && ula[5] == ula[1] && ula[4] == ula[0] && GetPositionScoreFromMeAndMyOpponent(moveToPlay) > 400) moveToPlay = secondMoveToPlay;

        pieceToMove = theBoard.GetPiece(moveToPlay.StartSquare);

        if (pieceToMove.IsQueen) earlyQueenMoveMalus = 0;
        if (earlyQueenMoveMalus > 0) earlyQueenMoveMalus -= 80;

        piecesToDevelop.Remove(pieceToMove);

        if (firstMove)
        {
            //Calculate from the first move how important the time factor will be in this game (between 1 and 2)
            timeFactorPercent = 100.0 * timer.MillisecondsElapsedThisTurn / timer.GameStartTimeMilliseconds;

            if (timeFactorPercent <= 1) timeImportanceFactor = 1;
            else if (timeFactorPercent >= 10) timeImportanceFactor = 2;
            else timeImportanceFactor = 1.0 / 9.0 * timeFactorPercent + 8.0 / 9.0; //for desmos.com: f\left(x\right)\ =\frac{1}{9}\ \cdot x\ +\ \frac{8}{9}
        }

        //Teufel noch eins, was für ein Schachzug!
        return moveToPlay;
    }

    bool MoveIsCheckmate(Move move)
    {
        theBoard.MakeMove(move);
        bool isMate = theBoard.IsInCheckmate();
        theBoard.UndoMove(move);
        return isMate;
    }

    void EvaluateAndSetBestMove(Move move)
    {
        //Can i add any bonus score to this move?
        double bonusScore = 0;
        if (!theBoard.IsInCheck())
        {
            pieceToMove = theBoard.GetPiece(move.StartSquare);

            //I have to develop my pieces!
            if (piecesToDevelop.Contains(pieceToMove)) bonusScore += 67;

            //But moving the queen out too early is dangerous
            if (pieceToMove.IsQueen) bonusScore -= earlyQueenMoveMalus;

            //Pawn moves who are closer to being promoted are more valuable
            if (pieceToMove.IsPawn) bonusScore += pawnMoveBonuses[pieceToMove.IsWhite ? pieceToMove.Square.Rank : 7 - pieceToMove.Square.Rank];

            //It is most likely good to castle...
            if (move.IsCastles) bonusScore += 133;
        }

        //Now let me see how much this move is worth 
        double score = GetMoveScore(move, depth) + bonusScore * Math.Pow(depth > 2 ? depth - 1 : 1, nextMovesCount);

        //Is this now my best move?
        if (score > highestScore)
        {
            highestScore = score;
            secondMoveToPlay = moveToPlay;
            moveToPlay = move;
        }
    }

    double GetMoveScore(Move move, int maxDepth = 1, int depthLevel = 1)
    {
        //A move that means checkmate is very good
        if (MoveIsCheckmate(move)) return 2000 * (maxDepth - depthLevel + 1);

        double moveScore = GetPositionScoreFromMeAndMyOpponent(move);

        //Should i go to the next move?
        if (depthLevel < maxDepth)
        {
            var badTradeMoves = new List<Move>();
            var nextBestMoves = new List<KeyValuePair<Move, double>>();

            foreach (Move nextMove in theBoard.GetLegalMoves())
            {
                //Here the same. If the move means loss of material, it is neglected
                if (GetTradeScore(nextMove) < 0)
                {
                    badTradeMoves.Add(nextMove);
                    continue;
                }

                nextBestMoves.Add(new KeyValuePair<Move, double>(nextMove, GetMoveScore(nextMove)));
            }

            //I should just pick the best next moves... 
            nextBestMoves.Sort((a, b) => b.Value.CompareTo(a.Value));
            nextBestMoves = nextBestMoves.Take(nextMovesCount).ToList();

            //Did i found any good moves?
            if (nextBestMoves.Count == 0)
            {
                //So i have to check the bad moves
                nextBestMoves.AddRange(badTradeMoves.Select(move => new KeyValuePair<Move, double>(move, GetMoveScore(move))));

                //I should just pick the best bad next moves... 
                nextBestMoves.Sort((a, b) => b.Value.CompareTo(a.Value));
                nextBestMoves = nextBestMoves.Take(nextMovesCount).ToList();
            }

            //Okay, what is the score from the next best moves?
            moveScore -= nextBestMoves.Sum(nbm => GetMoveScore(nbm.Key, maxDepth, depthLevel + 1)) * (nextBestMoves.Count == 1 ? 3 : (nextBestMoves.Count == 2 ? 1.5 : 1));
        }

        theBoard.UndoMove(move);
        return moveScore;
    }

    double GetTradeScore(Move move)
    {
        double tradeScore = pieceValues[(int)theBoard.GetPiece(move.TargetSquare).PieceType];
        theBoard.MakeMove(move);

        foreach (Move nextMove in theBoard.GetLegalMoves())
        {
            if (nextMove.IsCapture && nextMove.TargetSquare == move.TargetSquare)
            {
                tradeScore -= GetTradeScore(nextMove);
                break;
            }
        }

        theBoard.UndoMove(move);
        return tradeScore;
    }

    double GetPositionScoreFromMeAndMyOpponent(Move move)
    {
        theBoard.MakeMove(move);

        //What is the opponent score in this position?
        double positionScoreMO = -GetPositionScore();

        theBoard.ForceSkipTurn();
        //What is my score in this position?
        positionScoreMO += GetPositionScore();
        theBoard.UndoSkipTurn();

        //theBoard.UndoMove(move);
        return positionScoreMO;
    }

    double GetPositionScore()
    {
        double positionScore = 0;

        //PieceScore - Which pieces are still on the board? and FieldScore - Where are the pieces?
        foreach (PieceList pieceList in theBoard.GetAllPieceLists()) if (theBoard.IsWhiteToMove == pieceList.IsWhitePieceList) foreach (Piece piece in pieceList) positionScore += pieceValues[(int)piece.PieceType] + fieldValues[piece.Square.Index];

        //ControlScore - Which fields are controlled?
        foreach (Move nextMove in theBoard.GetLegalMoves()) positionScore += fieldValues[nextMove.TargetSquare.Index];

        return positionScore;
    }

    #region GetPositionScore() with pawn considered
    //double GetPositionScore()
    //{
    //  double positionScore = 0;

    //  //foreach(PieceList pieceList in theBoard.GetAllPieceLists()) if(theBoard.IsWhiteToMove == pieceList.IsWhitePieceList) foreach(Piece piece in pieceList) positionScore += pieceValues[(int)piece.PieceType] + fieldValues[piece.Square.Index];
    //  foreach(PieceList pieceList in theBoard.GetAllPieceLists())
    //  {
    //    if(theBoard.IsWhiteToMove == pieceList.IsWhitePieceList)
    //    {
    //      foreach(Piece piece in pieceList)
    //      {
    //        //PieceScore - Which pieces are still on the board?
    //        positionScore += pieceValues[(int)piece.PieceType];

    //        //FieldScore - Where are the pieces?
    //        positionScore += fieldValues[piece.Square.Index];

    //        //ControlScore from pawns
    //        if(piece.PieceType == PieceType.Pawn)
    //        {
    //          int[] skewedFields = {piece.Square.File != 0 ? (piece.IsWhite ? piece.Square.Index + 7 : piece.Square.Index - 9) : -1,
    //                                piece.Square.File != 7 ? (piece.IsWhite ? piece.Square.Index + 9 : piece.Square.Index - 7) : -1};

    //          foreach(int skewedField in skewedFields)
    //          {
    //            if(skewedField == -1) continue;
    //            Piece pieceAtSkewedField = theBoard.GetPiece(new Square(skewedField));
    //            if((pieceAtSkewedField.PieceType != PieceType.None) || (piece.IsWhite && !pieceAtSkewedField.IsWhite)) positionScore += fieldValues[skewedField];
    //          }
    //        }
    //      }
    //    }
    //  }

    //  //Control-Score - Which fields are controlled?
    //  foreach(Move nextMove in theBoard.GetLegalMoves()) if(nextMove.MovePieceType != PieceType.Pawn) positionScore += fieldValues[nextMove.TargetSquare.Index];

    //  return positionScore;
    //}
    #endregion

    #region Todo's

    //Todo: In open games B > N - in closed games N > B
    //Todo: replace fieldValues with heatmap for each piece
    //Todo: Bonus for: Pawn move which results in opponent having to move a piece (tempo)



    //######################################### Implementation of a heatmap is impossible... damn you capacity limit!  ######################################################

    //int[] GetFieldValues(PieceType pieceType, bool forWhite)
    //{
    //  if(pieceType == PieceType.King)
    //  {
    //    if(calculatetFieldValues.TryGetValue(theBoard.GetPieceBitboard(PieceType.Pawn, forWhite), out int[] fieldValuesKing))
    //    {
    //      return fieldValuesKing;
    //    }

    //    try
    //    {
    //      fieldValuesKing = new int[64];
    //      int step = kingSaveInitValue / 3;

    //      // Create an array with the basic values
    //      for(int i = 0; i < 64; i++)
    //      {
    //        //int modFile = i % 8;

    //        //// Default value
    //        //if((modFile == 3) || (modFile == 4))
    //        //{
    //        //  fieldValuesKing[i] = -initValue;
    //        //}
    //        ////else if((modFile == 2) || (modFile == 5))
    //        ////{
    //        ////  fieldValuesKing[i] = -(initValue / 2);
    //        ////}
    //        //else
    //        //{
    //        //  fieldValuesKing[i] = 0;
    //        //}
    //        fieldValuesKing[i] = 0;
    //      }

    //      PieceList pl = theBoard.GetPieceList(PieceType.Pawn, forWhite);
    //      bool[] filesWithPawns = { false, false, false, false, false, false, false, false };

    //      foreach(Piece piece in pl)
    //      {
    //        if(filesWithPawns[piece.Square.File] || piece.Square.File == 3 || piece.Square.File == 4)
    //        {
    //          continue;
    //        }

    //        //int value = initValue;

    //        if(forWhite)
    //        {
    //          fieldValuesKing[piece.Square.Index - 8] += kingSaveInitValue;
    //          if(piece.Square.Rank > 2)
    //          {
    //            fieldValuesKing[piece.Square.Index - 16] += step * 2;
    //            fieldValuesKing[piece.Square.Index - 24] += step;
    //          }
    //          else if(piece.Square.Rank == 2)
    //          {
    //            fieldValuesKing[piece.Square.Index - 16] += kingSaveInitValue;
    //          }

    //          //for(int rank = piece.Square.Rank - 1; rank >= 0; rank--)
    //          //{
    //          //  fieldValuesKing[rank * 8 + piece.Square.File] += value;
    //          //  if(value > 0) value -= step;
    //          //  filesWithPawns[piece.Square.File] = true;
    //          //}
    //        }
    //        else
    //        {
    //          fieldValuesKing[piece.Square.Index + 8] += kingSaveInitValue;
    //          if(piece.Square.Rank < 5)
    //          {
    //            fieldValuesKing[piece.Square.Index + 16] += step * 2;
    //            fieldValuesKing[piece.Square.Index + 24] += step;
    //          }
    //          else if(piece.Square.Rank == 5)
    //          {
    //            fieldValuesKing[piece.Square.Index + 16] += kingSaveInitValue;
    //          }
    //          //for(int rank = piece.Square.Rank + 1; rank <= 7; rank++)
    //          //{

    //          //  fieldValuesKing[rank * 8 + piece.Square.File] += value;
    //          //  if(value > 0) value -= step;
    //          //  filesWithPawns[piece.Square.File] = true;
    //          //}
    //        }

    //        filesWithPawns[piece.Square.File] = true;
    //      }

    //      for(int file = 0; file <= 7; file++)
    //      {
    //        if(!filesWithPawns[file])
    //        {//File 'd' and 'e' are always like they have no Pawn 
    //          for(int rank = 0; rank <= 7; rank++)
    //          {
    //            if(file > 0) fieldValuesKing[rank * 8 + file - 1] -= step;
    //            fieldValuesKing[rank * 8 + file] -= kingSaveInitValue;
    //            if(file < 7) fieldValuesKing[rank * 8 + file + 1] -= step;
    //          }
    //        }
    //      }

    //      //FieldValuesToConsole(fieldValuesKing, forWhite);
    //      calculatetFieldValues.Add(theBoard.GetPieceBitboard(PieceType.Pawn, forWhite), fieldValuesKing);

    //      return fieldValuesKing;
    //    }
    //    catch(Exception)
    //    {
    //      return fieldValues;
    //    }
    //  }
    //  else
    //  {
    //    return fieldValues;
    //  }
    //}

    //void FieldValuesToConsole(int[] p_fieldValues, bool white)
    //{

    //  Debug.WriteLine("");
    //  Debug.WriteLine("");
    //  Debug.WriteLine(theBoard.CreateDiagram());
    //  Debug.WriteLine("");
    //  Debug.WriteLine("");

    //  for(int rank = 7; rank >= 0; rank--)
    //  {
    //    for(int file = 0; file < 8; file++)
    //    {
    //      int value = p_fieldValues[rank * 8 + file];

    //      if(value > 9)
    //      {
    //        Debug.Write(" " + value.ToString() + " ");
    //      }
    //      else if(value == 0)
    //      {
    //        Debug.Write("  " + value.ToString() + " ");
    //      }
    //      else if(value < -9)
    //      {
    //        Debug.Write(value.ToString() + " ");
    //      }
    //      else
    //      {
    //        Debug.Write(" " + value.ToString() + " ");
    //      }
    //    }

    //    Debug.WriteLine("");
    //  }

    //  Debug.WriteLine("");
    //  Debug.WriteLine("");
    //  Debug.WriteLine("");
    //}
    #endregion

}