namespace auto_Bot_291;
using ChessChallenge.API;

public class Bot_291 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    private Move[] bestMove;// best move found so far
    private Board board;// board
    private bool side;// side bot is playing on


    public Move Think(Board board, Timer timer)
    {
        int depth = 4;
        bestMove = new Move[depth];// is array to ensure chess bot does not return illegal moves
        this.board = board;
        side = board.IsWhiteToMove;
        //DivertedConsole.Write("SEARCH");
        recFunct(depth - 1);
        return bestMove[depth - 1];
    }

    private int recFunct(int depth)
    {
        Move[] allMoves = board.GetLegalMoves();
        int ev = 0;
        int bestEval = 0;
        bool first = true; // to stop cases where previous move is played

        foreach (Move move in allMoves)//loop through moves
        {
            board.MakeMove(move);
            ev = eval();
            if (side == board.IsWhiteToMove)
            {
                ev += allMoves.Length;
            }
            if (depth > 0)
            {
                ev -= recFunct(depth - 1);// do next move
            }
            if (ev > bestEval || first)// if better make best move
            {
                bestEval = ev;
                bestMove[depth] = move;
                first = false;

            }
            board.UndoMove(move);
        }

        return bestEval;
    }
    private int eval()
    {
        int eval = 0;

        bool thisside = board.IsWhiteToMove;// count materials
        eval += countMaterials(!thisside) - countMaterials(thisside);
        if (board.IsInCheckmate())// checkmates and checks
        {
            return 100000;
        }
        else if (board.IsInCheck())
        {
            eval += 50;
        }

        return eval;
    }
    // count materials
    private int countMaterials(bool side)
    {
        int eval = 0;
        eval += board.GetPieceList(PieceType.Queen, side).Count * 900;
        eval += board.GetPieceList(PieceType.Rook, side).Count * 500;
        eval += board.GetPieceList(PieceType.Bishop, side).Count * 300;
        eval += board.GetPieceList(PieceType.Knight, side).Count * 300;
        eval += board.GetPieceList(PieceType.Pawn, side).Count * 100;

        return eval;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}