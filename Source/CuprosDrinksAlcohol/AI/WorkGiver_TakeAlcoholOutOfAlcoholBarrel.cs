using RimWorld;
using Verse;
using Verse.AI;

namespace CuprosDrinksAlcohol {

  public class WorkGiver_TakeAlcoholOutOfAlcoholBarrel : WorkGiver_Scanner {

    public override ThingRequest PotentialWorkThingRequest {
      get {
        return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
      }
    }

    public override PathEndMode PathEndMode {
      get {
        return PathEndMode.Touch;
      }
    }


    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
      Building_AlcoholBarrel building_AlcoholBarrel = t as Building_AlcoholBarrel;
      return building_AlcoholBarrel != null && building_AlcoholBarrel.Fermented && !t.IsBurning() && !t.IsForbidden(pawn) && pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced);
    }


    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
      return new Job(DefDatabase<JobDef>.GetNamed("CPD_TakeAlcoholOutOfAlcoholBarrel"), t);
    }
  }
}
