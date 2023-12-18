namespace auto_Bot_175;
using ChessChallenge.API;
using System.Collections.Generic;
using System.Linq;

public class Bot_175 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] legalMoves = board.GetLegalMoves();
        if (legalMoves.Length == 1) return legalMoves.First();

        bool botPlayWhite = board.IsWhiteToMove;
        int maxDeep = GetMaxDeep();
        List<(Move move, int score, bool targetSquareIsAttacked, bool isCapure)> scoredMoves = new();
        _ = NegaAlphaBeta(maxDeep, 0, -int.MaxValue, int.MaxValue, 1);
        int bestScore = scoredMoves.Max(x => x.score);
        List<Move> bestMoves = scoredMoves.Where(x => x.score == bestScore).Select(x => x.move).ToList();
        List<Move> moves = bestMoves.Where(m => m.IsCapture).ToList();
        if (moves.Count == 0) moves = bestMoves.Where(m => !m.IsCapture).ToList();
        return moves.First();

        #region Local methods
        int GetMaxDeep()
        {
            int remainingPieces = board.GetAllPieceLists().SelectMany(l => l).Count();
            int result = remainingPieces switch
            {
                < 4 => 6,
                < 10 => 5,
                < 20 => 4,
                _ => 3,
            };
            if (timer.MillisecondsRemaining < 5_000) result = 2;
            else if (timer.MillisecondsRemaining < 10_000) result = 3;
            return result;
        }

        //https://www.frayn.net/beowulf/theory.html
        //https://en.wikipedia.org/wiki/Negamax
        int NegaAlphaBeta(int deep, int addedDeep, int alpha, int beta, int color)
        {
            int bestScore = -int.MaxValue;
            int moveScore;
            int newDeep = deep - 1;
            int newAddedDeep = 0;
            if (board.GetLegalMoves().Length < 4 && timer.MillisecondsRemaining > 20_000) { newDeep++; newAddedDeep++; }

            foreach (Move legalMove in OrderedLegalMoves())
            {
                board.MakeMove(legalMove);
                {
                    if (board.IsInCheckmate()) moveScore = Evaluation.checkMate - maxDeep - addedDeep + deep;
                    else if (board.IsDraw()) moveScore = 0;
                    else if (newDeep < 0) moveScore = color * Evaluation.Position(board, legalMove, botPlayWhite);
                    else moveScore = -NegaAlphaBeta(newDeep, addedDeep + newAddedDeep, -beta, -alpha, -color);
                }
                board.UndoMove(legalMove);

                if (moveScore >= bestScore)
                {
                    bestScore = moveScore;
                    if (deep == maxDeep + addedDeep) scoredMoves.Add((legalMove, bestScore, board.SquareIsAttackedByOpponent(legalMove.TargetSquare), legalMove.IsCapture));
                }
                if (bestScore > alpha) alpha = bestScore;
                if (alpha >= beta) break;
            }
            return bestScore;

            #region Local methods
            Move[] OrderedLegalMoves()
            {
                List<Move> strongCaptures = new();
                List<Move> weakCaptures = new();
                List<Move> castlesBreaks = new();
                List<Move> otherMoves = new();
                foreach (Move move in board.GetLegalMoves())
                {
                    if (move.IsCapture)
                    {
                        if (move.CapturePieceType > move.MovePieceType || move.MovePieceType == PieceType.King) strongCaptures.Add(move);
                        else weakCaptures.Add(move);
                        continue;
                    }
                    if ((move.MovePieceType == PieceType.King && !move.IsCastles) || move.MovePieceType == PieceType.Rook)
                    {
                        if (board.HasKingsideCastleRight(board.IsWhiteToMove) || board.HasQueensideCastleRight(board.IsWhiteToMove))        //To be improved
                        {
                            castlesBreaks.Add(move);
                            continue;
                        }
                    }
                    otherMoves.Add(move);
                }
                return strongCaptures.Concat(weakCaptures).Concat(otherMoves).Concat(castlesBreaks).ToArray();
            }
            #endregion
        }
        #endregion
    }

    private static class Evaluation
    {
        public static int checkMate = 2_000_000;
        private static int[] pieceValues = { 0, 100, 300, 350, 525, 1_000, 10_000 };    // null, pawn, knight, bishop, rook, queen, king
        private static int[] passedPawns = { 0, 100, 120, 160, 240, 400, 800 };         // rank
        private static List<Piece> pawns;

        public static int Position(Board board, Move move, bool botPlayWhite)
        {
            pawns = board.GetAllPieceLists().Where(x => x.TypeOfPieceInList == PieceType.Pawn).SelectMany(x => x).ToList();
            return PiecesValue(board, botPlayWhite) + Promotion(move, botPlayWhite) + Mobility(board, botPlayWhite);

            #region Local methods
            static int PiecesValue(Board board, bool botPlayWhite)
            {
                int whitePiecesValue = 0;
                int blackPiecesValue = 0;
                foreach (PieceList pieces in board.GetAllPieceLists().Where(x => x.Count > 0 && x.TypeOfPieceInList != PieceType.King))
                {
                    if (pieces.IsWhitePieceList) whitePiecesValue += GetPiecesValue(pieces, board);
                    else blackPiecesValue += GetPiecesValue(pieces, board);
                }
                return (whitePiecesValue - blackPiecesValue) * (botPlayWhite ? 1 : -1);

                static int GetPiecesValue(PieceList pieces, Board board)
                {
                    if (pieces.TypeOfPieceInList == PieceType.Pawn)
                    {
                        int value = 0;
                        foreach (Piece piece in pieces)
                        {
                            value += GetPieceValue(piece, pawns);
                        }
                        return value;
                    }
                    return GetPieceValue(pieces.First(), pawns) * pieces.Count;
                }
            }

            static int Promotion(Move move, bool botPlayWhite) => move.IsPromotion ? pieceValues[(int)move.PromotionPieceType] * (move.TargetSquare.Rank == 7 && botPlayWhite ? 1 : -1) : 0;

            static int Mobility(Board board, bool botPlayWhite)
            {
                int mobility = board.GetLegalMoves().Length;
                if (board.TrySkipTurn())
                {
                    mobility -= board.GetLegalMoves().Length;
                    board.UndoSkipTurn();
                }
                else return 0; // must be improved
                return mobility * ((botPlayWhite && board.IsWhiteToMove) || (!botPlayWhite && !board.IsWhiteToMove) ? 1 : -1);
            }

            static int GetPieceValue(Piece piece, List<Piece> pawns)
            {
                if (piece.PieceType == PieceType.Pawn)
                {
                    int relativeRank = piece.IsWhite ? piece.Square.Rank : 7 - piece.Square.Rank;
                    bool passedPawn = pawns.Where(x => x.Square.File == piece.Square.File).Count() == 1;    //must be improved
                    return passedPawn ? passedPawns[relativeRank] : pieceValues[1];
                }
                return pieceValues[(int)piece.PieceType];
            }
            #endregion
        }
    }
}