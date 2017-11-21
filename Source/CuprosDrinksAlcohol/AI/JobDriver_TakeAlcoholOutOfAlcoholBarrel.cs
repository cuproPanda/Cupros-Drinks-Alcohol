using System.Collections.Generic;

using RimWorld;
using Verse;
using Verse.AI;

namespace CuprosDrinksAlcohol {

  public class JobDriver_TakeAlcoholOutOfAlcoholBarrel : JobDriver {

    private const TargetIndex BarrelInd = TargetIndex.A;
    private const TargetIndex AlcoholToHaulInd = TargetIndex.B;
    private const TargetIndex StorageCellInd = TargetIndex.C;

    protected Building_AlcoholBarrel Barrel {
      get {
        return (Building_AlcoholBarrel)job.GetTarget(TargetIndex.A).Thing;
      }
    }

    protected Thing Alcohol {
      get {
        return job.GetTarget(TargetIndex.B).Thing;
      }
		}


		public override bool TryMakePreToilReservations() {
			return pawn.Reserve(Barrel, job) && pawn.Reserve(Alcohol, job);
		}


		protected override IEnumerable<Toil> MakeNewToils() {

			// Verify barrel validity
			this.FailOnDespawnedNullOrForbidden(BarrelInd);
			this.FailOnBurningImmobile(BarrelInd);
			this.FailOn(() => !Barrel.Fermented);

      // Go to the barrel
			yield return Toils_Goto.GotoThing(BarrelInd, PathEndMode.Touch);

      // Add delay for collecting alcohol from barrel, if it is ready
			yield return Toils_General.Wait(200)
				.FailOnDestroyedNullOrForbidden(BarrelInd)
				.FailOnCannotTouch(BarrelInd, PathEndMode.Touch)
				.FailOn(() => !Barrel.Fermented)
				.WithProgressBarToilDelay(BarrelInd, false, -0.5f);

      // Collect alcohol
			yield return new Toil {
				initAction = delegate {
					Thing alcohol = Barrel.TakeOutProduct();
					GenPlace.TryPlaceThing(alcohol, pawn.Position, Map, ThingPlaceMode.Near);
					StoragePriority storagePriority = HaulAIUtility.StoragePriorityAtFor(alcohol.Position, alcohol);

					// Try to find a suitable storage spot for the alcohol
					if (StoreUtility.TryFindBestBetterStoreCellFor(alcohol, pawn, Map, storagePriority, pawn.Faction, out IntVec3 c)) {
						job.SetTarget(TargetIndex.C, c);
						job.SetTarget(TargetIndex.B, alcohol);
						job.count = alcohol.stackCount;
					}
					// If there is no spot to store the alcohol, end this job
					else {
						EndJobWith(JobCondition.Incompletable);
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};

      // Reserve the alcohol
      yield return Toils_Reserve.Reserve(AlcoholToHaulInd);

      // Reserve the storage cell
      yield return Toils_Reserve.Reserve(StorageCellInd);

      // Go to the alcohol
      yield return Toils_Goto.GotoThing(AlcoholToHaulInd, PathEndMode.ClosestTouch);

      // Pick up the alcohol
      yield return Toils_Haul.StartCarryThing(AlcoholToHaulInd);

      // Carry the alcohol to the storage cell, then place it down
      Toil carry = Toils_Haul.CarryHauledThingToCell(StorageCellInd);
      yield return carry;
      yield return Toils_Haul.PlaceHauledThingInCell(StorageCellInd, carry, true);
    }
  }
}
