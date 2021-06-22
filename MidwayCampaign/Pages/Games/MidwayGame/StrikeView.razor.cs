using Microsoft.AspNetCore.Components;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public class StrikeViewBase : ComponentBase
  {
    [CascadingParameter]
    public MidwayScenario? midway { get; set; }
    private Queue<Strike> strikes { get; } = new Queue<Strike>();
    private Strike? currentStrike = null;
    private Strike? playingStrike = null;
    private object syncCurrentStrike = new object();
    private bool _showStrikeView = false;
    protected bool showStrikeView
    {
      get => this._showStrikeView;
      set
      {
        if (this._showStrikeView != value)
        {
          this._showStrikeView = value;
          this.InvokeAsync(() =>
          {
            this.StateHasChanged();
          });
        }
      }
    }

    private List<StrikeEvent> _currentEvents { get; } = new List<StrikeEvent>();
    public IEnumerable<StrikeEvent> currentEvents => this._currentEvents.ToArray();

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
      this.EnsurePlayerRunning();
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
            this._midwayWatching.outputText -= _midwayWatching_outputText;
            this._midwayWatching.outputWord -= _midwayWatching_outputWord;
          }
          this._midwayWatching = value;
          if (this._midwayWatching != null)
          {
            this._midwayWatching.strikeHappeningChanged += this.MidwayWatching_strikeHappeningChanged;
            this._midwayWatching.outputText += _midwayWatching_outputText;
            this._midwayWatching.outputWord += _midwayWatching_outputWord;
          }
        }
      }
    }

    private void _midwayWatching_outputText(
      StrikeEventTypes? eventType,
      string message)
    {
      if (
        this.currentStrike != null &&
        eventType != null)
      {
        this.currentStrike.AddEvent(
          eventType.Value,
          message,
          true);
      }
    }

    private void _midwayWatching_outputWord(
      StrikeEventTypes? eventType,
      string message)
    {
      if (
        this.currentStrike != null &&
        eventType != null)
      {
        this.currentStrike.AddEvent(
          eventType.Value,
          message,
          false);
      }
    }

    private void MidwayWatching_strikeHappeningChanged()
    {
      if (this.midway!.strikeHappening)
      {
        lock (this.syncCurrentStrike)
        {
          this.currentStrike = new Strike();
          this.strikes.Enqueue(this.currentStrike);
        }
        System.Diagnostics.Trace.WriteLine("Starting new Strike Event Queue");
      }
      else
      {
        lock(this.syncCurrentStrike)
        {
          this.currentStrike = null;
        }
      }
    }

    private string _strikeTitle = string.Empty;
    protected string strikeTitle
    {
      get => this._strikeTitle;
      set
      {
        if (this._strikeTitle != value)
        {
          this._strikeTitle = value;
          this.InvokeAsync(() =>
          {
            this.StateHasChanged();
          });
        }
      }
    }

    private void EnsurePlayerRunning()
    {
      if (this.uidPlayer != this.midway!.uidGameInstance)
      {
        this.uidPlayer = this.midway!.uidGameInstance;
        Task.Run(async () =>
        {
          await this.PlayStrike(this.midway!.uidGameInstance);
        });
      }
    }

    private DateTimeOffset _lastEvent = DateTimeOffset.MinValue;
    private DateTimeOffset lastEvent
    {
      get => this._lastEvent;
      set
      {
        this._lastEvent = value;
        this.showStrikeView = true;
      }
    }
    private Guid uidPlayer = Guid.Empty;
    private async Task PlayStrike(Guid uid)
    {
      for (; ; )
      {
        if (this.uidPlayer == uid)
        {
          if (this.playingStrike != null)
          {
            StrikeEvent? se;

            se = this.playingStrike.nextEvent;
            if (se != null)
            {
              if (se.eventType == StrikeEventTypes.StrikeStarting)
              {
                if (this._currentEvents.Any())
                {
                  this._currentEvents.Clear();
                  await this.InvokeAsync(() =>
                  {
                    this.StateHasChanged();
                  });
                }
                this.strikeTitle = se.description.Replace(Environment.NewLine, string.Empty);
              }
              this.lastEvent = DateTimeOffset.UtcNow;
              this._currentEvents.Add(se);
              this.LimitLines(10);
              await this.InvokeAsync(() =>
              {
                this.StateHasChanged();
              });
              switch (se.eventType)
              {
                case StrikeEventTypes.Hit:
                case StrikeEventTypes.NearMiss:
                case StrikeEventTypes.Dud:
                case StrikeEventTypes.Miss:
                  await Task.Delay(400);
                  break;
                default:
                  await Task.Delay(1000);
                  break;
              }
            }
            else
            {
              this.playingStrike = null;
              await Task.Delay(2000);
            }
          }
          else
          {
            Strike? nextStrike;

            if (this.strikes.TryDequeue(out nextStrike))
            {
              System.Diagnostics.Trace.WriteLine("Dequeuing strike to play.");
              this.strikeTitle = string.Empty;
              this.playingStrike = nextStrike;
              this.lastEvent = DateTimeOffset.UtcNow;
            }
            else
            {
              if (DateTimeOffset.UtcNow - this.lastEvent > TimeSpan.FromSeconds(3.2))
              {
                this.showStrikeView = false;
              }
              await Task.Delay(500);
            }
          }
        }
        else
        {
          break;
        }
      }
    }

    private void LimitLines(int lines)
    {
      StrikeEvent strikeEvent;
      bool removeFromHere;
      bool isNewLine;
      int smallItems;
      int linesSoFar;
      int count;
      int i;

      linesSoFar = 0;
      smallItems = 0;
      removeFromHere = false;
      count = this._currentEvents.Count;
      for (i = count - 1; i >= 0; --i)
      {
        if (removeFromHere)
        {
          this._currentEvents.RemoveAt(i);
        }
        else
        {
          strikeEvent = this._currentEvents[i];
          if (strikeEvent.newLine)
          {
            isNewLine = true;
          }
          else
          {
            ++smallItems;
            isNewLine = (smallItems > 10);
          }
          if (isNewLine)
          {
            ++linesSoFar;
            smallItems = 0;
            if (linesSoFar > lines)
            {
              removeFromHere = true;
            }
          }
        }
      }
    }
  }
}
