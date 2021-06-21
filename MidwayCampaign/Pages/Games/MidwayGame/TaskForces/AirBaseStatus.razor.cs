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

    protected override void OnInitialized()
    {
      base.OnInitialized();
    }

    protected override void OnAfterRender(bool firstRender)
    {
      base.OnAfterRender(firstRender);
      this.midwayWatching = this.midway;
    }

    private MidwayScenario? _midwayWatching = null;
    private MidwayScenario? midwayWatching
    {
      get => this._midwayWatching;
      set
      {
        if (this._midwayWatching != value)
        {
          if (this._midwayWatching != null)
          {
            this._midwayWatching.taskForceUpdated -= AirBaseStatus_taskForceUpdated;
            this._midwayWatching.endingActivitiies -= AirBaseStatus_endingActivitiies;
          }
          this._midwayWatching = value;
          if (this._midwayWatching != null)
          {
            this._midwayWatching.taskForceUpdated += AirBaseStatus_taskForceUpdated;
            this._midwayWatching.endingActivitiies += AirBaseStatus_endingActivitiies;
          }
        }
      }
    }

    private void AirBaseStatus_endingActivitiies()
    {
      this.InvokeAsync(() =>
      {
        this.StateHasChanged();
      });
    }

    private void AirBaseStatus_taskForceUpdated()
    {
      this.InvokeAsync(() =>
      {
        this.StateHasChanged();
      });
    }
  }
}
