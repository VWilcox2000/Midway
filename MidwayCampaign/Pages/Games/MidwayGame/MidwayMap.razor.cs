using Microsoft.AspNetCore.Components;
using Midway;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame
{
  public class MidwayMapBase : ComponentBase
  {
    [CascadingParameter]
    public MidwayScenario? midway { get; set; }

    public MidwayMapBase()
    {
    }

    protected override void OnInitialized()
    {
      base.OnInitialized();
    }

    protected string forceStyle(ForceRef force)
    {
      StringBuilder sb;
      string ret;

      sb = new StringBuilder();
      sb.Append("left: ");
      sb.Append(force.x * 8.333M / 2.0M);
      sb.Append("%; top: ");
      sb.Append(force.y * 8.333M);
      sb.Append("%;");
      ret = sb.ToString();
      return ret;
    }
  }
}
