using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public class MidwayFinalScoreSummaryBase : ComponentBase
  {
    [CascadingParameter]
    public MidwayScenario? midway { get; set; }

    public MidwayFinalScoreSummaryBase()
    {
    }

    protected string finalVictorCss
    {
      get
      {
        string ret;

        switch (this.results.victor)
        {
          case "The United States":
            ret = "finalVictorDeclarationUS";
            break;
          case "Japan":
            ret = "finalVictorDeclarationJapan";
            break;
          default:
            ret = "finalVictorDeclarationDraw";
            break;
        }
        return ret;
      }
    }

    protected string finalVictorText
    {
      get
      {
        int points;
        string ret;

        points = Math.Abs(this.results.victoryPoints);
        switch (this.results.victor)
        {
          case "The United States":
            if (points >= 4000)
            {
              ret = "The United States scores an overwhelmingly decisive strategic victory.";
            }
            else if (points >= 3000)
            {
              ret = "The United States scores a decisive strategic victory.";
            }
            else if (points >= 2000)
            {
              ret = "The United States scores a strategic victory.";
            }
            else if (points >= 1000)
            {
              ret = "The United States scores a tactical victory.";
            }
            else
            {
              ret = "The United States scores a marginal victory.";
            }
            break;
          case "Japan":
            if (points >= 4000)
            {
              ret = "Japan scores an overwhelmingly decisive strategic victory.";
            }
            else if (points >= 3000)
            {
              ret = "Japan scores a decisive strategic victory.";
            }
            else if (points >= 2000)
            {
              ret = "Japan scores a strategic victory.";
            }
            else if (points >= 1000)
            {
              ret = "Japan scores a tactical victory.";
            }
            else
            {
              ret = "Japan scores a marginal victory.";
            }
            break;
          default:
            ret = "The battle is a draw";
            break;
        }
        return ret;
      }
    }

    protected string stateOf(AirBases airBase) => this.midway!.stateOf(airBase);
    protected bool includeZuiho => this.midway!.zuihoTouched;
    protected int usPlanesLost => this.midway!.usPlanesLost;
    protected int japanesePlanesLost => this.midway!.japanesePlanesLost;
    protected string usPlanesLostText
    {
      get
      {
        string ret;
        int count;

        count = this.usPlanesLost;
        if (count == 1)
        {
          ret = "1 plane";
        }
        else
        {
          ret = string.Format(
            "{0} planes",
            count);
        }
        return ret;
      }
    }

    protected string japanesePlanesLostText
    {
      get
      {
        string ret;
        int count;

        count = this.japanesePlanesLost;
        if (count == 1)
        {
          ret = "1 plane";
        }
        else
        {
          ret = string.Format(
            "{0} planes",
            count);
        }
        return ret;
      }
    }
    protected string midwayFallenStatus => this.midway!.midwayIsFallen ? "fallen" : "NOT fallen";

    private ResultsData? _results;
    protected ResultsData results
    {
      get
      {
        if (this._results == null)
        {
          this._results = new ResultsData(this.midway!);
        }
        return this._results;
      }
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
            this._midwayWatching!.playAgain -= MidwayFinalScoreSummaryBase_playAgain;
            this._midwayWatching!.gameNowOver -= MidwayFinalScoreSummaryBase_gameNowOver;
          }
          this._midwayWatching = value;
          if (this._midwayWatching != null)
          {
            this._midwayWatching!.playAgain += MidwayFinalScoreSummaryBase_playAgain;
            this._midwayWatching!.gameNowOver += MidwayFinalScoreSummaryBase_gameNowOver;
          }
        }
      }
    }

    private void MidwayFinalScoreSummaryBase_gameNowOver()
    {
      this._results = null;
    }

    private void MidwayFinalScoreSummaryBase_playAgain()
    {
      this._results = null;
    }

    protected void onPlayAgain(MouseEventArgs args)
    {
      this.midway!.FirePlayAgain();
    }
  }
}
