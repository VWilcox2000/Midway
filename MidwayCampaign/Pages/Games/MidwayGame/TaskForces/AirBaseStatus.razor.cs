using Microsoft.AspNetCore.Components;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame.TaskForces
{
  public class AirBaseStatusBase : ComponentBase
  {
    [CascadingParameter]
    public MidwayScenario? midway { get; set; }

    [Parameter]
    public ForceTypes? shipType { get; set; }

    [Parameter]
    public AirBases? airBase { get; set; }

    [Parameter]
    public bool? firstBase { get; set; }

    public AirBaseRef airBaseRef => this.midway!.getAirBaseRef(this.airBase!.Value);

    protected string airBaseName
    {
      get
      {
        string ret;

        switch(this.airBase)
        {
          case AirBases.Enterprise:
            ret = "CV-6 Enterprise";
            break;
          case AirBases.Hornet:
            ret = "CV-8 Hornet";
            break;
          case AirBases.Yorktown:
            ret = "CV-5 Yorktown";
            break;
          case AirBases.Midway:
            ret = "Midway Airfield";
            break;
          case AirBases.Akagi:
            ret = "IJN Akagi";
            break;
          case AirBases.Kaga:
            ret = "IJN Kaga";
            break;
          case AirBases.Soryu:
            ret = "IJN Soryu";
            break;
          case AirBases.Hiryu:
            ret = "IJN Hiryu";
            break;
          case AirBases.Zuiho:
            ret = "IJN Zuiho";
            break;
          default:
            ret = "?";
            break;
        }
        return ret;
      }
    }

    protected string rowModifier
    {
      get
      {
        string ret;

        if (this.firstBase == true)
        {
          ret = string.Empty;
        }
        else
        {
          ret = "airBaseNotFirstLine";
        }
        return ret;
      }
    }

    protected string armingModifier
    {
      get
      {
        string ret;

        if (this.airBaseRef.armed.sbds >= 1000)
        {
          ret = "armingInProgress";
        }
        else
        {
          ret = string.Empty;
        }
        return ret;
      }
    }

    public AirBaseStatusBase()
    {
    }
  }
}
