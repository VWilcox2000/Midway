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
    private bool showJapaneseLaunches = true; // for testing
    private bool showJapaneseCarrierHealths = true; // for testing

    private List<RecoveringCAP> recoveringCAPs { get; } = new List<RecoveringCAP>();

    public event OutputTextHandler? outputText;
    public event OutputTextHandler? outputWord;
    public event TaskForceUpdatedHandler? taskForceUpdated;

    public MidwayScenario()
    {
      this.Initialize();
      this.COriginal = (decimal[,])this.C.Clone();
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
    // F(x, 0) -- y.. or x?
    // F(x, 1) -- x.. or y?
    // F(x, 2) -- spotted == 1 seen, 2 identified
    // F(x, 3) -- 0, 1, 2
    // F(x, 4) -- course
    // F(x, 5) -- speed
    // F(x, 6) -- 
    // F(x, 7) -- AA proficiency
    private decimal[,] F = new decimal[,]
    {
      { 0M, 0M, 0M, 1M, 0M, 25M, 0.1M, 0.18M },  // reduce jap carrier group aa proficiency from 0.2 to 0.18 to be reasonable
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
    // C[x, 9] - planes attacking in current wave -- transient use?
    private decimal[,] C = new decimal[,]
    {
      { 0, 21, 21, 21, 0, 0, 0, 0, 0, 0 },
      { 0, 30, 23, 30, 0, 0, 0, 0, 0, 0 },
      { 0, 21, 21, 21, 0, 0, 0, 0, 0, 0 },
      { 0, 21, 21, 21, 0, 0, 0, 0, 0, 0 },
      { 3, 27, 38, 14, 0, 0, 0, 0, 0, 0 },
      { 3, 27, 35, 15, 0, 0, 0, 0, 0, 0 },
      { 4, 25, 37, 13, 0, 0, 0, 0, 0, 0 },
      { 5, 14, 14, 10, 0, 0, 0, 0, 0, 0 },
      { 1, 15, 0, 15, 0, 0, 0, 0, 0, 0 }
    };
    private decimal[,] COriginal;
    private decimal[] W = new decimal[]
    {
      1.5M, 1.4M, 1.3M, 1.3M, 1.2M, 1M
    };
    // strikes -- this could be made list array
    // game now limiting to 10 strikes (which IS a lot)
    // S[i, 0] - strike F4F's
    // S[i, 1] - if ((sbd's / (sbd's + tbd's)) > rnd(0 to 1.0) then 1 else 0   F4F's miss?
    // S[i, 2] - strike SBD's
    // S[i, 3] - 0/1 at launch -- SBD's miss?
    // S[i, 4] - strike TBD's
    // S[i, 5] - 0/1 at launch -- TBD's miss?
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
    private string[,] plane =
    {
      { "F4F", "SBD", "TBD" },
      { "Zero", "Val", "Kate" }
    };
    private string[,] planes =
    {
      { "F4F's", "SBD's", "TBD's" },
      { "Zeros", "Vals", "Kates" }
    };
    //[1, i / 2]
    //170 FOR I = 0 TO 9:S(I,9)=-1:NEXT:S6=.041:S7=.043:CLS:SCREEN 0:FOR X = 4 TO 7:LOCATE 12+X,1

    public string[] usSubmarines = new string[]
    {
      "Albacore",
      "Amberjack",
      "Argonaut",
      "Barbel",
      "Bonefish",
      "Bullhead",
      "Capelin",
      "Cisco",
      "Corvina",
      "Darter",
      "Dorado",
      "Escolar",
      "Flier",
      "Golet",
      "Grampus",
      "Grayback",
      "Grayling",
      "Grenadier",
      "Growler",
      "Grunion",
      "Gudgeon",
      "Harder",
      "Herring",
      "Kete",
      "Lagarto",
      "Nautilus",
      "Perch",
      "Pickerel",
      "Pompano",
      "Robalo",
      "Runner",
      "Scamp",
      "Scorpion",
      "Sculpin",
      "Sealion",
      "Seawolf",
      "Shark",
      "Snook",
      "Swordfish",
      "Tang",
      "Trigger",
      "Triton",
      "Trout",
      "Tullibee",
      "Wahoo"
    };

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
    private decimal s8; // chance of American sub reporting contact
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
      Debug.Assert((ret.armed.sbds % 1000) < 300);
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
      this.s6 = 0.031m; // original game had 0.041m, but Japanese scouting did not perform that well so reducing
      this.s7 = 0.043m;
      this.s8 = 0.005m;
      this.PlaceShips();
    }

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
      //int i;

      this.Initialize();
      for (; ;)
      {


        cmd = Console.In.ReadLine();
      }
    }

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
          Debug.Assert((C[carrier, 5] % 1000) < 300);
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
      Debug.Assert(this.C[carrier, 5] < 300);
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
      //int i;

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
                    decimal odds;
                    decimal rnd;

                    this.S[slot.Value, 0] = this.C[carrier, 4];
                    this.S[slot.Value, 2] = this.C[carrier, 5];
                    this.S[slot.Value, 4] = this.C[carrier, 6];
                    this.C[carrier, 4] = 0;
                    this.C[carrier, 5] = 0;
                    this.C[carrier, 6] = 0;
                    this.S[slot.Value, 6] = co;
                    this.S[slot.Value, 9] = carrier;
                    this.S[slot.Value, 7] = this.t + this.d * 1440 + flightTime;
                    this.S[slot.Value, 8] = this.t + this.d * 1440 + flightTime * 2.0m;
                    this.S[slot.Value, 3] = 1;
                    this.S[slot.Value, 5] = 0;
                    odds =
                      this.S[slot.Value, 2] /
                      (this.S[slot.Value, 2] +
                      this.S[slot.Value, 4]);
                    rnd = (decimal)this.random.NextDouble();
                    if (odds > rnd)
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
      this.taskForceUpdated?.Invoke();
    }

    private void ProcessActivities()
    {
      /*
      if (this.showJapaneseCarrierHealths)
      {
        this.outputText?.Invoke(
          string.Format(
            "Damages: Akagi {0}%, Kaga {1}%, Soryu {2}%, Hiryu {3}%",
            this.C[0, 8].ToString("0.0"),
            this.C[1, 8].ToString("0.0"),
            this.C[2, 8].ToString("0.0"),
            this.C[3, 8].ToString("0.0")));
      }
      */
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
      this.ProcessCAPReturns();
      this.LandStrikes();
      this.ProcessDamageControl();

      this.CheckForJapaneseAirCapabilities();
      this.UpdateContactList();
      this.taskForceUpdated?.Invoke();
      this.CheckForGameCompletion();
    }

    private string getTaskForceName(int i)
    {
      string ret;

      switch(i)
      {
        case 0:
          if (
            this.C[0, 8] >= 100 &&
            this.C[1, 8] >= 100 &&
            this.C[2, 8] >= 100 &&
            this.C[3, 8] >= 100)
          {
            ret = "Depleted IJN Carrier Group";
          }
          else
          {
            ret = "IJN Carrier Group";
          }
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
          Debug.Assert(
            (this.C[i, 5] >= 0 &&
            this.C[i, 5] < 300) ||
            (this.C[i, 5] >= 1000 &&
            this.C[i, 5] < 1300));
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
        if (this.C[i, 8] <= 60)
        {
          anyJapaneseCarriersFunctioning = true;
          break;
        }
      }
      this.allJapaneseCarriersIncapacitated = !anyJapaneseCarriersFunctioning;
    }

    private void AdvanceTime()
    {
      //decimal r;
      int t1;
      //int i;
      //int p;

      t1 = 10 + this.random.Next(0, 11);
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
          this.S[i, 7] > 0 &&
          this.S[i, 7] <= this.d * 1440m + this.t)
        {
          this.S[i, 7] = -Math.Abs(this.S[i, 7]); // strikes only get one attack!
          if (this.S[i, 1] != -1)
          {
            // was this strike on no longer existent cruiser fleet?
            if (
              this.S[i, 6] != 2 ||
              this.c7 <= 511)
            {
              this.ProcessStrike(i);
            }
          }
          else
          {
            if (
              this.S[i, 9] >= 4 &&
              this.S[i, 9] <= 7)
            {
              this.outputText?.Invoke(
                string.Format(
                  "{0}'s strike misses their target and turns back.",
                  this.vessels[(int) this.S[i, 9]]));
            }
            else if (this.random.NextDouble() < 0.5)
            {
              this.outputText?.Invoke(
                string.Format(
                  "{0}'s radar sees a group of planes searching the area then returning the way they came.",
                  this.getTaskForceName((int)S[i, 6])));
            }
          }
        }
      }
    }

    private void ProcessStrike(int si)
    {
      decimal flightTime;
      decimal planeCount;
      decimal odds;
      decimal bias;
      decimal dmg;
      decimal r;
      bool japanese;
      bool waveMisses;
      //bool sunk;
      int count;
      int h;
      int n;
      int i;
      int l;

      japanese = this.S[si, 6] > 2;
      if (this.S[si, 6] != 5) // not attacking midway
      {
        flightTime = Math.Abs(this.S[si, 8]) - Math.Abs(this.S[si, 7]);
        planeCount = this.S[si, 0] + this.S[si, 2] + this.S[si, 4];
        for (i = 0; i <= 4; i += 2)
        {
          // longer flight time = more chance of group missing
          // more planes -- trying to coordinate launch -- more chance
          if (japanese)
          {
            // Japanese were far more experienced at this point in the war in launching coordinated strikes while
            // US was still using varied techniques from different carries and experimenting in general with far less
            // experienced flight crew.  This changed dramatically after the battle and with more US carrier experience.
            odds = (flightTime - 35) / 120m;
            odds *= Math.Max(80, Math.Min(planeCount, 120)) / 120;
            odds /= 2m;
          }
          else
          {
            odds = (flightTime - 20) / 110m;
            odds *= Math.Max(40, Math.Min(planeCount, 70)) / 70;
          }
          if (
            this.S[si, i] == 0 ||
            ((decimal) this.random.NextDouble()) <= odds)
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
          int vp;

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
                  this.getTaskForceName((int) this.S[si, 6])));
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
                else if (r < odds * 2m)
                {
                  ++n;
                }
              }
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
              vp = (int)(dmg * (h + n / 3));
              this.outputText?.Invoke(
                string.Format(
                  "{0} victory points awarded!",
                  vp));
              if (japanese)
              {
                this.v1 += vp;
              }
              else
              {
                this.v0 += vp;
              }
              if (this.S[si, 6] == 2)
              {
                // cruiser attack
                this.c7 += vp;
                if (this.c7 >= 255)
                {
                  if (this.c7 - vp < 255)
                  {
                    this.outputText?.Invoke("IJN Cruisers are severely crippled.");
                    this.F[2, 6] *= 3;
                    this.F[2, 5] = 10;
                    this.c5 = 10;
                  }
                  if (this.c7 >= 512)
                  {
                    if (this.c7 - vp < 512)
                    {
                      this.outputText?.Invoke("All IJN Cruisers have been sunk!!!");
                      v0 = v0 - c7 + 512;
                      c7 = 512;
                      this.F[2, 2] = 0;
                      this.F[2, 5] = 0;
                      this.F[2, 0] = -1000;
                    }
                  }
                }
              }
            }
          }

          this.ProcessAADefense(
            japanese,
            si,
            false);
        }
        else
        {
          Dictionary<int, int> capHomeVessels;
          int cap;

          // attack carriers
          // ******** RESUME HERE ************
          capHomeVessels = new Dictionary<int, int>();
          cap = this.GatherCap(
            (int) this.S[si, 6],
            ref capHomeVessels);
          if (cap > 0)
          {
            int capTarget;

            // torp bombers or dive bombers cap victims?
            capTarget = (this.random.NextDouble() >= 0.5) ? 4 : 2;
            if (
              this.S[si, capTarget + 1] == -1 ||  // missed?
              this.S[si, capTarget] == 0)         // no such component
            {
              // go to the other target
              capTarget = 6 - capTarget;
            }
            if (
              this.S[si, capTarget + 1] != -1 &&  // missed?
              this.S[si, capTarget] > 0)         // no such component
            {
              // cap has a target of capTarget
              this.ProcessCapBattle(
                japanese,
                ref cap,
                si,
                capTarget,
                (int) this.S[si, capTarget]);
            }
          }
          if (this.StrikeWorthContinuing(si))
          {
            this.ProcessAADefense(
              japanese,
              si,
              true);
            this.ProcessCarrierAttack(
              japanese,
              si);
            this.ProcessAADefense(
              japanese,
              si,
              false);
            this.RestoreCap(
              !japanese,
              cap,
              (int)this.S[si, 6],
              capHomeVessels);
          }
          else if (this.S[si, 0] + this.S[si, 2] + this.S[si, 4] == 0)
          {
            this.outputText?.Invoke(
              string.Format(
                "{0} strike force was obliterated.",
                japanese ? "Japanese" : "US"));
          }
          else
          {
            this.outputText?.Invoke(
              string.Format(
                "{0} strike force turns back with no viable attacking units on target.",
                japanese ? "Japanese" : "US"));
          }
        }
      }
      this.S[si, 1] = -1;
      this.S[si, 3] = -1;
      this.S[si, 5] = -1;
    }

    private bool StrikeWorthContinuing(int si)
    {
      bool ret;

      if (
        (this.S[si, 3] == -1 ||
        this.S[si, 2] == 0) &&
        (this.S[si, 5] == -1 ||
        this.S[si, 4] == 0))
      {
        // all bombers/torpedo bombers either missed target or are gone.
        ret = false;
      }
      else
      {
        ret = true;
      }
      return ret;
    }

    private void ProcessCarrierAttack(
      bool japanese,
      int si)
    {
      int[] vesselIndices;
      List<int> targets;
      int attackerCount;
      int[] waves;
      int left;
      int ai;
      int l;

      vesselIndices =
        (new int[] { 0, 1, 2, 3, 4, 5, 6, 7 })
          .OrderBy(
            n =>
              this.random.NextDouble())
          .ToArray();
      waves = (this.random.NextDouble() > 0.5) ? new int[] { 2, 4 } : new int[] { 4, 2 };
      foreach (int wave in waves)
      {
        // wave have any planes in it?  did they not miss target
        if (
          this.S[si, wave] != 0 &&
          this.S[si, wave + 1] != -1)
        {
          targets = new List<int>();
          // assign targest to vessels
          for (l = 0; l <= 8; ++l)
          {
            this.C[l, 9] = 0;
            // is vessel intact and part of task force we are attacking?
            if (
              this.C[l, 8] < 100 &&
              this.C[l, 0] == this.S[si, 6])
            {
              targets.Add(l);
            }
          }
          // randomize targets
          targets = targets
            .OrderBy(
              t =>
                this.random.NextDouble())
            .ToList();
          if (targets.Count > 3)
          {
            if (this.random.Next() < 0.2)
            {
              targets.Remove(this.random.Next(targets.Count));
            }
          }
          if (targets.Count > 2)
          {
            if (this.random.Next() < 0.1)
            {
              targets.Remove(this.random.Next(targets.Count));
            }
          }
          ai = 0;
          left = (int) this.S[si, wave];
          foreach (int target in targets)
          {
            ++ai;
            if (ai == targets.Count)
            {
              attackerCount = left;
            }
            else
            {
              attackerCount = (int)(this.S[si, wave] * (0.3m + ((decimal)this.random.NextDouble()) * 0.06m));
            }
            this.C[target, 9] = attackerCount;
            left -= attackerCount;
          }

          foreach (int v in vesselIndices)
          {
            attackerCount = (int) this.C[v, 9];
            if (attackerCount > 0)
            {
              this.outputText?.Invoke(
                string.Format(
                  "{0} {1} {2} attacking {3}.",
                  attackerCount,
                  (attackerCount != 1) ?
                    this.planes[
                      japanese ? 1 : 0,
                      wave / 2] :
                    this.plane[
                      japanese ? 1 : 0,
                      wave / 2],
                  (attackerCount != 1) ? "are" : "is",
                  this.vessels[v]));
              this.ProcessAttackWave(
                japanese,
                wave == 2 ? PlaneTypes.Bomber : PlaneTypes.TorpedoBomber,
                attackerCount,
                v);
            }
          }
        }
      }
    }

    private void ProcessAttackWave(
      bool japanese,
      PlaneTypes planeType,
      int count,
      int vesselIndex)
    {
      decimal odds;
      decimal dmg;
      decimal r;
      decimal h;
      decimal n;
      int i;

      n = 0m;
      h = 0m;
      switch(planeType)
      {
        case PlaneTypes.Bomber:
        default:
          // original odds in game were 20% for bombs.
          // While Japanese were particularly good at evading bombs especially from B-17's
          // (which I might add to game later),
          // the US dive bombers that actually made it to the fleet, did better than 0.2.
          // meanwhile, IJN dive bombers fared much less effectively against Yorktown which
          // would have survived battle if not for a sub and a negligent picket line.
          // so giving US a 25% bonus here
          //odds = 0.2m;
          odds = japanese ? 0.2m : 0.25m;
          dmg = 16m;
          break;
        case PlaneTypes.TorpedoBomber:
          // American TBD is horrible (retired after action) and
          // US had barely tested, rarely working torpedos.
          // (though they did fairly well in Coral sea due to optimal conditions and attack pattern)
          odds = japanese ? 0.2m : 0.06m;
          if (vesselIndex == 7)
          {
            dmg = 16m;
          }
          else
          {
            // torpedo on ship, if it hits, is generally worse than bomb
            // though US was blessed by poor damage control and safety measures
            // on Japanese carriers leading to explosions and losses after hits
            // US carriers generally survived due to superior damage control.
            dmg = 24m;
          }
          break;
      }
      for (i = 0; i < count; ++i)
      {
        r = (decimal)this.random.NextDouble();
        if (r < odds)
        {
          this.outputWord?.Invoke("  HIT!!!");
          ++h;
        }
        // only near misses from bombs -- torpedoes no
        // might get into torpedo defense systems at some point later
        else if (
          planeType == PlaneTypes.Bomber &&
          r < odds * 2m)
        {
          ++n;
          this.outputWord?.Invoke("  Near miss.");
        }
        else
        {
          // US torpedoes still suck at this point
          // so some of the misses we will report as duds to
          // reflect that.
          if (
            planeType == PlaneTypes.TorpedoBomber &&
            !japanese &&
            this.random.Next() < 0.2)
          {
            this.outputWord?.Invoke("  A DUD.");
          }
          else
          {
            this.outputWord?.Invoke("  MISS.");
          }
        }
      }
      this.ApplyDamages(
        japanese,
        vesselIndex,
        planeType,
        dmg,
        (int) h,
        (int) n,
        false);
    }

    private void ProcessCapBattle(
      bool japanese,
      ref int capCount,
      int si,
      int capTarget,
      int capTargetCount)
    {
      int attackingFighterVictories;
      int attackingFighterCount;
      int capVictories;
      decimal odds;
      int i;

      this.outputText?.Invoke(
        string.Format(
          "{0} CAP of {1} {2} {3} {4} {5}.",
          japanese ? "US" : "Japanese",
          capCount,
          (capCount == 1) ?
            this.plane[
              japanese ? 0 : 1,
              0] :
            this.planes[
              japanese ? 0 : 1,
              0],
          (capCount == 1) ? "attacks" : "attack",
          capTargetCount,
          (capTargetCount == 1) ?
            this.plane[
              japanese ? 1 : 0,
              capTarget / 2] :
            this.planes[
              japanese ? 1 : 0,
              capTarget / 2]));
      if (4 - this.S[si, 1] * 2 == capTarget)
      {
        attackingFighterCount = (int) this.S[si, 0];
      }
      else
      {
        attackingFighterCount = 0;
      }
      if (attackingFighterCount > 0)
      {
        this.outputText?.Invoke(
          string.Format(
            "{0} {1} {2} the {3} {4}.",
            attackingFighterCount,
            (attackingFighterCount == 1) ?
              this.plane[japanese ? 1 : 0, 0] :
              this.planes[japanese ? 1 : 0, 0],
            attackingFighterCount == 1 ? "defends" : "defend",
            (int) this.S[si, capTarget],
            (this.S[si, capTarget] == 1) ?
              this.plane[japanese ? 1 : 0, capTarget / 2] :
              this.planes[japanese ? 1 : 0, capTarget / 2]));
      }
      // slight edge to zeroes on attacking non-fighters 1.4 to 1.3
      odds =
        (capCount * this.W[japanese ? 1 : 0]) /
        ((attackingFighterCount * this.W[japanese ? 0 : 1]) +
        this.S[si, capTarget] *
        this.W[capTarget + (japanese ? 0 : 1)]);
      if (this.S[si, 9] == 6)
      {
        // yorktown strike
        // yorktown F4F's -- were extra successful at Midway
        // first use of Thach weave by him and his fighter group VF-3 off Yorktown
        odds *= 0.8m;
      }
      else if (this.S[si, 6] == 4)
      {
        // attacking yorktown -- thach advantage to CAP
        odds *= 1.2m;
      }
      // odds of CAP shootdown affected by defending fighters count and some offsets
      // for superior Japanese fighters at the time.
      capVictories = 0;
      odds = Math.Min(0.8499999m, odds);
      for (i = 0; i < this.S[si, capTarget]; ++i)
      {
        if (((decimal) this.random.NextDouble()) < odds)
        {
          ++capVictories;
        }
      }
      if (capVictories > 0)
      {
        this.outputText?.Invoke(
          string.Format(
            "{0} CAP shoots down {1} {2}.",
            japanese ? "US" : "Japanese",
            capVictories,
            (capVictories == 1) ?
              this.plane[
                japanese ? 1 : 0,
                capTarget / 2] :
              this.planes[
                japanese ? 1 : 0,
                capTarget / 2]));
        this.S[si, capTarget] -= capVictories;
        if (this.S[si, capTarget] == 0)
        {
          this.outputText?.Invoke(
            string.Format(
              "Attacking {0} are completly eradicated!!!",
              (capVictories == 1) ?
              this.plane[
                japanese ? 1 : 0,
                capTarget / 2] :
              this.planes[
                japanese ? 1 : 0,
                capTarget / 2]));
        }
      }
      else
      {
        this.outputText?.Invoke(
          string.Format(
            "{0} CAP fails to score any kills.",
            japanese ? "US" : "Japanese"));
      }
      if (attackingFighterCount > 0)
      {
        this.outputText?.Invoke(
          string.Format(
            "{0} {1} {2} the {3} CAP of {4} {5}.",
            attackingFighterCount,
            (attackingFighterCount == 1) ?
              this.plane[japanese ? 1 : 0, 0] :
              this.planes[japanese ? 1 : 0, 0],
            attackingFighterCount == 1 ? "attacks" : "attack",
            japanese ? "US" : "Japanese",
            capCount,
            (capCount == 1) ?
              this.plane[japanese ? 0 : 1, 0] :
              this.planes[japanese ? 0 : 1, 0]));
        odds =
          (attackingFighterCount * this.W[japanese ? 0 : 1]) /
          (capCount * this.W[japanese ? 1 : 0]);
        if (this.S[si, 9] == 6)
        {
          // yorktown strike
          // yorktown F4F's -- were extra successful at Midway
          // first use of Thach weave by him and his fighter group VF-3 off Yorktown
          odds *= 1.2m;
        }
        else if (this.S[si, 6] == 4)
        {
          // attacking yorktown -- thach advantage to CAP
          odds *= 0.8m;
        }
        odds = Math.Min(0.8499999m, odds);
        attackingFighterVictories = 0;
        for (i = 0; i < capCount; ++i)
        {
          if (((decimal)this.random.NextDouble()) < odds)
          {
            ++attackingFighterVictories;
          }          
        }
        if (attackingFighterVictories > 0)
        {
          this.outputText?.Invoke(
            string.Format(
              "{0} escort {1} down {2} {3}.",
              japanese ? "Japanese" : "US",
              (attackingFighterCount == 1) ?
                "fighter shoots" :
                "fighters shoot",
              attackingFighterVictories,
              (attackingFighterVictories == 1) ?
                this.plane[
                  japanese ? 0 : 1,
                  0] :
                this.planes[
                  japanese ? 0 : 1,
                  0]));
          capCount -= attackingFighterVictories;
          if (capCount == 0)
          {
            this.outputText?.Invoke(
              string.Format(
                "{0} CAP is eradicated!!!",
                japanese ? "US" : "Japanese"));
          }
        }
        else
        {
          this.outputText?.Invoke(
            string.Format(
              "{0} escort {1} to score any kills.",
              japanese ? "US" : "Japanese",
              (attackingFighterCount == 1) ?
                "fighter fails" :
                "fighters fail"));
        }
        if (capCount > 0)
        {
          odds = (capCount * this.W[japanese ? 1 : 0]);
          odds /= (attackingFighterCount * this.W[japanese ? 0 : 1]);
          odds /= 2m;
          if (this.S[si, 9] == 6)
          {
            // yorktown strike
            // yorktown F4F's -- were extra successful at Midway
            // first use of Thach weave by him and his fighter group VF-3 off Yorktown
            odds *= 0.8m;
          }
          else if (this.S[si, 6] == 4)
          {
            // attacking yorktown -- thach advantage to CAP
            odds *= 1.2m;
          }
          capVictories = 0;
          for (i = 0; i < attackingFighterCount; ++i)
          {
            if (((decimal)this.random.NextDouble()) < odds)
            {
              ++capVictories;
            }
          }
          if (capVictories > 0)
          {
            this.outputText?.Invoke(
              string.Format(
                "{0} CAP shoots down {1} of the escorting {2}.",
                japanese ? "US" : "Japanese",
                capVictories,
                (capVictories == 1) ?
                  this.plane[
                    japanese ? 1 : 0,
                    0] :
                  this.planes[
                    japanese ? 1 : 0,
                    0]));
            this.S[si, 0] -= capVictories;
            if (this.S[si, 0] == 0)
            {
              this.outputText?.Invoke("Escort fighter squadron completely eradicated!!!");
            }
          }
          else
          {
            this.outputText?.Invoke(
              string.Format(
                "{0} CAP fails to score any kills on the escorting {1} {2}.",
                japanese ? "US" : "Japanese",
                japanese ? "Japanese" : "US",
                (attackingFighterCount == 1) ?
                  this.plane[japanese ? 1 : 0, 0] :
                  this.planes[japanese ? 1 : 0, 0]));
          }
        }
      }
    }

    private int GatherCap(
      int force,
      ref Dictionary<int, int> capHomeVessels)
    {
      int carrierCapCount;
      int ret;
      int i;

      ret = 0;
      for(i = 0; i <= 8; ++i)
      {
        if (this.C[i, 0] == force)
        {
          carrierCapCount = (int)this.C[i, 7];
          this.C[i, 7] = 0m;
          ret += carrierCapCount;
          capHomeVessels[i] = carrierCapCount;
        }
      }
      return ret;
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
          e = this.F[(int) this.S[si, 6], 7] * o;
          h = 0m;
          for (j = 1; j <= this.S[si, i]; ++j)
          {
            if (((decimal)this.random.NextDouble()) < e)
            {
              ++h;
            }
          }
          this.outputText?.Invoke(
            string.Format(
              "On the way {0}, {1} AA shoots down {2} {3}.",
              inbound ? "in" : "out",
              this.getTaskForceName((int)this.S[si, 6]),
              (int)h,
              this.planes[
                japanese ? 1 : 0,
                i / 2]));
          this.S[si, i] -= h;
        }
      }
    }

    private void ProcessPBYScoutPlanes()
    {
      bool spotHappened;
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
            spotHappened = false;
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
                    spotHappened = true;
                    break;
                  case 2:
                    spotHappened = true;
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
            if (!spotHappened)
            {
              if (
                this.F[i, 2] != 1 ||
                ((decimal)this.random.NextDouble()) < 3m * this.s8)
              {
                if (
                  ((decimal)this.random.NextDouble()) <= p * this.s8 ||
                  this.F[i, 2] != 0)
                {
                  string subName;

                  this.F[i, 2] = Math.Min(this.F[i, 2] + 1, 2);
                  if (((decimal)this.random.NextDouble()) > 3m * this.s8)
                  {
                    this.F[i, 2] = Math.Min(this.F[i, 2] + 1, 2);
                  }
                  subName = string.Concat(
                    "USS ",
                    this.usSubmarines[this.random.Next(this.usSubmarines.Length)]);
                  switch (this.F[i, 2])
                  {
                    case 1: // generic spot of something by japanese
                      this.output(
                        "{0} reports sighting surface vessels.",
                        subName);
                      break;
                    case 2:
                      switch (i)
                      {
                        case 0:
                          this.output(
                            "{0} reports sighting a Japanese carrier group.",
                            subName);
                          break;
                        case 1:
                          this.output(
                            "{0} reports sighting a Japanese troop transport group.",
                            subName);
                          break;
                        case 2:
                          this.output(
                            "{0} reports sighting a Japanese cruiser group.",
                            subName);
                          break;
                      }
                      break;
                  }
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
        if (this.allJapaneseCarriersIncapacitated)
        {
          // less likely to be aggressively scouting as much as running
          // Japanese did have 2 seaplane carriers with scout planes so
          // not like they could NOT be searching, though.
          p /= 2m;
        }
        for (i = 3; i <= 4; ++i) // tf 16 and 17
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
      bool strikeAgainstMidwayInAir;
      bool strikeExists;
      bool strikeReady;
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
          this.C[0, 5] > 0 || this.C[0, 6] > 0 ||
          this.C[1, 5] > 0 || this.C[1, 6] > 0 ||
          this.C[2, 5] > 0 || this.C[2, 6] > 0 ||
          this.C[3, 5] > 0 || this.C[3, 6] > 0;
        strikeReady =
          this.C[0, 5] < 1000 &&
          this.C[1, 5] < 1000 &&
          this.C[2, 5] < 1000 &&
          this.C[3, 5] < 1000;
        strikeTarget = null;
        secondaryStrikeTarget = null;
        if (
          strikeExists &&
          strikeReady)
        {
          strikeAgainstMidwayInAir = false;
          for (i = 0; i < 10; ++i)
          {
            if (
              this.S[i, 9] != -1 &&
              this.S[i, 6] == 5)
            {
              strikeAgainstMidwayInAir = true;
              break;
            }
          }
          for (i = 4; i <= 7; ++i)
          {
            // japanese didn't know carriers were close, but they knew it was possible
            // so they didn't launch endless waves against midway keeping strike forces ready
            // for carrier sightings.
            if (
              i != 5 ||
              !strikeAgainstMidwayInAir)
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
                this.S[i, 6] >= 5 &&
                this.S[i, 9] != -1 &&
                this.S[i, 1] != -1)
              {
                this.s9 = 0;
                break;
              }
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
            // original is hiryu < 12 bombers -- I check them all...  old game
            // had things go in order akagi, kaga, soryu, hiryu... order more random
            // in this implementation.
            this.a9 =
              (this.C[0, 2] + this.C[0, 5] +
              this.C[1, 2] + this.C[1, 5] +
              this.C[2, 2] + this.C[2, 5] +
              this.C[3, 3] + this.C[3, 5])
              < 42;
          }
        }
        if (this.a9)
        {
          this.a8 = false; // cancelling midway strike due to preference for fleet target
        }

        // consider launch strikes if ready and able
        if (
          this.s9 >= 3 &&
          strikeReady)
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
              this.S[j, 7] = this.d * 1440 + this.t + l;
              this.S[j, 8] = this.d * 1440 + this.t + l * 2m;
              this.S[j, 0] = 0;
              this.S[j, 2] = 0;
              this.S[j, 4] = 0;
              // add all carrier strikes to strike group
              for (i = 0; i <= 3; ++i)
              {
                if (this.C[i, 8] <= 60)
                {
                  Debug.Assert(
                    (this.C[i, 5] >= 0 &&
                    this.C[i, 5] < 300) ||
                    (this.C[i, 5] >= 1000 &&
                    this.C[i, 5] < 1300));
                  this.S[j, 0] += this.C[i, 4];
                  this.C[i, 4] = 0;
                  this.S[j, 2] += this.C[i, 5];
                  this.C[i, 5] = 0;
                  this.S[j, 4] += this.C[i, 6];
                  this.C[i, 6] = 0;
                }
              }
              if (this.showJapaneseLaunches)
              {
                this.outputText?.Invoke(
                  string.Format(
                    "Japanese carriers launch strike of {0} zeros, {1} vals, and {2} kates at {3}.",
                    this.S[j, 0],
                    this.S[j, 2],
                    this.S[j, 4],
                    this.getTaskForceName(this.s9)));
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
                  (((double) (this.S[j, 2] / (this.S[j, 2] + this.S[j, 4]))) > this.random.NextDouble()) ? 1 : 0;
              }
              break;
            }
          }
        }        

        // arm strikes?
        for (i = 0; i <= 3; ++i)
        {
          this.C[i, 5] %= 1000; // finish arming any pending strike preps
          Debug.Assert(this.C[i, 5] < 300);
          if (this.C[i, 8] <= 60)
          {
            int allFs;
            int allBs;
            int allTs;
            int capFs;
            int armFs;
            int armBs;
            int armTs;

            allFs = (int)(this.C[i, 4] + this.C[i, 1] + this.C[i, 7]);
            allBs = (int)((this.C[i, 5] % 1000) + this.C[i, 2]);
            Debug.Assert(allBs < 200);
            allTs = (int)(this.C[i, 6] + this.C[i, 3]);
            if (this.a9)
            {
              // carrier strike -- the kitchen sink
              // changing this to keep 1/6th of fighters as CAP
              armFs = allFs * 5 / 6;
              armBs = allBs;
              armTs = allTs;
            }
            else if (this.a8)
            {
              // midway strike -- take about half bombers and a fraction of fighters
              armFs = (allFs + 3) / 4;
              armBs = (allBs + 1) / 2;
              armTs = (allTs + 1) / 2;
            }
            else
            {
              armFs = 0;
              armBs = 0;
              armTs = 0;
            }
            Debug.Assert(armBs < 200);
            capFs = allFs - armFs;
            this.C[i, 1] = 0;
            this.C[i, 2] = allBs - armBs;
            this.C[i, 3] = allTs - armTs;
            if (
              this.C[i, 4] != armFs ||
              this.C[i, 5] != armBs ||
              this.C[i, 6] != armTs)
            {
              this.C[i, 4] = armFs;
              this.C[i, 5] = armBs;
              Debug.Assert(this.C[i, 5] < 300);
              this.C[i, 6] = armTs;
              if (armFs + armBs + armTs > 0)
              {
                this.C[i, 5] = (((int)this.C[i, 5]) % 1000) + 1000;
                Debug.Assert(
                  this.C[i, 5] >= 1000 &&
                  this.C[i, 5] < 1300);
              }
            }
            this.C[i, 7] = allFs - armFs;
            /*
            if (
              this.s9 == 0 &&
              !this.a8 &&
              !this.a9)
            {
              // no targets -- so get all our fighters into the CAP
              this.C[i, 7] += this.C[i, 1];
              this.C[i, 1] = 0;
            }
            */
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
          if (rn < 0.05)
          {
            ++h;
          }
          else if (rn < 0.1)
          {
            ++n;
          }
        }
        this.ApplyDamages(
          true,
          7,
          null,
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
      bool japanese,
      int vessel,
      PlaneTypes? planeType,
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
        //bool armedAircraftDamaged;
        bool secondaryExplosions;
        //int armedAircraftCount;
        //bool armedAircraft;
        int planesDestroyed;
        bool targetGone;
        int i;

        if (planeType == PlaneTypes.TorpedoBomber)
        {
          this.output(
            "{0} takes {1} hit{2}.",
            this.vessels[vessel],
            hits,
            (hits == 1) ? "" : "s");
        }
        else
        {
          this.output(
            "{0} takes {1} hit{2} and {3} near miss{4}.",
            this.vessels[vessel],
            hits,
            (hits == 1) ? "" : "s",
            nearMisses,
            (nearMisses == 1) ? "" : "es");
        }
        /*
        armedAircraftCount = (int)
          (this.C[vessel, 4] +
          this.C[vessel, 5] +
          this.C[vessel, 6]);
        armedAircraft = armedAircraftCount > 0;
        if (armedAircraft)
        {
          // armed aircraft more likely to have explosions if there are more of them.
          armedAircraftDamaged =
            this.random.Next(60) <
            armedAircraftCount;
        }
        else
        {
          armedAircraftDamaged = false;
        }
        */
        secondaryExplosions =
          (vessel == 7 ||
          forceSecondaryExplosions);
        targetGone = false;
        if (secondaryExplosions)
        {
          /*
          if (armedAircraftDamaged)
          {
            this.output("Explosions amidst the armed strike planes!!!");
          }
          else
          {
            this.output("Secondary Explosions are occuring!");
          }
          */
          this.output("Secondary Explosions are occuring!");
        }
        planesDestroyed = 0;
        for (i = 0; i < hits && !targetGone; ++i)
        {
          this.ApplyDamage(
            vessel,
            damageRating * (secondaryExplosions ? 2M : 1M),
            ref targetGone,
            ref planesDestroyed);
        }
        for (i = 0; i < nearMisses && !targetGone; ++i)
        {
          this.ApplyDamage(
            vessel,
            damageRating * (secondaryExplosions ? 1M : 0.333M),
            ref targetGone,
            ref planesDestroyed);
        }
        if (
          planesDestroyed > 0 &&
          !targetGone)
        {
          if (japanese)
          {
            this.output(
              "{0} of {1}'s planes {2} rendered inoperable.",
              planesDestroyed,
              this.vessels[vessel],
              (planesDestroyed == 1) ? "was" : "were");
          }
          else
          {
            this.output(
              "Some of {0} aircraft are exploding.",
              this.vessels[vessel]);
          }
        }
        /*
        // if strike was armed and carrier isn't outright sunk
        // let's remove some of those 'blown up' planes.
        if (
          armedAircraftDamaged &&
          !targetGone)
        {
          PlaneTypes dmgPlaneType;
          int planeDmgCount;

          planeDmgCount = 1;
          for (i = 0; i < hits; ++i)
          {
            planeDmgCount += 1 + this.random.Next(15);
          }
          for (i = 0; i < nearMisses; ++i)
          {
            planeDmgCount += this.random.Next(2);
          }
          for (i = 0; i < planeDmgCount; ++i)
          {
            dmgPlaneType = this.getPlaneType(
              (int) this.C[vessel, 4],
              (int) this.C[vessel, 5],
              (int) this.C[vessel, 6]);
            switch (dmgPlaneType)
            {
              case PlaneTypes.Fighter:
                if (this.C[vessel, 4] > 0)
                {
                  --this.C[vessel, 4];
                }
                break;
              case PlaneTypes.Bomber:
                if (
                  this.C[vessel, 5] > 1000 ||
                  (this.C[vessel, 5] > 0 &&
                  this.C[vessel, 5] < 1000))
                {
                  --this.C[vessel, 5];
                }
                break;
              case PlaneTypes.TorpedoBomber:
                if (this.C[vessel, 6] > 0)
                {
                  --this.C[vessel, 6];
                }
                break;
            }
          }
        }
        */
      }
    }

    private PlaneTypes getPlaneType(
      int fighterCount,
      int bomberCount,
      int torpedoBomberCount)
    {
      PlaneTypes ret;
      int r;

      r = this.random.Next(
        fighterCount +
        bomberCount +
        torpedoBomberCount);
      if (r < fighterCount)
      {
        ret = PlaneTypes.Fighter;
      }
      else if (r < fighterCount + bomberCount)
      {
        ret = PlaneTypes.Bomber;
      }
      else
      {
        ret = PlaneTypes.TorpedoBomber;
      }
      return ret;
    }

    private void ApplyDamage(
      int carrier,
      decimal damage,
      ref bool targetGone,
      ref int planesDestroyed)
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

        lc = 6 - this.intVal(this.t < 240 || this.t > 1140); // damage to planes including CAP if at night
        for (l1 = 1; l1 <= lc; ++l1)
        {
          if (this.C[carrier, l1] != 0)
          {
            for (l2 = 1; l2 <= this.C[carrier, l1]; ++l2)
            {
              if (this.random.Next(100) < damageTaken)
              {
                if (
                  this.C[carrier, l1] > 1000 ||
                  (this.C[carrier, l1] > 0 &&
                  this.C[carrier, l1] < 1000))
                {
                  --this.C[carrier, l1];
                  ++planesDestroyed;
                }
              }
            }
          }
        }
      }
      this.C[carrier, 8] += damageTaken;
      if (this.C[carrier, 8] >= 60.0M)
      {
        // carrier inoperable, unprepare that strike
        this.C[carrier, 5] = this.C[carrier, 5] % 1000;
        Debug.Assert(this.C[carrier, 5] < 300);
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

    private void RestoreCap(
      bool japanese,
      int capCount,
      int tfHome,
      Dictionary<int, int> capHomeVessels)
    {
      int delay;
      int tb;
      int i;

      delay = 0;
      for (i = 0; i < capCount; ++i)
      {
        delay +=
          Math.Min(
            this.random.Next(3),
            this.random.Next(3));
      }
      tb = this.d * 1440 + this.t + delay;
      this.recoveringCAPs.Add(
        new RecoveringCAP
        {
          japanese = japanese,
          capCount = capCount,
          tfHome = tfHome,
          tfDest = tfHome,
          capHomeVessels = capHomeVessels,
          timeBack = this.d * 1440 + this.t,
          attempt = 1
        });
      for (i = 0; i <= 8; ++i)
      {
        this.C[i, 9] = 0;
      }
    }

    // one of big events in reality was not just Zeros going after TBD's leaving bombers free... it was also
    // Zeros spending long periods chasing TBDs on the surface leaving their carriers open when Yorktown and Enterprise
    // SBD groups arrived on opposite side of their fleet.  As such, adding a delayed CAP recovery to status
    // to reward rapid attacks as was case in the real battle.
    private void ProcessCAPReturns()
    {
      RecoveringCAP recoveringCAP;
      int i;

      for (i = this.recoveringCAPs.Count - 1; i >= 0; --i)
      {
        recoveringCAP = this.recoveringCAPs[i];
        if (recoveringCAP.timeBack < this.d * 1440 + this.t)
        {
          this.recoveringCAPs.RemoveAt(i);
          this.RecoverCAP(recoveringCAP);
        }
      }
    }

    private void RecoverCAP(RecoveringCAP recoveringCAP)
    {
      if (recoveringCAP.capCount > 0)
      {
        Dictionary<int, int> availableSlots;
        decimal rng;
        int spots;
        int left;
        int pick;
        int max;
        int i;

        left = recoveringCAP.capCount;
        if (recoveringCAP.tfDest == recoveringCAP.tfHome)
        {
          // at home fleet so try to go to our home vessel if available
          foreach (KeyValuePair<int, int> homeSet in recoveringCAP.capHomeVessels)
          {
            if (this.C[homeSet.Key, 8] < 60)
            {
              this.C[homeSet.Key, 7] += homeSet.Value;
              left -= homeSet.Value;
            }
          }
        }
        availableSlots = new Dictionary<int, int>();
        for (i = 0; i <= 8; ++i)
        {
          max = (i == 8) ? 40 : 60;  // shuiho is small escort carrier
          if (
            this.C[i, 0] == recoveringCAP.tfDest &&
            this.C[i, 8] < 60 &&
            this.C[i, 0] < max)
          {
            spots =
              80 -
              (int) (this.C[i, 1] + this.C[i, 2] + this.C[i, 3] +
              this.C[i, 4] + this.C[i, 5] + this.C[i, 6]);
            if (spots > 0)
            {
              availableSlots[i] = spots;
            }
          }
        }
        if (left > 0)
        {
          KeyValuePair<int, int> slotRef;

          for (; ;)
          {
            if (availableSlots.Any())
            {
              pick = this.random.Next(availableSlots.Count);
              slotRef = availableSlots.ElementAt(pick);
              if (slotRef.Value < 2)
              {
                availableSlots.Remove(slotRef.Key);
              }
              else
              {
                availableSlots[slotRef.Key] = slotRef.Value - 1;
              }
              ++this.C[slotRef.Key, 1];
              --left;
            }
            else
            {
              break;
            }
          }
          if (left > 0)
          {
            if (recoveringCAP.attempt == 1)
            {
              List<int> tfDests;
              // let's try to divert
              int[] tfOptions;

              tfDests = new List<int>();
              if (recoveringCAP.japanese)
              {
                tfOptions = new int[] { 0, 1 };
              }
              else
              {
                tfOptions = new int[] { 3, 4, 5 };
              }
              foreach (int tfOption in tfOptions)
              {
                if (tfOption != recoveringCAP.tfDest)
                {
                  rng = this.CalculateRange(
                    tfOption,
                    recoveringCAP.tfDest);
                  if (rng < this.random.Next(recoveringCAP.japanese ? 65 : 50))
                  {
                    tfDests.Add(tfOption);
                  }
                }
              }
              if (tfDests.Any())
              {
                int tfDest;

                tfDest = tfDests[this.random.Next(tfDests.Count)];
                recoveringCAP.tfDest = tfDest;
                ++recoveringCAP.attempt;
                recoveringCAP.timeBack += this.random.Next(21) + 18;
                recoveringCAP.capCount = left;
                left = 0;
                this.recoveringCAPs.Add(recoveringCAP);
                if (!recoveringCAP.japanese)
                {
                  this.outputText?.Invoke(
                    string.Format(
                      "{0} of {1}'s CAP {2} attempt to divert to {3}.",
                      recoveringCAP.capCount,
                      this.getTaskForceName(recoveringCAP.tfHome),
                      this.planes[0, 0],
                      this.getTaskForceName(recoveringCAP.tfDest)));
                }
              }
              else
              {
                if (!recoveringCAP.japanese)
                {
                  this.outputText?.Invoke(
                    string.Format(
                      "{0} of {1}'s CAP {2} run out of fuel and splash.",
                      left,
                      this.getTaskForceName(recoveringCAP.tfHome),
                      this.planes[0, 0]));
                }
              }
            }
            else
            {
              if (!recoveringCAP.japanese)
              {
                this.outputText?.Invoke(
                  string.Format(
                    "{0} of {1}'s {2} {3}{4}.",
                    left,
                    this.getTaskForceName(recoveringCAP.tfHome),
                    (left == 1) ?
                      this.plane[0, 0] :
                      this.planes[0, 0],
                    (recoveringCAP.tfDest == 5) ?
                      (left == 1) ? "crashlands on Midway Island" : "crashland on Midway Island" :
                      (left == 1) ? "splashes into the sea" : "splash into the sea",
                    (recoveringCAP.tfDest != recoveringCAP.tfHome &&
                    recoveringCAP.tfDest != 5) ?
                      " after arriving at " + this.getTaskForceName(recoveringCAP.tfDest) :
                      ""));
              }
            }
          }
        }
      }
    }

    private void LandStrikes()
    {
      int i;

      for (i = 0; i <= 9; ++i)
      {
        if (this.S[i, 9] != -1)
        {
          if (this.d * 1440 + this.t > this.S[i, 8])
          {
            this.S[i, 8] = -Math.Abs(this.S[i, 8]);
            if (this.S[i, 9] < 4)
            {
              this.LandJapaneseStrike(i);
            }
            else
            {
              this.LandAmericanStrike(i);
            }
            this.S[i, 9] = -1; // mark it landed
          }
        }
      }
    }

    private void LandJapaneseStrike(int si)
    {
      Func<int[]> fnOptions;
      List<int> carriers;
      int[] planes;
      int[] options;
      int i;

      carriers = new List<int>();
      fnOptions = () =>
      {
        return carriers
          .OrderBy(
            c =>
              this.random.NextDouble())
          .ToArray();
      };
      planes = new int[]
      {
        0,
        (int) this.S[si, 0],
        (int) this.S[si, 2],
        (int) this.S[si, 4]
      };
      for (i = 0; i < 4; ++i)
      {
        if (this.C[i, 8] <= 60)
        {
          carriers.Add(i);
        }
      }
      if (carriers.Any())
      {
        int count;
        int p;

        for (p = 1; p <= 3; ++p)
        {
          count = planes[p];
          for (i = 0; i < count; ++i)
          {
            options = fnOptions();
            foreach (int j in options)
            {
              // do we have space for the plane -- capacity beyond the current armed/hangar of the type?
              if (this.C[j, p] + this.C[j, p + 3] < (this.COriginal[j, p] + this.COriginal[j, p + 3]))
              {
                // then park the plane in the hanger and remove it from plane count looking for landing
                ++this.C[j, p];
                --planes[p];
                break;
              }
            }
          }
        }
      }
    }

    private void LandAmericanStrike(int si)
    {
      int planesLeft;
      int c;

      c = (int)this.S[si, 9];
      planesLeft =
        (int) 
          (this.S[si, 0] +
          this.S[si, 2] +
          this.S[si, 4]);
      if (planesLeft > 0)
      {
        if (this.C[c, 8] <= 60m)
        {
          this.output(
            "Strike landing on {0}.",
            this.vessels[c]);
          this.C[c, 1] += this.S[si, 0];
          this.C[c, 2] += this.S[si, 2];
          this.C[c, 3] += this.S[si, 4];
        }
        else
        {
          SortedSet<int> carriersUsed;
          List<int> carriersTo;
          decimal lossPct;

          lossPct = 0m;
          carriersTo = new List<int>();
          carriersUsed = new SortedSet<int>();
          if (
            c == 4 &&
            this.C[5, 8] <= 60)
          {
            // divert to hornet
            carriersTo.Add(5);
          }
          else if (
            c == 5 &&
            this.C[4, 8] <= 60)
          {
            // divert to Enterprise
            carriersTo.Add(4);
          }
          else
          {
            List<int> tfOptions;
            int sourceTF;
            decimal r;
            int i;
            int j;

            sourceTF = (int)this.C[c, 0];
            tfOptions = new List<int>();
            for (i = 3; i <= 5; ++i)
            {
              if (i != sourceTF)
              {
                tfOptions.Add(i);
              }
            }
            tfOptions = tfOptions
              .OrderBy(
                o =>
                  this.random.NextDouble())
              .ToList();
            foreach (int tfOption in tfOptions)
            {
              r = this.CalculateRange(sourceTF, tfOption);
              if (r / 100m < (0.15m + 0.85m * ((decimal)this.random.NextDouble())))
              {
                lossPct = r / 100m;
                for (j = 4; j <= 7; ++j)
                {
                  if (
                    this.C[j, 0] == tfOption &&
                    this.C[j, 8] <= 60m)
                  {
                    carriersTo.Add(j);
                  }
                }
                if (carriersTo.Any())
                {
                  break;
                }
              }
            }
          }
          if (carriersTo.Any())
          {
            int planesLost;

            planesLost = 0;
            this.ProcessStrikeDiversion(
              carriersTo,
              (int)this.S[si, 0],
              (int)this.S[si, 2],
              (int)this.S[si, 4],
              lossPct,
              ref carriersUsed,
              ref planesLost);
            if (carriersUsed.Any())
            {
              this.output(
                "{0}'s strike is diverted to {1}.",
                this.vessels[c],
                this.vesselList(carriersUsed));
            }
            else
            {
              this.output(
                "{0}'s strike splashes attempting to divert to {1}.",
                this.vessels[c],
                this.vesselList(carriersTo));
            }
            this.output(
              "{0} of {1}'s strike planes are lost.",
              planesLost,
              this.vessels[c]);
          }
          else
          {
            this.output(
              "{0}'s strike force of {1} {2} splashes!",
              this.vessels[c],
              planesLeft,
              this.strikePlaneTypesList(
                false,
                this.S[si, 0] > 0,
                this.S[si, 2] > 0,
                this.S[si, 4] > 0));
          }
        }
      }
      else
      {
        this.output(
          "None of {0}'s strike force survived to return home.",
          this.vessels[c]);
      }
    }

    private string vesselList(IEnumerable<int> vessels)
    {
      List<string> names;
      string ret;

      names = new List<string>();
      foreach (int v in vessels)
      {
        names.Add(this.vessels[v]);
      }
      ret = this.wordList(
        names
          .OrderBy(
            n =>
              n));
      return ret;
    }

    private string wordList(IEnumerable<string> words)
    {
      StringBuilder sb;
      string ret;
      int count;
      int i;

      sb = new StringBuilder();
      count = words.Count();
      for (i = 0; i < count; ++i)
      {
        if (i == count - 1)
        {
          sb.Append(", and ");
        }
        else if (i > 0)
        {
          sb.Append(", ");
        }
        sb.Append(words.ElementAt(i));
      }
      ret = sb.ToString();
      return ret;
    }

    private string strikePlaneTypesList(
      bool japanese,
      bool fighters,
      bool bombers,
      bool torpedoBombers)
    {
      List<string> names;
      string ret;

      names = new List<string>();
      if (fighters)
      {
        names.Add("fighters");
      }
      if (bombers)
      {
        names.Add("bombers");
      }
      if (torpedoBombers)
      {
        names.Add("torpedo bombers");
      }
      ret = this.wordList(names);
      return ret;
    }

    private void ProcessStrikeDiversion(
      IEnumerable<int> carriersTo,
      int fighters,
      int bombers,
      int torpedoBombers,
      decimal lossPct,
      ref SortedSet<int> carriersUsed,
      ref int planesLost)
    {
      this.ProcessStrikeDiversion(
        carriersTo,
        0,
        fighters,
        lossPct,
        ref carriersUsed,
        ref planesLost);
      this.ProcessStrikeDiversion(
        carriersTo,
        1,
        bombers,
        lossPct,
        ref carriersUsed,
        ref planesLost);
      this.ProcessStrikeDiversion(
        carriersTo,
        2,
        torpedoBombers,
        lossPct,
        ref carriersUsed,
        ref planesLost);
    }

    private void ProcessStrikeDiversion(
      IEnumerable<int> carriersTo,
      int planeType,
      int planeCount,
      decimal lossPct,
      ref SortedSet<int> carriersUsed,
      ref int planesLost)
    {
      if (planeCount > 0)
      {
        Dictionary<int, int> optionSlots;
        int optionIndex;
        int slotsLeft;
        int carrierId;
        int i;

        optionSlots = new Dictionary<int, int>();
        foreach (int cid in carriersTo)
        {
          slotsLeft =
            Math.Max(
              0,
              (int) ((this.COriginal[cid, planeType + 1] + (this.COriginal[cid, planeType + 4]) * 5m / 4m) -
              (int) (this.C[cid, planeType + 1] + this.C[cid, planeType + 4])));
          optionSlots[cid] = slotsLeft;
        }
        for (i = 0; i < planeCount; ++i)
        {
          if (((decimal)this.random.NextDouble()) < lossPct)
          {
            ++planesLost;
          }
          else
          {
            optionIndex = this.PickCarrierOption(ref optionSlots);
            if (optionIndex >= 0)
            {
              carrierId = optionSlots.ElementAt(optionIndex).Key;
              optionSlots[carrierId] -= 1;
              if (!carriersUsed.Contains(carrierId))
              {
                carriersUsed.Add(carrierId);
              }
              ++this.C[carrierId, planeType + 1];
            }
            else
            {
              ++planesLost;
            }
          }
        }
      }
    }

    private int PickCarrierOption(
      ref Dictionary<int, int> options)
    {
      int total;
      int index;
      int ret;
      int r;

      total = 0;
      foreach (int slotCount in options.Values)
      {
        total += slotCount;
      }
      r = this.random.Next(total);
      total = 0;
      ret = -1;
      index = 0;
      foreach (int slotCount in options.Values)
      {
        total += slotCount;
        if (r < total)
        {
          ret = index;
          break;
        }
        else
        {
          ++index;
        }
      }
      return ret;
    }

    private void ProcessDamageControl()
    {

    }

    private void CheckForGameCompletion()
    {

    }
  }
}
