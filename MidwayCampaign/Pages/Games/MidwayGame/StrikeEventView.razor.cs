using Microsoft.AspNetCore.Components;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public class StrikeEventViewBase : ComponentBase
  {
    [CascadingParameter]
    public MidwayScenario? midway { get; set; }

    [Parameter]
    public StrikeEvent? strikeEvent { get; set; }

    [Parameter]
    public StrikeEvent? previousStrikeEvent { get; set; }

    public string text => this.strikeEvent?.description ?? string.Empty;
    public bool newLine =>
      this.strikeEvent?.newLine == true ||
      this.firstAttackItem;

    private bool firstAttackItem
    {
      get
      {
        bool ret;

        switch(this.strikeEvent?.eventType)
        {
          case StrikeEventTypes.Hit:
          case StrikeEventTypes.NearMiss:
          case StrikeEventTypes.Dud:
          case StrikeEventTypes.Miss:
            switch(this.previousStrikeEvent?.eventType)
            {
              case StrikeEventTypes.Hit:
              case StrikeEventTypes.NearMiss:
              case StrikeEventTypes.Dud:
              case StrikeEventTypes.Miss:
                ret = false;
                break;
              default:
                ret = true;
                break;
            }
            break;
          default:
            ret = false;
            break;
        }
        return ret;
      }
    }

    protected string divClass
    {
      get
      {
        string ret;

        switch(this.strikeEvent?.eventType)
        {
          case StrikeEventTypes.StrikeStarting:
            if (this.strikeEvent.description.StartsWith("Japanese_area"))
            {
              ret = "strikeStart_Japanese_area";
            }
            else
            {
              ret = "strikeStart_USA_area";
            }
            break;
          case StrikeEventTypes.Hit:
            ret = "strike_HIT_area";
            break;
          case StrikeEventTypes.NearMiss:
            ret = "strike_NearMiss_area";
            break;
          case StrikeEventTypes.Dud:
            ret = "strike_Dud_area";
            break;
          case StrikeEventTypes.Miss:
            ret = "strike_Miss_area";
            break;
          default:
            ret = "strike_event_area";
            break;
        }
        return ret;
      }
    }

    protected string spanClass
    {
      get
      {
        string ret;

        switch (this.strikeEvent?.eventType)
        {
          case StrikeEventTypes.StrikeStarting:
            if (this.strikeEvent.description.StartsWith("Japanese"))
            {
              ret = "strikeStart_Japanese_text";
            }
            else
            {
              ret = "strikeStart_USA_text";
            }
            break;
          case StrikeEventTypes.Hit:
            ret = "strike_HIT_text";
            break;
          case StrikeEventTypes.NearMiss:
            ret = "strike_NearMiss_text";
            break;
          case StrikeEventTypes.Dud:
            ret = "strike_Dud_text";
            break;
          case StrikeEventTypes.Miss:
            ret = "strike_Miss_text";
            break;
          default:
            ret = "strike_event_text";
            break;
        }
        return ret;
      }
    }

    public StrikeEventViewBase()
    {
    }
  }
}
