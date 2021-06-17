using Microsoft.AspNetCore.Components;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public class MidwayStatusBase : ComponentBase
  {
    [CascadingParameter]
    public MidwayScenario? midway { get; set; }

    protected string gameDate => this.midway!.dateText;

    protected string gameTime => this.midway!.timeText;

    public MidwayStatusBase()
    {
    }

    protected override void OnInitialized()
    {
      base.OnInitialized();
    }
  }
}
