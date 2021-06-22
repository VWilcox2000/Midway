using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public sealed class StrikeEvent
  {
    public StrikeEventTypes eventType { get; }
    public string description { get; }
    public bool newLine { get; }

    public StrikeEvent(
      StrikeEventTypes eventType,
      string description,
      bool newLine)
    {
      this.eventType = eventType;
      this.description = description;
      this.newLine = newLine;
    }
  }
}
