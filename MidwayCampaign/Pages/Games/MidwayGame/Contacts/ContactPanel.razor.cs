using Microsoft.AspNetCore.Components;
using MidwayEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MidwayCampaign.Pages.Games.MidwayGame.Contacts
{
  public class ContactPanelBase : ComponentBase
  {
    [CascadingParameter]
    public MidwayScenario? midway { get; set; }

    [Parameter]
    public int? contactNumber { get; set; }

    public ContactRef? contactRef => this.midway!.getContactRef(this.contactNumber);

    protected bool showThisContact => this.contactRef != null;

    public string? contactNumberText => string.Concat(
      "Contact ",
      this.contactRef?.number.ToString());
    public string? contactName
    {
      get
      {
        string? ret;

        switch(this.contactRef?.force?.letter)
        {
          case ".":
          default:
            ret = "Surface Vessels";
            break;
          case "cv":
            ret = "Japanese Carriers";
            break;
          case "cr":
            ret = "Japanese Cruisers";
            break;
          case "tt":
            ret = "Japanese Troop Transports";
            break;
        }
        return ret;
      }
    }
    public string contactBearing =>
      string.Concat(
        this.contactRef?.bearing.ToString("0.0"),
        "°");
    public string contactRange =>
      string.Concat(
        this.contactRef?.range.ToString("0"),
        "mi");

    public ContactPanelBase()
    {
    }

    protected override void OnInitialized()
    {
      base.OnInitialized();
      this.midway!.taskForceUpdated += ContactPanel_taskForceUpdated;
    }

    private void ContactPanel_taskForceUpdated()
    {
      this.InvokeAsync(() =>
      {
        this.StateHasChanged();
      });
    }
  }
}
