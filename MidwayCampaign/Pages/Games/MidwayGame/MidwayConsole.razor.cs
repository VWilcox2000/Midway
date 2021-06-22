using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public class MidwayConsoleBase : ComponentBase
  {
    [CascadingParameter]
    public MidwayScenario? midway { get; set; }

    [Inject]
    protected IJSRuntime? jsRuntime { get; set; }

    private string _consoleEntry = string.Empty;
    protected string consoleEntry
    {
      get => this._consoleEntry;
      set
      {
        if (this._consoleEntry != value)
        {
          this._consoleEntry = value;
          this.InvokeAsync(() =>
          {
            this.StateHasChanged();
          });
        }
      }
    }

    private string _logLineText = string.Empty;
    protected string logLineText
    {
      get => this._logLineText;
      set
      {
        if (this._logLineText != value)
        {
          this._logLineText = value;
          this.InvokeAsync(() =>
          {
            this.StateHasChanged();
          });
        }
      }
    }

    public MidwayConsoleBase()
    {
    }

    protected override void OnInitialized()
    {
      base.OnInitialized();
    }

    private Guid uidOurGameInstance = Guid.Empty;
    protected override void OnAfterRender(bool firstRender)
    {
      base.OnAfterRender(firstRender);
      if (this.uidOurGameInstance != this.midway!.uidGameInstance)
      {
        this.uidOurGameInstance = this.midway!.uidGameInstance;
        this._logLineText =
          "Welcome to the Midway Campaign..." +
          Environment.NewLine +
          Environment.NewLine;
      }
      this.jsRuntime?.InvokeVoidAsync(
        "midway.scrollLog",
        "logTextArea");
      this.jsRuntime?.InvokeVoidAsync(
        "midway.focus",
        "inputCommand");
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
            this._midwayWatching.outputText -= this.Midway_outputText;
            this._midwayWatching.outputWord -= this.MidwayConsoleBase_outputWord;
            this._midwayWatching.endingActivitiies -= this.MidwayConsoleBase_endingActivitiies;
            this._midwayWatching.strikeHappeningChanged -= _midwayWatching_strikeHappeningChanged;
          }
          this._midwayWatching = value;
          if (this._midwayWatching != null)
          {
            this._midwayWatching.outputText += this.Midway_outputText;
            this._midwayWatching.outputWord += this.MidwayConsoleBase_outputWord;
            this._midwayWatching.endingActivitiies += this.MidwayConsoleBase_endingActivitiies;
            this._midwayWatching.strikeHappeningChanged += _midwayWatching_strikeHappeningChanged;
          }
        }
      }
    }

    private void _midwayWatching_strikeHappeningChanged()
    {
      if (this.midway?.strikeHappening == true)
      {
        this.logLineText =
          string.Concat(
            this.logLineText,
            Environment.NewLine,
            "---------------------------------------------------------",
            Environment.NewLine);
      }
    }

    private void MidwayConsoleBase_endingActivitiies()
    {
      this.UpdateReadOnlyStatus();
    }

    private void Midway_outputText(
      StrikeEventTypes? eventType,
      string message)
    {
      this.logLineText =
        string.Concat(
          this.logLineText,
          Environment.NewLine,
          message);
    }

    private void MidwayConsoleBase_outputWord(
      StrikeEventTypes? eventType,
      string message)
    {
      this.logLineText =
        string.Concat(
          this.logLineText,
          message);
      Task.Delay(250);
    }

    protected void onKeyPress(KeyboardEventArgs args)
    {
      if (
        args.Code == "Enter" ||
        args.Code == "NumpadEnter")
      {
        string command;

        command = this.consoleEntry.Trim();
        this.consoleEntry = string.Empty;
        Task.Run(() =>
        {
          this.ProcessCommand(command);
        });
      }
    }

    private void ProcessCommand(string command)
    {
      this.midway!.ProcessCommand(command);
    }

    private void UpdateReadOnlyStatus()
    {
      this.readOnly = !this.canTakeCommands;
    }

    private bool _readOnly;
    public bool readOnly
    {
      get => this._readOnly;
      set
      {
        if (this._readOnly != value)
        {
          this.InvokeAsync(() =>
          {
            this.StateHasChanged();
          });
          this._readOnly = value;
        }
      }
    }

    protected bool canTakeCommands =>
      this.midway != null &&
      !this.midway.gameOver &&
      !this.midway.activitiesHappening;
  }
}
