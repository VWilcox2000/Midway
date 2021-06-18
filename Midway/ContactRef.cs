using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidwayEngine
{
  public sealed class ContactRef
  {
    public int number { get; set; }
    public ForceRef? force { get; set; }
    public decimal bearing { get; set; }
    public decimal range { get; set; }
  }
}
