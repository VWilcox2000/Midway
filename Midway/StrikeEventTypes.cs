using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidwayEngine
{
  public enum StrikeEventTypes
  {
    StrikeStarting,
    ComponentMissesTarget,
    ComponentAttacksFleet,
    ComponentObliterated,
    ComponentAttackingCarrier,
    VictoryPointsAwarded,
    TargetSeverelyDamaged,
    TargetsSunk,
    StrikeTurnsBack,
    Hit,
    NearMiss,
    Dud,
    Miss,
    CAPAttacksComponent,
    CAPShootsDown,
    CAPEradicatesComponent,
    CAPNoKillsOnComponent,
    CAPNoKillsOnCAP,
    CAPEradicated,
    CAPVictoriesAgainstEscorts,
    EscortsDefendFromCAP,
    EscortsAttackCAP,
    EscortsScoreVictories,
    EscortsNoKills,
    EscortsEradicated,
    ThachUsed,
    AAOnWayIn,
    AAOnWayOut,
    TorpedoBomberResults,
    BomberResults,
    SecondaryExplosions,
    PlanesDamaged,
    PlanesExploding,
    MidwayAirbaseDestroyed,
    CarrierSunk,
    FleetAttackResults
  }
}
