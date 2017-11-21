using System;

using RimWorld;
using Verse;
using Verse.AI;

namespace CuprosDrinksAlcohol {

  public class WorkGiver_FillAlcoholBarrel : WorkGiver_Scanner {


    private static string TemperatureTrans;
    private static string NoIngredientTrans;

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
      if (building_AlcoholBarrel == null || building_AlcoholBarrel.Fermented || building_AlcoholBarrel.SpaceLeftForIngredient <= 0) {
        return false;
      }
      float ambientTemperature = building_AlcoholBarrel.AmbientTemperature;
      CompProperties_TemperatureRuinable compProperties = building_AlcoholBarrel.def.GetCompProperties<CompProperties_TemperatureRuinable>();
      if (ambientTemperature < compProperties.minSafeTemperature + 2f || ambientTemperature > compProperties.maxSafeTemperature - 2f) {
        JobFailReason.Is(TemperatureTrans);
        return false;
      }
      if (t.IsForbidden(pawn) || !pawn.CanReserveAndReach(t, PathEndMode.Touch, pawn.NormalMaxDanger(), 1, -1, null, forced)) {
        return false;
      }
      if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null) {
        return false;
      }
      if (FindIngredient(pawn, building_AlcoholBarrel) == null) {
        JobFailReason.Is(NoIngredientTrans);
        return false;
      }
      return !t.IsBurning();
    }


    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
      Building_AlcoholBarrel building_AlcoholBarrel = (Building_AlcoholBarrel)t;
      Thing t2 = FindIngredient(pawn, building_AlcoholBarrel);
      return new Job(DefDatabase<JobDef>.GetNamed("CPD_FillAlcoholBarrel"), t, t2) {
        count = building_AlcoholBarrel.SpaceLeftForIngredient
      };
    }


    private Thing FindIngredient(Pawn pawn, Building_AlcoholBarrel barrel) {
      Predicate<Thing> predicate = (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x);
      Predicate<Thing> validator = predicate;
      return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(barrel.Ingredient), PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator);
    }
  }
}
