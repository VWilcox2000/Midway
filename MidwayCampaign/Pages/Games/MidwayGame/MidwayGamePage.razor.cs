using Midway;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MidwayEngine;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public class MidwayGamePageBase : ComponentBase
  {
    protected MidwayScenario midway { get; } = new MidwayScenario();

    public MidwayGamePageBase()
    {
    }


    protected override void OnInitialized()
    {
      base.OnInitialized();
    }
  }
}
