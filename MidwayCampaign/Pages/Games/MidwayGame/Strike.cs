using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public sealed class Strike
  {
    private Queue<StrikeEvent> strikeEvents { get; } = new Queue<StrikeEvent>();

    public void AddEvent(
      StrikeEventTypes eventType,
      string description,
      bool newLine)
    {
      this.strikeEvents.Enqueue(
        new StrikeEvent(
          eventType,
          description,
          newLine));
    }

    public bool hasMoreEvents
    {
      get
      {
        bool ret;

        ret = this.strikeEvents.Any();
        return ret;
      }
    }

    public StrikeEvent? nextEvent
    {
      get
      {
        StrikeEvent? ret;

        if (!this.strikeEvents.TryDequeue(out ret))
        {
          ret = null;
        }
        return ret;
      }
    }
  }
}
