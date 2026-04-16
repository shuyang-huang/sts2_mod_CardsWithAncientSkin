using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.IO;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace CardsWithAncientSkin;

[HarmonyPatch]
internal static class CiCoreRunnerValidationPatch
{
    private static MethodBase? TargetMethod()
    {
        return AccessTools.Method("RiderTestRunner.CiCoreRunner:AddModValidationJobs");
    }

    public static bool Prefix(object __instance)
    {
        var type = __instance.GetType();
        var jobModels = GetField<IList>(type, __instance, "_jobModels");
        var jobSuffixes = GetField<IList>(type, __instance, "_jobSuffixes");
        var jobPreviewModes = GetField<IList>(type, __instance, "_jobPreviewModes");
        var jobPortraitOverrides = GetField<IList>(type, __instance, "_jobPortraitOverrides");
        var jobOriginalBannerSources = GetField<IList>(type, __instance, "_jobOriginalBannerSources");
        var jobBorderOverrides = GetField<IList>(type, __instance, "_jobBorderOverrides");

        jobModels.Clear();
        jobSuffixes.Clear();
        jobPreviewModes.Clear();
        jobPortraitOverrides.Clear();
        jobOriginalBannerSources.Clear();
        jobBorderOverrides.Clear();

        var portraitIds = AncientSkinResources.GetAvailablePortraitIds();
        var summaryLines = new System.Collections.Generic.List<string>();
        var outputDir = Path.Combine(ProjectSettings.GlobalizePath("res://"), "CardsWithAncientSkin", "test_output");
        Directory.CreateDirectory(outputDir);

        foreach (var idEntry in AncientSkinConfig.GetEnabledCardIds())
        {
            if (!portraitIds.Contains(idEntry, StringComparer.OrdinalIgnoreCase))
            {
                summaryLines.Add($"{idEntry}: skipped (enabled in config but no portrait file)");
                continue;
            }

            var originalCard = ModelDb.AllCards.FirstOrDefault(card =>
                string.Equals(card.Id.Entry, idEntry, StringComparison.OrdinalIgnoreCase));
            if (originalCard == null)
            {
                Log.Warn("[CardsWithAncientSkin] Validation skipped missing card id: " + idEntry);
                summaryLines.Add($"{idEntry}: skipped (portrait exists but no card model found)");
                continue;
            }

            AddJob(jobModels, jobSuffixes, jobPreviewModes, jobPortraitOverrides, jobOriginalBannerSources, jobBorderOverrides,
                originalCard, "mod_base", CardPreviewMode.Normal);
            summaryLines.Add($"{idEntry}: render base");

            if (!originalCard.IsUpgradable)
            {
                summaryLines.Add($"{idEntry}: no upgraded render");
                continue;
            }

            var upgradedCard = originalCard.ToMutable();
            upgradedCard.UpgradeInternal();
            AddJob(jobModels, jobSuffixes, jobPreviewModes, jobPortraitOverrides, jobOriginalBannerSources, jobBorderOverrides,
                upgradedCard, "mod_upgraded", CardPreviewMode.Upgrade);
            summaryLines.Add($"{idEntry}: render upgraded");
        }

        File.WriteAllLines(Path.Combine(outputDir, "validator_summary.txt"), summaryLines);

        Log.Info("[CardsWithAncientSkin] Validation harness configured " + jobModels.Count + " jobs from resources + card_config.");
        return false;
    }

    private static void AddJob(
        IList jobModels,
        IList jobSuffixes,
        IList jobPreviewModes,
        IList jobPortraitOverrides,
        IList jobOriginalBannerSources,
        IList jobBorderOverrides,
        CardModel model,
        string suffix,
        CardPreviewMode previewMode)
    {
        jobModels.Add(model);
        jobSuffixes.Add(suffix);
        jobPreviewModes.Add(previewMode);
        jobPortraitOverrides.Add(null);
        jobOriginalBannerSources.Add(null);
        jobBorderOverrides.Add(null);
    }

    private static T GetField<T>(Type type, object instance, string fieldName) where T : class
    {
        var field = AccessTools.Field(type, fieldName)
            ?? throw new MissingFieldException(type.FullName, fieldName);
        return (field.GetValue(instance) as T)
            ?? throw new InvalidOperationException($"Field {fieldName} on {type.FullName} was not of expected type {typeof(T).FullName}.");
    }
}
