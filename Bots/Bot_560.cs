namespace auto_Bot_560;
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/// Filename: MyBot.cs
/// Author: Mia Kellett
/// Date Created: 10/08/2023
/// Purpose: MyBot thinks about what the best move it can make is to win a game of chess. Thinking multiple moves ahead.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
using ChessChallenge.API;
using System;

//MiaKBotv4.4
public class Bot_560 : IChessBot
{
    //Consts.
    const int EVALUATION_RECURSIVE_DEPTH = 3;//This is how many moves ahead the bot will think about.
    const int MAX_TIMES_TO_RANDOMLY_GO_DEEPER = 50000;
    //The consts after this line are values of a move based on the state of the board after that move 
    const int NO_ENEMY_CAPTURE_VALUE = -10; //When a move doesn't capture anything it is given this weight.
    const int ENEMY_CAPTURED_MULTIPLIER = 10;
    const int CHECKMATE_VALUE = 1000000;
    const int POTENTIAL_CHECKMATE_VALUE = 500;
    const int CHECK_VALUE = 100;
    const int DRAW_VALUE = -1000000;
    //The consts after this line are the weights added to each move when it doesn't
    //lead to a capture depending on the piece being moved.
    const int KING_MOVE_SCORE_WEIGHT = -1000;
    const int QUEEN_MOVE_SCORE_WEIGHT = 1;
    const int ROOK_MOVE_SCORE_WEIGHT = 1;
    const int BISHOP_MOVE_SCORE_WEIGHT = 1;
    const int KNIGHT_MOVE_SCORE_WEIGHT = 1;
    const int PAWN_MOVE_SCORE_WEIGHT = 100;

