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
      this.midway!.outputText += Midway_outputText;
    }

    protected override void OnAfterRender(bool firstRender)
    {
      base.OnAfterRender(firstRender);
      this.jsRuntime?.InvokeVoidAsync(
        "midway.scrollLog",
        "logTextArea");
    }

    private void Midway_outputText(string message)
    {
      this.logLineText =
        string.Concat(
          this.logLineText,
          Environment.NewLine,
          message);
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
        this.ProcessCommand(command);
      }
    }

    private void ProcessCommand(string command)
    {
      this.midway!.ProcessCommand(command);
    }
  }
}
