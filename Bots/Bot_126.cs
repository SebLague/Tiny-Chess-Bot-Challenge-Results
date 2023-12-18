namespace auto_Bot_126;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_126 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {

        //get colour
        bool isWhite = board.IsWhiteToMove;
        //find good short term move
        Move[] moves = board.GetLegalMoves();
        List<MoveOption> posibleMoves = new List<MoveOption>();
        foreach (Move move in moves)
        {
            MoveOption moveOption = GetMoveInfo(move, board);
            if (moveOption.Check || moveOption.CheckMate || (moveOption.TakePieceDiffrence != 100))
            {
                posibleMoves.Add(moveOption);
            }

        }
        //get the kings location
        Square enemyKing = board.GetKingSquare(!(isWhite));
        //work backwards from this location to get the moves needed to get to the king for the keypieces and how safe the moves are 
        PieceType[] attackingPieces = new PieceType[] { PieceType.Knight, PieceType.Rook, PieceType.Bishop, PieceType.Pawn, PieceType.Queen };
        List<MoveOption> posibleAttacks = new List<MoveOption>();
        foreach (PieceType type in attackingPieces)
        {
            try
            {
                (Move move, int totalMoves) = findPath(enemyKing, board, isWhite, type);
                MoveOption moveOption = GetMoveInfo(move, board);
                moveOption.MovesToWin = totalMoves;
                if (moves.Contains(move)) posibleAttacks.Add(moveOption);//make sure it is legal and e.g. dose not put us in check

            }
            catch
            {
                //there is not path           
            }
        }
        //dicide what is the best move to take out of found moves and the best moves in each catagory
        MoveOption bestSafeTake = new MoveOption();
        MoveOption safeCheck = new MoveOption();
        MoveOption bestTake = new MoveOption();

        foreach (MoveOption move in posibleMoves)
        {
            //if there is a move in checkMate make that move
            if (move.CheckMate) return move.Move;

            //check take 
            if (bestSafeTake.DangerLevel >= move.DangerLevel && move.TakePieceDiffrence >= bestSafeTake.TakePieceDiffrence) bestSafeTake = move;

            if (move.TakePieceDiffrence >= bestTake.TakePieceDiffrence) bestTake = move;

            // check check
            if (safeCheck.DangerLevel >= move.DangerLevel && (move.Check || !safeCheck.Check)) safeCheck = move;

        }
        MoveOption SafeAttack = new MoveOption();
        MoveOption ClosesAttack = new MoveOption();
        MoveOption SafeClosesAttack = new MoveOption();
        foreach (MoveOption move in posibleAttacks)
        {
            //safe
            if (move.DangerLevel < SafeAttack.DangerLevel) SafeAttack = move;

            //close
            if (move.MovesToWin < ClosesAttack.MovesToWin) ClosesAttack = move;

            //both
            if (move.DangerLevel <= SafeClosesAttack.DangerLevel && move.MovesToWin <= SafeClosesAttack.MovesToWin) SafeClosesAttack = move;

        }

        //play the best move found if no good moves are found make a random one 
        if (SafeAttack.MovesToWin < 4 && SafeAttack.DangerLevel == DangerLevel.None) return SafeAttack.Move;


        if (ClosesAttack.DangerLevel < DangerLevel.High) return ClosesAttack.Move;



        if (bestSafeTake.TakePieceDiffrence >= -1 && bestSafeTake.DangerLevel < DangerLevel.High) return bestSafeTake.Move;

        if (safeCheck.DangerLevel < DangerLevel.High) return safeCheck.Move;



        //if there is an unsafe attack see if there is a safe way for a piece to defend it 


        foreach (PieceType type in attackingPieces)
        {
            try
            {
                (Move move, int totalMoves) = findPath(ClosesAttack.Move.TargetSquare, board, isWhite, type);
                MoveOption moveOption = GetMoveInfo(move, board);
                if (moves.Contains(move) && moveOption.DangerLevel < DangerLevel.High && move.StartSquare != ClosesAttack.Move.StartSquare) return move;//make sure it is legal and e.g. dose not put us in check

            }

            catch
            {
                //there is not path           
            }
        }
        if (SafeAttack.MovesToWin < 10 && SafeAttack.DangerLevel <= DangerLevel.Protected) return SafeAttack.Move;

        if (bestTake.TakePieceDiffrence >= 0) return bestTake.Move;


        Random rand = new Random();
        return moves[rand.Next(0, moves.Length)];


    }

    public (Move, int) findPath(Square enemyKing, Board board, bool isWhite, PieceType typeOfPiece) //return the starting move and how long it should take to get there
    {

        Dictionary<Square, moveData> allsquares = new Dictionary<Square, moveData>();
        allsquares[enemyKing] = new moveData(0, new Square());

        Queue<Square> queue = new Queue<Square>();
        Square currentSquare = enemyKing;
        Square LastSquare = new Square();
        while (true)
        {

            moveData currentData = allsquares[currentSquare];
            //get the bitboard of moving depending on the type of piece
            ulong attacks = 0;
            switch (typeOfPiece)
            {
                case PieceType.Knight:
                    attacks = BitboardHelper.GetKnightAttacks(currentSquare);
                    break;
                case PieceType.Queen:
                case PieceType.Rook:
                case PieceType.Bishop:
                    attacks = BitboardHelper.GetSliderAttacks(typeOfPiece, currentSquare, board);
                    break;
                case PieceType.Pawn:
                    attacks = BitboardHelper.GetPawnAttacks(currentSquare, isWhite);
                    break;
                default:
                    break;

            }


            while (BitboardHelper.GetNumberOfSetBits(attacks) > 0)
            {
                int index = BitboardHelper.ClearAndGetIndexOfLSB(ref attacks);
                Square possibleSquare = new Square(index);
                //check if it is a destination square
                Piece pieceinSquare = board.GetPiece(possibleSquare);
                if (pieceinSquare.PieceType == typeOfPiece && isWhite == pieceinSquare.IsWhite)
                {
                    //found way back to original piece 

                    //create the move
                    Move move = new Move(possibleSquare.Name + currentSquare.Name, board);

                    //return the move info
                    return (move, currentData.Depth + 1);
                }

                // if not esists and it is a valid square add it to all squares and queue

                else if (!allsquares.ContainsKey(possibleSquare) && (board.GetPiece(possibleSquare).IsWhite != isWhite || board.GetPiece(possibleSquare).PieceType == PieceType.None))
                {
                    allsquares[possibleSquare] = new moveData(currentData.Depth + 1, LastSquare);
                    queue.Enqueue(possibleSquare);
                }
            }

            LastSquare = currentSquare;
            currentSquare = queue.Dequeue();
            //if to deep stop


        }
        //if not found a way back 
        throw new Exception();
    }
    public struct moveData
    {
        public int Depth;
        public Square preSquare; //index in the allsquares list

        public moveData(int depth, Square previ)
        {
            Depth = depth;
            preSquare = previ;
        }

    }
    public struct MoveOption
    {
        public int MovesToWin;
        public DangerLevel DangerLevel;
        public bool CheckMate;
        public bool Check;
        public int TakePieceDiffrence;
        public Move Move;

        public MoveOption(int movesToWin, DangerLevel dangerLevel, Move move, bool checkMate, bool check, int takePieceDiffrence)
        {
            MovesToWin = movesToWin;
            DangerLevel = dangerLevel;
            Move = move;
            CheckMate = checkMate;
            Check = check;
            TakePieceDiffrence = takePieceDiffrence;
        }
        public MoveOption()
        {
            MovesToWin = 100;
            DangerLevel = DangerLevel.High;
            Move = new Move();
            Check = false;
            CheckMate = false;
            TakePieceDiffrence = -100;

        }

    }
    public MoveOption GetMoveInfo(Move move, Board board) //returns the danger of a move and if it puts the other team in check or check mate. and if a piece if taken the diffrence between the pieces values
    {
        //get piece values diffrence
        int value = 100;//value to show that there is no piece to be taken
        Piece takenPiece = board.GetPiece(move.TargetSquare);
        if (!takenPiece.IsNull)//if a piece is taken
        {
            Piece takingPiece = board.GetPiece(move.StartSquare);
            value = takenPiece.PieceType - takingPiece.PieceType;
        }
        DangerLevel moveDanger = DangerLevel.None;
        bool attackable = false;
        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            attackable = true;
        }
        //make the move
        board.MakeMove(move);


        //if can be attacked see if its protected protected
        if (attackable)
        {
            if (board.SquareIsAttackedByOpponent(move.TargetSquare)) moveDanger = DangerLevel.Protected;

            else moveDanger = DangerLevel.High;

        }
        //see if the move puts oponent in check
        bool check = board.IsInCheck();
        //see if the move puts oponent in check mate
        bool checkMate = board.IsInCheckmate();
        //return the board
        board.UndoMove(move);


        return new MoveOption(-1, moveDanger, move, checkMate, check, value);
    }

    public enum DangerLevel
    {
        None,
        Protected,
        High
    }



}