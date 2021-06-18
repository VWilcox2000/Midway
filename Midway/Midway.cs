using Midway;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidwayEngine
{
  public delegate void OutputTextHandler(string message);
  public delegate void TaskForceUpdatedHandler();

  public sealed class MidwayScenario
  {
    public event OutputTextHandler? outputText;
    public event TaskForceUpdatedHandler? taskForceUpdated;

    public MidwayScenario()
    {
      this.Initialize();
    }

    public int day => this.d;
    public string dateText => string.Concat(
      this.day,
      " June 1942");
    public string timeText
    {
      get
      {
        TimeSpan ts;
        string ret;

        ts = TimeSpan.FromMinutes(t);
        ret = string.Concat(
          (Convert.ToInt32(ts.TotalHours) % 24).ToString("00"),
          ':',
          (Convert.ToInt32(ts.TotalMinutes) % 60).ToString("00"));
        return ret;
      }
    }

    private Random random { get; } = new Random();
    // F(x, 0) -- y
    // F(x, 1) -- x
    // F(x, 2) -- spotted == 1 seen, 2 identified
    // F(x, 3) -- 0, 1, 2
    // F(x, 4) -- course
    // F(x, 5) -- speed
    private decimal[,] F = new decimal[,]
    {
      { 0M, 0M, 0M, 1M, 0M, 25M, 0.1M, 0.2M },
      { 0M, 0M, 0M, 1M, 0M, 18M, 0.2M, 0.01M },
      { 0M, 0M, 0M, 1M, 0M, 25M, 0.1M, 0.01M },
      { 0M, 0M, 0M, 3M, 0M, 25M, 0.1M, 0.06M },
      { 0M, 0M, 0M, 4M, 0M, 25M, 0.1M, 0.04M },
      { 0M, 0M, 2M, 5M, 0M, 0M, 0.25M, 0.04M }
    };
    // C[x, 0] - contact group Id??
    // C[x, 1] - F4F's in hangar
    // C[x, 2] - SBD's in hangar
    // C[x, 3] - TBD's in hangar
    // C[x, 4] - F4F's armed
    // C[x, 5] - SBD's armed
    // C[x, 6] - TBD's armed
    // C[x, 7] - CAP F4Fs
    // C[x, 8] - damage status?  >0 light, >= 60 non-op, >= 100 sunk/destroyed
    private decimal[,] C = new decimal[,]
    {
      { 0, 21, 21, 21, 0, 0, 0, 0, 0 },
      { 0, 30, 23, 30, 0, 0, 0, 0, 0 },
      { 0, 21, 21, 21, 0, 0, 0, 0, 0 },
      { 0, 21, 21, 21, 0, 0, 0, 0, 0 },
      { 3, 27, 38, 14, 0, 0, 0, 0, 0 },
      { 3, 27, 35, 15, 0, 0, 0, 0, 0 },
      { 4, 25, 37, 13, 0, 0, 0, 0, 0 },
      { 5, 14, 14, 10, 0, 0, 0, 0, 0 },
      { 1, 15, 0, 15, 0, 0, 0, 0, 0 }
    };
    private decimal[] W = new decimal[]
    {
      1.5M, 1.4M, 1.3M, 1.3M, 1.2M, 1M
    };
    // strikes -- this could be made list array
    // game now limiting to 10 strikes (which IS a lot)
    // S[i, 0] - strike F4F's
    // S[i, 1] - if ((sbd's / (sbd's + tbd's)) > rnd(0 to 1.0) then 1 else 0
    // S[i, 2] - strike SBD's
    // S[i, 3] - 1 at launch
    // S[i, 4] - strike TBD's
    // S[i, 5] - 0 at launch
    // S[i, 6] - target contact group
    // S[i, 7] - target arrival time
    // S[i, 8] - landing time
    // S[i, 9] - strike home carrier (or seems TF?!??)
    private decimal[,] S = new decimal[,]
    {
      { 0, 0, 0, 0, 0, 0, 0, 0, 0, -1 },
      { 0, 0, 0, 0, 0, 0, 0, 0, 0, -1 },
      { 0, 0, 0, 0, 0, 0, 0, 0, 0, -1 },
      { 0, 0, 0, 0, 0, 0, 0, 0, 0, -1 },
      { 0, 0, 0, 0, 0, 0, 0, 0, 0, -1 },
      { 0, 0, 0, 0, 0, 0, 0, 0, 0, -1 },
      { 0, 0, 0, 0, 0, 0, 0, 0, 0, -1 },
      { 0, 0, 0, 0, 0, 0, 0, 0, 0, -1 },
      { 0, 0, 0, 0, 0, 0, 0, 0, 0, -1 },
      { 0, 0, 0, 0, 0, 0, 0, 0, 0, -1 }
    };
    private string[] vessels =
    {
      "Akagi",
      "Kaga",
      "Soryu",
      "Hiryu",
      "Enterprise",
      "Hornet",
      "Yorktown",
      "Midway",
      "Zuiho"
    };
    private string[,] planes =
    {
      { "F4F's", "SBD's", "TBD's" },
      { "Zeros", "Vals", "Kates" }
    };
    //[1, i / 2]
    //170 FOR I = 0 TO 9:S(I,9)=-1:NEXT:S6=.041:S7=.043:CLS:SCREEN 0:FOR X = 4 TO 7:LOCATE 12+X,1

    private bool interruptTimeAdvancement = false; // not sure -- if 0 stop taking interaction and progress time until decision??
    // don't need -- we loop internal
    private bool allJapaneseCarriersIncapacitated; // all Japanese carriers lost? -- was j9
    private int v0; // victory points?
    private int v1; // victory points?
    private int v2; // victory points?
    private int v; // victor points standing
    private decimal p1; // japanese planes lost
    private decimal p; // american planes lost
    private int t; // time 0-hour
    private int d; // day
    private decimal s6; // chance of Japanese scout plane sighting Americans
    private decimal s7; // chance of American scout planes sighting Japanese
    private string m { get; } = "12367M";
    private bool a8 = false; // IJN attack midway flag
    private bool a9 = false; // IJN attack fleet flag
    private int s9 = 0; // IJN attack target task force
    private ForceTypes[] forceTypes { get; } =
    {
      ForceTypes.JapaneseCarrierGroup,
      ForceTypes.JapaneseTransports,
      ForceTypes.JapaneseCruisers,
      ForceTypes.TaskForce16,
      ForceTypes.TaskForce17,
      ForceTypes.MidwayIsland
    };
    private int[,] initArray = new int[,]
    {
      { 270, 90, 525 },
      { 230, 60, 560 },
      { 230, 60, 560 },
      { 25, 20, 380 },
      { 25, 20, 380 },
      { 0, 0, 0 }
    };
    private int c5;
    private bool firstCruiserAttack; // was c6
    private int c7;
    private decimal[] FX = { 0, 0, 0, 0, 0, 0 }; // x on map
    private decimal[] FY = { 0, 0, 0, 0, 0, 0 }; // y on map
    private bool[] FZ = { false, false, false, false, false, false };  // spotted by opponent
    private int[] C1 = { 0, 0, 0 };
    private bool showOutcome = false;

    private int rand(int max)
    {
      int ret;

      ret = this.random.Next(max);
      return ret;
    }

    private decimal rand(decimal m)
    {
      decimal ret;

      ret = Convert.ToDecimal(this.random.NextDouble() * ((double)m));
      return ret;
    }

    public ContactRef? getContactRef(int? contact)
    {
      ContactRef? ret;

      if (
        contact != null &&
        contact >= 1 &&
        contact <= this.contactList.Count)
      {
        ForceRef? force;
        int tf;

        tf = this.contactList[contact.Value - 1];
        force = this.forces
          .ElementAtOrDefault(tf);
        if (force != null)
        {
          ret = new ContactRef
          {
            number = contact.Value,
            force = force,
            bearing = this.CourseTo(5, tf),
            range = this.CalculateRange(5, tf)
          };
        }
        else
        {
          ret = null;
        }
      }
      else
      {
        ret = null;
      }
      return ret;
    }

    private decimal cos(decimal m)
    {
      decimal ret;

      ret = Convert.ToDecimal(Math.Cos(((double)m - 90.0) * Math.PI * 2.0 / 360.0));
      return ret;
    }

    private decimal sin(decimal m)
    {
      decimal ret;

      ret = Convert.ToDecimal(Math.Sin(((double)m - 90.0) * Math.PI * 2.0 / 360.0));
      return ret;
    }

    private decimal arcTan(decimal x, decimal y)
    {
      decimal ret;

      if (x == 0M)
      {
        if (y > 0)
        {
          ret = 180M;
        }
        else
        {
          ret = 0M;
        }
      }
      else
      {
        ret = Convert.ToDecimal(Math.Atan((double)(y / x))) * 360.0M / ((decimal)Math.PI) / 2.0M + 90M;
        if (x < 0M)
        {
          ret += 180M;
        }
      }
      return ret;
    }

    private int intVal(bool flag)
    {
      int ret;

      ret = flag ? -1 : 0;
      return ret;
    }

    private decimal mVal(bool flag)
    {
      decimal ret;

      ret = flag ? -1M : 0M;
      return ret;
    }

    public AirBaseRef getAirBaseRef(AirBases airBase)
    {
      AirBaseRef ret;
      int index;

      switch(airBase)
      {
        default:
          Debug.Assert(false);
          index = 4;
          break;
        case AirBases.Enterprise:
          index = 4;
          break;
        case AirBases.Hornet:
          index = 5;
          break;
        case AirBases.Yorktown:
          index = 6;
          break;
        case AirBases.Midway:
          index = 7;
          break;
        case AirBases.Akagi:
          index = 0;
          break;
        case AirBases.Kaga:
          index = 1;
          break;
        case AirBases.Soryu:
          index = 2;
          break;
        case AirBases.Hiryu:
          index = 3;
          break;
        case AirBases.Zuiho:
          index = 8;
          break;
      }
      ret = new AirBaseRef
      {
        airBase = airBase,
        name = this.vessels[index],
        cap = (int)this.C[index, 7]
      };
      ret.armed.f4fs = (int) this.C[index, 4];
      ret.armed.sbds = (int) this.C[index, 5];
      ret.armed.tbds = (int) this.C[index, 6];
      ret.below.f4fs = (int)this.C[index, 1];
      ret.below.sbds = (int)this.C[index, 2];
      ret.below.tbds = (int)this.C[index, 3];
      ret.damage = this.C[index, 8];
      return ret;
    }

    private void Initialize()
    {
      int i;
      int q;
      decimal j;
      decimal k;
      decimal l;

      /*
      Console.Out.WriteLine(this.arcTan(-1, -1));
      Console.Out.WriteLine(this.arcTan(1, -1));
      Console.Out.WriteLine(this.arcTan(1, 1));
      Console.Out.WriteLine(this.arcTan(-1, 1));
      */
      this.allJapaneseCarriersIncapacitated = false;
      this.v0 = 0;
      this.v1 = 0;
      this.p1 = 1M; // 0.017453293M;
      this.t = 720;
      this.d = 3;
      for (i = 0; i <= 5; ++i)
      {
        j = this.initArray[i, 0];
        k = this.initArray[i, 1];
        l = this.initArray[i, 2];
        l += this.rand(175) - this.rand(200) * mVal(i < 3);
        j = (j + this.rand(k)) * p1;
        this.F[i, 0] = 850.0M - l * cos(j) * mVal(i != 5);
        this.F[i, 1] = 450.0M - l * sin(j) * mVal(i != 5);
        if (i >= 3)
        {
          if (this.F[i, 0] > 1124M)
          {
            this.F[i, 0] = 1124M;
          }
          if (this.F[i, 1] > 1149M)
          {
            this.F[i, 1] = 1149M;
          }
        }
        j = j + 180M * this.p1 + 360M * p1 * mVal(j > 180 * p1);
        if (i < 3)
        {
          this.F[i, 4] = j;
        }
        else
        {
          this.F[i, 4] = 205M * p1 * (-mVal(i != 5));
        }
      }
      this.c7 = 0;
      this.firstCruiserAttack = true;
      this.c5 = 0;
      this.C[8, 7] = this.C[8, 1];
      this.C[8, 1] = 0;
      for (i = 4; i <= 7; ++i)
      {
        for (q = 4; q <= 6; ++q)
        {
          this.C[i, q] = this.C[i, q - 3];
          this.C[i, q - 3] = 0M;
        }
      }
      for (i = 3; i <= 4; ++i)
      {
        this.F[i, 4] = this.getHeading(i, 5);
      }
      this.s6 = 0.041M;
      this.s7 = 0.043M;
      this.PlaceShips();
    }

    // set heading ?
    /*
    3460 A=F(Y,0)-F(X,0):Y=F(Y,1)-F(X,1):X=A:IF Y=0 THEN A=(90-180*(X<0))*P1:RETURN
    3470 A=ATN(X/Y):IF Y>0 THEN A=A-360*P1*(A<0):RETURN
    3480 A=A+180*P1:RETURN
    */
    private decimal getHeading (int xi, int yi)
    {
      decimal a;
      decimal x;
      decimal y;

      a = this.F[yi, 0] - this.F[xi, 0];
      x = xi;
      y = this.F[yi, 1] - this.F[xi, 1];
      x = a;
      if (y == 0)
      {
        a = (90 - 180 * intVal(x < 0)) * this.p1;
      }
      else
      {
        a = this.arcTan(x, y);
        if (y > 0)
        {
          a = a - 360M * this.p1 * mVal(a < 0M);
        }
        else
        {
          a = a + 180M * this.p1;
        }
      }
      return a;
    }

    public void Play()
    {
      string? cmd;
      int i;

      this.Initialize();
      for (; ;)
      {


        cmd = Console.In.ReadLine();
      }
    }


    /*
10 DIM F(5,7),C(8,9),S(9,9),W(5),FX(5),FY(5),FZ(5),C1(3)
20 RESTORE:CLS:SCREEN 0:KEY OFF:LOCATE 7,9,0:PRINT "** MIDWAY CAMPAIGN **":LOCATE 11,2
30 Y$=TIME$:Z$=MID$(Y$,1,2)+MID$(Y$,4,2)+MID$(Y$,7,2):RANDOMIZE((VAL(Z$)-INT(VAL(Z$)/65538!)*65538!)-32768!)
40 PRINT "                                     "
50 J9=0:V0=0:V1=0:P1=.017453293#:T=720:D=3:M$="12367M"
60 FOR I=0 TO 5:FOR J=2 TO 7:READ F(I,J):NEXT J,I
70 FOR I=0 TO 5:READ J,K,L:GOSUB 3050:NEXT:F9=1
80 FOR I=0 TO 8:FOR J=0 TO 3:READ C(I,J):NEXT:FOR J=4 TO 8:C(I,J)=0:NEXT J,I:C7=0:C6=1:C5=0
90 FOR I=0 TO 5:READ W(I):FX(I)=0:FY(I)=0:FZ(I)=0:NEXT:C(8,7)=C(8,1):C(8,1)=0
100 FOR I=4 TO 7:FOR J=4 TO 6:C(I,J)=C(I,J-3):C(I,J-3)=0:NEXT J,I
110 FOR I=3 TO 4:X=I:Y=5:GOSUB 3460:F(I,4)=A:NEXT
170 FOR I=0 TO 9:S(I,9)=-1:NEXT:S6=.041:S7=.043:CLS:SCREEN 0:FOR X=4 TO 7:LOCATE 12+X,1

180 GOSUB 3810:NEXT
190 FOR I=1 TO 12:LOCATE I+1,1:PRINT ". . . . . . . . . . . .";:NEXT
200 LOCATE 2,25:PRINT "TF-16";:LOCATE 4,25:PRINT "TF-17";
210 LOCATE 14,11:PRINT "CAP - ON DECK - -- BELOW --";:LOCATE 15,15:PRINT "F4F SBD TBD F4F SBD TBD";
220 GOSUB 3910
230 GOSUB 3530:GOSUB 3650:GOSUB 3670:IF F9=0 THEN 890
240 GOSUB 3790:IF F9=0 THEN 860
250 GOSUB 4270:LINE INPUT;"COMMAND ",A$:LOCATE 22,1:IF A$="" THEN 860
260 GOSUB 4150
270 X=ASC(A$):IF X>47 AND X<58 THEN 870
280 IF MID$(A$,1,1)="T" THEN 430
290 IF MID$(A$,1,1)="A" THEN 480
300 IF MID$(A$,1,1)="L" THEN 670
310 IF LEN(A$)=1 THEN 340
320 IF MID$(A$,1,2)="CA" THEN 600
330 IF MID$(A$,1,2)="CL" THEN 580
340 BEEP:GOSUB 3790:PRINT "COMMANDS ARE:":PRINT "T-CHANGE TF COURSE  CA-SET CAP"
350 PRINT "A-ARM STRIKE        CL-CLEAR DECK"
360 PRINT "L-LAUNCH STRIKE      #-WAIT # HOURS";:X=600:GOSUB 4000
370 GOSUB 4300:GOSUB 3790:PRINT "TRY AGAIN. ";:GOTO 250
380 I=0:GOSUB 4270:LINE INPUT;"WHICH CARRIER ",A$:LOCATE 23,1:IF A$="" THEN RETURN
390 GOSUB 4150
400 I=ASC(A$):I=-4*(I=69)-5*(I=72)-6*(I=89)-7*(I=77):IF I=0 THEN RETURN
410 IF C(I,8)<60 THEN RETURN
420 X=I:I=0:GOSUB 3810:PRINT " IS NOT OPERATIONAL.":X=300:GOSUB 4000:RETURN
430 GOSUB 4270:LINE INPUT;"WHICH TASK FORCE ",A$:LOCATE 23,1:IF A$="" THEN 370
440 I=LEN(A$):I=ASC(MID$(A$,I,1))-51:IF I<>3 AND I<>4 THEN 370
450 PRINT USING "NEW COURSE FOR TF-## ";I+13;:GOSUB 2940
460 IF J<0 OR J>360 THEN 370
470 F(I,4)=J*P1:GOSUB 3650:GOTO 250
480 GOSUB 380:IF I=0 THEN 370
490 X=I:IF C(I,4)+C(I,6)=0 AND C(I,5)=1000 THEN C(I,5)=0
500 IF C(I,4)+C(I,5)+C(I,6)=0 THEN 520
510 GOSUB 3810:PRINT " STRIKE ALREADY ON DECK.":X=300:GOSUB 4000:GOTO 370
520 GOSUB 3790:PRINT "BRING AIRCRAFT TO ";:GOSUB 3810:PRINT " DECK.":PRINT "F4F,SBD,TBD:":GOSUB 2940
530 IF J>C(I,1) THEN J=C(I,1)
540 IF K>C(I,2) THEN K=C(I,2)
550 IF L>C(I,3) THEN L=C(I,3)
560 C(I,4)=J:C(I,1)=C(I,1)-J:C(I,5)=1000+K:C(I,2)=C(I,2)-K:C(I,6)=L:C(I,3)=C(I,3)-L:GOSUB 3910:GOTO 250
570 C(I,5)=1000+K
580 GOSUB 380:IF I=0 THEN 370
590 GOSUB 3120:GOSUB 3910:GOTO 250
600 GOSUB 380:IF I=0 THEN 370
610 GOSUB 3790:PRINT "F4F's FOR ";:X=I:GOSUB 3810:PRINT " CAP:":GOSUB 2940
620 C(I,1)=C(I,1)+C(I,7):C(I,7)=0:IF J>C(I,1) THEN 640
630 C(I,7)=J:C(I,1)=C(I,1)-J:GOTO 660
640 C(I,7)=C(I,1):C(I,1)=0:J=J-C(I,7):C(I,7)=C(I,7)-J*(J<C(I,4))-C(I,4)*(J>=C(I,4))
650 C(I,4)=-(C(I,4)-J)*(J<C(I,4))
660 GOSUB 3910:GOTO 250
670 L=0:FOR K=0 TO 2:IF F(K,2)>0 THEN L=L+1:C1(L)=K
680 NEXT:IF L=0 THEN PRINT "NO TARGETS.":X=300:GOSUB 4000:GOTO 370
690 GOSUB 380:IF I=0 THEN 370
700 IF C(I,5)+C(I,6)>0 AND C(I,5)<1000 THEN 720
710 X=I:GOSUB 3810:PRINT " HAS NO STRIKE READY.":X=300:GOSUB 4000:GOTO 370
720 J=L:C=L:IF L>1 THEN PRINT "TARGET CONTACT ";:GOSUB 2940:IF J<1 OR J>C THEN 370
730 J=C1(J):X=J:Y=C(I,0):GOSUB 3490:IF R<=200 THEN 750
740 GOSUB 3790:PRINT -INT(-R);" NAUTICAL MILES, OUT OF RANGE.":X=300:GOSUB 4000:GOTO 370
750 L=R*.3:IF I=7 OR (T+L+L>240 AND T+L+L<=1140) THEN 770
760 GOSUB 3790:PRINT "NO NIGHT CARRIER LANDINGS.":X=300:GOSUB 4000:GOTO 370
770 IF T+L>=240 AND T+L<=1140 THEN 790
780 GOSUB 3790:PRINT "NO NIGHT ATTACKS.":X=300:GOSUB 4000:GOTO 370
790 K=0
800 IF S(K,9)<0 THEN 830
810 K=K+1:IF K<10 THEN 800
820 GOSUB 3790:PRINT "TOO MANY STRIKES ALOFT.":X=300:GOSUB 4000:GOTO 370
830 S(K,0)=C(I,4):S(K,2)=C(I,5):S(K,4)=C(I,6):C(I,4)=0:C(I,5)=0:C(I,6)=0:S(K,6)=J:S(K,9)=I
840 S(K,7)=T+L:S(K,8)=T+L+L:S(K,3)=1:S(K,5)=0:S(K,1)=-(S(K,2)/(S(K,2)+S(K,4))>RND)
850 X=I:GOSUB 3810:PRINT " STRIKE TAKING OFF.";:GOSUB 4170:GOSUB 3910:GOTO 250
860 A$="0"
870 GOSUB 3790:T0=T+INT(VAL(A$)*60):D0=D-(T0>1440):T0=T0+1440*(D0>D)
880 FOR I=4 TO 7:C(I,5)=C(I,5) MOD 1000:NEXT
890 FOR I=3 TO 4:X=I:Y=2:GOSUB 3490:IF R<50 THEN C5=10
900 NEXT:F9=0:X=1:Y=5:GOSUB 3490:IF R<15 THEN F(1,5)=0
910 IF J9<>0 THEN F(1,4)=270*P1:F(1,5)=18
920 IF J9>0 OR C5>9 THEN F(2,5)=25+15*(C7>255):F(2,4)=270*P1
930 IF C5>9 THEN 990
940 X=2:Y=5:GOSUB 3490:IF R>15 THEN 990
950 PRINT "CRUISERS BOMBARD ";:X=7:F(2,2)=2:C5=C5+1:IF J9=0 AND C7<=255 THEN F(2,5)=0
960 IF C6>0 THEN F9=1:C6=0
970 N=0:H=0:FOR K=C7 TO 255 STEP 4:R=RND:H=H-(R<.05):N=N-(R<.1):NEXT:N=N-H:D8=24:GOSUB 3190
980 GOSUB 3910
990 X=5:Y=0:GOSUB 3490:IF R>250 THEN X=0:Y=5:GOSUB 3460:F(0,4)=A:
1000 IF R<100 THEN X=5:Y=0:GOSUB 3460:F(0,4)=A
1010 FOR K=6 TO 4 STEP -1:X=0:Y=C(K,0):IF F(Y,2)>0 AND C(K,8)<100 THEN GOSUB 3460:F(0,4)=A
1020 NEXT
1030 IF J9>0 THEN F(0,4)=270*P1
1040 FOR I=0 TO 3:IF C(I,7)=5 OR C(I,8)>=60 THEN 1080
1050 C(I,7)=C(I,7)+C(I,1):C(I,1)=0:IF C(I,7)<5 THEN 1070
1060 C(I,1)=C(I,7)-5:C(I,7)=5:GOTO 1080
1070 C(I,7)=C(I,7)+C(I,4):C(I,4)=0:IF C(I,7)>5 THEN C(I,4)=C(I,7)-5:C(I,7)=5
1080 NEXT
1090 S9=0:A9=S9:A8=S9:I=0:IF T>1140 THEN 1330
1100 IF C(I,4)+C(I,5)+C(I,6)>0 THEN I=4:GOTO 1130
1110 I=I+1:IF I<4 THEN 1100
1120 S9=0:GOTO 1280
1130 IF C(I,8)>=60 THEN 1150
1140 X=C(I,0):Y=0:GOSUB 3500:IF E=1 THEN 1160
1150 I=I+1:IF I<8 THEN 1130
1160 IF I<8 THEN 1230
1170 I=4
1180 IF C(I,8)>=100 THEN 1200
1190 X=C(I,0):Y=0:GOSUB 3500:IF E=1 THEN 1210
1200 I=I+1:IF I<8 THEN 1180
1210 IF I<8 THEN 1230
1220 Y=0:X=5:GOSUB 3500:I=-7*(E=1)
1230 S9=C(I,0):IF S9<5 THEN 1280
1240 I=0
1250 IF S(I,6)<5 OR S(I,9)=-1 OR S(I,1)=-1 THEN 1270
1260 S9=0:GOTO 1280
1270 I=I+1:IF I<10 THEN 1250
1280 IF F(3,2)+F(4,2)>0 THEN A9=1
1290 Y=0:X=5:GOSUB 3490:IF R>235 THEN 1320
1300 L=60*R/235:IF T+L<240 OR T+L+L>1140 THEN 1320
1310 A8=1:IF C(3,2)<12 THEN A9=1
1320 IF A9=1 THEN A8=0
1330 IF S9<3 THEN 1430
1340 J=0
1350 IF S(J,9)=-1 THEN 1380
1360 J=J+1:IF J<10 THEN 1350
1370 GOTO 1430
1380 S(J,6)=S9:S(J,9)=0:X=0:Y=S9:GOSUB 3490:L=60*R/235:S(J,7)=T+L:S(J,8)=T+L+L:S(J,0)=0:S(J,2)=0:S(J,4)=0
1390 FOR I=0 TO 3:IF C(I,8)>60 THEN 1410
1400 S(J,0)=S(J,0)+C(I,4):S(J,2)=S(J,2)+C(I,5):C(I,4)=0:C(I,5)=0:S(J,4)=S(J,4)+C(I,6):C(I,6)=0
1410 NEXT:IF S(J,2)+S(J,4)=0 THEN S(J,9)=-1
1420 S(J,3)=1:S(J,5)=0:IF S(J,9)<>-1 THEN S(J,1)=ABS((S(J,2)/(S(J,2)+S(J,4)))>RND)
1430 FOR I=0 TO 3:GOSUB 3120:IF C(I,8)>60 THEN 1500
1440 IF A9=0 THEN 1460
1450 C(I,4)=C(I,1):C(I,5)=C(I,2):C(I,6)=C(I,3):C(I,1)=0:C(I,2)=0:C(I,3)=0:GOTO 1490
1460 IF A8=0 THEN 1490
1470 C(I,4)=INT(C(I,3)/2):C(I,5)=INT(C(I,2)/2):C(I,1)=C(I,1)-C(I,4):C(I,2)=C(I,2)-C(I,5)
1480 C(I,6)=INT(C(I,3)/2):C(I,3)=C(I,3)-C(I,6)
1490 IF S9+A8+A9=0 THEN C(I,7)=C(I,7)+C(I,1):C(I,1)=0
1500 NEXT
1510 T1=30+INT(30*RND):T=T+T1:IF T>=T0 AND D=D0 THEN F9=1
1520 D=D-(T>1440):T=T+1440*(T>1440):IF T>=T0 AND D>=D0 THEN F9=1
1530 FOR I=0 TO 4:F(I,0)=F(I,0)+T1*F(I,5)*SIN(F(I,4))/60
1540 F(I,1)=F(I,1)+T1*F(I,5)*COS(F(I,4))/60:NEXT
1550 IF T>1140 OR T<240 THEN 1720
1560 P=1-2*(T<300 OR (T>720 AND T<780)):FOR I=0 TO 2:IF F(I,2)=2 OR (C7>=512 AND I=2) THEN 1650
1570 IF F(I,5)=0 THEN F(I,5)=2
1580 IF F(I,2)=1 AND RND>3*S7 THEN 1650
1590 IF RND>P*S7 AND F(I,2)=0 THEN 1650
1600 F(I,2)=F(I,2)-(F(I,2)<2)
1610 IF RND>3*S7 THEN 1630
1620 F(I,2)=F(I,2)-(F(I,2)<2)
1630 PRINT "PBY SPOTS JAPANESE ";:IF F(I,2)=1 THEN PRINT "SHIPS." ELSE X=I:GOSUB 4010:PRINT "."
1640 F9=1:X=300:GOSUB 4000:GOSUB 3670
1650 NEXT:IF F(0,2)=2 THEN F(0,3)=2
1660 P=1-(T>720 AND T<780):FOR I=3 TO 4:IF F(I,2)=2 THEN 1710
1670 IF RND<P*S6 THEN F(I,2)=1
1680 IF F(I,2)=0 OR RND>3*S6 THEN 1710
1690 PRINT "JAPANESE SCOUT PLANES SIGHTED OVER":X=I:GOSUB 4010:PRINT ".":X=300:GOSUB 4000
1700 F(I,2)=2:F9=1:GOSUB 3650:I=X
1710 NEXT:GOTO 1730
1720 FOR I=0 TO 4:F(I,2)=0:NEXT:F(0,3)=1
1730 FOR I=0 TO 9:IF S(I,9)=-1 OR S(I,7)>T OR S(I,1)=-1 THEN 2430
1740 IF S(I,6)=2 AND C7>511 THEN 2430
1750 J=1-(S(I,6)>2):IF S(I,6)=5 THEN 1830 'J=1 FOR JAPS
1755 GOSUB 5000
1760 FOR K=0 TO 4 STEP 2
1770 IF S(I,K)=0 THEN 1790
1780 IF RND>(S(I,8)-S(I,7)-20)/100 THEN 1800
1790 S(I,K+1)=-1
1800 NEXT
1810 IF S(I,1)=-1 THEN 1830
1820 IF S(I,5-S(I,1)*2)=-1 THEN S(I,1)=1-S(I,1):IF S(I,5-S(I,1)*2)=-1 THEN S(I,1)=-1 'PUT FTR COVER ON WHO EVER MAKES IT THRU
1825 GOSUB 5000
1830 IF J=2 THEN 1870
1840 X=0:FOR K=0 TO 4 STEP 2:IF S(I,K)=0 OR S(I,K+1)>-1 THEN 1860
1850 X=S(I,9):GOSUB 3810:X=J:Y=K:PRINT " ";:GOSUB 4080:PRINT " MISS TARGET,":F9=1:X=300
1860 NEXT:GOSUB 4000:GOSUB 3790
1870 IF S(I,3)+S(I,5)=-2 OR S(I,2)+S(I,4)=0 THEN 2430
1880 F(C(S(I,9),0),2)=2:F(S(I,6),2)=2
1890 IF F(0,2)=2 THEN F(0,3)=2
1900 IF J=1 THEN X=S(I,9):GOSUB 3810
1910 IF J=2 THEN PRINT "JAPANESE";
1920 PRINT " AIR STRIKE IS ATTACKING":IF J=1 THEN PRINT "JAPANESE ";
1930 X=S(I,6):GOSUB 4010:PRINT "!":GOSUB 4240:GOSUB 3790:F9=1
1940 K=0:IF S(I,6)=2 THEN 1980
1950 IF S(I,6)=C(K,0) AND C(K,8)<100 THEN 2060
1960 K=K+1:IF K<9 THEN 1950
1970 REM ATTACK FLEETS
1980 A$="IN":GOSUB 3140:FOR K=4 TO 2 STEP -2:IF S(I,K)=0 OR S(I,K+1)=-1 THEN 2030
1990 PRINT S(I,K);" ";:X=J:Y=K:GOSUB 4080:PRINT " ATTACK ";
2000 E=F(S(I,6),6)*(1+.25*(K=4)*(1-(J=1))):H=0:N=0:FOR L=1 TO S(I,K):R=RND
2010 H=H-(R<E):N=N-(R<E+E):NEXT:N=N-H:D8=16:IF K=4 AND S(I,6)<>5 THEN D8=24:N=0
2020 X=S(I,6):GOSUB 3350
2030 NEXT
2040 A$="OUT":GOSUB 3140:GOTO 2420
2050 REM ATTACK CARRIERS
2060 C=0:FOR K=0 TO 8:IF C(K,0)=S(I,6) THEN C=C+C(K,7):C(K,7)=0
2070 NEXT:IF C=0 THEN 2250
2080 K=2-2*(RND>.5)
2090 IF S(I,K+1)=-1 OR S(I,K)=0 THEN K=6-K:IF S(I,K+1)=-1 OR S(I,K)=0 THEN 2350
2100 X=J:Y=K:PRINT "CAP ATTACKS ";:GOSUB 4080:PRINT ".":L1=0:IF 4-S(I,1)*2=K THEN L1=S(I,0) 'L1 IS FTRS PROTECTING
2110 IF L1>0 THEN X=J:Y=0:GOSUB 4080:Y=K:PRINT " DEFEND ";:GOSUB 4080:PRINT "."
2120 E=(C*W(J-1))/(L1*W(ABS(J=1))+S(I,K)*W(K-(J=1))):IF E>.8499999 THEN E=.8499999
2130 H=0:FOR L=1 TO S(I,K):H=H-(RND<E):NEXT
2140 PRINT "CAP SHOOTS DOWN";H;" ";:X=J:Y=K:GOSUB 4080:PRINT ".":S(I,K)=S(I,K)-H:IF L1=0 THEN 2230
2150 X=300:GOSUB 4000:GOSUB 3790:X=J:Y=0:GOSUB 4080:PRINT " ATTACK CAP."
2160 E=(L1*W(ABS(J=1)))/(C*W(J-1)):IF E>.8499999 THEN E=.8499999
2170 H=0:FOR L=1 TO C:H=H-(RND<E):NEXT
2180 X=J:Y=0:GOSUB 4080:PRINT " SHOOT DOWN";H;" ";:X=1-(J=1):GOSUB 4080:PRINT "."
2190 C=C-H:IF C=0 THEN 2240
2200 E=.5*(C*W(J-1))/(L1*W(ABS(J=1))):IF E>.8499999 THEN E=.8499999
2210 H=0:FOR L=1 TO L1:H=H-(RND<E):NEXT:PRINT "CAP SHOOTS DOWN";H;" ";:X=J:GOSUB 4080:PRINT "."
2220 S(I,0)=S(I,0)-H
2230 IF (S(I,3)=-1 OR S(I,2)=0) AND (S(I,5)=-1 OR S(I,4)=0) THEN 2350
2240 X=300:GOSUB 4000:GOSUB 3790
2250 A$="IN":GOSUB 3140:FOR K=4 TO 2 STEP -2:IF S(I,K)=0 OR S(I,K+1)=-1 THEN 2340
2260 M=0:FOR L=0 TO 8:C(L,9)=0:M=M-(C(L,8)<100 AND C(L,0)=S(I,6)):NEXT
2270 O=-1:FOR N=1 TO M
2280 O=O+1:IF C(O,0)<>S(I,6) OR (C(O,8)>=100 AND M>0) THEN 2280
2290 C(O,9)=INT((S(I,K)+M-(M=0)-N)/(M-(M=0))):NEXT:FOR L=0 TO 8:IF C(L,9)=0 THEN 2330
2300 PRINT C(L,9);" ";:X=J:Y=K:GOSUB 4080:PRINT " ATTACK ";
2310 N=0:H=0:E=.2-(K=4)*.06*(J=1):FOR M=1 TO C(L,9):R=RND:H=H-(R<E):N=N-(R<E+E):NEXT
2320 D8=16-8*(K=4 AND L<>7):N=-(N-H)*(D8=16):X=L:GOSUB 3190:GOSUB 3910
2330 NEXT
2340 NEXT:A$="OUT":X=300:GOSUB 4000:GOSUB 3790:GOSUB 3140
2350 IF C=0 THEN 2420
2360 M=0:FOR L=0 TO 8:C(L,9)=0:M=M-(C(L,8)<=60 AND C(L,0)=S(I,6)):NEXT
2370 IF M=0 THEN X=S(I,6):GOSUB 4010:PRINT " CAP SPLASHES.":X=300:GOSUB 4000:GOTO 2420
2380 O=-1:FOR N=1 TO M
2390 O=O+1:IF C(O,0)=S(I,6) AND C(O,8)<=60 THEN C(O,7)=INT((C+M-N)/M):GOTO 2410
2400 GOTO 2390
2410 NEXT
2420 FOR K=1 TO 5 STEP 2:S(I,K)=-1:NEXT:GOSUB 3910
2430 NEXT
2440 FOR L=0 TO 8:IF C(L,8)<10 OR C(L,8)>=100 THEN 2490
2450 IF RND>.05*(1-(L<4)) THEN 2470
2460 X=L:PRINT "EXPLOSION ON ";:D8=12:K=2:N=0:H=1:GOSUB 3190:GOSUB 3910
2470 IF C(L,8)>=100 OR RND>.2*(1-(L>3 AND L<8)) THEN 2490
2480 C(L,8)=C(L,8)-5*RND:IF C(L,8)<0 THEN C(L,8)=0
2490 NEXT
2500 FOR J=0 TO 9:IF S(J,9)=-1 THEN 2710
2510 IF T<S(J,8) THEN 2710
2520 IF S(J,9)<4 THEN 2630
2530 F9=1:I=S(J,9):IF C(I,8)>60 THEN 2560
2540 PRINT "STRIKE LANDING ON ";:X=I:GOSUB 3810:PRINT ".":GOSUB 3120:C(I,1)=C(I,1)+S(J,0)
2550 C(I,2)=C(I,2)+S(J,2):C(I,3)=C(I,3)+S(J,4):GOSUB 3910:GOTO 2700
2560 IF I>5 OR (C(4,8)>60 AND C(5,8)>60) THEN K=3:GOTO 2580
2570 K=4-(I=4):GOTO 2620
2580 K=K+1:IF C(K,8)>60 THEN 2600
2590 X=C(I,0):Y=C(K,0):GOSUB 3490:IF R/100<RND THEN 2620
2600 IF K<7 THEN 2580
2610 X=I:GOSUB 3810:PRINT " STRIKE SPLASHES!":X=300:GOSUB 4000:GOSUB 3790:GOTO 2700
2620 X=I:GOSUB 3810:PRINT " STRIKE DIVERTED TO ";:I=K:X=I:GOSUB 3810:PRINT:GOTO 2540
2630 L=0:FOR I=0 TO 3:GOSUB 3120:L=L-(C(I,8)<=60):NEXT:IF L=0 THEN 2700
2640 FOR K=0 TO 4 STEP 2:M=-1:FOR I=1 TO L
2650 M=M+1:IF C(M,8)>=60 THEN 2650
2660 C(M,1+K/2)=C(M,1+K/2)+INT((L+S(J,K)-I)/L):NEXT I,K:FOR I=0 TO 3
2670 IF C(I,1)+C(I,2)+C(I,3)<96 THEN 2690
2680 FOR K=1 TO 3:C(I,K)=C(I,K)+(C(I,K)>0):NEXT:GOTO 2670
2690 NEXT
2700 S(J,9)=-1
2710 NEXT:V2=0:FOR J=0 TO 9:IF S(J,9)<>-1 THEN V2=1:J=10
2720 NEXT
2730 X=0:FOR I=0 TO 3:X=X-(C(I,8)<=60):NEXT:J9=-(X=0):IF V2=1 THEN 230
2740 IF J9=1 AND F(0,0)<0 THEN 2780
2750 C=0:FOR I=0 TO 3:C=C+C(I,8):NEXT:IF C>=400 THEN 2780
2760 IF F(3,0)>=1150 OR F(4,0)>=1150 THEN 2780
2770 C=0:FOR I=4 TO 7:C=C+C(I,8):NEXT:IF C<400 THEN 230
2780 CLS:LOCATE 1,1:PRINT "THE BATTLE IS OVER. REPORT:":V2=0:V3=0:P=0:V=0:PRINT "CARRIER    DAMAGE":PRINT "__________ ______"
2790 FOR X=0 TO 3:GOSUB 2900:NEXT:X=8:V2=V2+V:V=0:GOSUB 2900:V2=V2+V/2:P1=302-P:V=0:P=0
2800 PRINT "__________ ______"
2810 FOR X=4 TO 7:GOSUB 2900:NEXT:PRINT:PRINT "THE JAPANESE LOST";P1;" PLANES.":V2=V2+P1*5
2820 P=269-P:V3=V3+V+P*5:PRINT "THE UNITED STATES LOST";P;" PLANES.":PRINT
2830 X=5:GOSUB 4010:PRINT " HAS ";:IF J9>0 THEN PRINT "NOT ";
2840 PRINT "FALLEN.":V3=V3-1000*(J9=0):V0=V2+V0:V1=V1+V3:V=V0-V1:IF V<0 THEN 2860
2850 PRINT "UNITED STATES ";:GOTO 2870
2860 PRINT "JAPANESE ";:V=-V
2870 A$="MARGINAL":IF V>=1000 THEN A$="TACTICAL":IF V>=2000 THEN A$="STRATEGIC"
2880 PRINT A$;" VICTORY":PRINT:PRINT:INPUT;"PLAY AGAIN (Y/N) ",A$:GOSUB 4150:IF A$="Y" THEN 20
2890 END
2900 GOSUB 3810
2910 A$="NONE":IF C(X,8)>0 THEN A$="LIGHT":IF C(X,8)>=60 THEN A$="HEAVY":IF C(X,8)>=100 THEN A$="SUNK":IF X=7 THEN A$="DESTROYED"
2920 LOCATE ,12:PRINT A$:V=V-100*(C(X,8)>0)-200*(C(X,8)>=60)-700*(C(X,8)>=100)
2930 FOR Y=1 TO 7:P=P+C(X,Y):NEXT:RETURN
2940 GOSUB 4270:LINE INPUT;A$:LOCATE 24,1:J=0:K=J:L=J:IF LEN(A$)=0 THEN RETURN
2950 GOSUB 4150:IF MID$(A$,1,1)="A" THEN J=999:K=J:L=J:RETURN
2960 X=1:GOSUB 2980:J=L:L=0:IF X>LEN(A$) THEN RETURN
2970 GOSUB 2980:K=L:L=0:IF X>LEN(A$) THEN RETURN
2980 Y=X
2990 IF Y>LEN(A$) THEN 3010
3000 L=ASC(MID$(A$,Y,1)):IF L>47 AND L<58 THEN Y=Y+1:GOTO 2990
3010 L=0:IF Y>X THEN L=VAL(MID$(A$,X,Y-X))
3020 X=Y:IF X>=LEN(A$) THEN RETURN
3030 Y=ASC(MID$(A$,X,1)):IF (Y<48 OR Y>57) AND X<LEN(A$) THEN X=X+1:GOTO 3030
3040 RETURN

3050 L=L+175*RND-200*RND*(I<3):J=(J+K*RND)*P1
3060 F(I,0)=850-L*SIN(J)*(I<>5):F(I,1)=450-L*COS(J)*(I<>5):IF I<3 THEN 3090
3070 IF F(I,0)>1124 THEN F(I,0)=1124
3080 IF F(I,1)>1149 THEN F(I,1)=1149
3090 J=J+180*P1+360*P1*(J>180*P1)
3100 IF I<3 THEN F(I,4)=J:RETURN
3110 F(I,4)=205*P1*-(I<>5):RETURN

3120 C(I,5)=C(I,5) MOD 1000
3130 C(I,1)=C(I,1)+C(I,4):C(I,4)=0:C(I,2)=C(I,2)+C(I,5):C(I,5)=0:C(I,3)=C(I,3)+C(I,6):C(I,6)=0:RETURN
3140 PRINT "ON THE WAY ";A$;", ";:X=S(I,6):GOSUB 4010:PRINT " AA":PRINT "SHOOTS DOWN ";
3150 FOR K=0 TO 4 STEP 2:IF S(I,K)=0 OR S(I,K+1)=-1 THEN 3180
3160 E=F(S(I,6),7)*(-.4*(K=0)-.7*(K=2)-(K=4)):H=0:FOR X=1 TO S(I,K):H=H-(RND<E):NEXT
3170 PRINT H;" ";:X=J:Y=K:GOSUB 4080:PRINT ". ";:S(I,K)=S(I,K)-H
3180 NEXT:X=300:GOSUB 4000:GOTO 3790
3190 IF C(X,8)>=100 THEN X=C(X,0):GOTO 3350
3200 GOSUB 3810:PRINT "!":GOSUB 3810:GOSUB 3440:S=ABS((K=2 OR X=7) AND C(X,4)+C(X,5)+C(X,6)>0)
3210 IF S AND H+N>0 THEN PRINT "SECONDARY EXPLOSIONS!";
3220 D9=D8*(1+S):IF H>0 THEN FOR Y=1 TO H:GOSUB 3250:NEXT
3230 D9=D8*(1+S+S)/3:IF N>0 THEN FOR Y=1 TO N:GOSUB 3250:NEXT
3240 RETURN
3250 D7=D9*RND:IF X=7 THEN D7=D7/3
3260 IF X=8 THEN D7=D7*2
3270 IF K=4 AND X<>7 THEN 3310
3280 FOR L1=1 TO 6-(T<240 OR T>1140):IF C(X,L1)=0 THEN 3300
3290 FOR L2=1 TO C(X,L1):C(X,L1)=C(X,L1)+(RND*100<D7):NEXT
3300 NEXT
3310 C(X,8)=C(X,8)+D7:IF C(X,8)<60 THEN RETURN
3320 L1=I:I=X:GOSUB 3120:I=L1:IF C(X,8)<100 THEN RETURN
3330 F9=1:C(X,8)=100:H=0:N=0:Y=100:GOSUB 3790:GOSUB 3810:IF X=7 THEN PRINT " AIRBASE DESTROYED!" ELSE PRINT " BLOWS UP AND SINKS!"
3340 FOR L1=1 TO 7:C(X,L1)=0:NEXT:RETURN
3350 GOSUB 4010:PRINT "!":GOSUB 4010:GOSUB 3440:V=INT(D8*(H+N/3))
3360 PRINT V;" VICTORY POINTS AWARDED.";:V1=V1+V:IF J=1 THEN V0=V0+V:V1=V1-V
3370 IF X<>2 THEN 3430
3380 C7=C7+V:IF C7<255 THEN 3430
3390 IF C7-V<255 THEN X=300:GOSUB 4000:GOSUB 3790:PRINT "CRUISERS SEVERELY CRIPPLED.":F(2,6)=F(2,6)*3
3400 F(2,5)=10:C5=10:IF C7<512 THEN 3430
3410 X=300:GOSUB 4000:GOSUB 3790:PRINT "ALL CRUISERS ARE SUNK!":V0=V0-C7+512:C7=512
3420 F(2,2)=0:F(2,5)=0:F(2,0)=-1000
3430 X=300:GOSUB 4000:GOTO 3790
3440 PRINT " TAKES";H;" HITS";:IF N>0 THEN PRINT:PRINT "AND";N;" NEAR MISSES";
3450 PRINT ".":RETURN
3460 A=F(Y,0)-F(X,0):Y=F(Y,1)-F(X,1):X=A:IF Y=0 THEN A=(90-180*(X<0))*P1:RETURN
3470 A=ATN(X/Y):IF Y>0 THEN A=A-360*P1*(A<0):RETURN
3480 A=A+180*P1:RETURN
3490 R=SQR((F(X,0)-F(Y,0))^2+(F(X,1)-F(Y,1))^2):RETURN
3500 E=0:GOSUB 3490:IF F(X,2)=0 OR R>235 THEN RETURN
3510 L=R*60/235:IF T+L<240 OR T+L+L>1140 THEN RETURN
3520 E=1:RETURN
3530 L=0:FOR I=0 TO 5:IF FZ(I)=0 AND I<3 THEN 3560
3540 J=FX(I):K=FY(I):A$=" ":IF J MOD 2=0 THEN A$="."
3550 GOSUB 3630
3560 FZ(I)=0:FX(I)=INT(F(I,0)*.02+.5):FY(I)=INT(F(I,1)*.01+.5):NEXT:FOR I=0 TO 5:IF I>2 THEN 3590
3570 IF F(I,2)=0 THEN 3610
3580 L=L+1:A$=MID$(M$,L,1):GOTO 3600
3590 A$=MID$(M$,I+1,1)
3600 J=FX(I):K=FY(I):FZ(I)=1:GOSUB 3630
3610 NEXT:K=INT(T/60):A$=RIGHT$(STR$(1000*(100+K)+100+T-60*K),6):MID$(A$,1,1)=" ":MID$(A$,4,1)=":"
3620 LOCATE 1,1:PRINT D;"JUNE 1942";:LOCATE 1,18:PRINT A$,:RETURN
3630 IF J<0 OR J>22 OR K<0 OR K>11 THEN RETURN
3640 LOCATE 13-K,2+J-1:PRINT A$;:LOCATE 20,1:RETURN
3650 FOR X=3 TO 4:LOCATE 2*X-4,31:IF F(X,2)=2 THEN PRINT "SPOTTED"; ELSE PRINT SPC(7);
3660 LOCATE 2*X-3,24:GOSUB 3760:NEXT:GOTO 3790
3670 Y=0:FOR X=0 TO 2:IF F(X,2)=0 THEN 3750
3680 Y=Y+1:LOCATE 2*Y+5,25:PRINT USING "CONTACT  # ";Y;:IF F(X,2)<2 THEN 3730
3690 ON X+1 GOTO 3700,3710,3720:STOP
3700 PRINT "CV";:GOTO 3740
3710 PRINT "TT";:GOTO 3740
3720 PRINT "CA";:GOTO 3740
3730 PRINT "??";
3740 LOCATE 2*Y+6,24:GOSUB 3760
3750 NEXT:FOR X=2*Y+1 TO 6:LOCATE X+6,25:PRINT SPC(13);:NEXT:GOTO 3790
3760 PRINT USING "####";INT(F(X,4)/P1+.5),:J=0:GOSUB 3770:J=1
3770 K=INT(F(X,J)+.5):IF K<0 THEN K=0
3780 PRINT USING "#####";K,:RETURN
3790 FOR IT=24 TO 20 STEP -1:LOCATE IT,1,0:PRINT SPC(79);:NEXT:LOCATE 21,1:RETURN
3800 FOR IT=25 TO 20 STEP -1:LOCATE IT,1,0:PRINT SPC(40):NEXT:LOCATE 21,1:RETURN
3810 ON X+1 GOTO 3820,3830,3840,3850,3860,3870,3880,3890,3900:STOP
3820 PRINT "AKAGI";:RETURN
3830 PRINT "KAGA";:RETURN
3840 PRINT "SORYU";:RETURN
3850 PRINT "HIRYU";:RETURN
3860 PRINT "ENTERPRISE";:RETURN
3870 PRINT "HORNET";:RETURN
3880 PRINT "YORKTOWN";:RETURN
3890 PRINT "MIDWAY";:RETURN
3900 PRINT "ZUIHO";:RETURN
3910 FOR X=4 TO 7:LOCATE 12+X,11:IF C(X,8)>=60 THEN 3930
3920 PRINT USING "###";C(X,7);:FOR Y=4 TO 6:PRINT USING "####";C(X,Y) MOD 1000,:NEXT:GOTO 3950
3930 IF C(X,8)>=100 THEN 3960
3940 PRINT " HEAVY DAMAGE  ";
3950 FOR Y=1 TO 3:PRINT USING "####";C(X,Y),:NEXT:GOTO 3990
3960 IF X=7 THEN 3980
3970 PRINT " ** SUNK **";SPC(16);:GOTO 3990
3980 PRINT " ** AIRBASE DESTROYED **   ";
3990 NEXT:GOTO 3790
4000 FOR Y=1 TO X*10:NEXT:RETURN
4010 ON X+1 GOTO 4020,4030,4040,4050,4060,4070:STOP
4020 PRINT "CARRIER GROUP";:RETURN
4030 PRINT "TRANSPORT GROUP";:RETURN
4040 PRINT "CRUISER GROUP";:RETURN
4050 PRINT "TASK FORCE 16";:RETURN
4060 PRINT "TASK FORCE 17";:RETURN
4070 PRINT "MIDWAY ISLAND";:RETURN
4080 ON X+Y GOTO 4090,4100,4110,4120,4130,4140:STOP
4090 PRINT "F4F's";:RETURN
4100 PRINT "ZEKES";:RETURN
4110 PRINT "SBD's";:RETURN
4120 PRINT "VALS";:RETURN
4130 PRINT "TPD's";:RETURN
4140 PRINT "KATES";:RETURN
4150 FOR IT=1 TO LEN(A$):JT=ASC(MID$(A$,IT,1)):IF JT>96 AND JT<123 THEN MID$(A$,IT,1)=CHR$(ASC(MID$(A$,IT,1))-32)
4160 NEXT:RETURN
4170 FOR IT=37 TO 42
4180 SOUND IT,3
4190 SOUND 32767,1
4200 NEXT:SOUND 32767,1
4210 FOR IT=42 TO 37 STEP -1:SOUND IT,5:NEXT
4220 FOR IT=37 TO 50:SOUND IT,4:NEXT:SOUND 50,20
4230 RETURN
4240 FOR IT=5000 TO 2000 STEP -30
4250 SOUND IT,1:NEXT
4260 RETURN
4270 N1=200
4280 SOUND N1,1:N1=N1*.8499999:IF N1>120 THEN 4280
4290 RETURN
4300 KY$=INKEY$:WHILE(KY$<>""):KY$=INKEY$:WEND:RETURN
5000 RETURN
    */

    public IEnumerable<ForceRef> forces
    {
      get
      {
        ForceRef force;
        decimal y;
        decimal x;
        string l;
        int i;

        for (i = 0; i < 6; ++i)
        {
          x = this.FX[i];
          y = this.FY[i];
          if (
            i >= 3 ||
            this.F[i, 2] == 2)
          {
            switch(i)
            {
              case 0:
                l = "cv";
                break;
              case 1:
                l = "tt";
                break;
              case 2:
                l = "cr";
                break;
              case 3:
                l = "6";
                break;
              case 4:
                l = "7";
                break;
              case 5:
                l = "*";
                break;
              default:
                l = ".";
                break;
            }
          }
          else
          {
            l = ".";
          }
          force = new ForceRef
          {
            x = x,
            y = y,
            gridX = this.F[i, 1],
            gridY = this.F[i, 0],
            heading = this.F[i, 4],
            speed = this.F[i, 5],
            letter = l,
            type = this.forceTypes[i],
            visible = this.FZ[i]
          };
          yield return force;
        }
      }
    }

    public void ProcessCommand (string command)
    {
      string carrier;
      string[] parts;
      bool good;

      parts = command
        .Split(' ')
        .Where(
          p =>
            !string.IsNullOrWhiteSpace(p))
        .Select(
          p =>
            p.ToLower())
        .ToArray();
      good = false;
      switch (parts.ElementAtOrDefault(0))
      {
        case "?":
          this.output(
            "{0} course IJNCV to Midway",
            this.CourseTo(0, 5));
          this.output(
            "{0} course Midway to IJNCV",
            this.CourseTo(5, 0));
          this.output(
            "{0} course TF-16 to IJNCV",
            this.CourseTo(3, 0));
          this.output(
            "{0} course IJNCV to TF-16",
            this.CourseTo(0, 3));
          this.output(
            "{0} course TF-17 to IJNCV",
            this.CourseTo(4, 0));
          this.output(
            "{0} course IJNCV to TF-17",
            this.CourseTo(0, 4));
          break;
        case "0":
          this.AdvanceTime(0);
          good = true;
          break;
        case "1":
          this.AdvanceTime(1);
          good = true;
          break;
        case "2":
          this.AdvanceTime(2);
          good = true;
          break;
        case "3":
          this.AdvanceTime(3);
          good = true;
          break;
        case "4":
          this.AdvanceTime(4);
          good = true;
          break;
        case "5":
          this.AdvanceTime(5);
          good = true;
          break;
        case "6":
          this.AdvanceTime(6);
          good = true;
          break;
        case "7":
          this.AdvanceTime(7);
          good = true;
          break;
        case "8":
          this.AdvanceTime(8);
          good = true;
          break;
        case "9":
          this.AdvanceTime(9);
          good = true;
          break;
        case "t":
          if (parts.Length ==3)
          {
            decimal course;

            decimal.TryParse(
              parts[2],
              out course);
            if (
              course >= 0M &&
              course <= 360M)
            {
              switch (parts[1])
              {
                case "6":
                case "16":
                  this.F[3, 4] = course * this.p1;
                  this.output(
                    "TF-16 new course {0}°",
                    this.F[3, 4]);
                  good = true;
                  this.taskForceUpdated?.Invoke();
                  break;
                case "7":
                case "17":
                  this.F[4, 4] = course * this.p1;
                  this.output(
                    "TF-17 new course {0}°",
                    this.F[4,4]);
                  good = true;
                  this.taskForceUpdated?.Invoke();
                  break;
              }
            }
          }
          break;
        case "s":
          if (parts.Length == 3)
          {
            decimal speed;

            decimal.TryParse(
              parts[2],
              out speed);
            speed = Math.Max(0, Math.Min(speed, 26));
            switch (parts[1])
            {
              case "6":
              case "16":
                this.F[3, 5] = speed;
                this.output(
                  "TF-16 new speed {0} knots",
                  this.F[3, 5]);
                good = true;
                this.taskForceUpdated?.Invoke();
                break;
              case "7":
              case "17":
                this.F[4, 5] = speed;
                this.output(
                  "TF-17 new speed {0} knots",
                  this.F[4, 5]);
                good = true;
                this.taskForceUpdated?.Invoke();
                break;
            }
          }
          break;
        case "a":
          if (parts.Length == 5)
          {
            int f4fs;
            int sbds;
            int tbds;

            carrier = parts[1];
            if (
              int.TryParse(
                parts[2],
                out f4fs) &&
              int.TryParse(
                parts[3],
                out sbds) &&
              int.TryParse(
                parts[4],
                out tbds))
            {
              int c;

              switch(carrier)
              {
                case "e":
                  c = 4;
                  break;
                case "h":
                  c = 5;
                  break;
                case "y":
                  c = 6;
                  break;
                case "m":
                  c = 7;
                  break;
                default:
                  c = -1;
                  break;
              }
              if (c > -1)
              {
                this.ArmStrike(
                  c,
                  f4fs,
                  sbds,
                  tbds);
                good = true;
                this.taskForceUpdated?.Invoke();
              }
            }
          }
          break;
        case "l":
          if (
            parts.Length == 2 ||
            parts.Length == 3)
          {
            carrier = parts[1];
            int c;

            switch (carrier)
            {
              case "e":
                c = 4;
                break;
              case "h":
                c = 5;
                break;
              case "y":
                c = 6;
                break;
              case "m":
                c = 7;
                break;
              default:
                c = -1;
                break;
            }
            if (c > -1)
            {
              int? contact;
              int cn;

              if (parts.Length == 3)
              {
                if (int.TryParse(
                  parts[2],
                  out cn))
                {
                  contact = cn;
                }
                else
                {
                  break;
                }
              }
              else
              {
                contact = null;
              }
              this.LaunchStrike(
                c,
                contact);
              good = true;
            }
          }
          break;
        case "ca":
          if (parts.Length == 3)
          {
            int c;

            carrier = parts[1];
            switch (carrier)
            {
              case "e":
                c = 4;
                break;
              case "h":
                c = 5;
                break;
              case "y":
                c = 6;
                break;
              case "m":
                c = 7;
                break;
              default:
                c = -1;
                break;
            }
            if (c > -1)
            {
              int f4fs;

              if (int.TryParse(
                parts[2],
                out f4fs))
              {
                this.SetCAP(
                  c,
                  f4fs);
                good = true;
                this.taskForceUpdated?.Invoke();
              }
            }
          }
          break;
        case "cl":
          if (parts.Length == 2)
          {
            int c;

            carrier = parts[1];
            switch (carrier)
            {
              case "e":
                c = 4;
                break;
              case "h":
                c = 5;
                break;
              case "y":
                c = 6;
                break;
              case "m":
                c = 7;
                break;
              default:
                c = -1;
                break;
            }
            if (c > -1)
            {
              this.ClearDecks(c);
              good = true;
              this.taskForceUpdated?.Invoke();
            }
          }
          break;
      }
      if (!good)
      {
        this.output("COMMANDS ARE:");
        this.output("T-CHANGE TF COURSE  CA-SET CAP");
        this.output("A-ARM STRIKE        CL-CLEAR DECK");
        this.output("L-LAUNCH STRIKE      #-WAIT # HOURS");
      }
    }

    private void PlaceShips()
    {
      int i;

      for (i = 0; i < 6; ++i)
      {
        this.FX[i] = this.F[i, 0] * 0.02M + 0.5M;
        this.FY[i] = this.F[i, 1] * 0.01M + 0.5M;
      }
    }

    private void ArmStrike(
      int carrier,
      int f4fCount,
      int sbdCount,
      int tbdCount)
    {
      if (this.C[carrier, 8] >= 60)
      {
        this.output(
          "{0} is not operational.",
          this.vessels[carrier]);
      }
      else
      {
        if (
          this.C[carrier, 4] == 0 &&
          this.C[carrier, 6] == 0 &&
          this.C[carrier, 5] == 1000)
        {
          this.C[carrier, 5] = 0;
        }
        if (
          this.C[carrier, 4] == 0 &&
          this.C[carrier, 5] == 0 &&
          this.C[carrier, 6] == 0)
        {
          f4fCount = Math.Min(f4fCount, (int) this.C[carrier, 1]);
          sbdCount = Math.Min(sbdCount, (int) this.C[carrier, 2]);
          tbdCount = Math.Min(tbdCount, (int) this.C[carrier, 3]);
          this.C[carrier, 4] = f4fCount;
          this.C[carrier, 5] = 1000 + sbdCount; // not sure wtf, the 1000....
          this.C[carrier, 6] = tbdCount;
          this.C[carrier, 1] -= f4fCount;
          this.C[carrier, 2] -= sbdCount;
          this.C[carrier, 3] -= tbdCount;
          this.output(
            "{0} is preparing strike of {1} F4F's, {2} SBD's, and {3} TBD's.",
            this.vessels[carrier],
            f4fCount,
            sbdCount,
            tbdCount);
        }
        else
        {
          this.output(
            "{0} already has a strike prepared to launch.",
            this.vessels[carrier]);
        }
      }
    }

    private void ClearDecks(int carrier)
    {
      this.C[carrier, 5] = this.C[carrier, 5] % 1000;
      this.C[carrier, 1] = this.C[carrier, 1] + this.C[carrier, 4];
      this.C[carrier, 4] = 0;
      this.C[carrier, 2] = this.C[carrier, 2] + this.C[carrier, 5];
      this.C[carrier, 5] = 0;
      this.C[carrier, 3] = this.C[carrier, 3] + this.C[carrier, 6];
      this.C[carrier, 6] = 0;
      this.output(
        "{0} has cleared her decks.",
        this.vessels[carrier]);
    }

    private void SetCAP(
      int carrier,
      int f4fCount)
    {
      int needed;
      int max;

      f4fCount = Math.Max(0, f4fCount);
      max =
        (int)this.C[carrier, 1] + // standby
        (int)this.C[carrier, 4] + // in strike
        (int)this.C[carrier, 7];  // already in cap
      f4fCount = Math.Min(f4fCount, max);
      needed = f4fCount - (int) this.C[carrier, 7];
      if (needed > 0)
      {
        StringBuilder sb;
        int standby;
        int divert;

        standby = Math.Min(needed, (int) this.C[carrier, 1]);
        divert = needed - standby;
        sb = new StringBuilder();
        sb.Append(this.vessels[carrier]);
        sb.Append(" ");
        if (standby > 0)
        {
          sb.Append("launches another ");
          sb.Append(standby);
          sb.Append(" F4F's ");
          this.C[carrier, 1] -= standby;
        }
        if (divert > 0)
        {
          if (standby > 0)
          {
            sb.Append("and ");
          }
          sb.Append("diverts ");
          sb.Append(divert);
          sb.Append(" F4F's from strike group ");
          this.C[carrier, 4] -= divert;
        }
        sb.Append("now running a CAP of ");
        sb.Append(f4fCount);
        sb.Append(" F4F's.");
        this.output(sb.ToString());
      }
      else if (needed < 0)
      {
        this.C[carrier, 1] -= needed;
        this.output(
          "{0} lands {1} F4F's leaving CAP of {2} F4F's.",
          this.vessels[carrier],
          -needed,
          f4fCount);
      }
      else
      {
        this.output(
          "{0} continues running CAP of {1} F4F's.",
          this.vessels[carrier],
          f4fCount);
      }
      this.C[carrier, 7] = f4fCount;
    }

    private void output(
      string s,
      params object?[] args)
    {
      if (args.Any())
      {
        string fs;

        fs = string.Format(
          s,
          args);
        this.outputText?.Invoke(fs);
      }
      else
      {
        this.outputText?.Invoke(s);
      }
    }

    private void LaunchStrike(
      int carrier,
      int? contact)
    {
      int cn;
      int i;

      cn = this.contactList.Count;
      if (contact != null)
      {
        if (
          contact < 1 ||
          contact > this.contactList.Count)
        {
          cn = -1;
        }
      }
      /*
      for (i = 0; i <= 2; ++i)
      {
        if (this.F[i, 2] > 0)
        {
          ++cn;
          this.C1[cn] = i;
        }
      }
      */
      if (cn > 0)
      {
        if (this.C[carrier, 5] + this.C[carrier, 6] > 0)
        {
          if (this.C[carrier, 5] < 1000)
          {
            if (
              cn == 1 &&
              contact == null)
            {
              contact = 1;
            }
            if (
              contact >= 1 &&
              contact <= cn)
            {
              decimal r;
              int src;
              int co;

              //co = this.C1[contact.Value];
              co = this.contactList[contact.Value - 1];
              src = (int) this.C[carrier, 0];
              r = this.CalculateRange(
                co,
                src);
              if (r <= 200)
              {
                decimal flightTime;
                bool clear;

                flightTime = r * 0.3m;
                if (carrier < 7) // not midway
                {
                  if (
                    this.t + flightTime * 2 <= 240 ||
                    this.t + flightTime * 2 > 1140)
                  {
                    this.output("Cannot launch strike; no night landing capabilities.");
                    clear = false;
                  }
                  else if (
                    this.t + flightTime <= 240 ||
                    this.t + flightTime > 1140)
                  {
                    this.output("Strike force does not have night attack capabilities.");
                    clear = false;
                  }
                  else
                  {
                    clear = true;
                  }
                }
                else
                {
                  clear = true;
                }
                if (clear)
                {
                  int? slot;

                  slot = null;
                  for (int slotTest = 0; slotTest < 10; ++slotTest)
                  {
                    if (this.S[slotTest, 9] < 0)
                    {
                      slot = slotTest;
                      break;
                    }
                  }
                  if (slot != null)
                  {
                    this.S[slot.Value, 0] = this.C[carrier, 4];
                    this.S[slot.Value, 2] = this.C[carrier, 5];
                    this.S[slot.Value, 4] = this.C[carrier, 6];
                    this.C[carrier, 4] = 0;
                    this.C[carrier, 5] = 0;
                    this.C[carrier, 6] = 0;
                    this.S[slot.Value, 6] = co;
                    this.S[slot.Value, 9] = carrier;
                    this.S[slot.Value, 7] = this.t + flightTime;
                    this.S[slot.Value, 8] = this.t + flightTime * 2.0m;
                    this.S[slot.Value, 3] = 1;
                    this.S[slot.Value, 5] = 0;
                    if (
                      ((double)
                      (this.S[slot.Value, 2] /
                      (this.S[slot.Value, 2] +
                      this.S[slot.Value, 4]))) >
                      this.random.NextDouble())
                    {
                      this.S[slot.Value, 1] = 1;
                    }
                    else
                    {
                      this.S[slot.Value, 1] = 0;
                    }
                    this.output(
                      "{0}'s strike force is taking off.",
                      this.vessels[carrier]);
                  }
                  else
                  {
                    this.output(
                      "Too many strikes aloft.");
                  }
                }
              }
              else
              {
                this.output(
                  "Contact {0} at {1} nautical miles is out of range of 200 nautical miles.",
                  contact.Value,
                  r.ToString("0.0"));
              }
            }
            else
            {
              this.output("Invalid target specified.");
            }
          }
          else
          {
            this.output(
              "{0}'s strike force is still arming.",
              this.vessels[carrier]);
          }
        }
        else
        {
          this.output(
            "{0} has not prepared a strike force.",
            this.vessels[carrier]);
        }
      }
      else
      {
        this.output("No targets.");
      }
    }

    private decimal CalculateRange(
      int force1,
      int force2)
    {
      decimal ret;
      double d;
      double x1;
      double x2;
      double y1;
      double y2;

      x1 = (double) this.F[force1, 0];
      y1 = (double) this.F[force1, 1];
      x2 = (double) this.F[force2, 0];
      y2 = (double) this.F[force2, 1];
      d = Math.Sqrt(
        Math.Pow(
          x1 - x2,
          2.0) +
        Math.Pow(
          y1 - y2,
          2.0));
      ret = Convert.ToDecimal(d);
      return ret;
    }

    private void AdvanceTime(int hours)
    {
      int dStop;
      int tStop;

      dStop = this.d;
      tStop = this.t + hours * 60;
      while (tStop > 1440)
      {
        ++dStop;
        tStop -= 1440;
      }
      this.interruptTimeAdvancement = false;
      for (; ; )
      {
        this.ProcessActivities();
        if (
          this.interruptTimeAdvancement ||
          this.d > dStop ||
          (this.d == dStop &&
          this.t >= tStop))
        {
          break;
        }
      }
    }

    private void ProcessActivities()
    {
      this.interruptTimeAdvancement = false;
      this.SpotAnyVisualRangeForces();
      this.PrepareUSStrikes();
      this.UpdateIJNCruisers();
      this.UpdateIJNTransports();
      this.IJNCruisersBombardMidway();
      this.UpdateIJNCarrierHeading();
      this.UpdateIJNCarrierCAPs();
      this.UpdateIJNCarrierStrikeStatus();
      this.AdvanceTime();
      this.ProcessPBYScoutPlanes();
      this.ProcessJapaneseScoutPlanes();
      this.ProcessStrikes();

      this.CheckForJapaneseAirCapabilities();
      this.UpdateContactList();
      this.taskForceUpdated?.Invoke();
    }

    private string getTaskForceName(int i)
    {
      string ret;

      switch(i)
      {
        case 0:
          ret = "IJN Carriers";
          break;
        case 1:
          ret = "IJN Troop Transports";
          break;
        case 2:
          ret = "IJN Cruisers";
          break;
        case 3:
          ret = "Task Force 16";
          break;
        case 4:
          ret = "Task Force 17";
          break;
        case 5:
          ret = "Midway Island";
          break;
        default:
          ret = string.Empty;
          break;
      }
      return ret;
    }

    private string getGenericTaskForceName(int i)
    {
      string ret;

      switch (i)
      {
        case 0:
          ret = "Carrier Group";
          break;
        case 1:
          ret = "Troop Transports";
          break;
        case 2:
          ret = "Cruisers";
          break;
        case 3:
          ret = "Task Force 16";
          break;
        case 4:
          ret = "Task Force 17";
          break;
        case 5:
          ret = "Midway Island";
          break;
        default:
          ret = string.Empty;
          break;
      }
      return ret;
    }

    private List<int> contactList = new List<int>();
    private void UpdateContactList()
    {
      List<int> visibleContacts;
      int[] addIds;
      int i;

      visibleContacts = new List<int>();
      for (i = 0; i <= 2; ++i)
      {
        if (this.F[i, 2] == 0)
        {
          this.contactList.Remove(i);
        }
        else
        {
          visibleContacts.Add(i);
        }
      }
      addIds =
        visibleContacts
          .ToArray()
          .Where(
            ci =>
              !this.contactList.Contains(ci))
          .OrderBy(
            ci =>
              this.random.Next())
          .ToArray();
      this.contactList.AddRange(addIds);
      Debug.Assert(this.contactList.Count <= 3);
      foreach (int iv in this.contactList)
      {
        Debug.Assert(
          this.contactList
            .Where(
              c =>
                c == iv)
            .Count() == 1);
      }
    }

    private string getContactAlias(int tf)
    {
      int index;
      string ret;

      this.UpdateContactList();
      index = this.contactList.IndexOf(tf);
      if (index > -1)
      {
        ret = (index + 1).ToString();
      }
      else
      {
        ret = "";
      }
      return ret;
    }

    private void SpotAnyVisualRangeForces()
    {
      decimal radarRange;
      decimal r;
      decimal c;
      int i;
      int j;

      for (i = 0; i <= 2; ++i)
      {
        for (j = 3; j <= 5; ++j)
        {
          switch(j)
          {
            case 3:
            case 4:
              radarRange = 14.0m;
              break;
            case 5:
            default:
              if (this.C[7, 8] > 70m)
              {
                // radar out
                radarRange = -1000m;
              }
              else
              {
                radarRange = 25.0m;
              }
              break;
          }
          r = this.CalculateRange(i, j);
          if (this.F[i, 2] < 2)
          {
            if (r <= 6.0M)
            {
              if (
                this.t >= 240 &&
                this.t <= 1140)
              {
                // visual detection
                c = this.CourseTo(j, i);
                // radar detection
                this.F[i, 2] = 2;
                this.outputText?.Invoke(
                  string.Format(
                    "{0} has visual sighting of Japanese {1} bearing {2}° at {3} nautical miles.",
                    this.getTaskForceName(j),
                    this.getTaskForceName(i),
                    c.ToString("0"),
                    r.ToString("0.0")));
                this.interruptTimeAdvancement = true;
              }
            }
            if (this.F[i, 2] < 1)
            {
              if (r <= radarRange)
              {
                c = this.CourseTo(j, i);
                // radar detection
                this.F[i, 2] = 1;
                this.outputText?.Invoke(
                  string.Format(
                    "{0} radar has identified contact {1} bearing {2}° at {3} nautical miles.",
                    this.getTaskForceName(j),
                    this.getContactAlias(i),
                    c.ToString("0"),
                    r.ToString("0.0")));
                this.interruptTimeAdvancement = true;
              }
            }
          }
          if (
            this.F[j, 2] < 2 &&
            this.t >= 240 &&
            this.t <= 1140)
          {
            if (r <= 8.0M)
            {
              // visual detection
              if (this.F[j, 2] < 1)
              {
                this.F[j, 2] = 1;
                if (this.F[i, 2] == 2)
                {
                  this.outputText?.Invoke(
                    string.Format(
                      "{0} has come within visual sight range of {1}.",
                      this.getTaskForceName(i),
                      this.getTaskForceName(j)));
                }
                else
                {
                  this.outputText?.Invoke(
                    string.Format(
                      "{0} has come within visual sight range of {1}.",
                      this.getTaskForceName(i),
                      this.getContactAlias(j)));
                }
              }
            }
            if (r <= 5.5m)
            {
              // radar detection
              if (this.F[j, 2] < 2)
              {
                this.F[j, 2] = 2;
              }
            }
          }
        }
      }
    }

    private void PrepareUSStrikes()
    {
      int i;

      for (i = 4; i <= 7; ++i)
      {
        if (this.C[i, 5] >= 1000)
        {
          this.C[i, 5] %= 1000; // finish arming any strikes
          this.output(
            "{0}'s strike is prepared.",
            this.vessels[i]);
        }
      }
    }

    private void UpdateIJNCruisers()
    {
      decimal r;
      int i;

      for (i = 3; i <= 4; ++i)
      {
        r = this.CalculateRange(
          i,
          2);
        if (r < 50M)
        {
          this.c5 = 10;
        }
      }
      if (
        this.allJapaneseCarriersIncapacitated ||
        this.c5 > 9)
      {
        this.F[2, 5] = 25m + 15m * mVal(this.c7 > 255);
        this.F[2, 4] = 270m;
      }
    }

    private void UpdateIJNTransports()
    {
      decimal r;

      r = this.CalculateRange(1, 5);
      if (r < 15M)
      {
        // transports stop by Midway
        this.F[1, 5] = 0;
      }
      if (this.allJapaneseCarriersIncapacitated)
      {
        // if Japanese have lost carriers, they flee westward
        // no considering US carriers gone -- more coming and Japanese didn't know how many more there were anyway
        this.F[1, 4] = 270;
        this.F[1, 5] = 18;
      }
    }

    private void CheckForJapaneseAirCapabilities()
    {
      bool anyJapaneseCarriersFunctioning;
      int i;

      // do we have any major carriers functioning?
      // if not, Japanese will abandon theater -- if Shuiho is still around, she will
      // provide escort with her minimal fighter complement for CAP
      anyJapaneseCarriersFunctioning = false;
      for (i = 0; i < 4; ++i)
      {
        if (this.C[i, i] <= 60)
        {
          anyJapaneseCarriersFunctioning = true;
          break;
        }
      }
      this.allJapaneseCarriersIncapacitated = !anyJapaneseCarriersFunctioning;
    }

    private void AdvanceTime()
    {
      decimal r;
      int t1;
      int i;
      int p;

      t1 = 30 + this.random.Next(0, 31);
      this.t += t1;
      /* -- to proceed no input -- we handle differently
      if (
        this.t >= this.t0 &&
        this.d == this.d0)
      {
        this.interruptTimeAdvancement = 1;
      }
      */
      if (this.t >= 1440)
      {
        ++this.d;
        this.t -= 1440;
      }
      /* -- to proceed no input -- we handle differently
      if (
        this.t >= this.t0 &&
        this.d >= this.d0)
      {
        this.interruptTimeAdvancement = 1;
      }
      */
      this.MoveShips(t1);
    }

    private void MoveShips(decimal t)
    {
      int i;

      for (i = 0; i <= 4; ++i)
      {
        this.F[i, 0] += t * this.F[i, 5] * this.cos(this.F[i, 4]) / 60m;
        this.F[i, 1] += t * this.F[i, 5] * this.sin(this.F[i, 4]) / 60m;
        this.FZ[i] = this.F[i, 2] > 0;
        this.FX[i] = this.F[i, 0] * 0.02m + 0.5m;
        this.FY[i] = this.F[i, 1] * 0.01m + 0.5m;
      }
    }

    private void ProcessStrikes()
    {
      int i;

      for (i = 0; i <= 9; ++i)
      {
        if (
          this.S[i, 9] != -1 &&
          this.S[i, 7] > this.t &&
          this.S[i, 1] != -1)
        {
          if (
            this.S[i, 6] != 2 ||
            this.c7 <= 511)
          {
            this.ProcessStrike(i);
          }
        }
      }
    }

    private void ProcessStrike(int si)
    {
      decimal odds;
      decimal bias;
      decimal dmg;
      decimal r;
      bool japanese;
      bool sunk;
      int count;
      int h;
      int n;
      int i;
      int l;

      japanese = this.S[si, 6] > 2;
      if (this.S[si, 6] != 5) // not attacking midway
      {
        for (i = 0; i <= 4; i += 2)
        {
          if (
            this.S[si, i] == 0 ||
            ((decimal) this.random.NextDouble()) <= (this.S[i, 8] - this.S[i, 7] - 20) / 100m)
          {
            this.S[si, i + 1] = -1;
          }
        }
        if (this.S[si, 1] == -1)
        {
          if (this.S[si, (int) (5 - this.S[si, 1] * 2m)] == -1)
          {
            this.S[si, 1] = 1 - this.S[si, 1];
            if (this.S[si, (int) (5 - this.S[si, 1] * 2m)] == -1)
            {
              this.S[si, 1] = -1; // put fighter cover on whoever makes it through
            }
          }
        }
      }
      if (!japanese)
      {
        for (i = 0; i <= 4; i += 2)
        {
          if (
            this.S[si, i] != 0 &&
            this.S[si, i + 1] == -1)
          {
            this.outputText?.Invoke(
              string.Format(
                "{0}'s {1} miss target.",
                this.vessels[(int)this.S[si, 9]],
                this.planes[0, i / 2]));
          }
        }
      }

      if (
        this.S[si, 3] + this.S[si, 5] != -2 &&
        this.S[si, 2] + this.S[si, 4] != 0)
      {
        bool attackCarriers;
        string origin;
        string target;

        this.F[(int) this.C[(int) this.S[si, 9], 0], 2] = 2;
        this.F[(int)this.S[si, 6], 2] = 2;
        if (this.F[0, 2] == 2)
        {
          this.F[0, 3] = 2;
        }
        if (japanese)
        {
          origin = "Japanese";
          target = this.getTaskForceName((int) this.S[si, 6]);
        }
        else
        {
          origin = this.vessels[(int) this.S[si, 9]] + "'s";
          target =
            "Japanese " +
            this.getGenericTaskForceName((int)this.S[si, 6]);
        }
        this.outputText?.Invoke(
          string.Format(
            "{0} air strike is attacking {1}!!!",
            origin,
            target));
        this.interruptTimeAdvancement = true;

        attackCarriers = false;
        for (i = 0; i < 9; ++i)
        {
          if (
            this.S[si, 6] == this.C[i, 0] &&
            this.C[i, 8] < 100)
          {
            attackCarriers = true;
            break;
          }
        }

        if (!attackCarriers)
        {
          // attack fleet
          this.ProcessAADefense(
            japanese,
            si,
            true);
          for (i = 4; i >= 2; i -= 2)
          {
            if (
              this.S[si, i] != 0 &&
              this.S[si, i + 1] != -1)
            {
              count = (int)this.S[si, i];
              this.outputText?.Invoke(
                string.Format(
                  "{0} {1} attack {2}.",
                  count,
                  this.planes[
                    japanese ? 1 : 0,
                    i / 2],
                  this.vessels[(int) this.S[si, 6]]));
              switch (i)
              {
                case 2:
                default:
                  bias = 1m;
                  break;
                case 4:
                  bias = 1.25m;
                  break;
              }
              if (!japanese)
              {
                bias *= 2m;
              }
              odds =
                this.F[(int) this.S[si, 6], 6] * bias;
              h = 0;
              n = 0;
              for (l = 0; l < count; ++l)
              {
                r = ((decimal)this.random.NextDouble());
                if (r < odds)
                {
                  ++h;
                }
                if (r < odds * 2m)
                {
                  ++n;
                }
              }
              n -= h;
              if (
                i == 4 &&
                this.S[i, 6] != 5)
              {
                // torpedoes on ships
                dmg = 24m;
              }
              else
              {
                // bombs
                dmg = 16m;
              }
              this.ApplyDamages(
                (int)this.S[si, 6],
                dmg,
                h,
                n,
                i == 2); // secondary explosions if bomb hits and planes out
            }
          }
          this.ProcessAADefense(
            japanese,
            si,
            false);
        }
        else
        {
          // attack carriers
          // ******** RESUME HERE ************


        }

      }

    }

    private void ProcessAADefense(
      bool japanese,
      int si,
      bool inbound)
    {
      string source;
      decimal e;
      decimal o;
      decimal h;
      int i;
      int j;

      source = this.vessels[(int)this.S[si, 6]];
      for (i = 0; i <= 4; i += 2)
      {
        if (
          this.S[si, i] != 0 &&
          this.S[si, i + 1] != -1)
        {
          switch(i)
          {
            case 0:
            default:
              o = 0.4m;
              break;
            case 2:
              o = 0.7m;
              break;
            case 4:
              o = 1m;
              break;
          }
          e = this.F[(int) this.S[i, 6], 7] * o;
          h = 0m;
          for (j = 1; j <= this.S[si, i]; ++j)
          {
            if (((decimal)this.random.NextDouble()) > e)
            {
              ++h;
            }
          }
          this.outputText?.Invoke(
            string.Format(
              "On the way {0}, {1} AA shoots down {2} {3}.",
              inbound ? "in" : "out",
              this.vessels[(int)this.S[i, 6]],
              (int)h,
              this.planes[
                japanese ? 1 : 0,
                i / 2]));
        }
      }

    }

    private void ProcessPBYScoutPlanes()
    {
      int i;

      if (
        this.t >= 240 &&
        this.t <= 1140)
      {
        p = (this.t < 300 || (this.t > 720 && this.t < 780)) ? 3 : 1;
        for (i = 0; i < 2; ++i)
        {
          if (
            this.F[i, 2] != 2 &&
            this.c7 < 512)
          {
            if (this.F[i, 5] == 0)
            {
              this.F[i, 5] = 2;
            }
            if (
              this.F[i, 2] != 1 ||
              ((decimal)this.random.NextDouble()) < 3m * this.s7)
            {
              if (
                ((decimal)this.random.NextDouble()) <= p * this.s7 ||
                this.F[i, 2] != 0)
              {
                this.F[i, 2] = Math.Min(this.F[i, 2] + 1, 2);
                if (((decimal)this.random.NextDouble()) > 3m * this.s7)
                {
                  this.F[i, 2] = Math.Min(this.F[i, 2] + 1, 2);
                }
                switch (this.F[i, 2])
                {
                  case 1: // generic spot of something by japanese
                    this.outputText?.Invoke("PBY spots Japanese ships.");
                    break;
                  case 2:
                    switch (i)
                    {
                      case 0:
                        this.outputText?.Invoke("PBY spots a Japanese carrier group.");
                        break;
                      case 1:
                        this.outputText?.Invoke("PBY spots a Japanese troop transport group.");
                        break;
                      case 2:
                        this.outputText?.Invoke("PBY spots a Japanese cruiser group.");
                        break;
                    }
                    break;
                }
              }
            }
          }
        }
      }
      else
      {
        decimal radarRange;
        decimal r;
        bool onRadar;
        int j;

        for (i = 0; i <= 2; ++i)
        {
          onRadar = false;
          for (j = 3; j <= 5; ++j)
          {
            switch (j)
            {
              case 3:
              case 4:
                radarRange = 14.0m;
                break;
              case 5:
              default:
                if (this.C[7, 8] > 70m)
                {
                  // radar out
                  radarRange = -1000m;
                }
                else
                {
                  radarRange = 25.0m;
                }
                break;
            }
            r = this.CalculateRange(i, j);
            if (r <= radarRange)
            {
              onRadar = true;
              break;
            }
          }
          if (!onRadar)
          {
            this.F[i, 2] = 0; // unspot at night
          }
        }
        this.F[i, 3] = 1; // not sure on this one, yet
      }
    }

    private void ProcessJapaneseScoutPlanes()
    {
      decimal p;
      int i;

      if (
        this.t >= 240 &&
        this.t <= 1140)
      {
        if (this.F[0, 2] == 2)
        {
          this.F[0, 3] = 2;
        }
        p = (this.t > 720 && this.t < 780) ? 2 : 1;
        for (i = 3; i <= 4; ++i)
        {
          if (this.F[i, 2] < 2)
          {
            if (((decimal) this.random.NextDouble()) < p * this.s6)
            {
              this.F[i, 2] = 1;
            }
            if (
              this.F[i, 2] == 1 &&
              ((decimal) this.random.NextDouble()) <= 3m * this.s6)
            {
              switch(i)
              {
                case 3:
                  this.outputText?.Invoke("Japanese scout planes sighted over Task Force 16.");
                  break;
                case 4:
                  this.outputText?.Invoke("Japanese scout planes sighted over Task Force 17.");
                  break;
              }
              this.F[i, 2] = 2;
              this.interruptTimeAdvancement = true;
            }
          }
        }
      }
      else
      {
        for (i = 3; i < 4; ++i)
        {
          this.F[i, 2] = 0;
        }
        this.F[i, 3] = 1;
      }
    }

    private void UpdateIJNCarrierStrikeStatus()
    {
      bool strikeExists;
      int? secondaryStrikeTarget;
      int? strikeTarget;
      decimal r;
      decimal l;
      int tf;
      int i;
      int j;

      this.s9 = 0;
      this.a9 = false;
      this.a8 = false;
      if (this.t <= 1140)
      {
        strikeExists =
          this.C[0, 4] > 0 || this.C[0, 5] > 0 || this.C[0, 6] > 0 ||
          this.C[1, 4] > 0 || this.C[1, 5] > 0 || this.C[1, 6] > 0 ||
          this.C[2, 4] > 0 || this.C[2, 5] > 0 || this.C[2, 6] > 0 ||
          this.C[3, 4] > 0 || this.C[3, 5] > 0 || this.C[3, 6] > 0;
        strikeTarget = null;
        secondaryStrikeTarget = null;
        if (strikeExists)
        {
          for (i = 4; i <= 7; ++i)
          {
            if (this.C[i, 8] < 100)
            {
              tf = (int)this.C[i, 0];
              if (this.F[tf, 2] > 0)
              {
                r = this.CalculateRange(
                  0, tf);
                if (r <= 235)
                {
                  // known contact in reach
                  if (this.C[i, 8] < 60)
                  {
                    strikeTarget = i;
                    break;
                  }
                  else if (secondaryStrikeTarget == null)
                  {
                    secondaryStrikeTarget = i;
                  }
                  break;
                }
              }
            }
          }
          strikeTarget = strikeTarget ?? secondaryStrikeTarget;
          if (strikeTarget != null)
          {
            this.s9 = (int)this.C[strikeTarget.Value, 0];
          }
          if (this.s9 >= 5)
          {
            for (i = 0; i < 10; ++i)
            {
              // checking strikes -- do japanese not launch more than one at a time?
              if (
                this.S[i, 6] >= 5 ||
                this.S[i, 9] != -1 ||
                this.S[i, 1] != -1)
              {
                this.s9 = 0;
                break;
              }
            }
          }
          if (this.F[3, 2] + this.F[4, 2] > 0) // IJN has seen carrier force(s)
          {
            this.a9 = true;
          }
          r = this.CalculateRange(0, 5);
          if (r <= 235M)
          {
            l = 60m * r / 235m;
            if (
              this.t + l >= 240 &&
              this.t + l + l <= 1140)
            {
              this.a8 = true;
              this.a9 = this.C[3, 2] < 12;
            }
          }
          if (this.a9)
          {
            this.a8 = false; // cancelling midway strike due to preference for fleet target
          }
        }
        if (this.s9 >= 3)
        {
          // process strike on target task force
          for (j = 0; j < 10; ++j)
          {
            if (this.S[j, 9] == -1)
            {
              this.S[j, 6] = this.s9;
              this.S[j, 9] = 0;
              r = this.CalculateRange(
                0, s9);
              l = 60M * r / 235M;
              this.S[j, 7] = this.t + l;
              this.S[j, 8] = this.t + l * 2m;
              this.S[j, 0] = 0;
              this.S[j, 2] = 0;
              this.S[j, 4] = 0;
              // add all carrier strikes to strike group
              for (i = 0; i <= 3; ++i)
              {
                if (this.C[i, 8] <= 60)
                {
                  this.S[j, 0] += this.C[i, 4];
                  this.C[i, 4] = 0;
                  this.S[j, 2] += this.C[i, 5];
                  this.C[i, 5] = 0;
                  this.S[j, 4] += this.C[i, 6];
                  this.C[i, 6] = 0;
                }
              }
              if (this.S[j, 2] + this.S[j, 4] == 0m)
              {
                this.S[j, 9] = -1;
              }
              this.S[j, 3] = 1;
              this.S[j, 5] = 0;
              if (this.S[j, 9] != -1)
              {
                this.S[j, 1] =
                  ((double) Math.Abs((this.S[j, 2] / (this.S[j, 2] + this.S[j, 4])))) > this.random.NextDouble() ? -1 : 0;
              }
              break;
            }
          }
        }        
        for (i = 0; i <= 3; ++i)
        {
          this.C[i, 5] %= 1000; // finish arming any pending strike preps
          if (this.C[i, 8] <= 60)
          {
            if (this.a9)
            {
              // carrier strike -- the kitchen sink
              this.C[i, 4] = this.C[i, 1];
              this.C[i, 5] = this.C[i, 2];
              this.C[i, 6] = this.C[i, 3];
              this.C[i, 1] = 0;
              this.C[i, 2] = 0;
              this.C[i, 3] = 0;
            }
            else if (this.a8)
            {
              // midway strike -- take about half bombers and a fraction of fighters
              this.C[i, 4] = (int) (this.C[i, 3] / 2m); 
              this.C[i, 5] = (int) (this.C[i, 2] / 2m);
              this.C[i, 1] -= this.C[i, 4];
              this.C[i, 2] -= this.C[i, 5];
            }
            if (
              this.s9 == 0 &&
              !this.a8 &&
              !this.a9)
            {
              // no targets -- so get all our fighters into the CAP
              this.C[i, 7] += this.C[i, 1];
              this.C[i, 1] = 0;
            }
          }
        }
      }
    }

    private void UpdateIJNCarrierHeading()
    {
      decimal td;
      decimal r;
      decimal c;
      int tf;
      int i;

      c = this.F[0, 4];
      r = this.CalculateRange(0, 5); // range to midway
      // adjust for midway (primary [KNOWN] target) considerations
      if (r > 250)
      {
        // as long as range is over 250, point IJN carriers at midway
        c = this.CourseTo(0, 5);
      }
      else if (r < 100)
      {
        // if we get within 100 miles with carriers, let's sail away -- we don't land carriers
        c = this.CourseTo(5, 0);
      }
      td = 1000000m;
      for (i = 6; i >= 4; --i) // go through us carriers
      {
        tf = (int)this.C[i, 0];
        if (this.F[tf, 2] > 0) // is carrier seen?
        {
          if (this.C[i, 8] < 100) // is carrier still afloat?
          {
            // estimate range to 
            r = this.CalculateRange(0, tf) * (1M + 0.3M * ((decimal)this.random.NextDouble()));
            if (r < td)
            {
              c = this.CourseTo(0, tf);
            }
          }
        }
      }
      if (this.allJapaneseCarriersIncapacitated)
      {
        c = 270;
      }
      this.F[0, 4] = c;
    }

    private void UpdateIJNCarrierCAPs()
    {
      int i;

      for (i = 0; i < 3; ++i)
      {
        if (this.C[i, 7] != 5 && this.C[i, 8] >= 60)
        {
          this.C[i, 7] += this.C[i, 1];
          this.C[i, 1] = 0;
          if (this.C[i, 7] >= 5)
          {
            this.C[i, 1] = this.C[i, 7] - 5;
            this.C[i, 7] = 5;
          }
          else
          {
            this.C[i, 7] += this.C[i, 4];
            this.C[i, 4] = 0;
            if (this.C[i, 7] > 5)
            {
              this.C[i, 4] = this.C[i, 7] - 5m;
              this.C[i, 7] = 5m;
            }
          }
        }
      }
    }

    private void IJNCruisersBombardMidway()
    {
      decimal r;

      r = this.CalculateRange(2, 5);
      if (
        r <= 15 &&
        this.c5 < 10)
      {
        double rn;
        int n;
        int h;
        int k;

        this.outputText?.Invoke("Japanese Cruisers are bombarding Midway!!!");
        // cruisers bombard
        //x = 7;
        this.F[2, 2] = 2;
        ++this.c5;
        if (
          !this.allJapaneseCarriersIncapacitated &&
          this.c7 <= 255)
        {
          this.F[2, 5] = 0;
        }
        if (this.firstCruiserAttack)
        {
          this.firstCruiserAttack = false;
          this.interruptTimeAdvancement = true;
        }
        this.interruptTimeAdvancement = true;
        n = 0;
        h = 0;
        for (k = this.c7; k < 255; k += 4)
        {
          rn = this.random.NextDouble();
          h -= this.intVal(rn < 0.05);
          n -= this.intVal(rn < 0.1);
        }
        n -= h;
        this.ApplyDamages(
          7,
          24,
          h,
          n,
          false);
      }
    }

    private decimal CourseTo(int tfFrom, int tfTo)
    {
      decimal ret;
      decimal x1;
      decimal y1;
      decimal x2;
      decimal y2;

      x1 = this.F[tfFrom, 0];
      x2 = this.F[tfTo, 0];
      y1 = this.F[tfFrom, 1];
      y2 = this.F[tfTo, 1];
      ret = this.arcTan(
        x2 - x1,
        y2 - y1);
      return ret;
    }

    private void ApplyDamages(
      int vessel,
      decimal damageRating,
      int hits,
      int nearMisses,
      bool forceSecondaryExplosions)
    {
      if (this.C[vessel, 8] >= 100)
      {
        int tf;
        
        // ship sunk so process victory points on its task force
        tf = (int) this.C[vessel, 0];
        this.ProcessVictoryPoints(
          tf,
          damageRating,
          hits,
          nearMisses);
      }
      else
      {
        bool secondaryExplosions;
        bool targetGone;
        int i;

        this.output(
          "{0} takes {1} hit{2} and {3} near miss{4}.",
          this.vessels[vessel],
          hits,
          (hits == 1) ? "" : "s",
          nearMisses,
          (nearMisses == 1) ? "" : "es");                
        secondaryExplosions =
          (vessel == 7 ||
          forceSecondaryExplosions) &&
          (this.C[vessel, 4] +
          this.C[vessel, 5] +
          this.C[vessel, 6]) > 0;
        targetGone = false;
        this.output("Secondary Explosions are occuring!");
        for (i = 0; i < hits && !targetGone; ++i)
        {
          this.ApplyDamage(
            vessel,
            damageRating * (secondaryExplosions ? 2M : 1M),
            ref targetGone);
        }
        for (i = 0; i < nearMisses && !targetGone; ++i)
        {
          this.ApplyDamage(
            vessel,
            damageRating * (secondaryExplosions ? 1M : 0.333M),
            ref targetGone);
        }
      }
    }

    private void ApplyDamage(
      int carrier,
      decimal damage,
      ref bool targetGone)
    {
      decimal damageTaken;

      damageTaken =
        damage *
        Convert.ToDecimal(this.random.NextDouble());
      if (carrier == 7)
      {
        // midway takes much more damage to put out of action
        damageTaken /= 3M;
      }
      else if (carrier == 8)
      {
        // zuiho is particularly vulnerable
        damageTaken *= 2M;
      }
      if (
        //K != 4 ||
        carrier != 7)
      {
        int lc;
        int l1;
        int l2;

        lc = 6 - this.intVal(this.t < 240 || this.t > 1140);
        for (l1 = 1; l1 <= lc; ++l1)
        {
          if (this.C[carrier, l1] != 0)
          {
            for (l2 = 1; l2 <= this.C[carrier, l1]; ++l2)
            {
              this.C[carrier, l1] += intVal(this.random.Next(100) < damageTaken);
            }
          }
        }
      }
      this.C[carrier, 8] += damageTaken;
      if (this.C[carrier, 8] >= 60.0M)
      {
        // carrier inoperable, unprepare that strike
        this.C[carrier, 5] = this.C[carrier, 5] % 1000;
        this.C[carrier, 1] += this.C[carrier, 4];
        this.C[carrier, 4] = 0;
        this.C[carrier, 2] += this.C[carrier, 5];
        this.C[carrier, 5] = 0;
        this.C[carrier, 3] += this.C[carrier, 6];
        this.C[carrier, 6] = 0;
        if (this.C[carrier, 8] >= 100.0M)
        {
          this.interruptTimeAdvancement = true;
          this.C[carrier, 8] = 100M; // limit damage to 100%
          targetGone = true;
          if (carrier == 7)
          {
            this.output("Midway's airbase is destroyed!");
          }
          else
          {
            this.output(
              "{0} blows up and sinks!",
              this.vessels[carrier]);
          }
        }
      }
    }

    private void ProcessVictoryPoints(
      int tf,
      decimal damageRating,
      int hits,
      int nearMisses)
    {
      string takeWord;
      string tfName;

      switch (this.forceTypes[tf])
      {
        case ForceTypes.JapaneseCarrierGroup:
          tfName = "Japanese carrier group";
          takeWord = "takes";
          break;
        case ForceTypes.JapaneseCruisers:
          tfName = "Japanese cruisers";
          takeWord = "take";
          break;
        case ForceTypes.JapaneseTransports:
          tfName = "Japanese troop transports";
          takeWord = "take";
          break;
        case ForceTypes.MidwayIsland:
          tfName = "Midway Island";
          takeWord = "takes";
          break;
        case ForceTypes.TaskForce16:
          tfName = "Task Force 16";
          takeWord = "takes";
          break;
        case ForceTypes.TaskForce17:
          tfName = "Task Force 17";
          takeWord = "takes";
          break;
        default:
          tfName = "Unidentified ships";
          takeWord = "Take";
          break;
      }
      this.output(
        "{0} {1} {2} hit{3} and {4} near miss{5}.",
        tfName,
        takeWord,
        hits,
        (hits == 1) ? "" : "s",
        nearMisses,
        (nearMisses == 1) ? "" : "es");
    }
  }
}
