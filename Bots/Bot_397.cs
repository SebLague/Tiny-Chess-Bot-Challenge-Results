namespace auto_Bot_397;

// #define VEGETABLES

using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using D = System.Collections.Generic.Dictionary<object, object>;
using F = System.Func<object, object>;
using L = System.Collections.Generic.List<object>;

/**
 * This bot my not be the most challenging chess opponent
 * but it can surely make your brain tickle when tryying
 * to make sense of its internals :]
 * 
 * Here is a broad overview of what is happening:
 * 
 * 1.   The actual chess bot (search + eval) is implemented in
 *      tiny lisp. When asked to think an interpreter runs the bot.
 *
 * 2.   This lisp code is converted to a bytecode representation
 *      to simplify stuff and make the code just raw data.
 * 
 * 3.   The bytecode is packed together in C#'s decimal literals
 *      and unpacked & parsed once in the constructor.
 *
 */
public class Bot_397 : IChessBot
{
    D env;
    object nil;

    // this type system is pure pain
    object binop_factory(int id) =>
        (object x) =>
        {
            var l = (L)x;
            return binop((long)(((L)x)[0]), (long)(((L)x)[1]), id);
        };

    object binop(long a, long b, int id) => id switch
    {
        0x14 => a + b,
        0x15 => a - b,
        0x16 => a * b,
        // 0x17 => a / b,
        0x18 => a == b,
        0x19 => a >= b,
        0x1a => a < b,

    };


    public Bot_397()
    {
        nil = new L();

        /* core library */
        env = new D {
            { 0x0a, (object x) => cons(car(x), car(cdr(x))) },
            { 0x0b, (object x) => car(car(x)) },
            { 0x0c, (object x) => cdr(car(x)) },
            { 0x0d, (object x) => (object)nilq(car(x)) },
            { 0x0e, (object x) => { DivertedConsole.Write(); return car(x); }},


            { 0x10, 0L },
            { 0x11, 1L },

            // { 0x14, (object x) => (object)((long)(((L)x)[0]) + (long)(((L)x)[1])) },
            // { 0x15, (object x) => (object)((long)(((L)x)[0]) - (long)(((L)x)[1])) },
            // { 0x16, (object x) => (object)((long)(((L)x)[0]) * (long)(((L)x)[1])) },
            // { 0x17, (object x) => (object)((long)(((L)x)[0]) / (long)(((L)x)[1])) },
            { 0x14, binop_factory(0x14) },
            { 0x15, binop_factory(0x15) },
            { 0x16, binop_factory(0x16) },
            // { 0x17, binop_factory(0x17) },
            { 0x18, binop_factory(0x18) },
            { 0x19, binop_factory(0x19) },
            { 0x1a, binop_factory(0x1a) },

            { 0x20, (object x) => (object)((Board)car(x)).GetLegalMoves().Cast<object>().ToList() },
            { 0x21, (object x) => {
                var board = (Board)car(x);
                return board.GetAllPieceLists()
                    .Select((PieceList x) => (object)x.Cast<object>().ToList()).ToList(); } },
            { 0x22, (object x) => (object)((Board)car(x)).IsWhiteToMove },
            { 0x23, (object x) => {
                var board = (Board)cxr(x, 0);
                var move  = (Move)cxr(x, 1);
                board.MakeMove(move);
                return (object)board;
            }},
            { 0x24, (object x) => {
                var board = (Board)cxr(x, 0);
                var move  = (Move)cxr(x, 1);
                board.UndoMove(move);
                return (object)board;
            }},
        };


#if VEGETABLES
        vegetables(); // #DEBUG
#endif

        Stack<L> stack = new();

        var code = new[] {

11461830436651866321582096641m,
315534401000332556375229953m,
3096238658501728552043806986m,
621392630686569659027816743m,
671152925677380178790121986m,
13321159085100470694808979457m,
621397356010228806450675970m,
12380619559571061804462833922m,
354220818265854564416749834m,
5285428701025779710968267307m,
2786583495982854656344796417m,
2162305336945323398662586670m,
65614463155566888570685620995m,
76503042610666096741320176085m,
2162305336945921533021715613m,
622611093084424137489646339m,
2725118871084975882754379307m,
77065857177318770133331149666m,
69948433626919553934998766588m,
2725398081003275550527639080m,
354220855159342715358479202m,
6190984720466154887818916144m,
15178068424679278392136242689m,
678264071805984662643343874m,
680630641425191065064964610m,
2477099297832075009758139905m,
621397630768368039832527361m,
17397907459216898778653589762m,
378436305176200190169186562m,
380816652206550999149720843m,
4977152362581093695119504163m,
319161175782930189784252982m,
321580005675767798458432025m,
372353911845957144258622244m,
8047856978712240007203599140m,
621397356959082104276269622m,
17397907418730624694467232258m,
680861373362604244780712194m,
621653342267613390509913089m,
17397907459216898778787807490m,
17651531739970119605734670909m,
18260900450612718188784585473m,
6500404592530584453889860353m,
2477098486165790991224813072m,
3096347272931116029685930241m,
18570318996871451886461592577m,
319161179460664411978671105m,
319162175726203344315364890m,
621397353053118735597581850m,
2786584495637613572900061442m,
65799429658453748139945886005m,
30340175976672717603453400881m,
311912343246073675495831811m,
990403051594967477414657801m,
620188427306898040695149725m,
621397353053053628184658434m,

        }.SelectMany(decimal.GetBits).Where(x => x != 0).SelectMany(BitConverter.GetBytes).ToArray();

        for (int i = 0; i < code.Length; ++i)
            switch (code[i])
            {
                // case 0x00: continue;
                case 0x01: // lpa
                    stack.Push(new());
                    break;
                case 0x02: // rpa
                    var top = stack.Pop();
                    stack.Peek().Add(top);
                    break;
                case 0x03: // i64
                    stack.Peek().Add(BitConverter.ToInt64(code, i + 1) ^ 0x306fc9df731d49e);
                    i += 8;
                    break;
                default:
                    stack.Peek().Add((int)code[i]);
                    break;
            }

        eval(stack.Peek()[0], env);
    }

