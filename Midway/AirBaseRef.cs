using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidwayEngine
{
  public sealed class AirBaseRef
  {
    public AirBases airBase { get; set; }
    public string name { get; set; } = string.Empty;
    public int cap { get; set; } = 0;
    public FlightGroups armed { get; } = new FlightGroups();
    public FlightGroups below { get; } = new FlightGroups();
    public decimal damage { get; set; }
    public bool escort { get; set; }
    public bool landBase { get; set; }
  }
}
