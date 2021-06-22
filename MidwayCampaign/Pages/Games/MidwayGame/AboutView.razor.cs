using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public class AboutViewBase : ComponentBase
  {
    [CascadingParameter]
    public MidwayScenario? midway { get; set; }

    [Inject]
    protected IJSRuntime? jsRuntime { get; set; }

    private bool _showAbout = false;
    protected bool showAbout
    {
      get => this._showAbout;
      set
      {
        if (this._showAbout != value)
        {
          this._showAbout = value;
          this.InvokeAsync(() =>
          {
            this.StateHasChanged();
          });
        }
      }
    }

    public AboutViewBase()
    {
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
            this._midwayWatching!.showAboutRequested -= AboutViewBase_showAboutRequested;
          }
          this._midwayWatching = value;
          if (this._midwayWatching != null)
          {
            this._midwayWatching!.showAboutRequested += AboutViewBase_showAboutRequested;
          }
        }
      }
    }

    private void AboutViewBase_showAboutRequested()
    {
      this.showAbout = true;
    }

    protected void onOk(MouseEventArgs args)
    {
      this.showAbout = false;
    }

    protected string aboutStyleModifier
    {
      get
      {
        string ret;

        if (this.showAbout)
        {
          ret = string.Empty;
        }
        else
        {
          ret = "display: none;";
        }
        return ret;
      }
    }
  }
}
