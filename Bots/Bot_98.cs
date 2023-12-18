namespace auto_Bot_98;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_98 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        var goodmoves = moves.ToList();


        System.Random random = new();

        bool wantdraw;


        int whitepieces = 0;
        int blackpieces = 0;
        int PieceDifference = 0;




        var Oppmoves = board.GetLegalMoves().ToList();

























        bool botiswhite = true;
        bool onetimelock = true;


        //this tells the bot if it is playing as black or white
        if (board.IsWhiteToMove && onetimelock)
        {
            botiswhite = true;
            onetimelock = false;
        }

        if (board.IsWhiteToMove == false && onetimelock)
        {
            botiswhite = false;
            onetimelock = false;
        }

        //this works



        //evaluates position by counting material
        for (int i = 0; i < board.GetPieceList(PieceType.Pawn, white: true).Count + board.GetPieceList(PieceType.Bishop, white: true).Count * 3 + board.GetPieceList(PieceType.Knight, white: true).Count * 3 + board.GetPieceList(PieceType.Rook, white: true).Count * 5 + board.GetPieceList(PieceType.Queen, white: true).Count * 9; i++)
        {
            whitepieces += 100;
        }

        for (int i = 0; i < board.GetPieceList(PieceType.Pawn, white: false).Count + board.GetPieceList(PieceType.Bishop, white: false).Count * 3 + board.GetPieceList(PieceType.Knight, white: false).Count * 3 + board.GetPieceList(PieceType.Rook, white: false).Count * 5 + board.GetPieceList(PieceType.Queen, white: false).Count * 9; i++)
        {
            blackpieces += 100;
        }




















        if (botiswhite && whitepieces < blackpieces || botiswhite == false && whitepieces > blackpieces)
        {
            wantdraw = true;
        }
        else
        {
            wantdraw = false;
        }
        //if you have less material then wantdraw = true








        //makes the first move e4/e5
        if (board.PlyCount <= 1)
        {
            return moves[16];
        }


        //this looks for mate in one

        for (int i = 0; i < board.GetLegalMoves().Length; i++)
        {
            board.MakeMove(moves[i]);


            if (board.IsInCheckmate())
            {
                return moves[i];
            }

            board.UndoMove(moves[i]);


        }










        //this makes a seperate list of moves that dont end in a draw (when you are winning) called goodmoves
        moves = board.GetLegalMoves();
        goodmoves = moves.ToList();
        int j = 0;
        for (int i = 0; i < board.GetLegalMoves().Length; i++)
        {
            board.MakeMove(moves[i]);
            if (board.IsDraw())
            {
                goodmoves.RemoveAt(i - j);
                j++;
            }
            board.UndoMove(moves[i]);
        }


        var bettermoves = goodmoves.ToList();



        if (wantdraw == false)
        {
            int z = 0;
            for (int i = 0; i < goodmoves.Count; i++)
            {
                board.MakeMove(goodmoves[i]);
                Oppmoves = board.GetLegalMoves().ToList();
                for (int k = 0; k < Oppmoves.Count; k++)
                {


                    board.MakeMove(Oppmoves[k]);//opp makes a move (player)

                    if (botiswhite)
                    {
                        PieceDifference = whitepieces - blackpieces;
                    }
                    else
                    {
                        PieceDifference = blackpieces - whitepieces;
                    }



                    if (wantdraw == true || board.IsDraw() || botiswhite && whitepieces < blackpieces || botiswhite == false && whitepieces > blackpieces)
                    {
                        bettermoves.RemoveAt(i - z);
                        z++;
                        board.UndoMove(Oppmoves[k]);
                        break;
                    }


                    board.UndoMove(Oppmoves[k]);



                }
                board.UndoMove(goodmoves[i]);
            }


        }


























        //this ensures that pawns always promote to queens
        //this will favor a capture with a promotion rather than a promotion//not working???//update worked but tested twice
        for (int i = 0; i < bettermoves.Count; i++)
        {
            if (bettermoves[i].PromotionPieceType == PieceType.Queen && bettermoves[i].IsCapture)//if its a promotion to a queen
            {
                return bettermoves[i];
            }

        }
        for (int i = 0; i < bettermoves.Count; i++)
        {
            if (bettermoves[i].PromotionPieceType == PieceType.Queen)
            {
                return bettermoves[i];
            }
        }











        //command to checkmate with a queen

        // not found ;(















        //plays a capture if there is no M1 or promotion
        for (int i = 0; i < bettermoves.Count; i++)
        {
            if (board.IsInCheck())
            {
                board.MakeMove(bettermoves[i]);
                if (board.IsInCheck() == false && bettermoves[i].IsCapture)
                {
                    return bettermoves[i];
                }
                board.UndoMove(bettermoves[i]);

            }
        }


        for (int i = 0; i < bettermoves.Count; i++)
        {
            if (bettermoves[i].IsCapture && board.IsInCheck() == false)
            {
                return bettermoves[i];
            }

        }



        //the bot will draw in one if it has less material
        if (wantdraw == true)
        {
            for (int i = 0; i < board.GetLegalMoves().Length; i++)
            {
                board.MakeMove(moves[i]);
                if (board.IsDraw())
                {
                    return moves[i];
                }
                board.UndoMove(moves[i]);
            }
        }







        //this plays a random move if everymove is a draw
        //this will result in an error if 50 move rule so this is will play move 0 in that case




        try
        {
            if (bettermoves != null)
            {
                return bettermoves[random.Next(bettermoves.Count)];
            }
        }
        catch (Exception)
        {
            return moves[0];
        }
        return moves[0];


        //else play a random move



    }
}


