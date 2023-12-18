namespace auto_Bot_305;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_305 : IChessBot
{
    public int[] KingPrefBoard = new int[]
    {4, 5, 1, 1, 1, 4, 5, 4,
     2, 2, 1, 1, 6, 2, 2, 2,
     1, 1, 1, 1, 1, 1, 1, 1,
     1, 1, 1, 1, 1, 1, 1, 1,
     1, 1, 1, 1, 1, 1, 1, 1,
     1, 1, 1, 1, 1, 1, 1, 1,
     1, 1, 1, 1, 1, 1, 1, 1,
     1, 1, 1, 1, 1, 1, 1, 1
     };

    public int[] PawnPrefBoard = new int[]
    {0, 0, 0, 0, 0, 0, 0, 0,
     6, 6, 5, 1, 1, 6, 6, 6,
     0, 3, 4, 4, 4, 4, 3, 0,
     0, 0, 3, 5, 10, 3, 0, 0,
     0, 2, 4, 5, 5, 4, 2, 0,
     2, 3, 3, 4, 4, 3, 3, 2,
     4, 4, 4, 4, 4, 4, 4, 4,
     6, 6, 6, 6, 6, 6, 6, 6
     };


    public int[] PieceValues = { 0, 100, 300, 300, 500, 900, 1000000 };

    public bool BotIsWhite(Board board) { return board.IsWhiteToMove; }

    public Move Think(Board board, Timer timer)
    {
        int depth = 1;
        Move[] AvaiableMoves = board.GetLegalMoves();
        Random rng = new();
        Move PlayMove = AvaiableMoves[rng.Next(AvaiableMoves.Length)];
        foreach (Move checkmatemove in AvaiableMoves)
        {
            if (MoveIsCheckmate(board, checkmatemove, depth, BotIsWhite(board)))
            {
                return checkmatemove;
            }
        }
        PlayMove = MakeGoodMove(board, BotIsWhite(board), depth);
        return PlayMove;
    }
    public int[] Evaluate(Board board, bool BotIsWhite)
    {

        string MyPawnsOnBoard = ConvertDecToBin(board, board.GetPieceBitboard(PieceType.Pawn, BotIsWhite), BotIsWhite);
        string EnemyPawnsOnBoard = ConvertDecToBin(board, board.GetPieceBitboard(PieceType.Pawn, !BotIsWhite), !BotIsWhite);
        string MyKingsOnBoard = ConvertDecToBin(board, board.GetPieceBitboard(PieceType.King, BotIsWhite), BotIsWhite);
        string EnemyKingsOnBoard = ConvertDecToBin(board, board.GetPieceBitboard(PieceType.King, !BotIsWhite), !BotIsWhite);

        int PiecePositionMultiplier = 3;

        int MyMaterial = 0;
        int EnemyMaterial = 0;

        int PositionEvaluation = 0;
        foreach (PieceList MyPieces in board.GetAllPieceLists())
        {
            foreach (Piece MyPiece in MyPieces)
            {
                int PieceWeight = 0;
                switch (MyPiece.PieceType)
                {
                    case PieceType.Pawn:
                        PieceWeight += PieceValues[1];
                        break;
                    case PieceType.Knight:
                        PieceWeight += PieceValues[2];
                        break;
                    case PieceType.Bishop:
                        PieceWeight += PieceValues[3];
                        break;
                    case PieceType.Rook:
                        PieceWeight += PieceValues[4];
                        break;
                    case PieceType.Queen:
                        PieceWeight += PieceValues[5];
                        break;
                    case PieceType.King:
                        PieceWeight += PieceValues[6];
                        break;
                    default:
                        PieceWeight = 0;
                        break;
                }
                if (MyPiece.IsWhite == BotIsWhite)
                {
                    MyMaterial += PieceWeight;
                }
                else
                {
                    EnemyMaterial += PieceWeight;
                }
            }
            if (!BotIsWhite)
            {
                Array.Reverse(PawnPrefBoard);
                Array.Reverse(KingPrefBoard);
            }
            int[] EnemyPawnPrefBoard = PawnPrefBoard;
            Array.Reverse(EnemyPawnPrefBoard);
            int[] EnemyKingPrefBoard = KingPrefBoard;
            Array.Reverse(EnemyKingPrefBoard);

            for (int i = 0; i < MyPawnsOnBoard.Length; i++)
            {
                PositionEvaluation += (Convert.ToInt32(MyPawnsOnBoard[i]) * PawnPrefBoard[i] - Convert.ToInt32(EnemyPawnsOnBoard[i]) * EnemyPawnPrefBoard[i]) * PiecePositionMultiplier;
            }
            for (int i = 0; i < MyKingsOnBoard.Length; i++)
            {
                PositionEvaluation += (Convert.ToInt32(MyKingsOnBoard[i]) * KingPrefBoard[i] - Convert.ToInt32(EnemyKingsOnBoard[i]) * EnemyKingPrefBoard[i]) * PiecePositionMultiplier;
            }
        }
        return new int[] { MyMaterial - EnemyMaterial + PositionEvaluation, MyMaterial, EnemyMaterial };
    }
    string ConvertDecToBin(Board board, ulong number, bool BotIsWhite)
    {
        int remainer = 0;
        string answer = "";
        while (number > 0)
        {
            remainer = (int)number % 2;
            number /= 2;
            answer += Convert.ToString(remainer);
        }
        if (BotIsWhite)
        {
            return answer + String.Concat(Enumerable.Repeat("0", (64 - answer.Length)));
        }
        else
        {
            return Reverse(answer + String.Concat(Enumerable.Repeat("0", (64 - answer.Length))));
        }
    }
    bool MoveIsCheckmate(Board board, Move move, int depth, bool BotisWhite)
    {
        board.MakeMove(move);
        bool IsCheckmate = board.IsInCheckmate();
        board.UndoMove(move);
        return IsCheckmate;
    }
    Move MakeGoodMove(Board board, bool BotisWhite, int depth)
    {
        Move[] LegalMoves = board.GetLegalMoves();
        Random rng = new();
        Move PlayMove = LegalMoves[rng.Next(LegalMoves.Length)];
        depth = depth - 1;
        foreach (Move LegalMove in LegalMoves)
        {
            if (PredictHowGoodMove(board, LegalMove, BotisWhite, depth) < PredictHowGoodMove(board, PlayMove, BotisWhite, depth))
            {
                PlayMove = LegalMove;
            }
        }
        return PlayMove;
    }
    int PredictHowGoodMove(Board board, Move BotMove, bool BotisWhite, int depth)
    {
        int FutureEvaluation;
        int FutureEvaluation2 = Evaluate(board, BotisWhite)[0];
        board.MakeMove(BotMove);
        Move[] LegalMoves = board.GetLegalMoves();
        FutureEvaluation = FutureEvaluation2;
        foreach (Move move in LegalMoves)
        {
            if (depth < 0)
            {
                break;
            }
            FutureEvaluation2 = Evaluate(board, BotisWhite)[0];
            if (FutureEvaluation < FutureEvaluation2)
            {
                FutureEvaluation = FutureEvaluation2;
            }
        }
        board.UndoMove(BotMove);
        return FutureEvaluation;
    }

    public static string Reverse(string s)
    {
        char[] charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }
}
