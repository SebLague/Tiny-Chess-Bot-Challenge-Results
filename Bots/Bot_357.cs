namespace auto_Bot_357;
using ChessChallenge.API;
using System;

public class Bot_357 : IChessBot
{
    public int turn = 0;
    public int tillFinish = 10000;
    public string opening;
    public string[] openMoves;
    public string[] White_openings =
    {
        "g2g3 f1g2 d2d3 b1d2 c2c4", //ok
        "b1c3 g1f3 e2e4 h2h3",
        "b2b4 c1b2 b2e5 a2a3 d2d3 e5b2",
        "g1h3 g2g3 f2f3 h3f2"
    };
    public string[] Black_openings =
    {
        "g7g6 f8g7 c7c5 d8c7 c7c5",
        "c7c5 e7e6 b8c6 g8f6 d7d5",
        "d7d5 e7e6 c7c5 d5d4 e6e5",
        "d7d5 d5d4 e7e5 c7c5"
    };
    Random rnd = new Random();
    public Move Think(Board b, Timer t)
    {
        turn++;
        if (turn == 1)
        {
            // setup opening
            if (b.IsWhiteToMove)
            {
                opening = White_openings[rnd.Next(0, White_openings.Length - 1)];
            }
            else
            {
                opening = Black_openings[rnd.Next(0, Black_openings.Length - 1)];
            }

            openMoves = opening.Split(" ");
            tillFinish = openMoves.Length;
        }

        bool isWhite = b.IsWhiteToMove;
        Move m = new Move();
        Move[] moves = b.GetLegalMoves();

        var values = Enum.GetValues(typeof(PieceType));
        PieceType danger = new PieceType();
        foreach (PieceType pieceType in values)
        {
            PieceList pList = b.GetPieceList(pieceType, !isWhite); // wow
            PieceList pMeList = b.GetPieceList(pieceType, isWhite);
            if (pList != null)
            {
                foreach (Piece oof in pMeList)
                {
                    foreach (Piece p in pList)
                    {
                        ulong checkBits = 0;
                        if (pieceType == PieceType.Bishop)
                            checkBits = BitboardHelper.GetSliderAttacks(pieceType, p.Square, b);
                        if (pieceType == PieceType.Knight)
                            checkBits = BitboardHelper.GetKnightAttacks(p.Square);
                        if (pieceType == PieceType.Pawn)
                            checkBits = BitboardHelper.GetPawnAttacks(p.Square, !isWhite);
                        BitboardHelper.VisualizeBitboard(checkBits);
                        //Console.ReadKey();
                        //ulong wack = goof & checkBits;
                        if (BitboardHelper.SquareIsSet(checkBits, oof.Square))
                        {
                            if (oof.PieceType > danger)
                            {
                                danger = oof.PieceType;
                            }
                        }

                    }
                }
            }
        }
        if (danger != PieceType.None)
        {
            Move trash = new Move();
            Move kill = new Move();
            foreach (Move yeah in moves)
            {
                if (yeah.MovePieceType == danger)
                {
                    if (yeah.IsCapture) { kill = yeah; }
                    trash = yeah;
                }
            }
            if (!kill.IsNull) return kill;
            return trash;
        }
        Move bestCapture = new Move();
        foreach (Move move in moves)
        {
            if (move.CapturePieceType != PieceType.None)
            {
                if (move.CapturePieceType > bestCapture.CapturePieceType)
                {
                    // do some tests
                    bool bad = false;
                    b.MakeMove(move);
                    var values2 = Enum.GetValues(typeof(PieceType));
                    foreach (PieceType pieceType in values2)
                    {
                        PieceList pList = b.GetPieceList(pieceType, !isWhite); // wow
                        if (pList != null)
                        {
                            foreach (Piece p in pList)
                            {
                                ulong checkBits = 0;
                                if (pieceType == PieceType.Bishop)
                                    checkBits = BitboardHelper.GetSliderAttacks(pieceType, p.Square, b);
                                if (pieceType == PieceType.Knight)
                                    checkBits = BitboardHelper.GetKnightAttacks(p.Square);
                                if (pieceType == PieceType.Pawn)
                                    checkBits = BitboardHelper.GetPawnAttacks(p.Square, !isWhite);
                                //ulong wack = goof & checkBits;
                                if (BitboardHelper.SquareIsSet(checkBits, move.TargetSquare) && move.CapturePieceType < move.MovePieceType)
                                {
                                    bad = true;
                                }

                            }
                        }
                    }

                    if (!bad)
                        bestCapture = move;
                    b.UndoMove(move);
                }
            }
            // Test checkmate
            b.MakeMove(move);
            if (b.IsInCheckmate())
            {
                return move;
            }
            b.UndoMove(move);
        }
        m = bestCapture;

        if (m.IsNull)
        {
            foreach (Move j in moves)
            {
                b.MakeMove(j);
                if (!b.IsInStalemate() && !b.IsInsufficientMaterial() && !b.IsFiftyMoveDraw() && !b.IsRepeatedPosition() && !b.IsDraw())
                {
                    m = j;
                }
                b.UndoMove(j);
            }
            if (m.IsNull)
            {
                m = moves[rnd.Next(0, moves.Length - 1)];
            }

            int checkTimes = 0;

            // check for promotion
            if (b.GetPieceList(PieceType.Pawn, !isWhite).Count < 2)
            {
                foreach (Move mo in moves)
                {
                    if (mo.MovePieceType == PieceType.Pawn && mo.StartSquare.Index < mo.TargetSquare.Index)
                    {
                        return mo;
                    }
                }
            }
            // Use queens
            if (b.GetPieceList(PieceType.Queen, isWhite).Count > 1)
            {
                foreach (Move mo in moves)
                {
                    if (mo.MovePieceType == PieceType.Queen && mo.TargetSquare.Index == b.GetPieceList(PieceType.King, !isWhite)[0].Square.Index)
                    {
                        return mo;
                    }
                }
            }

            if (turn < tillFinish && !b.IsInCheck())
            {
                Move goof = new Move(openMoves[turn - 1], b);
                foreach (Move d in moves)
                {
                    if (d.Equals(goof))
                    {
                        return goof;
                    }
                }
            }
        }

        return m;
    }
}