﻿using Midway;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MidwayEngine;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public class MidwayGamePageBase : ComponentBase
  {
    protected MidwayScenario midway { get; private set; } = new MidwayScenario();
    protected bool showFinalSummary => this.midway.gameOver;

    public MidwayGamePageBase()
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
            this._midwayWatching.playAgain -= Midway_playAgain;
            this._midwayWatching.gameNowOver -= Midway_gameNowOver;
            this._midwayWatching.endingActivitiies -= MidwayGamePageBase_endingActivitiies;
          }
          this._midwayWatching = value;
          if (this._midwayWatching != null)
          {
            this._midwayWatching.playAgain += Midway_playAgain;
            this._midwayWatching.gameNowOver += Midway_gameNowOver;
            this._midwayWatching.endingActivitiies += MidwayGamePageBase_endingActivitiies;
          }
        }
      }
    }

    private void MidwayGamePageBase_endingActivitiies()
    {
      this.InvokeAsync(() =>
      {
        this.StateHasChanged();
      });
    }

    private void Midway_gameNowOver()
    {
      this.InvokeAsync(() =>
      {
        this.StateHasChanged();
      });
    }

    private void Midway_playAgain()
    {
      this.midway = new MidwayScenario();
      this.InvokeAsync(() =>
      {
        this.StateHasChanged();
      });
    }
  }
}