    // L as_list(object x) => (L)x;

    object cons(object car, object cdr) => ((L)cdr).Prepend(car).ToList();

    object cxr(object x, int i) => ((L)x)[i];

    object car(object x) => ((L)x).First();

    object cdr(object x) => ((L)x).Skip(1).ToList();

    bool nilq(object x) => x switch
    {
        L l => !l.Any(),
        _ => false,
    };

    // ------------------------------------------------------------
    // ------------------------------------------------------------
    // ------------------------------------------------------------
    // ------------------------------------------------------------

    object eval(object x, D e)
    {
    TCO:

        if (x is not L)
            return e.ContainsKey(x) ? e[x] : x;

        if (nilq(x))
            return x;


        var func = eval(car(x), e);
        var args = (L)cdr(x);

        var args0 = args[0];

        if (func is int)
            switch (func)
            {
                case 0x05: // eval
                    x = eval(args0, e);
                    goto TCO;

                case 0x06: // quote
                    return args0;

                // case 0x07: // define
                //     var tmp = eval(args[1], env);
                //     env[args0] = tmp;
                //     return tmp;

                case 0x08: // if
                    x = (bool)eval(args0, e) ? args[1] : args[2];
                    goto TCO;

                case 0x09: // let
                    e[args0] = eval(args[1], e);
                    x = args[2];
                    goto TCO;
            }

        // this is a lazy iterator
        // for macros its never realized
        var iter = args.Select(arg => eval(arg, e));

        if (func is F)
            return ((F)func)(iter.ToList());

        var list = (L)func;

        if (!nilq(car(func)))
            args = iter.ToList();


        // e = ((L)list[^2]).Zip(args).ToDictionary(pair => pair.First, pair => pair.Second);
        x = list[^1];

        e = new(e);
        foreach ((var k, var v) in ((L)list[^2]).Zip(args))
            e[k] = v;
        // x = car(cdr(func));

        goto TCO;
    }

    public Move Think(Board board, Timer timer)
    {
        // DivertedConsole.Write("material-heuristic: {0}", eval(new L{0x30, board}, env)); // #DEBUG
        var res = eval(new L { 0xff, board, timer }, env);
        // print(res);
        return (Move)res;
    }

