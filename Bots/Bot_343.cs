namespace auto_Bot_343;
using ChessChallenge.API;
using System;

public class Bot_343 : IChessBot
{
    //test Tuple with public (Move, int) function
    Random random = new Random();

    //private bool i_am_white; //zero black, one white 
    private int GlobalDepth = 3;
    //private int GlobalLimit = 100;
    private int army_weight = 10;
    private int freedom_weight = 3;
    private int power_weight = 100;
    private int fluctuation = 2;

    private readonly int[] piece_values = { 0, 2, 5, 7, 11, 17, 0 };

    private int Evaluate_army_size(Board board)
    {
        int army_size_dom = 0;
        army_size_dom += BitboardHelper.GetNumberOfSetBits(board.WhitePiecesBitboard);
        army_size_dom -= BitboardHelper.GetNumberOfSetBits(board.BlackPiecesBitboard);
        return army_size_dom;
    }

    private int Evaluate_freedom(Board board) //absolute score
    {
        int white_move_mult = (board.IsWhiteToMove ? 1 : -1);
        int freedom_current = board.GetLegalMoves().Length;

        board.ForceSkipTurn();
        int freedom_opponent = board.GetLegalMoves().Length;
        board.UndoSkipTurn();

        return white_move_mult * (freedom_current - freedom_opponent);
    }
    private int Evaluate_avoid(Board board)
    {
        int result = 0;
        int white_move_mult = (board.IsWhiteToMove ? 1 : -1);
        if (board.IsInCheckmate() | board.IsDraw())
        { // | board.IsInStalemate() | board.IsFiftyMoveDraw()
            result = int.MinValue + 100;
        }
        return white_move_mult * result;
    }
    private int Evaluate_power(Board board)
    {
        int result = 0;
        int val = 0;
        int mult = 0;
        PieceList[] piece_info = board.GetAllPieceLists();
        foreach (PieceList list in piece_info)
        {
            foreach (Piece alive in list)
            {
                if (alive.IsWhite) { mult = 1; } else { mult = -1; }
                val += mult * piece_values[(int)alive.PieceType] + mult * alive.Square.Rank;
            }
        }

        return result;
    }

    private int Evaluate_position(Board board) //higher better for white
    {
        int result = 0;
        result += Evaluate_avoid(board);
        if (Math.Abs(result) > 9999999) { return result; }
        result += freedom_weight * Evaluate_freedom(board);
        result += army_weight * Evaluate_army_size(board);
        result += power_weight * Evaluate_power(board);
        result += random.Next(0, fluctuation); // Fluctuation for vriability in gameplay
        return result;
    }

    private (int, Move) Minimax(Board board, int depth, bool maximizingPlayer)
    {
        Move BestMove = new Move(); // = board.GetLegalMoves()[0];
        int BestValue;

        if (depth == 0) //| board.IsInCheckmate()
        {
            int score = Evaluate_position(board);
            return (score, BestMove); //new MoveResult {BestMove = null, BestValue = score};
        }

        if (maximizingPlayer)
        {
            BestValue = int.MinValue;
            foreach (Move child in board.GetLegalMoves())
            {
                board.MakeMove(child);
                var (LocalValue, _) = Minimax(board, depth - 1, !maximizingPlayer);
                if (LocalValue > BestValue)
                {
                    BestValue = LocalValue;
                    BestMove = child;
                }
                board.UndoMove(child);
                if (BestValue > 999999)
                {
                    return (BestValue, BestMove);
                }
            }
        }
        else
        {
            BestValue = int.MaxValue;
            foreach (Move child in board.GetLegalMoves())
            {
                board.MakeMove(child);
                var (LocalValue, LocalMove) = Minimax(board, depth - 1, !maximizingPlayer);
                if (LocalValue < BestValue)
                {
                    BestValue = LocalValue;
                    BestMove = child;
                }
                board.UndoMove(child);
            }
        }
        return (BestValue, BestMove);
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        foreach (Move move in allMoves)
        {
            if (MoveIsCheckmate(board, move))
            {
                Move moveToPlay = move;
                return move;
            }
        }

        if (BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) < 16)
        {
            GlobalDepth = 4;
        }
        if (BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) < 6)
        {
            GlobalDepth = 5;
        }
        bool i_am_white = board.IsWhiteToMove;
        var (_, final_move) = Minimax(board, GlobalDepth, i_am_white); //.Item1

        if (final_move == null)
        {
            final_move = allMoves[0];
        }
        return final_move;
    }
}