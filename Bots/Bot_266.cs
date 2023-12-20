namespace auto_Bot_266;
/* Game      */                                                             // Story:
/* Tech      */                              using System;                  // https://youtu.be/5vsLmM756LA
/* Explained */                          using ChessChallenge.API;
/* Bot       */                       using static System.Math;public
                                    class MyBot:IChessBot{static E[]tr=
                                   new E[16777216];float bc=121,b;bool o
                                  ;Timer t;Board bo;float eg(bool wh){int
                                 s=0;foreach(var pl in bo.GetAllPieceLists
                                ())if(pl.IsWhitePieceList==wh)s+=(0x0942200
                                >>4*(int)pl.TypeOfPieceInList&0xf)*pl.Count
                                ;return Min(1,s*0.04f);}int EB(){float e=eg
                                 (false),og=eg(true),s=0.0f;foreach(var pl
                                 in bo.GetAllPieceLists())s+=0b00010000010
                                  >>(int)pl.TypeOfPieceInList!=0x00000000
                               ?(pl. IsWhitePieceList ?e:- og)*T(S, pl)+(pl.
                             IsWhitePieceList?1.0f-e:og-1.0f)*T(Es,pl):T(S,pl)
                           ;return (int)(bo.IsWhiteToMove?s:-s);}ulong T(ulong[]
                          []t,PieceList pl){ulong v=0;foreach(var p in pl){var sq=
                        p.Square;v+=t[(int)p.PieceType][sq.File>=0x4?07-sq.File:sq.
                       File]<<8*(pl.IsWhitePieceList?07-sq.Rank:sq.Rank)>>56;}return
                       25*v;}(int,Move,bool)SE(int dL,int cL,bool co,int a,int b){if(
                      bo.IsInCheckmate())return(-32_100,default,true);if(bo.IsDraw())
                      return(0,default,bo.IsInStalemate()||bo.IsInsufficientMaterial()
                     );if(dL==0){++dL;if(bo.IsInCheck()&&cL>0)--cL;else if(!co&&cL==4)
                     return SE(0x08,cL,true,a,b);else return(EB(),default,true);}ulong
                     k=bo.ZobristKey;E ta=tr[k%16_7_7_7_2_16];int bS=-32150,s;Move be=
                      default;if(ta.K==k&&Abs(ta.D)>=dL){bo.MakeMove(ta.M);bool tD=bo.
                      IsDraw();bo.UndoMove(ta.M);if(tD)ta=default;else{a=Max(a,bS=ta.
                       S);be=ta.M;if(b<a||ta.D>=0)return(ta.S,ta.M,true);}}if(co&&(s
                        =EB())>bS&&b<(a =Max(a,bS=s))) return(s,default,true);Span<
                         Move>le=stackalloc Move[256];bo.GetLegalMovesNonAlloc(ref
                           le,co);Span<(int,Move)>prioms=stackalloc(int,Move)[le.
                            Length];int lv=0;foreach(var lm in le)prioms[lv++]=
                          ((ta.K==k&&lm==ta.M?5_0_0_0:km.Contains(lm)?500:0)+(lm.
                       PromotionPieceType==PieceType.Queen?5:0)+(0x953310>>4*(int)lm
                    .CapturePieceType&0xf),lm);prioms.Sort((a,b)=>-a.Item1.CompareTo(b.
                  Item1));bool cT=true,ax=false,cU;lv=0;foreach(var(_,m)in prioms){if(o=t
                .MillisecondsElapsedThisTurn>=this.b)return(bS,be,cT);bo.MakeMove(m);try{if(
              dL>=3&&++lv>=4&&!m.IsCapture){s=-SE(dL-2,cL,co,-b,-a).Item1;if(o)break;if(s<bS)
            continue;}(s,_,cU)=SE(dL-01,cL,co,-b,-a);if(o)break;s=-s+(Abs(s)>=30000?Sign(s):0);
          if(s<=bS)continue;bS=s;be =m;a=Max(a,s);cT=cU;if(ax=b<a){ km.Add(m);break;}}finally{bo.
        UndoMove(m);}}if(!o&&!co&&cT&&bS!=0)tr[k%16777216]=new E{K=k,D=(short)(ax?-dL:dL),S=(short)
       bS,M=be};return(bS,be,cT);}public Move Think(Board c,Timer ti){bo=c;t=ti;b=Min(0.0333333333f,
      2.0f/--bc)*ti.MillisecondsRemaining;o=false;km.Clear();Move M=default,m;int d=0;while(++d<=15&&
     !o)if((m=SE(d,4,false,-32200,32200).Item2)!=default)M=m;return M==default?c.GetLegalMoves()[0]:M;
    }System.Collections.Generic.HashSet<Move>km=new();static readonly ulong[]Ks={0x3234363_636363432ul,
    0x3438_3c3d3_c3d38_34ul,0x363_c3e3f_3f3e3_c36ul,0x363_c3f40_403f3_d36ul},Bs={0x3c3_e3e3e_3e3e_3e3cul,
   0x3e40_4041_404241_3eul,0x3e_40414_14242_403eul,0x3e_40424_2424240_3eul},Rs={0x646_5636_3636_363_64ul,
  0x646_6646_46464_646_4ul,0x6_4666_46464_646_46_4ul,0x6466_64646_46464_65ul},qS={0xb_0b2b2b3_b4b2_b2b0ul,
  0xb_2b4b_4b4_b4_b5b4_b2ul, 0xb_2b4_b5b_5b5_b_5b5b_2ul,0xb3_b4b_5b_5b5_b5b4_b3ul};ulong[][]S={null,new[]{
 0x141e161514151514ul,0x141e16151_4131614ul,0x141e181614_121614ul,0x141e1a1_918141014ul},Ks,Bs,Rs,qS,new[]{
 0x0004_080a0c0e1414ul,0x02040608_0a0c1416ul,0x02040_6080a0c0f12ul,0x02040_406080_c0f10ul}},Es={null,new[]{
0x14241e1a181_61614ul,0x14241e_1a18161614ul,0x14241e1a1_8161614ul,0x14241e1_a18161614ul},Ks,Bs,Rs,qS,new[]{
0x0c0f0e0d0c0b0a06ul,0x0e100f0e0d0c0b0aul,0x0e1114171614100aul,0x0e1116191815100aul}};struct E{public ulong
K;public short S,D; public Move M;}} /* Thank you for hosting this competition, Sebastian!  <3 <3 <3 <3  */
