using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;

namespace CardsWithAncientSkin;

internal static class AncientSkinConfig
{
    private static readonly string ModRoot =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new InvalidOperationException("Could not resolve mod root.");

    private static readonly string ConfigPath = Path.Combine(ModRoot, "card_config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static Dictionary<string, bool>? _cardFlags;

    public static void Load()
    {
        _cardFlags = LoadInternal();
    }

    public static bool ShouldApply(string cardId)
    {
        _cardFlags ??= LoadInternal();
        return _cardFlags.TryGetValue(cardId.ToLowerInvariant(), out var enabled) && enabled;
    }

    private static Dictionary<string, bool> LoadInternal()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                Log.Warn("[CardsWithAncientSkin] Config file not found, using default visuals: " + ConfigPath);
                return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            }

            var json = File.ReadAllText(ConfigPath);
            var root = JsonSerializer.Deserialize<AncientSkinConfigRoot>(json, JsonOptions);
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            if (root?.Cards == null)
            {
                return result;
            }

            foreach (var pair in root.Cards)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key))
                {
                    result[pair.Key.Trim().ToLowerInvariant()] = pair.Value;
                }
            }

            Log.Info("[CardsWithAncientSkin] Loaded config for " + result.Count + " cards.");
            return result;
        }
        catch (Exception ex)
        {
            Log.Error("[CardsWithAncientSkin] Failed to load config, using default visuals:\n" + ex);
            return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private sealed class AncientSkinConfigRoot
    {
        public Dictionary<string, bool>? Cards { get; set; }
    }
}