    // debugging:

#if VEGETABLES

/*

    void vegetables()
    {
        test_builtins();

        DivertedConsole.Write("passed all tests!");
    }

    void test_builtins() {
        object res, tmp;

        // identity
        res = eval(0x42, new D());
        assume(res, 0x42);

        // nil handling
        res = eval(nil, new D());
        assume(nilq(res), true);

        // lookup
        res = eval(0xab, new D {{0xab, 0xcd}});
        assume(res, 0xcd);

        // quote 1
        res = eval(cons(0x06, cons(0x55, nil)), new D());
        assume(res, 0x55);

        // quote 2
        tmp = cons(0xab, nil);
        res = eval(cons(0x06, cons(tmp, nil)), new D());
        assume(res, tmp);

        // quote 3
        tmp = cons(0xfe, cons(0xdc, nil));
        res = eval(cons(0x06, cons(tmp, nil)), new D());
        assume(res, tmp);

        // if 1
        tmp = cons(0x08, cons(true, cons(0xab, cons(0xcd, nil))));
        res = eval(tmp, new D());
        assume(res, 0xab);
        
        tmp = cons(0x08, cons(false, cons(0xab, cons(0xcd, nil))));
        res = eval(tmp, new D());
        assume(res, 0xcd);

        // if 2
        tmp = cons(0x08, cons(true, cons(cons(0x06, cons(0xab, nil)), cons(0xcd, nil))));
        res = eval(tmp, new D());
        assume(res, 0xab);

        tmp = cons(0x08, cons(false, cons(0xab, cons(cons(0x06, cons(0xcd, nil)), nil))));
        res = eval(tmp, new D());
        assume(res, 0xcd);

        // if 3
        tmp = cons(0x08, cons(cons(0x06, cons(true, nil)), cons(0xab, cons(0xcd, nil))));
        res = eval(tmp, new D());
        assume(res, 0xab);

        tmp = cons(0x08, cons(cons(0x06, cons(false, nil)), cons(0xab, cons(0xcd, nil))));
        res = eval(tmp, new D());
        assume(res, 0xcd);

        // define 1  // (d 0xfe 0x33) ; (v 0xfe)
        eval(cons(0x07, cons(0xfe, cons(0x33, nil))), new D());
        res = eval(0xfe, new D(env));
        assume(res, 0x33);

        // define 2 // (d 0xfe (q 0x33)) ; (v 0xfe)
        eval(cons(0x07, cons(0xfe, cons(cons(0x06, cons(0x33, nil)), nil))), new D());
        res = eval(0xfe, new D(env));
        assume(res, 0x33);

        // eval 1  // (v (q 0xab))[ab:=cd]
        res = eval(cons(0x05, cons(cons(0x06, cons(0xab, nil)), nil)), new D{{0xab, 0xcd}}); 
        assume(res, 0xcd);

        // primitives
        res = eval(cons(0xab, cons(0xcd, nil)), new D {{ 0xab, (object x) => car(x) }});
        assume(res, 0xcd);

        object func;

        // functions 1  // ((q ((x) x)) 0x66)
        func = cons(cons(0x77, nil), cons(0x77, nil));
        res = eval(cons(cons(0x06, cons(func, nil)), cons(0x66, nil)), new D());
        assume(res, 0x66);

        // functions 2  // ((q ((x) (q x))) 0x66)
        func = cons(cons(0x77, nil), cons(cons(0x06, cons(0x77, nil)), nil));
        res = eval(cons(cons(0x06, cons(func, nil)), cons(0x66, nil)), new D());
        assume(res, 0x77);

        // functions 3  // ((q ((x y) y)) 0xcc 0xdd)
        var args = cons(0x76, cons(0x77, nil));
        func = cons(args, cons(0x77, nil));
        res = eval(cons(cons(0x06, cons(func, nil)), cons(0xcc, cons(0xdd, nil))), new D());
        assume(res, 0xdd);

        // define + function
        // (d 0xfe (q ((x) x)))
        func = cons(cons(0x77, nil), cons(0x77, nil));
        eval(cons(0x07, cons(0xfe, cons(cons(0x06, cons(func, nil)), nil))), new D());
        res = eval(cons(0xfe, cons(0xcc, nil)), new D(env));
        assume(res, 0xcc);

        // let 1
        res = eval(cons(0x09, cons(0xab, cons(0xcd, cons(0xab, nil)))), new D());
        assume(res, 0xcd);

        // let 2
        res = eval(cons(0x09, cons(0xab, cons(cons(0x06, cons(0xcd, nil)), cons(0xab, nil)))), new D());
        assume(res, 0xcd);


    }

    bool deep_eq(object a, object b) => (a, b) switch {
            (int ia, int ib) => ia == ib,
            (bool ba, bool bb) => ba == bb,
            (L la, L lb) => la.Zip(lb).All(ab => deep_eq(ab.First, ab.Second)),
            (_, _) => false,
        };

    void assume(object a, object b) {
        if (!deep_eq(a, b))
            throw new ArgumentException("test failed!");
    }

*/

#endif

}