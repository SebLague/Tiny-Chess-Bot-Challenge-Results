namespace auto_Bot_399;
using ChessChallenge.API;
using System.Linq;

public class Bot_399 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        return ThinkOnePosition(board, true).Item1;
    }

    (Move, float) ThinkOnePosition(Board board, bool OwnMove, int depth = 0)
    {
        // We will from this select the move with the best possible value
        float bestMoveValue = OwnMove ? float.MinValue : float.MaxValue;
        Move bestMove = Move.NullMove;
        foreach (Move move in board.GetLegalMoves())
        {

            // continue early if move is stupid
            if (board.PlyCount < 4 && move.MovePieceType != PieceType.Pawn)
                continue;
            if (board.PlyCount < 21 && move.MovePieceType == PieceType.King && !move.IsCastles && !board.IsInCheck())
                continue;

            board.MakeMove(move);

            // return early if this move is checkmate
            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                bestMoveValue = OwnMove ? float.MaxValue : float.MinValue;
                return (move, bestMoveValue);
            }
            // Check what the best possible counter move is by the opponent
            float MoveValue = board.IsDraw() ? 0 : (depth < 1) ? ThinkOnePosition(board, !OwnMove, depth + 1).Item2 : GetBoardValue(board, board.IsWhiteToMove);

            board.UndoMove(move);
            // Using XOR for some complicated logic
            // If it is our move, we are checking the countermoves of the opponent, who will take the worst one
            // If it is the opponents move, we are checking our countermoves, and will take the best one
            if (OwnMove ^ MoveValue < bestMoveValue)
            {
                bestMoveValue = MoveValue;
                bestMove = move;
                //Log($"      Value: {cMoveValue} (New worst)", false, System.ConsoleColor.Cyan);
                //Log($"      New move ({move}) at depth {depth} with value: {MoveValue}", false, System.ConsoleColor.Cyan);
            }
        }
        return (bestMove, bestMoveValue);
    }
    float[] positionModifiers =
        { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f,
          1.0f, 1.0f, 1.3f, 1.3f, 1.3f, 1.3f, 1.0f, 1.0f,
          1.3f, 1.45f, 1.6f, 1.6f, 1.6f, 1.6f, 1.45f, 1.3f,
          1.3f, 1.45f, 1.6f, 1.8f, 1.8f, 1.6f, 1.45f, 1.3f,
          1.3f, 1.45f, 1.6f, 1.8f, 1.8f, 1.6f, 1.45f, 1.3f,
          1.3f, 1.45f, 1.6f, 1.6f, 1.6f, 1.6f, 1.45f, 1.3f,
          1.0f, 1.0f, 1.3f, 1.3f, 1.3f, 1.3f, 1.0f, 1.0f,
          1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f
        };


    // get the value of the board. This is done as follows:
    // 1. Get a list of all pieces
    // 2. Each piece has a base value based on it's type
    // 3. Each piece has a modifier based ot its position on the board
    // 4. Each piece get's a modifier based on how much it threatens
    // 5. Each piece get's a modifier based on how much it is protected and threatened
    // The color that is indicated will receive a positive score, the other a negative score
    // The value of each piece is added
    // The end results indicates the advantage the indicated color has
    float GetBoardValue(Board board, bool isWhite)
    {
        PieceList[] pieces = board.GetAllPieceLists();
        int[] nrOfPieces = CountPieces(pieces);
        Move[] captures = board.GetLegalMoves(true);
        if (board.TrySkipTurn())    // This is done to get the legal moves for the other color
        {
            captures = captures.Concat(board.GetLegalMoves(true)).ToArray();
            board.UndoSkipTurn();
        }
        float[] basevalues = { 0, 1.75f, 2.25f, 2.8f, 3.3f, 8.1f, 20 };
        (byte[,] whiteAttackArray, float whiteDomination) = BuildAttackArray(board, true);
        (byte[,] blackAttackArray, float blackDomination) = BuildAttackArray(board, false);
        float boardValue = (whiteDomination - blackDomination) * (isWhite ? 1 : -1);
        foreach (PieceList pieceList in pieces)
        {
            bool pieceIsWhite = pieceList.IsWhitePieceList;
            float listValue = basevalues[(int)pieceList.TypeOfPieceInList] * (pieceIsWhite == isWhite ? 1 : -1);
            foreach (Piece piece in pieceList)
            {
                float pieceValue = listValue;
                if (piece.IsKing)
                    pieceValue = System.MathF.Pow(nrOfPieces[pieceIsWhite ? 1 : 0], 2.15f) * (pieceIsWhite == isWhite ? 1 : -1);
                // add position modifier
                float positionModifier = positionModifiers[piece.Square.Index];
                if (piece.IsPawn)   // pawns get a bonus for progressing, bonus is larger in the late game
                    if (pieceIsWhite)
                        positionModifier += (piece.Square.Rank * (nrOfPieces[0] < 5 ? 0.9f : 0.35f));
                    else
                        positionModifier += ((7 - piece.Square.Rank) * (nrOfPieces[1] < 5 ? 0.9f : 0.35f));
                if (piece.IsKing)
                    if (board.PlyCount < 20)       // For kings, their position in the beginning of the game should be in the castle positions
                        if (new int[] { 2, 6, 58, 62 }.Contains(piece.Square.Index))
                            positionModifier = 2.0f;
                        else    // elswere on the board is fine, we prevent the king from moving by using the 'stupid move' check
                            positionModifier = 1.0f;
                    else if (nrOfPieces[pieceIsWhite ? 0 : 1] > 5)   // After that, as long as the enemy has many pieces on the field, don't try to manouver
                        positionModifier = 1.0f;
                // Afterward, they can take up the center like the other pieces (default bahaviour)
                pieceValue *= positionModifier;

                // add protection modifier
                // Get the number of times this piece is attacked and protected from the whiteAttackArray and blackAttackArray
                // standard, white is the one we are calculating for
                int nrOfThreaths = blackAttackArray[piece.Square.Rank, piece.Square.File];
                int nrOfProtectors = whiteAttackArray[piece.Square.Rank, piece.Square.File] + board.PlyCount > 6 ? 0 : 1;
                if (!pieceIsWhite) // if we are calculating for black, we need to swap the arrays
                {
                    int a = nrOfThreaths;
                    nrOfThreaths = nrOfProtectors;
                    nrOfProtectors = a;
                }
                if (nrOfThreaths != 0)
                    if (nrOfProtectors == 0)    // Very Bad situation
                        pieceValue *= 0.30f;
                    else if (nrOfThreaths > nrOfProtectors) // still bad situation
                        pieceValue *= 0.70f;
                    else if (nrOfThreaths == nrOfProtectors)    // neutral situation
                        pieceValue *= 1.1f;
                    else    // More protectors then attackers, good situation
                        pieceValue *= 1.25f;
                else if (nrOfProtectors == 0)   // Not under attack, but also not protected. Not great
                    pieceValue *= 1.35f;
                else    // Not under attack, and protected. Great
                    pieceValue *= 1.45f;

                // add threating modifier
                float threatingValue = 0;
                // Any threat is meaningless when under attack and not protected and the other goes next
                if (nrOfThreaths != 0 && nrOfProtectors == 0 && (board.IsWhiteToMove != pieceIsWhite))
                    goto skipthreat;
                foreach (Move capture in captures)
                {
                    if (capture.StartSquare == piece.Square)    // This piece can do this capture
                        threatingValue += basevalues[(int)capture.CapturePieceType];
                }
            skipthreat:
                pieceValue *= (1 + threatingValue * 0.45f);

                boardValue += pieceValue;
            }
        }
        return boardValue;
    }


    // Build an array that indicates how many times each square is 'attacked' by the indicated color
    // This incluces the positions of allied pieces that are protected
    // Also count how many tiles this color is attacking, weighted by the tile weight
    (byte[,], float) BuildAttackArray(Board board, bool isWhite)
    {
        byte[,] attackArray = new byte[8, 8];
        ulong allPiecesBitboard = board.AllPiecesBitboard;
        foreach (Piece piece in board.GetAllPieceLists().Where(list => list.IsWhitePieceList == isWhite).SelectMany(piecelist => piecelist))
            AddBitboardToArray(ref attackArray, BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, isWhite), piece.IsPawn ? (byte)2 : (byte)1);
        // int tileDomination = attackArray.Cast<byte>().Sum(x => x);
        float tileDomination = Enumerable.Range(0, 64).Sum(i => (attackArray[i / 8, i % 8] == 0) ? 0 : (positionModifiers[i] + (isWhite ? i % 8 : 7 - i % 8) * 0.1f));


        return (attackArray, tileDomination);
    }

    void AddBitboardToArray(ref byte[,] array, ulong bitboard, byte amount)
    {
        for (int i = 0; i < 64; i++)
            if ((bitboard & (1UL << i)) != 0)
                array[i / 8, i % 8] += amount;
    }

    int[] CountPieces(PieceList[] pieces)
    {
        int[] nrOfPieces = new int[2];
        foreach (PieceList pieceList in pieces)
        {
            if (pieceList.IsWhitePieceList) // isWhite is always used as true, so it uses index 1, black is false, uses 0
                nrOfPieces[1] += pieceList.Count;
            else
                nrOfPieces[0] += pieceList.Count;
        }
        return nrOfPieces;
    }
}