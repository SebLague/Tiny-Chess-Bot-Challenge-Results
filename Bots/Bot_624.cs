namespace auto_Bot_624;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_624 : IChessBot
{
    //I thought these were more accurate values personally.
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    Dictionary<ulong, Dictionary<int, float>> Transposition = new();
    ulong one = 1;

    public Move Think(Board board, Timer timer)
    {
        int i = 1;
        if (Transposition.Count > 100000)
        {
            Transposition.Clear();
        }

        (List<Move> move, _) = Evaluation(board, 0, -50000, false, new());
        while (timer.MillisecondsElapsedThisTurn < (Math.Min(board.PlyCount, 40) * timer.MillisecondsRemaining) / 4000)
        {
            (move, _) = Evaluation(board, i++, -50000, false, move);
        }
        return move[0];
    }

    //I only use move for the first use but copying this function to only need the int all other times and only move the others is a waste of brain power.
    //Apparently ref means * (new to C#)
    (List<Move>, float) Evaluation(Board board, int Depth, float BestMoveValue, bool turn, List<Move> PriorityMoves)
    {
        System.Span<Move> allMoves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref allMoves);

        if (PriorityMoves.Any())
        {
            try
            {
                allMoves[allMoves.IndexOf(PriorityMoves[0])] = allMoves[0];
                allMoves[0] = PriorityMoves[0];
                PriorityMoves.RemoveAt(0);
            }
            catch { }
        }

        List<Move> moveList, moveToPlay = new() { allMoves[0] };
        float highestMoveValue = -50000, moveValue;
        ulong zobristKey;

        foreach (Move move in allMoves)
        {
            moveList = new() { move };
            bool newTrans = true;
            board.MakeMove(move);
            zobristKey = board.ZobristKey;
            if (Transposition.ContainsKey(zobristKey))
            {
                if (Transposition[zobristKey].ContainsKey(Depth))
                {
                    board.UndoMove(move);
                    newTrans = false;
                }
                else
                {
                    Transposition[zobristKey].Add(Depth, 0);
                }
            }
            //One million should be safe to get under the limit.
            else
            {
                Transposition.Add(zobristKey, new());
                Transposition[zobristKey].Add(Depth, 0);
            }

            if (newTrans)
            {
                if (board.IsInCheckmate())
                {
                    board.UndoMove(move);
                    return (moveList, 50000);
                }

                if (board.IsDraw())
                {
                    moveValue = 0;
                }
                else if (Depth > 0)
                {
                    (List<Move> move, float value) OponnentMove = Evaluation(board, Depth - 1, -highestMoveValue, !turn, PriorityMoves);
                    moveValue = -OponnentMove.value;
                    foreach (Move OppMove in OponnentMove.move)
                    {
                        moveList.Add(OppMove);
                    }
                }
                else
                {
                    float AIValue = 0, OpponentValue = 0, AIInvestment, OpponentInvestment;
                    List<int> AIThreats = new(), OpponentThreats = new();
                    int i, squareAI, squareOpp, AICount, OppCount;
                    PieceList[] AllPieces = board.GetAllPieceLists();
                    List<BoardControl> result = new();
                    ulong PieceAttacks;
                    List<int> AIAttackingPieces, OpponentAttackingPieces;

                    for (i = 0; i < 64; i++)
                    {
                        result.Add(new());
                    }

                    //This will only be called after a make move so it'll be always evaluating on the opponent's turn according to the AI.
                    bool Colour = !board.IsWhiteToMove;

                    //Looks for numbers of each type of piece on both sides.
                    foreach (PieceList piecelist in AllPieces)
                    {
                        if (piecelist.IsWhitePieceList == Colour)
                        {
                            AIValue += (piecelist.Count * pieceValues[(int)piecelist.TypeOfPieceInList]);
                        }
                        else
                        {
                            OpponentValue += (piecelist.Count * pieceValues[(int)piecelist.TypeOfPieceInList]);
                        }


                        //Goes through each piece gets their bitboard attacks anbd adds them to the corresponding piece 2dlist.
                        foreach (Piece piece in piecelist)
                        {
                            PieceAttacks = BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite);
                            i = 0;
                            foreach (BoardControl square in result)
                            {
                                if ((PieceAttacks & (one << i)) != 0)
                                {
                                    if (piece.IsWhite == Colour)
                                    {
                                        square.AIPieces.Add(pieceValues[(int)piece.PieceType]);
                                    }
                                    else
                                    {
                                        square.OpponentPieces.Add(pieceValues[(int)piece.PieceType]);
                                    }
                                }
                                i++;
                            }
                            if (Colour == piece.IsWhite)
                            {
                                result[piece.Square.Index].AIValue = pieceValues[(int)piece.PieceType];
                            }
                            else
                            {
                                result[piece.Square.Index].OpponentValue = pieceValues[(int)piece.PieceType];
                            }
                        }
                    }
                    foreach (BoardControl square in result)
                    {
                        //Splits pieces by colour and sorts them by value in descending order.
                        AIAttackingPieces = square.AIPieces;
                        OpponentAttackingPieces = square.OpponentPieces;
                        AICount = AIAttackingPieces.Count;
                        OppCount = OpponentAttackingPieces.Count;
                        squareAI = square.AIValue;
                        squareOpp = square.OpponentValue;
                        AIInvestment = squareAI;
                        OpponentInvestment = squareOpp;
                        for (i = 0; i < Math.Max(AICount, OppCount); i++)
                        {
                            if (OppCount <= i && AICount > i)
                            {
                                AIValue += ControlValuation(0, OpponentInvestment, AIAttackingPieces, ref AIThreats, squareOpp);

                                break;
                            }
                            else if (OppCount > i && AICount <= i)
                            {
                                OpponentValue += ControlValuation(0, AIInvestment, OpponentAttackingPieces, ref OpponentThreats, squareAI);

                                break;
                            }
                            if (i == 0)
                            {
                                if (OpponentAttackingPieces[0] < AIInvestment)
                                {
                                    OpponentValue += ControlValuation(0, AIInvestment, OpponentAttackingPieces, ref OpponentThreats, squareAI);
                                    break;
                                }
                                else if (AIAttackingPieces[0] < OpponentInvestment)
                                {
                                    AIValue += ControlValuation(0, OpponentInvestment, AIAttackingPieces, ref AIThreats, squareOpp);
                                    break;
                                }
                            }

                            AIInvestment += AIAttackingPieces[i];
                            OpponentInvestment += OpponentAttackingPieces[i];
                            if (AIInvestment > OpponentInvestment)
                            {
                                if (OppCount > i + 1)
                                {
                                    if (AIInvestment > OpponentInvestment + OpponentAttackingPieces[i + 1])
                                    {
                                        OpponentValue += ControlValuation(OpponentInvestment, AIInvestment, OpponentAttackingPieces, ref OpponentThreats, squareAI);
                                        break;
                                    }
                                }
                            }
                            else if (AIInvestment < OpponentInvestment)
                            {
                                if (AICount > i + 1)
                                {
                                    if (OpponentInvestment > AIInvestment + AIAttackingPieces[i + 1])
                                    {
                                        AIValue += ControlValuation(AIInvestment, OpponentInvestment, AIAttackingPieces, ref AIThreats, squareOpp);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (OpponentThreats.Any())
                    {
                        OpponentValue += OpponentThreats.Max();
                    }
                    moveValue = AIValue - OpponentValue;
                }

                board.UndoMove(move);

                try
                {
                    Transposition[zobristKey][Depth] = moveValue;
                }
                catch { }
            }
            else
            {
                moveValue = Transposition[zobristKey][Depth];
            }

            if (moveValue > highestMoveValue)
            {
                moveToPlay = moveList;
                highestMoveValue = moveValue;

                //Shody Minimax
                if (turn && highestMoveValue > BestMoveValue)
                {
                    return (moveToPlay, highestMoveValue);
                }
            }

        }

        return (moveToPlay, highestMoveValue);
    }

    //Ideally I'd like to initialise the int and bool later than the list but I don't know whether that's possible (new to C#)
    public class BoardControl
    {
        public int AIValue = 0, OpponentValue = 0;
        public List<int> AIPieces = new(), OpponentPieces = new();
    }

    float ControlValuation(float WinnerInvestment, float LoserInvestment, List<int> AttackingPieces, ref List<int> Threats, int PieceValue)
    {
        LoserInvestment += 1;
        WinnerInvestment += (float)(Math.Pow(AttackingPieces[0] / AttackingPieces.Count, 1));
        if (PieceValue > 0)
        {
            Threats.Add(PieceValue);
        }
        return LoserInvestment / WinnerInvestment;
    }
}

//I hate this speghettified mess and can only blame it on me doing it in a couple days as my first use of C#