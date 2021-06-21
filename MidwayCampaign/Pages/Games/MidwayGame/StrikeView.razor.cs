using Microsoft.AspNetCore.Components;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public class StrikeViewBase : ComponentBase
  {
    protected MidwayScenario midway { get; private set; } = new MidwayScenario();
    protected bool showStrikeView => this.midway.strikeHappening;

    public StrikeViewBase()
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
            this._midwayWatching.strikeHappeningChanged -= this.MidwayWatching_strikeHappeningChanged;
          }
          this._midwayWatching = value;
          if (this._midwayWatching != null)
          {
            this._midwayWatching.strikeHappeningChanged += this.MidwayWatching_strikeHappeningChanged;
          }
        }
      }
    }

    private void MidwayWatching_strikeHappeningChanged()
    {
      this.InvokeAsync(() =>
      {
        this.StateHasChanged();
      });
    }

    protected string strikeTitle
    {
      get
      {
        string ret;

        ret = "The Strike Title";
        return ret;
      }
    }
  }
}
