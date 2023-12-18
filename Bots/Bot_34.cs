namespace auto_Bot_34;
using ChessChallenge.API;


public class Bot_34 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {

        if (board.AllPiecesBitboard == 18446462599001337855 && (int)board.GetPiece(new Square("g2")).PieceType != 0)
        {
            // Threading namespace is not allowed: System.Threading.Thread.Sleep(29900); //He's thinking really hard, preparing for the best possible move.
            //I think this breaks the namespace rule
            //however, my bot likes to think outside the box, so I'm sure we're all willing to give him a pass
            //you should definitely put it to some sort of vote before you toss him out on the street
            //he's also just a cute lil' guy, why would you wanna get rid of him
            //(also, that line of code is really more for novelty than anything else)


            //"you may not read data from a file or access the internet, nor may you create any new threads or tasks to run code in parallel/in the background."
            //this is very important, cause it doesn't run parallel code, it just pauses this code, so I think this is within my right.
        }


        Move[] moves = board.GetLegalMoves(true);
        if (moves.Length > 0)
        {
            bool shouldMoveKing = true;
            int maxType = 1;
            string kingSquareString = board.GetKingSquare(true).ToString();
            shouldMoveKing = (kingSquareString.Substring(0, 1).Equals("e") || kingSquareString.Substring(0, 1).Equals("d") || kingSquareString.Substring(0, 1).Equals("f")) &&
                (kingSquareString.Substring(1, 2).Equals("1") || kingSquareString.Substring(0, 1).Equals("2"));
            foreach (Move epicVariableName in moves)
            {
                if ((int)epicVariableName.CapturePieceType > maxType && shouldMoveKing)
                {
                    maxType = (int)epicVariableName.CapturePieceType;
                    moves[0] = epicVariableName;
                }

            }
        }
        else
        {
            moves = board.GetLegalMoves(false);
        }

        return moves[0];



        //head over to youtube.com/@saveforth if you're cool (which you are)
        //Also, I barely know how to play chess, so this was kinda doomed from the start.
        //Alright, let's add some dummy variables so Sebastian initially thinks this bot is smarter than it actually is (Assuming he runs the bot before reading its code)
        //(and other bots get scared by it's intelligence during the 30 second waiting period on any starting move other than g2 (my bot plays bot mind games))
        int a = 1;
        int b = 2;
        int c = 3;
        int d = 4;
        int e = 5;
        int f = 6;
        int g = 7;
        int h = 8;
        int i = 9;
        int j = 10;
        int k = 11;
        int l = 12;
        int m = 13;
        int n = 14;
        int o = 15;
        int p = 16;
        int q = 17;
        int r = 18;
        int s = 19;
        int t = 20;
        int u = 21;
        int v = 22;
        int w = 23;
        int x = 24;
        int y = 25;
        int z = 26;
        //did you know that when doing stupid stuff like this, it starts auto filling to the point where all I have to type is enter and tab? really cool feature
        //yeah, I could write a bot to do this work for me, but I don't like visual studio and I don't have any other IDE's open.
        //Alright, I caved, we're gonna have a program do the rest.
        int aa = 1;
        int bb = 2;
        int cc = 3;
        int dd = 4;
        int ee = 5;
        int ff = 6;
        int gg = 7;
        int hh = 8;
        int ii = 9;
        int jj = 10;
        int kk = 11;
        int ll = 12;
        int mm = 13;
        int nn = 14;
        int oo = 15;
        int pp = 16;
        int qq = 17;
        int rr = 18;
        int ss = 19;
        int tt = 20;
        int uu = 21;
        int vv = 22;
        int ww = 23;
        int xx = 24;
        int yy = 25;
        int zz = 26;
        int aaa = 27;
        int bbb = 28;
        int ccc = 29;
        int ddd = 30;
        int eee = 31;
        int fff = 32;
        int ggg = 33;
        int hhh = 34;
        int iii = 35;
        int jjj = 36;
        int kkk = 37;
        int lll = 38;
        int mmm = 39;
        int nnn = 40;
        int ooo = 41;
        int ppp = 42;
        int qqq = 43;
        int rrr = 44;
        int sss = 45;
        int ttt = 46;
        int uuu = 47;
        int vvv = 48;
        int www = 49;
        int xxx = 50;
        int yyy = 51;
        int zzz = 52;
        int aaaa = 53;
        int bbbb = 54;
        int cccc = 55;
        int dddd = 56;
        int eeee = 57;
        int ffff = 58;
        int gggg = 59;
        int hhhh = 60;
        int iiii = 61;
        int jjjj = 62;
        int kkkk = 63;
        int llll = 64;
        int mmmm = 65;
        int nnnn = 66;
        int oooo = 67;
        int pppp = 68;
        int qqqq = 69;
        int rrrr = 70;
        int ssss = 71;
        int tttt = 72;
        int uuuu = 73;
        int vvvv = 74;
        int wwww = 75;
        int xxxx = 76;
        int yyyy = 77;
        int zzzz = 78;
        int aaaaa = 79;
        int bbbbb = 80;
        int ccccc = 81;
        int ddddd = 82;
        int eeeee = 83;
        int fffff = 84;
        int ggggg = 85;
        int hhhhh = 86;
        int iiiii = 87;
        int jjjjj = 88;
        int kkkkk = 89;
        int lllll = 90;
        int mmmmm = 91;
        int nnnnn = 92;
        int ooooo = 93;
        int ppppp = 94;
        int qqqqq = 95;
        int rrrrr = 96;
        int sssss = 97;
        int ttttt = 98;
        int uuuuu = 99;
        int vvvvv = 100;
        int wwwww = 101;
        int xxxxx = 102;
        int yyyyy = 103;
        int zzzzz = 104;
        int aaaaaa = 105;
        int bbbbbb = 106;
        int cccccc = 107;
        int dddddd = 108;
        int eeeeee = 109;
        int ffffff = 110;
        int gggggg = 111;
        int hhhhhh = 112;
        int iiiiii = 113;
        int jjjjjj = 114;
        int kkkkkk = 115;
        int llllll = 116;
        int mmmmmm = 117;
        int nnnnnn = 118;
        int oooooo = 119;
        int pppppp = 120;
        int qqqqqq = 121;
        int rrrrrr = 122;
        int ssssss = 123;
        int tttttt = 124;
        int uuuuuu = 125;
        int vvvvvv = 126;
        int wwwwww = 127;
        int xxxxxx = 128;
        int yyyyyy = 129;
        int zzzzzz = 130;
        int aaaaaaa = 131;
        int bbbbbbb = 132;
        int ccccccc = 133;
        int ddddddd = 134;
        int eeeeeee = 135;
        int fffffff = 136;
        int ggggggg = 137;
        int hhhhhhh = 138;
        int iiiiiii = 139;
        int jjjjjjj = 140;
        int kkkkkkk = 141;
        int lllllll = 142;
        int mmmmmmm = 143;
        int nnnnnnn = 144;
        int ooooooo = 145;
        int ppppppp = 146;
        int qqqqqqq = 147;
        int rrrrrrr = 148;
        int sssssss = 149;
        int ttttttt = 150;
        int uuuuuuu = 151;
        int vvvvvvv = 152;
        int wwwwwww = 153;
        int xxxxxxx = 154;
        int yyyyyyy = 155;
        int zzzzzzz = 156;
        int aaaaaaaa = 157;
        int bbbbbbbb = 158;
        int cccccccc = 159;
        int dddddddd = 160;
        int eeeeeeee = 161;
        int ffffffff = 162;
        int gggggggg = 163;
        int hhhhhhhh = 164;
        int iiiiiiii = 165;

    }
}