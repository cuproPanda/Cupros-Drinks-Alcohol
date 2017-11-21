using System.Collections.Generic;

using Verse;
using Verse.AI;

namespace CuprosDrinksAlcohol {

  public class JobDriver_FillAlcoholBarrel : JobDriver {

    private const TargetIndex BarrelInd = TargetIndex.A;
    private const TargetIndex IngredientInd = TargetIndex.B;
    private const int Duration = 200;

    protected Building_AlcoholBarrel Barrel {
      get {
        return (Building_AlcoholBarrel)job.GetTarget(TargetIndex.A).Thing;
      }
    }

    protected Thing Ingredient {
      get {
        return job.GetTarget(TargetIndex.B).Thing;
      }
    }


		public override bool TryMakePreToilReservations() {
			return pawn.Reserve(Barrel, job) && pawn.Reserve(Ingredient, job);
		}


		protected override IEnumerable<Toil> MakeNewToils() {

			// Verify barrel and ingredient validity
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			this.FailOnBurningImmobile(TargetIndex.A);
			AddEndCondition(() => (Barrel.SpaceLeftForIngredient > 0) ? JobCondition.Ongoing : JobCondition.Succeeded);

      // Reserve resources
			yield return Toils_General.DoAtomic(delegate {
				job.count = Barrel.SpaceLeftForIngredient;
			});
			Toil reserveIngredient = Toils_Reserve.Reserve(IngredientInd);
			yield return reserveIngredient;
			
			// Haul and add resources to barrel
			yield return Toils_Goto.GotoThing(IngredientInd, PathEndMode.ClosestTouch)
				.FailOnDespawnedNullOrForbidden(IngredientInd)
				.FailOnSomeonePhysicallyInteracting(IngredientInd);
			yield return Toils_Haul.StartCarryThing(IngredientInd, false, true, false)
				.FailOnDestroyedNullOrForbidden(IngredientInd);
			yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveIngredient, IngredientInd, TargetIndex.None, true, null);
			yield return Toils_Goto.GotoThing(BarrelInd, PathEndMode.Touch);
			yield return Toils_General.Wait(200)
				.FailOnDestroyedNullOrForbidden(IngredientInd)
				.FailOnDestroyedNullOrForbidden(BarrelInd)
				.FailOnCannotTouch(BarrelInd, PathEndMode.Touch)
				.WithProgressBarToilDelay(BarrelInd);

			// Use ingredients
			yield return new Toil() {
				initAction = () => {
					int amountAccepted = Barrel.AddIngredient(Ingredient);
					if (amountAccepted <= 0) {
						// The ingredients don't match, end the job
						EndJobWith(JobCondition.Incompletable);
					}
					if (amountAccepted >= pawn.carryTracker.CarriedThing.stackCount) {
						pawn.carryTracker.CarriedThing.Destroy();
					}
					else {
						pawn.carryTracker.CarriedThing.stackCount -= amountAccepted;
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
    }
  }
}
