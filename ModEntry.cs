using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace CardsWithAncientSkin;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
    private const string HarmonyId = "shuya.CardsWithAncientSkin";
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;

        try
        {
            Log.Info("[CardsWithAncientSkin] Initializer reached.");
            AncientSkinConfig.Load();

            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(ModEntry).Assembly);

            Log.Info("[CardsWithAncientSkin] Harmony patches applied.");
            Log.Info("[CardsWithAncientSkin] Mod loaded.");
        }
        catch (Exception ex)
        {
            Log.Error("[CardsWithAncientSkin] Failed during initialization:\n" + ex);
            throw;
        }
    }
}
