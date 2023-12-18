namespace auto_Bot_107;
using ChessChallenge.API;
using System.Collections.Generic;
using System.Linq;

public class Bot_107 : IChessBot
{
    int points;
    int currentBestMove;
    int currentBestPoints;
    public bool IsWhiteToMove { get; private set; }

    public ChessChallenge.API.Move Think(ChessChallenge.API.Board board, Timer timer)
    {
        Dictionary<PieceType, int> piecePoints = new Dictionary<PieceType, int>
        {
            { PieceType.Pawn, 100 },
            { PieceType.Knight, 300 },
            { PieceType.Bishop, 320 },
            { PieceType.Rook, 500 },
            { PieceType.Queen, 900 }
        };
        currentBestPoints = 0;
        currentBestMove = 0;
        ChessChallenge.API.Move[] moves = board.GetLegalMoves();
        int[] movePoints = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            if (piecePoints.TryGetValue(moves[i].CapturePieceType, out int value))
            {
                points += value;
            }

            if (moves[i].MovePieceType == PieceType.Pawn && moves[i].StartSquare.Rank == 6 && moves[i].TargetSquare.Rank == 4)
            {
                points += 1;
            }
            if ((moves[i].MovePieceType == PieceType.Knight || moves[i].MovePieceType == PieceType.Bishop || moves[i].MovePieceType == PieceType.Queen) && moves[i].StartSquare.Rank == 7 && moves[i].TargetSquare.Rank >= 5)
            {
                points += 2;
            }
            if (board.SquareIsAttackedByOpponent(moves[i].StartSquare) && piecePoints.TryGetValue(moves[i].MovePieceType, out int value1))
            {
                if (board.TrySkipTurn())
                {
                    if (!board.SquareIsAttackedByOpponent(moves[i].StartSquare))
                    {
                        points += value1;
                    }
                    board.UndoSkipTurn();
                }
            }
            ChessChallenge.API.Board newBoard = ChessChallenge.API.Board.CreateBoardFromFEN(board.GetFenString());
            newBoard.MakeMove(moves[i]);
            ChessChallenge.API.Move[] newNewOpponentMoves = newBoard.GetLegalMoves();
            for (int w = 0; w < newNewOpponentMoves.Length; w++)
            {
                ChessChallenge.API.Board newNewBoard = ChessChallenge.API.Board.CreateBoardFromFEN(board.GetFenString());
                newNewBoard.MakeMove(newNewOpponentMoves[w]);
                if (newNewBoard.IsInCheckmate())
                {
                    points -= 20002;
                }
            }
            if (board.SquareIsAttackedByOpponent(moves[i].TargetSquare) && piecePoints.TryGetValue(moves[i].MovePieceType, out int value2))
            {
                points -= value2;
            }
            else
            {
                ChessChallenge.API.Board burd = ChessChallenge.API.Board.CreateBoardFromFEN(board.GetFenString());
                burd.MakeMove(moves[i]);
                Piece nextMovePiece = burd.GetPiece(moves[i].StartSquare);
                ChessChallenge.API.Move[] pieceMoves = burd.GetLegalMoves().Where((x) => x.TargetSquare == nextMovePiece.Square).ToArray();
                for (int w = 0; w < pieceMoves.Length; w++)
                {
                    if (piecePoints.TryGetValue(pieceMoves[w].CapturePieceType, out int value3))
                    {
                        points += value3;
                        if (burd.SquareIsAttackedByOpponent(pieceMoves[w].TargetSquare) && piecePoints.TryGetValue(pieceMoves[w].MovePieceType, out int value4))
                        {
                            if (board.TrySkipTurn())
                            {
                                if (!board.SquareIsAttackedByOpponent(moves[i].TargetSquare))
                                {
                                    points -= value4;
                                }
                                board.UndoSkipTurn();
                            }
                        }
                    }
                }
                if (burd.IsInCheckmate())
                {
                    points += 10000;
                }
            }
            if (board.IsInCheck())
            {
                bool works = true;
                ChessChallenge.API.Board bird = ChessChallenge.API.Board.CreateBoardFromFEN(board.GetFenString());
                bird.MakeMove(moves[i]);
                ChessChallenge.API.Move[] opponentMates = bird.GetLegalMoves();
                for (int w = 0; w < opponentMates.Length; w++)
                {
                    ChessChallenge.API.Board byrd = ChessChallenge.API.Board.CreateBoardFromFEN(bird.GetFenString());
                    byrd.MakeMove(opponentMates[w]);
                    if (byrd.IsInCheckmate())
                    {
                        works = false;
                        break;
                    }
                }
                if (works)
                {
                    points += 20002;
                }
            }
            movePoints[i] = points;
            points = 0;
        }
        if (board.TrySkipTurn())
        {
            ChessChallenge.API.Move[] opponentMoves = board.GetLegalMoves();
            board.UndoSkipTurn();
            bool over = false;
            for (int w = 0; w < opponentMoves.Length; w++)
            {
                if (over)
                {
                    break;
                }
                board.TrySkipTurn();
                ChessChallenge.API.Board bard = ChessChallenge.API.Board.CreateBoardFromFEN(board.GetFenString());
                board.UndoSkipTurn();
                bard.MakeMove(opponentMoves[w]);
                if (bard.IsInCheckmate())
                {
                    for (int z = 0; z < moves.Length; z++)
                    {
                        ChessChallenge.API.Board bord = ChessChallenge.API.Board.CreateBoardFromFEN(board.GetFenString());
                        bord.MakeMove(moves[z]);
                        ChessChallenge.API.Move[] newOpponentMoves = bord.GetLegalMoves();
                        bool isLegal = false;
                        for (int q = 0; q < newOpponentMoves.Length; q++)
                        {
                            if (newOpponentMoves[q] == opponentMoves[w])
                            {
                                isLegal = true;
                                break;
                            }
                        }
                        if (isLegal)
                        {
                            bord.MakeMove(opponentMoves[w]);
                            if (!bord.IsInCheckmate())
                            {
                                movePoints[z] += 20001;
                                over = true;
                                break;
                            }
                        }
                        else
                        {
                            movePoints[z] += 20001;
                            over = true;
                            break;
                        }
                    }
                }
            }
        }
        for (int i = 0; i < movePoints.Length; i++)
        {
            if (movePoints[i] > currentBestPoints)
            {
                if (board.TrySkipTurn())
                {
                    board.UndoSkipTurn();
                }
                currentBestMove = i;
                currentBestPoints = movePoints[i];
            }
        }
        return moves[currentBestMove];
    }
}