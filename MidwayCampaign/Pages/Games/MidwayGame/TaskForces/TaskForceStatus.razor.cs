using Microsoft.AspNetCore.Components;
using Midway;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame.TaskForces
{
  public class TaskForceStatusBase : ComponentBase
  {
    [CascadingParameter]
    public MidwayScenario? midway { get; set; }


    [Parameter]
    public ForceTypes? forceType { get; set; }

    protected bool firstBase { get; set; } = true;

    protected string taskForceTitle
    {
      get
      {
        string ret;

        switch(this.forceType)
        {
          case ForceTypes.TaskForce16:
            ret = "TF-16";
            break;
          case ForceTypes.TaskForce17:
            ret = "TF-17";
            break;
          case ForceTypes.MidwayIsland:
            ret = "Midway Island";
            break;
          case ForceTypes.JapaneseCarrierGroup:
            ret = "IJ-CV";
            break;
          case ForceTypes.JapaneseCruisers:
            ret = "IJ-Cruisers";
            break;
          case ForceTypes.JapaneseTransports:
            ret = "IJ-Troop Transports";
            break;
          default:
            ret = "?";
            break;
        }
        return ret;
      }
    }

    private bool forceRefLookedUp = false;
    private ForceRef? _forceRef = null;
    protected ForceRef? forceRef
    {
      get
      {
        if (
          this._forceRef == null &&
          !this.forceRefLookedUp)
        {
          this._forceRef = this.midway!.forces
            .Where(
              f =>
                f.type == this.forceType)
            .FirstOrDefault();
          this.forceRefLookedUp = true;
        }
        return this._forceRef;
      }
    }

    protected string taskForceHeading => Convert.ToInt32((this.forceRef?.heading ?? 0M) + 0.5M) + "°";
    protected string taskForceSpeed => Convert.ToInt32((this.forceRef?.speed ?? 0M) + 0.5M) + " knots";
    protected string taskForceGridX => Convert.ToInt32(this.forceRef?.gridX ?? 0M).ToString();
    protected string taskForceGridY => Convert.ToInt32(this.forceRef?.gridY ?? 0M).ToString();

    protected string baseAreaColoring => (this.forceRef?.type == ForceTypes.MidwayIsland) ? "taskForceMidwayBaseArea" : "taskForceCarrierBaseArea";

    protected IEnumerable<AirBases> airBases
    {
      get
      {
        switch (this.forceType)
        {
          case ForceTypes.TaskForce16:
            yield return AirBases.Enterprise;
            yield return AirBases.Hornet;
            break;
          case ForceTypes.TaskForce17:
            yield return AirBases.Yorktown;
            break;
          case ForceTypes.MidwayIsland:
            yield return AirBases.Midway;
            break;
          case ForceTypes.JapaneseCarrierGroup:
            yield return AirBases.Akagi;
            yield return AirBases.Kaga;
            yield return AirBases.Soryu;
            yield return AirBases.Hiryu;
            break;
          case ForceTypes.JapaneseCruisers:
            break;
          case ForceTypes.JapaneseTransports:
            break;
          default:
            break;
        }
      }
    }

    public TaskForceStatusBase()
    {
    }

    protected override void OnAfterRender(bool firstRender)
    {
      base.OnAfterRender(firstRender);
      this.midway!.taskForceUpdated += TaskForceStatusBase_taskForceUpdated;
    }

    private void TaskForceStatusBase_taskForceUpdated()
    {
      this.forceRefLookedUp = false;
      this._forceRef = null;
      this.InvokeAsync(() =>
      {
        this.StateHasChanged();
      });
    }
  }
}
