namespace auto_Bot_424;
using ChessChallenge.API;
using System;
using System.Linq;
using static System.BitConverter;

// IL VM in C#, shit bot but fun concept I think
public class Bot_424 : IChessBot
{
    public decimal[] Data =
    {
        36267959055879642439549321m,
        20551813001900354069200926m,
        4837418825676063451929856m,
        112430102449144011499241489m,
        80280518822889679552516m,
        17646709103528773934018462045m,
        633477130703339455508381713m,
        1258506184622120229767523346m,
        78620389281542422116614473984m,
        3714830704216206846048410307m,
        2786102266384981306135283972m,
        23834015035509948875116775684m,
        1252450930724967845859889873m,
        330046359511834747627700233m,
        39545177004941026099194547456m,
        8348884304079309381736355610m,
        14663046864463110985023689m,
        39610501593522610089845979657m,
        633477295574338382464102938m,
        939344972676158293941420049m,
        5565941605655758038826489337m,
        324001589177704698052411392m,
        78002288342848111571218663764m,
        1289262038906146938157833m,
        8201083603030804149316029645m,
        2790259821184333383360643308m,
        2787150475183574153305394176m,
        77692798555466409693426157826m,
        5986604114685358271875906569m,
        3716329318063063860682951682m,
        939342539533480587855399420m,
        296196436777955960897405201m,
        21983126092274560806445285319m,
        296191718012107539924189196m,
        3715071601215053500330605021m,
        2790243328858663891595432196m,
        545350672608711659578259717m,
        20551777052018222541571593m,
        226484697166946605952078208m,
        8201102530832783378800510732m,
        1558319770450573183366529514m,
        296205881655039087286944017m,
        2785383984559966195388415943m,
        2788226806236334380164188669m,
        8201088271568856884776689149m,
        1350381319022812551528907298m,
        1238379259096793628477950208m,
        78929557910704950749253470464m,
        1560427608m
    };

    public int ProgramCounter, StackPointer, CurrentStack;

    public Board Board;

    public byte Next => Memory[ProgramCounter++];

    public sbyte SNext => (sbyte)Next;

    public short NextShort => ToInt16(new[] { Next, Next });

    public dynamic[] Memory;

    public byte[] Code;

    public int[] CallStack;

    public int LocalPointer => CallStack[CurrentStack];

    public dynamic Pop => Memory[StackPointer--];

    public Bot_424()
    {
        Code = new byte[10240];

        // Load program
        Data
            .SelectMany(d => decimal.GetBits(d)
                .Where((_, i) => i != 3)
                .SelectMany(GetBytes))
            .ToArray()
            .CopyTo(Code, 0);
    }

    public object Push(object value)
    {
        return Memory[++StackPointer] = value;
    }

    public short GetShort(int address)
    {
        return (short)(Memory[address] | (Memory[address + 1] << 8));
    }

    public int MakeMove(Move move)
    {
        Board.MakeMove(move);
        return 0;
    }

    public int UndoMove(Move move)
    {
        Board.UndoMove(move);
        return 0;
    }

    public int Subroutine()
    {
        var returnAddress = ProgramCounter + 2;
        ProgramCounter = NextShort;
        Push(returnAddress);
        CallStack[++CurrentStack] = StackPointer;
        return 0;
    }

    public dynamic Return(dynamic value)
    {
        StackPointer = CallStack[CurrentStack--];
        var args = Next;
        ProgramCounter = Pop;
        StackPointer -= args;
        return value;
    }

    public Move Think(Board board, Timer timer)
    {
        Memory = new dynamic[10240];

        Code.CopyTo(Memory, 0);

        Board = board;
        ProgramCounter = GetShort(0);
        StackPointer = GetShort(2) + 4;
        Push(0);
        CurrentStack = 1;
        CallStack = new int[100];
        CallStack[CurrentStack] = StackPointer;

        while (ProgramCounter > 0)
        {
            var next = Next;

            var pop1 = next > 0b101111_11 ? Pop : 0;
            var pop2 = next > 0b111101_11 ? Pop : 0;
            var pop3 = next > 0b111110_11 ? Pop : 0;

            var result = (next >> 2) switch
            {
                // No operation
                0b000000 => 0,

                // Locals
                0b000001 => StackPointer += Next,
                0b000010 => Memory[LocalPointer + SNext + 1],
                0b000011 => Memory[LocalPointer + SNext + 1] = Pop,

                // Data
                0b000100 => NextShort,

                // Stack
                0b000101 => Memory[StackPointer],
                0b000110 => Pop != 0 ? NextShort : ProgramCounter + 2,
                0b000111 => Pop,

                // Engine
                0b001000 => Board.GetLegalMoves(),
                0b001001 => Board.GetPiece(pop1),
                0b001010 => Pop.TargetSquare,
                0b001011 => (short)Pop.PieceType,
                0b001100 => Board.IsInCheckmate(),
                0b001101 => new Random().Next(),
                0b001110 => Board.GetAllPieceLists().SelectMany(a => a).ToArray(),
                0b001111 => Board.IsInCheck(),
                0b010000 => Pop.IsWhite,
                0b010001 => Board.IsWhiteToMove,
                0b010010 => Pop.Square.File,
                0b010011 => Pop.Square.Rank,
                0b010100 => MakeMove(Pop),
                0b010101 => UndoMove((Move)Pop),

                // Subroutines
                0b010110 => Subroutine(),
                0b010111 => Return(Pop),

                // Arithmetic
                0b011111 => Pop == 0,   // A == 0

                // Stack: A B, B is popped first
                0b110000 => Pop == pop1, // A Equal B
                0b110001 => Pop >= pop1, // A Greater than or qqual B
                0b110010 => Pop > pop1,  // A Greater than B
                0b110011 => Pop + pop1,  // A + B
                0b110100 => Pop - pop1, // A - B
                0b110101 => Pop * pop1,  // A * B
                //0b110110 => Pop / pop1,  // A / B
                0b110111 => Pop % pop1,  // A % B
                /*0b111000 => Pop & pop1,  // A & B
                0b111001 => Pop | pop1,  // A | B
                0b111010 => Pop ^ pop1,  // A ^ B
                0b111011 => Pop << pop1, // A << B
                0b111100 => Pop >> pop1, // A >> B
                */
                // Array
                0b111101 => pop1 is Array a ? a.Length : GetShort(pop1),
                0b111110 => pop2 is Array a ? a.GetValue(pop1) : GetShort(pop2 + 2 + pop1 * 2),
                0b111111 => pop3 is Array a ? a.SetValue(pop1, pop2) : 0,
            };

            result = (next & 0b00000011) switch
            {
                0b00 => 0,
                0b01 => Push(result),
                0b11 => Push((short)(result ? 1 : 0)),
                0b10 => ProgramCounter = result
            };
        }

        return (Move)Pop;
    }
}