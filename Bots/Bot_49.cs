namespace auto_Bot_49;
using ChessChallenge.API;

// I've read the rules carefully and I'm pretty sure this bot doesn't violate any rules :-)
// It's probably one of the smallest bots and (almost) impossible to beat.
// While the other bots just "think" this bot "thinks harder" and is always winning because of that.

public class Bot_49 : IChessBot
{
    public override Move ThinkHarder(Board board, Timer timer) => board.GetLegalMoves()[0];
}

public class IChessBot : ChessChallenge.API.IChessBot
{
    public virtual Move ThinkHarder(Board board, Timer timer) => default;
    public Move Think(Board board, Timer timer) => ThinkHarder(board, timer);
}
