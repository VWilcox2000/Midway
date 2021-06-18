using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidwayEngine
{
  public sealed class ForceRef
  {
    public ForceTypes type { get; set; }
    public decimal gridX { get; set; }
    public decimal gridY { get; set; }
    public decimal heading { get; set; }
    public decimal speed { get; set; }
    public decimal x { get; set; }
    public decimal y { get; set; }
    public string? letter { get; set; }
    public bool visible { get; set; }
    public bool isAllied
    {
      get
      {
        bool ret;

        switch(this.type)
        {
          case ForceTypes.MidwayIsland:
          case ForceTypes.TaskForce16:
          case ForceTypes.TaskForce17:
            ret = true;
            break;
          default:
            ret = false;
            break;
        }
        return ret;
      }
    }
  }
}
