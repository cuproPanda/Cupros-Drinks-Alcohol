using System.Collections.Generic;
using System.Text;

using UnityEngine;
using RimWorld;
using Verse;

namespace CuprosDrinksAlcohol {
  [StaticConstructorOnStartup]
  public class Building_AlcoholBarrel : Building {

    public const int MaxCapacity = 25;
    public const float MinIdealTemperature = 7f;
    
    private static readonly Vector2 BarSize = new Vector2(0.55f, 0.1f);
    private static readonly Color BarZeroProgressColor = new Color(0.4f, 0.27f, 0.22f);
    private static readonly Color BarFermentedColor = new Color(0.9f, 0.85f, 0.2f);
    private static readonly Material BarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f), false);

    private int ingredientCount;
    private float progressInt;
    private Material barFilledCachedMat;

    private CompAlcoholBarrel barrelComp;

    public ThingDef Ingredient {
      get {
        if (barrelComp == null) {
          barrelComp = GetComp<CompAlcoholBarrel>();
        }
        return barrelComp.Props.ingredient;
      }
    }

    public ThingDef Product {
      get {
        if (barrelComp == null) {
          barrelComp = GetComp<CompAlcoholBarrel>();
        }
        return barrelComp.Props.product;
      }
    }

    public float Progress {
      get { return progressInt; }
      set {
        if (value == progressInt) {
          return;
        }
        progressInt = value;
        barFilledCachedMat = null;
      }
    }

    private int FermentationDuration {
      get {
        return barrelComp.Props.fermentationDays * GenDate.TicksPerDay;
      }
    }

    private Material BarFilledMat {
      get {
        if (barFilledCachedMat == null) {
          barFilledCachedMat = SolidColorMaterials.SimpleSolidColorMaterial(Color.Lerp(BarZeroProgressColor, BarFermentedColor, Progress), false);
        }
        return barFilledCachedMat;
      }
    }

    public int SpaceLeftForIngredient {
      get {
        if (Fermented) {
          return 0;
        }
        return 25 - ingredientCount;
      }
    }

    private bool Empty {
      get {
        return ingredientCount <= 0;
      }
    }

    public bool Fermented {
      get {
        return !Empty && Progress >= 1f;
      }
    }

    private float CurrentTempProgressSpeedFactor {
      get {
        CompProperties_TemperatureRuinable compProperties = def.GetCompProperties<CompProperties_TemperatureRuinable>();
        float ambientTemperature = AmbientTemperature;
        if (ambientTemperature < compProperties.minSafeTemperature) {
          return 0.1f;
        }
        if (ambientTemperature < 7f) {
          return GenMath.LerpDouble(compProperties.minSafeTemperature, 7f, 0.1f, 1f, ambientTemperature);
        }
        return 1f;
      }
    }

    private float ProgressPerTickAtCurrentTemp {
      get {
        return (1f / FermentationDuration) * CurrentTempProgressSpeedFactor;
      }
    }

    private int EstimatedTicksLeft {
      get {
        return Mathf.Max(Mathf.RoundToInt((1f - Progress) / ProgressPerTickAtCurrentTemp), 0);
      }
    }


    public override void Draw() {
      base.Draw();
      if (!Empty) {
        Vector3 drawPos = DrawPos;
        drawPos.y += 0.0483870953f;
        drawPos.z += 0.25f;
        GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest {
          center = drawPos,
          size = BarSize,
          fillPercent = ingredientCount / 25f,
          filledMat = BarFilledMat,
          unfilledMat = BarUnfilledMat,
          margin = 0.1f,
          rotation = Rot4.North
        });
      }
    }


    public override void ExposeData() {
      base.ExposeData();
      Scribe_Values.Look(ref ingredientCount, "CPD_AlcoholBarrel_IngredientCount", 0, false);
      Scribe_Values.Look(ref progressInt, "CPD_AlcoholBarrel_Progress", 0f, false);
    }


    public override void SpawnSetup(Map map, bool respawningAfterLoad) {
      base.SpawnSetup(map, respawningAfterLoad);

      barrelComp = GetComp<CompAlcoholBarrel>();
    }


    public override void TickRare() {
      base.TickRare();

      if (!Empty) {
        Progress = Mathf.Min(Progress + 250f * ProgressPerTickAtCurrentTemp, 1f);
      }
    }


    public int AddIngredient(Thing ingredient) {
      int count = 0;
      if (ingredient.def != Ingredient) {
        return 0;
      }

      if (ingredient.stackCount <= SpaceLeftForIngredient) {
        count = ingredient.stackCount;
      }
      else {
        count = SpaceLeftForIngredient;
      }
      AddIngredient(count);
      return count;
    }


    public void AddIngredient(int count) {
      GetComp<CompTemperatureRuinable>().Reset();
      if (Fermented) {
        Log.Warning("Cupro's Drinks:: Tried to add ingredient to an alcohol barrel full of alcohol. Colonists should take the alcohol first.");
        return;
      }
      int num = Mathf.Min(count, 25 - ingredientCount);
      if (num <= 0) {
        return;
      }
      Progress = GenMath.WeightedAverage(0f, num, Progress, ingredientCount);
      ingredientCount += num;
    }


    protected override void ReceiveCompSignal(string signal) {
      if (signal == "RuinedByTemperature") {
        Reset();
      }
    }


    private void Reset() {
      ingredientCount = 0;
      Progress = 0f;
    }


    public Thing TakeOutProduct() {
      if (!Fermented) {
        Log.Warning("Cupro's Drinks:: Tried to get alcohol but it's not yet fermented.");
        return null;
      }
      Thing thing = ThingMaker.MakeThing(Product, null);
      thing.stackCount = Mathf.Min((int)(ingredientCount * barrelComp.Props.productMultiplier), Product.stackLimit);
      Reset();
      return thing;
    }


    public override IEnumerable<Gizmo> GetGizmos() {

      // Add button for finishing the fermenting
      Command_Action DevFinish = new Command_Action() {
        defaultLabel = "DEBUG: Finish",
        activateSound = SoundDefOf.Click,
        action = () => { Progress = 1f; },
      };

      if (Prefs.DevMode && !Empty) {
        yield return DevFinish;
      }

      foreach (Command c in base.GetGizmos()) {
        yield return c;
      }
    }


    public override string GetInspectString() {
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append(base.GetInspectString());
      if (stringBuilder.Length != 0) {
        stringBuilder.AppendLine();
      }
      CompTemperatureRuinable comp = GetComp<CompTemperatureRuinable>();
      if (!Empty && !comp.Ruined) {
        if (Fermented) {
          stringBuilder.AppendLine("CPD_ContainsAlcohol".Translate(new object[]
          {
            ingredientCount,
            25,
            Product.label
          }));
        }
        else {
          stringBuilder.AppendLine("CPD_ContainsIngredient".Translate(new object[]
          {
            ingredientCount,
            25,
            Ingredient.label
          }));
        }
      }
      if (!Empty) {
        if (Fermented) {
          stringBuilder.AppendLine("Fermented".Translate());
        }
        else {
          stringBuilder.AppendLine("FermentationProgress".Translate(new object[]
          {
            Progress.ToStringPercent(),
            EstimatedTicksLeft.ToStringTicksToPeriod(true, false, true)
          }));
          if (CurrentTempProgressSpeedFactor != 1f) {
            stringBuilder.AppendLine("FermentationBarrelOutOfIdealTemperature".Translate(new object[]
            {
              CurrentTempProgressSpeedFactor.ToStringPercent()
            }));
          }
        }
      }
      stringBuilder.AppendLine("Temperature".Translate() + ": " + AmbientTemperature.ToStringTemperature("F0"));
      stringBuilder.AppendLine(string.Concat(new string[]
      {
        "IdealFermentingTemperature".Translate(),
        ": ",
        7f.ToStringTemperature("F0"),
        " ~ ",
        comp.Props.maxSafeTemperature.ToStringTemperature("F0")
      }));
      return stringBuilder.ToString().TrimEndNewlines();
    }
  }
}
