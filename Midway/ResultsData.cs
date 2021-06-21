using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidwayEngine
{
  public sealed class ResultsData
  {
    public Dictionary<string, decimal> japaneseCarrierHealth { get; } = new Dictionary<string, decimal>();
    public Dictionary<string, decimal> usCarrierHealth { get; } = new Dictionary<string, decimal>();
    public int japanesePlanesLost { get; }
    public int usPlanesLost { get; }
    public bool midwayFell { get; }
    public int victoryPoints { get; }
    public string? victor
    {
      get
      {
        string? ret;

        if (this.victoryPoints > 0)
        {
          ret = "The United States";
        }
        else if (this.victoryPoints < 0)
        {
          ret = "Japan";
        }
        else
        {
          ret = null;
        }
        return ret;
      }
    }

    public ResultsData(MidwayScenario scenario)
    {
      foreach (AirBaseRef airBase in scenario.airBaseRefs)
      {
        switch (airBase.airBase)
        {
          case AirBases.Akagi:
          case AirBases.Kaga:
          case AirBases.Soryu:
          case AirBases.Hiryu:
          case AirBases.Zuiho:
            this.japaneseCarrierHealth[airBase.airBase.ToString()] = airBase.damage;
            break;
          case AirBases.Enterprise:
          case AirBases.Hornet:
          case AirBases.Yorktown:
          case AirBases.Midway:
            this.usCarrierHealth[airBase.airBase.ToString()] = airBase.damage;
            break;
        }
      }
      this.japanesePlanesLost = scenario.japanesePlanesLost;
      this.usPlanesLost = scenario.usPlanesLost;
      this.midwayFell = scenario.midwayIsFallen;
      this.victoryPoints = scenario.victoryPoints;
    }
  }
}
