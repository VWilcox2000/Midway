using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.API
{
  [ApiController]
  [Route("api/MW")]
  public class MidwayAPI
  {
    [HttpGet]
    [Route("")]
    public string Identify()
    {
      return "MW.TestController";
    }

    [HttpGet]
    [Route("ping")]
    public string Ping()
    {
      DateTimeOffset now;

      now = DateTimeOffset.UtcNow;
      return string.Concat(
        now.Date.ToShortDateString(),
        "  ",
        now.DateTime.ToShortTimeString());
    }

    [HttpGet]
    [Route("add/{a}/{b}")]
    public string Add(
      decimal a,
      decimal b)
    {
      return (a + b).ToString();
    }
  }
}
