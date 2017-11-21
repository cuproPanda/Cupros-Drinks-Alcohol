using Verse;

namespace CuprosDrinksAlcohol {

  public class CompProperties_AlcoholBarrel : CompProperties {

    public ThingDef ingredient;
    public ThingDef product;
    public int fermentationDays = 10;
    public float productMultiplier = 1;

    public CompProperties_AlcoholBarrel() {
      compClass = typeof(CompAlcoholBarrel);
    }
  }
}
