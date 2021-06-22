using Microsoft.AspNetCore.Components;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public class MidwayStatusBase : ComponentBase
  {
    [CascadingParameter]
    public MidwayScenario? midway { get; set; }

    protected string gameDate => this.midway!.dateText;

    protected string gameTime => this.midway!.timeText;

    public bool isDay => this.midway!.isDay;

    public MidwayStatusBase()
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
            this._midwayWatching.taskForceUpdated -= MidwayMapBase_taskForceUpdated;
            this._midwayWatching.endingActivitiies -= MidwayStatusBase_endingActivitiies;
          }
          this._midwayWatching = value;
          if (this._midwayWatching != null)
          {
            this._midwayWatching.taskForceUpdated += MidwayMapBase_taskForceUpdated;
            this._midwayWatching.endingActivitiies += MidwayStatusBase_endingActivitiies;
          }
        }
      }
    }

    private void MidwayStatusBase_endingActivitiies()
    {
      this.InvokeAsync(() =>
      {
        this.StateHasChanged();
      });
    }

    private void MidwayMapBase_taskForceUpdated()
    {
      this.InvokeAsync(() =>
      {
        this.StateHasChanged();
      });
    }
  }
}
