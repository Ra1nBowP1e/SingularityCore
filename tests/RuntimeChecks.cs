using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using VoidMinerProject;

// This assembly is built only into an isolated temporary test mod, never distributed.
[StaticConstructorOnStartup]
public static class SingularityRuntimeChecks
{
    private static readonly List<string> Results = new List<string>();
    static SingularityRuntimeChecks() { LongEventHandler.ExecuteWhenFinished(Run); }

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Results.Add("PASS " + name);
    }

    private static void Run()
    {
        int exitCode = 0;
        try
        {
            var mod = LoadedModManager.GetMod<VoidMinerMod>();
            Check(mod != null, "standalone mod loaded");
            var defs = DefDatabase<ThingDef>.AllDefs.Cast<Def>()
                .Concat(DefDatabase<DamageDef>.AllDefs).Concat(DefDatabase<ResearchProjectDef>.AllDefs)
                .Concat(DefDatabase<RecipeDef>.AllDefs).Concat(DefDatabase<WorkGiverDef>.AllDefs)
                .Concat(DefDatabase<HediffDef>.AllDefs).Concat(DefDatabase<SoundDef>.AllDefs)
                .Concat(DefDatabase<FleckDef>.AllDefs).Concat(DefDatabase<EffecterDef>.AllDefs);
            var problems = defs.Where(d => d.modContentPack == mod.Content)
                .SelectMany(d => d.ConfigErrors().Select(error => d.defName + ": " + error)).ToList();
            Check(problems.Count == 0, "mod def configuration: " + string.Join("; ", problems));
            Check(typeof(CompAutoVoidMiner).Assembly.GetType("VoidMinerProject.DynamicRecipeManager") == null,
                "unused per-item recipe generator removed");
            Check(DefDatabase<DamageDef>.GetNamed("SC_KineticImpact").workerClass == typeof(DamageWorker_AddInjury),
                "kinetic damage uses the vanilla worker");
            Check(DefDatabase<WorkGiverDef>.GetNamed("SC_OperateAtomicManipulator").fixedBillGiverDefs.Single().defName == "SC_Workbench",
                "atomic manipulator work assigned to the correct building");
            Check(DefDatabase<ThingDef>.GetNamed("HiTechResearchBench").GetCompProperties<CompProperties_AffectedByFacilities>()
                .linkableFacilities.Contains(DefDatabase<ThingDef>.GetNamed("EccentricAuroraCore")),
                "Aurora is linked to the research bench");

            Current.Game = new Game();
            var building = (Building_AutoVoidMiner)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("SC_AutoVoidMiner"));
            var comp = building.GetComp<CompAutoVoidMiner>();
            Check(comp != null, "Void Miner component created by the game");
            comp.SetProductionCount(-4);
            Check(comp.ProductionCount == 1, "production count cannot be negative");
            comp.SetProductionCount(int.MaxValue);
            comp.IncreaseProductionCount();
            Check(comp.ProductionCount == int.MaxValue, "production count cannot overflow");
            comp.SetProductionTarget(ThingDefOf.Steel);
            Check(comp.GetCurrentState() == CompAutoVoidMiner.ProductionState.Paused, "unpowered selection waits for power");
            comp.ResumeProduction();
            Check(comp.GetCurrentState() == CompAutoVoidMiner.ProductionState.Producing, "resume starts the selected product");
            comp.CompTick();
            Check(comp.GetCurrentState() == CompAutoVoidMiner.ProductionState.Paused, "loss of power pauses production");
            comp.PauseProduction();
            comp.SetProductionMode(CompAutoVoidMiner.ProductionMode.Maintain);
            typeof(CompAutoVoidMiner).GetMethod("CheckMaintainQuantityMode", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(comp, null);
            Check(comp.GetCurrentState() == CompAutoVoidMiner.ProductionState.Paused, "maintain mode respects a manual pause");
            comp.StopProduction();
            Check(comp.GetCurrentProductionTarget() == null && comp.GetCurrentState() == CompAutoVoidMiner.ProductionState.Stopped,
                "stop clears the old target");

            var settings = VoidMinerSettings.Instance;
            Check(settings.ResolveProduct("def_that_does_not_exist") == null, "missing saved targets do not become random items");
            var initial = settings.GetAllowedProductDefs();
            settings.AddAllowedProduct("SC_Mundite");
            Check(settings.GetAllowedProductDefs().Any(d => d.defName == "SC_Mundite"), "allowed item cache updates on addition");
            settings.RemoveAllowedProduct("SC_Mundite");
            Check(!settings.GetAllowedProductDefs().Any(d => d.defName == "SC_Mundite"), "allowed item cache updates on removal");
            var gene = DefDatabase<GeneDef>.AllDefs.First();
            var geneTarget = settings.ResolveProduct("genepack_" + gene.defName);
            Check(geneTarget != null, "saved single-gene targets resolve after startup");
            settings.AddAllowedProduct(geneTarget.defName);
            comp.SetProductionTarget(geneTarget);
            comp.ResumeProduction();
            typeof(CompAutoVoidMiner).GetField("currentWorkAmount", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(comp, 500f);
            typeof(CompAutoVoidMiner).GetField("productionTicks", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(comp, 125);
            comp.PauseProduction();
            string saveFile = Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("SINGULARITY_TEST_REPORT")), "component.xml");
            Scribe.saver.InitSaving(saveFile, "ComponentTest");
            comp.PostExposeData();
            Scribe.saver.FinalizeSaving();
            var reloadedBuilding = (Building_AutoVoidMiner)ThingMaker.MakeThing(building.def);
            var reloaded = reloadedBuilding.GetComp<CompAutoVoidMiner>();
            Scribe.loader.InitLoading(saveFile);
            reloaded.PostExposeData();
            Scribe.loader.FinalizeLoading();
            Scribe.mode = LoadSaveMode.PostLoadInit;
            reloaded.PostExposeData();
            Scribe.mode = LoadSaveMode.Inactive;
            Check(reloaded.GetCurrentProductionTarget()?.defName == geneTarget.defName, "single-gene production target survives save/load");
            Check(reloaded.GetRemainingProductionTicks() == 375 && Math.Abs(reloaded.GetProductionProgress() - 0.25f) < 0.001f,
                "production duration and progress survive save/load");
            typeof(CompAutoVoidMiner).GetMethod("CheckMaintainQuantityMode", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(reloaded, null);
            Check(reloaded.GetCurrentState() == CompAutoVoidMiner.ProductionState.Paused, "manual pause survives save/load");
            settings.RemoveAllowedProduct(geneTarget.defName);
            settings.InitializeAfterDefsLoaded();
            Check(!settings.IsProductAllowed(geneTarget), "removed genes are not silently added back");
            Check("SC_SelectProductionTarget".Translate().ToString() != "SC_SelectProductionTarget", "interface translations loaded");
            bool russian = Environment.GetEnvironmentVariable("SINGULARITY_TEST_LANGUAGE") == "Russian";
            Check(building.def.label == (russian ? "автоматический пустотный экстрактор" : "automatic void miner"), "building label localized");
            Check(DefDatabase<ThingDef>.GetNamed("TE_KineticRifle").label == (russian ? "кинетическая винтовка" : "kinetic rifle"), "weapon label localized");
            Check(DefDatabase<ThingDef>.GetNamed("Apparel_ReflactorArmor").label == (russian ? "рефлекторная броня" : "reflector carapace"), "armor label localized");
            Check(DefDatabase<ThingDef>.GetNamed("SC_DarkMatterAlloy").stuffProps.stuffAdjective == (russian ? "сплава тёмной материи" : "dark matter alloy"), "material adjective localized");
            Check(DefDatabase<HediffDef>.GetNamed("dvd_Vacshock").stages[0].label == (russian ? "лёгкий" : "minor"), "named hediff stage localized");
            Check(DefDatabase<ThingDef>.GetNamed("Turret_SuvTurret").GetCompProperties<CompProperties_Refuelable>().fuelLabel == (russian ? "Выстрелов до замены ствола" : "Shots until barrel change"), "named turret comp localized");
            Check(DefDatabase<RecipeDef>.GetNamed("Make_Steel_From_Chunks").jobString == (russian ? "Переработка каменных глыб в ресурсы." : "Converting stone chunks to resources."), "inherited recipe job string localized");
            Check(DefDatabase<ResearchProjectDef>.GetNamed("Eccentric_Core").label == (russian ? "ядро Авроры" : "aurora core"), "research label localized");
            Check(DefDatabase<DamageDef>.GetNamed("SC_KineticImpact").deathMessage == (russian ? "Кинетический удар раздавил {0}." : "{0} was crushed by a kinetic impact."), "damage death message localized");
            var generatedRecipe = DefDatabase<RecipeDef>.GetNamed("Make_TE_KineticRifle");
            Check(generatedRecipe.label.Contains(russian ? "кинетическ" : "kinetic"), "generated crafting recipe follows the localized item");
            Check(EccentricTools.CoreUtility.Aurora.Name.ToString().Contains(russian ? "Аврора" : "Aurora"), "Aurora pilot name localized");
            var language = LanguageDatabase.activeLanguage;
            var translationErrors = language.loadErrors.Concat(language.defInjections.SelectMany(package => package.loadErrors)).ToList();
            File.WriteAllLines(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("SINGULARITY_TEST_REPORT")), "translation-errors.txt"), translationErrors);
            var ownInjections = language.defInjections.SelectMany(package => package.injections.Values)
                .Where(injection => injection.fileSource == "SingularityCore.xml").ToList();
            Check(ownInjections.Count == 115 && ownInjections.All(injection => injection.injected),
                "all 115 mod def translations applied: " + string.Join("; ", ownInjections.Where(injection => !injection.injected).Select(injection => injection.path)));
        }
        catch (Exception error)
        {
            exitCode = 1;
            Results.Add("FAIL " + error);
        }
        File.WriteAllLines(Environment.GetEnvironmentVariable("SINGULARITY_TEST_REPORT"), Results);
        Application.Quit(exitCode);
    }
}
