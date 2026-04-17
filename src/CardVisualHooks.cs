using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace CardsWithAncientSkin;

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class CardVisualHooks
{
    private static readonly HashSet<string> LoggedCards = new();

    public static void Postfix(NCard __instance, PileType pileType, CardPreviewMode previewMode)
    {
        var model = __instance.Model;
        if (model == null)
        {
            return;
        }

        var key = $"{model.Id}|{previewMode}|{model.CurrentUpgradeLevel}|{pileType}";
        if (LoggedCards.Add(key))
        {
            Log.Info(
                "[CardsWithAncientSkin] NCard.UpdateVisuals hit: " +
                $"id={model.Id}, title={model.Title}, rarity={model.Rarity}, upgrade={model.CurrentUpgradeLevel}, " +
                $"pile={pileType}, preview={previewMode}");
        }

        AncientSkinApplicator.ApplyToCard(__instance);
    }
}

[HarmonyPatch(typeof(NCard), "Reload")]
public static class CardReloadHooks
{
    public static void Postfix(NCard __instance)
    {
        AncientSkinApplicator.ApplyToCard(__instance);
        AncientSkinApplicator.LogAppliedCard(__instance);
    }
}

[HarmonyPatch(typeof(NCard), "ReloadOverlay")]
public static class CardReloadOverlayHooks
{
    public static void Postfix(NCard __instance)
    {
        AncientSkinApplicator.ApplyToCard(__instance);
    }
}