    //Variables.
    Board m_board;
    Timer m_timer;
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };// Piece values: null, pawn, knight, bishop, rook, queen, king
    int BLACK_MULTIPLIER;
    int WHITE_MULTIPLIER;
    bool myBotIsWhite;

    //Static variables.
    static Random rng;
    static int s_timesGoneDeeper = 0;

    public Move Think(Board board, Timer timer)
    {
        //Reset gone deeper counter.
        s_timesGoneDeeper = 0;

        //Cache the state of the board.
        m_board = board;
        m_timer = timer;
        myBotIsWhite = m_board.IsWhiteToMove;

        //Get all the legal moves.
        Move[] moves = m_board.GetLegalMoves();

        //Evalulate each move and choose best one.
        Move bestMove = moves[0];
        int highestValue = int.MinValue;
        foreach (Move move in moves)
        {
            //Evaluate the move.
            int value = Evaluate(move, EVALUATION_RECURSIVE_DEPTH, 0);
            if (value > highestValue)
            {
                bestMove = move;
                highestValue = value;
            }
        }

        //Return the move to make.
        return bestMove;
    }

    public int Evaluate(Move a_move, int a_depthToGo, int a_currentDepth)
    {
        PieceType movePieceType = a_move.MovePieceType;
        int movePieceValue = pieceValues[(int)movePieceType];
        int capturedPieceValue = pieceValues[(int)m_board.GetPiece(a_move.TargetSquare).PieceType];
        int depthRemaining = a_depthToGo - 1;
        int currentDepth = a_currentDepth + 1;
        //Initalise return value.
        int moveEvaluationScore = 0;

        //Get the current turn's colour.
        bool currentTurnIsWhite = m_board.IsWhiteToMove;
        if (currentTurnIsWhite)
        {
            //Count white pieces vs black pieces.
            //White pieces give positive value and black pieces give negative value.
            BLACK_MULTIPLIER = -1;
            WHITE_MULTIPLIER = 1;
        }
        else
        {
            //Count white pieces vs black pieces.
            //White pieces give negative value and black pieces give positive value.
            BLACK_MULTIPLIER = 1;
            WHITE_MULTIPLIER = -1;
        }

        //Get the original board value.
        int boardValueBeforeMove = GetValueOfBoard();

        //Make move then get the score of the state of the board afterwards.
        m_board.MakeMove(a_move);

        //Check the board for different main game states.
        if (m_board.IsInCheckmate())
        {
            if (currentDepth == 1 || m_board.IsWhiteToMove != myBotIsWhite)
            {
                moveEvaluationScore += CHECKMATE_VALUE;
                m_board.UndoMove(a_move);
                return moveEvaluationScore; //Clip branch since it will likely lead to checkmate.
            }
            else
            {
                moveEvaluationScore += POTENTIAL_CHECKMATE_VALUE;
                //Since we might be putting a piece in checkmate, decide randomly if it's worth checking one level deeper.
                bool canGoDeeper = s_timesGoneDeeper < MAX_TIMES_TO_RANDOMLY_GO_DEEPER;
                if (RandomChanceToPass((int)(InverseLerp(0, m_timer.GameStartTimeMilliseconds, m_timer.MillisecondsRemaining) * 100)) && canGoDeeper)
                {
                    depthRemaining++;
                    s_timesGoneDeeper++;
                }
            }
        }

        if (m_board.IsInCheck())
        {
            //moveEvaluationScore += CHECK_VALUE;
            if (SquareIsGoingToBeAttackedByOpponent(a_move.TargetSquare))
            {
                //We don't want to check the enemy if they're gonna take one of our high value pieces.
                moveEvaluationScore += (-(CHECK_VALUE * movePieceValue));
            }
            else
            {
                moveEvaluationScore += CHECK_VALUE;
            }
        }

        if (m_board.IsDraw())
        {
            moveEvaluationScore += DRAW_VALUE;
        }

        //Get the value of the whole board if the move is made.
        int valueOfBoardIfMoveIsMade = GetValueOfBoard();
        int netMoveScore = (valueOfBoardIfMoveIsMade - boardValueBeforeMove);
        if (netMoveScore <= 0)
        {
            //If move does not capture a piece.
            moveEvaluationScore += NO_ENEMY_CAPTURE_VALUE; //Discourage bot from not capturing.

            //Then give it more incentive to move certain pieces in that case.
            switch (movePieceType)
            {
                case PieceType.King:
                    {
                        moveEvaluationScore += KING_MOVE_SCORE_WEIGHT;
                        break;
                    }
                case PieceType.Queen:
                    {
                        moveEvaluationScore += QUEEN_MOVE_SCORE_WEIGHT;
                        break;
                    }
                case PieceType.Rook:
                    {
                        moveEvaluationScore += ROOK_MOVE_SCORE_WEIGHT;
                        break;
                    }
                case PieceType.Bishop:
                    {
                        moveEvaluationScore += BISHOP_MOVE_SCORE_WEIGHT;
                        break;
                    }
                case PieceType.Knight:
                    {
                        moveEvaluationScore += KNIGHT_MOVE_SCORE_WEIGHT;//Initial move weight.

                        //Move weight to be added depedning on if the knight is close to the center.
                        int KNIGHT_MOVE_TO_CENTER_WEIGHT = KNIGHT_MOVE_SCORE_WEIGHT;
                        bool isCloseToCenter = SquareIsCloseToCenter(a_move.TargetSquare);
                        if (isCloseToCenter)
                        {
                            moveEvaluationScore += KNIGHT_MOVE_TO_CENTER_WEIGHT;
                        }
                        else
                        {
                            moveEvaluationScore += (-KNIGHT_MOVE_TO_CENTER_WEIGHT);
                        }
                        break;
                    }
                case PieceType.Pawn:
                    {
                        moveEvaluationScore += PAWN_MOVE_SCORE_WEIGHT;

                        //Move weight to be added depending on if the pawn is close to an end rank.
                        int PAWN_MOVE_TO_END_RANK_WEIGHT = PAWN_MOVE_SCORE_WEIGHT;
                        bool pawnIsNotOnEndRank = IsSquareAnEndRank(a_move.StartSquare);
                        if (pawnIsNotOnEndRank)
                        {
                            moveEvaluationScore += PAWN_MOVE_TO_END_RANK_WEIGHT;
                        }
                        else
                        {
                            moveEvaluationScore += (-PAWN_MOVE_TO_END_RANK_WEIGHT);
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }

            }
        }
        else
        {
            //If the value of the piece we're moving is higher than the value of the piece we're capturing
            //then we want to check if it will be captured next turn.
            if (movePieceValue >= capturedPieceValue && SquareIsGoingToBeAttackedByOpponent(a_move.TargetSquare))
            {
                //We don't want to make the move so we want to negatively weight this capture.
                moveEvaluationScore += (-(movePieceValue * ENEMY_CAPTURED_MULTIPLIER));
            }
            else
            {
                //Mutliply the captured piece's value by the enemy captured multiplier.
                moveEvaluationScore += capturedPieceValue * ENEMY_CAPTURED_MULTIPLIER;

            }

            //Since we captured a piece, decide randomly if it's worth checking one level deeper.
            bool canGoDeeper = s_timesGoneDeeper < MAX_TIMES_TO_RANDOMLY_GO_DEEPER;
            if (RandomChanceToPass((int)(InverseLerp(0, m_timer.GameStartTimeMilliseconds, m_timer.MillisecondsRemaining) * 100)) && canGoDeeper)
            {
                depthRemaining++;
                s_timesGoneDeeper++;
            }
        }

        if (depthRemaining > 0)
        {
            //Get list of next posisble moves.
            //Evaluate each of those moves with 1 less depth than the previous call of evaluate.
            //When it reaches 0 then the recursive loop will exit with the best approximate move.
            int worstScoreForPreviousPlayer = int.MaxValue;
            foreach (Move move in m_board.GetLegalMoves())
            {
                int moveScore = -Evaluate(move, depthRemaining, currentDepth); //Inverts the evaluation score as what's best for the next player won't be best for the current player.
                if (moveScore < worstScoreForPreviousPlayer)
                {
                    worstScoreForPreviousPlayer = moveScore;
                }
            }

            //add the score to the moves score.
            moveEvaluationScore += worstScoreForPreviousPlayer;
        }


        //Return board to original state.
        m_board.UndoMove(a_move);

        //Return the value.
        return moveEvaluationScore;
    }

    private int GetValueOfBoard()
    {
        PieceList[] allPieces = m_board.GetAllPieceLists();
        int boardValue = 0;
        foreach (PieceList pieces in allPieces)
        {
            bool isWhitePieceList = pieces.IsWhitePieceList;
            int piecesCountTimesPieceValue = pieces.Count * pieceValues[(int)pieces.TypeOfPieceInList];
            if (isWhitePieceList)
            {
                boardValue += (WHITE_MULTIPLIER * piecesCountTimesPieceValue);
            }
            else
            {
                boardValue += (BLACK_MULTIPLIER * piecesCountTimesPieceValue);
            }
        }
        return boardValue;
    }

    private bool SquareIsCloseToCenter(Square a_square)
    {
        if (a_square.File > 1 && a_square.File < 6 && a_square.Rank > 1 && a_square.Rank < 6)
        {
            return true; //Square is close to the center of the board.
        }

        return false;//Square is not close to the center of the board.
    }

    private bool IsSquareAnEndRank(Square a_square)
    {
        if (a_square.Rank <= 0 || a_square.Rank >= 7)
        {
            return true;//Square is an end rank.
        }

        return false;//Square is not an end rank.
    }

    private bool RandomChanceToPass(int a_percentageChance)
    {
        //Seed rng
        if (rng == null)
        {
            rng = new Random(((int)DateTime.UtcNow.Ticks));
        }
        int randomValue = rng.Next(100);
        return randomValue < a_percentageChance;
    }

    private bool SquareIsGoingToBeAttackedByOpponent(Square originalSquare)
    {
        //Get list of next posisble moves
        foreach (Move move in m_board.GetLegalMoves())
        {
            //Compare them with the original.
            if (move.TargetSquare == originalSquare)
            {
                return true;
            }
        }
        //No moves will capture the piece.
        return false;
    }
    public static float InverseLerp(float a, float b, float value)
    {
        return (value - a) / (b - a);
    }
}