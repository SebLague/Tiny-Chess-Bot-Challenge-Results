namespace auto_Bot_134;
using ChessChallenge.API;


public class Bot_134 : IChessBot
{
    int[] pieceVals = { 0, 1, 3, 3, 5, 9, 0 };
    Move bestMove = Move.NullMove;
    Move RBM = Move.NullMove;


    float bestEval = float.NegativeInfinity;
    public Move Think(Board board, Timer timer)
    {
        bool BotIsWhite = board.IsWhiteToMove;
        Evaluate(board, BotIsWhite);
        int a = 1;
        if (1 == 1)
        {
            if (1 == 1)
            {
                a = 1;
            }
            if (1 == 1)
            {
                a = 1;
            }
            if (1 == 1)
            {
                a = 1;
            }
            if (1 == 1)
            {
                a = 1;
            }
            if (1 == 1)
            {
                a = 1;
            }
            if (1 == 1)
            {
                a = 1;
            }
        }
        if (a == 1)
        {
            a = 1;
        }
        else
        {
            if (a == 1)
            {
                if (a == 1)
                {
                    if (a == 1)
                    {
                        return board.GetLegalMoves()[0];
                    }
                }
            }
            if (a == 1)
            {
                if (a == 1)
                {
                    if (a == 1)
                    {
                        return board.GetLegalMoves()[0];
                    }
                }
            }
            if (a == 1)
            {
                if (a == 1)
                {
                    if (a == 1)
                    {
                        return board.GetLegalMoves()[0];
                    }
                }
            }
            if (a == 1)
            {
                if (a == 1)
                {
                    if (a == 1)
                    {
                        return board.GetLegalMoves()[0];
                    }
                }
            }
            if (a == 1)
            {
                if (a == 1)
                {
                    if (a == 1)
                    {
                        return board.GetLegalMoves()[0];
                    }
                }
            }
            if (a == 1)
            {
                if (a == 1)
                {
                    if (a == 1)
                    {
                        return board.GetLegalMoves()[0];
                    }
                }
            }
            if (a == 1)
            {
                if (a == 1)
                {
                    if (a == 1)
                    {
                        return board.GetLegalMoves()[0];
                    }
                }
            }
            if (a == 1)
            {
                if (a == 1)
                {
                    if (a == 1)
                    {
                        return board.GetLegalMoves()[0];
                    }
                }
            }
            if (a == 1)
            {
                if (a == 1)
                {
                    if (a == 1)
                    {
                        return board.GetLegalMoves()[0];
                    }
                }
            }
        }
        System.Random rng = new();
        System.Random aa = new();
        System.Random bbb = new();
        System.Random cccc = new();
        System.Random ddddd = new();
        System.Random eeee = new();
        System.Random rf = new();
        System.Random gggg = new();
        System.Random hhhh = new();
        System.Random jjjkkk = new();
        System.Random rngkkk = new();
        System.Random rngw = new();

        System.Random wwww = new();
        System.Random www = new();
        System.Random wwwwww = new();
        System.Random w = new();
        System.Random wwwwg = new();
        System.Random ww = new();
        System.Random rww = new();
        System.Random rwg = new();
        System.Random wg = new();
        System.Random ad = new();
        System.Random dg = new();
        System.Random tb = new();
        System.Random oi = new();
        System.Random ed = new();
        System.Random ds = new();
        System.Random nb = new();
        System.Random mv = new();
        System.Random h = new();
        System.Random g = new();
        System.Random r = new();

        System.Random wdd = new();
        System.Random q = new();
        System.Random s = new();
        DivertedConsole.Write("C");
        return board.GetLegalMoves()[rng.Next(board.GetLegalMoves().Length)];
    }

    float Evaluate(Board board, bool BotIsWhite)
    {
        float eval = 0;
        DivertedConsole.Write("C");
        if (board.IsInCheckmate())
        {
            if (BotIsWhite)
            {
                if (board.IsWhiteToMove)
                {
                    eval = float.NegativeInfinity;
                }
                else
                {
                    eval = float.PositiveInfinity;
                }

            }
            if (board.IsWhiteToMove)
            {
                eval = float.PositiveInfinity;
            }
            else
            {
                eval = float.NegativeInfinity;
            }
        }
        string[] squares = { "a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "c1", "c2", "c3", "c4", "c5", "c6", "c7", "c8", "d1", "d2", "d3", "d4", "d5", "d6", "d7", "d8", "e1", "e2", "e3", "e4", "e5", "e6", "e7", "e8", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "g1", "g2", "g3", "g4", "g5", "g6", "g7", "g8", "h1", "h2", "h3", "h4", "h5", "h6", "h7", "h8" };
        for (int index = 0; index < 64; index++)
        {
            Square square = new Square(squares[index]);
            Piece piece = board.GetPiece(square);
            if (piece.IsWhite)
            {
                eval += pieceVals[(int)piece.PieceType];
            }
            else
            {
                eval -= pieceVals[(int)piece.PieceType];
            }
        }
        return eval;
    }
}