using Microsoft.AspNetCore.Components;
using Midway;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public class MidwayMapBase : ComponentBase
  {
    [CascadingParameter]
    public MidwayScenario? midway { get; set; }

    public MidwayMapBase()
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
            this._midwayWatching.endingActivitiies -= MidwayMapBase_endingActivitiies;
          }
          this._midwayWatching = value;
          if (this._midwayWatching != null)
          {
            this._midwayWatching.taskForceUpdated += MidwayMapBase_taskForceUpdated;
            this._midwayWatching.endingActivitiies += MidwayMapBase_endingActivitiies;
          }
        }
      }
    }

    private void MidwayMapBase_endingActivitiies()
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

    protected string forceStyle(ForceRef force)
    {
      StringBuilder sb;
      string ret;

      sb = new StringBuilder();
      sb.Append("left: ");
      sb.Append(force.x * 8.333M / 2.0M);
      sb.Append("%; top: ");
      sb.Append(force.y * 8.333M);
      sb.Append("%;");
      ret = sb.ToString();
      return ret;
    }
  }
}
