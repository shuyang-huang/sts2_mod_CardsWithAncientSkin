using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Assets;

namespace CardsWithAncientSkin;

internal static class AncientSkinResources
{
    private const int AncientPortraitWidth = 250;
    private const int AncientPortraitHeight = 351;

    private static readonly string ModRoot =
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new InvalidOperationException("Could not resolve mod root.");

    private static readonly string ResourceRoot = Path.Combine(ModRoot, "resources");
    private static readonly string BorderRoot = Path.Combine(ResourceRoot, "mod_borders");
    private static readonly string PortraitRoot = Path.Combine(ResourceRoot, "mod_card_portraits_ancient_form");

    private static readonly Dictionary<string, Texture2D> FileTextureCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Texture2D> GodotTextureCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Material> MaterialCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> MissingFileWarnings = new(StringComparer.OrdinalIgnoreCase);

    public static bool ShouldApply(CardModel model)
    {
        return model.Rarity != CardRarity.Ancient
            && AncientSkinConfig.ShouldApply(model.Id.Entry);
    }

    public static Texture2D? GetPortraitTexture(CardModel model)
    {
        var overridePath = Path.Combine(PortraitRoot, model.Id.Entry.ToLowerInvariant() + ".png");
        return File.Exists(overridePath)
            ? LoadTextureFromFile(overridePath, normalizeToAncientPortrait: true)
            : model.Portrait;
    }

    public static Texture2D? GetBorderTexture(CardModel model)
    {
        var borderName = GetBorderFileName(model);
        var borderPath = Path.Combine(BorderRoot, borderName);
        return LoadTextureFromFile(borderPath);
    }

    public static Texture2D? GetAncientTextBackground(CardModel model)
    {
        var cardType = GetAncientVisualCardType(model.Type).ToString().ToLowerInvariant();
        var resourcePath = ImageHelper.GetImagePath($"atlases/compressed.sprites/card_template/ancient_card_text_bg_{cardType}.tres");
        return LoadTextureFromGodot(resourcePath);
    }

    public static Material? GetAncientMaskMaterial()
    {
        return LoadMaterial("res://scenes/cards/card_canvas_group_mask_material.tres");
    }

    public static Material? GetAncientBlurMaskMaterial()
    {
        return LoadMaterial("res://scenes/cards/card_canvas_group_mask_blur_material.tres");
    }

    private static string GetBorderFileName(CardModel model)
    {
        if (model.Rarity == CardRarity.Curse)
        {
            return "Curse_card_border_source.png";
        }

        if (model.Rarity == CardRarity.Status)
        {
            return "Status_card_border_source.png";
        }

        return model.Pool.GetType().Name switch
        {
            "IroncladCardPool" => "Ironcald_card_border_source.png",
            "SilentCardPool" => "Silent_card_border_source.png",
            "DefectCardPool" => "Defeat_card_border_source.png",
            "RegentCardPool" => "Regent_card_border_source.png",
            "NecrobinderCardPool" => "Necrobinder_card_border_source.png",
            _ => "ancient_card_border_source.png"
        };
    }

    private static CardType GetAncientVisualCardType(CardType type) => type switch
    {
        CardType.None => CardType.Skill,
        CardType.Status => CardType.Skill,
        CardType.Curse => CardType.Skill,
        CardType.Attack => CardType.Attack,
        CardType.Skill => CardType.Skill,
        CardType.Power => CardType.Power,
        CardType.Quest => CardType.Quest,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private static Texture2D? LoadTextureFromFile(string path, bool normalizeToAncientPortrait = false)
    {
        var cacheKey = normalizeToAncientPortrait ? path + "|ancient-normalized" : path;
        if (FileTextureCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        if (!File.Exists(path))
        {
            WarnMissingFile(path);
            return null;
        }

        var image = Image.LoadFromFile(path);
        if (image == null)
        {
            WarnMissingFile(path);
            return null;
        }

        if (normalizeToAncientPortrait)
        {
            image.Resize(AncientPortraitWidth, AncientPortraitHeight, Image.Interpolation.Lanczos);
        }

        var texture = ImageTexture.CreateFromImage(image);
        FileTextureCache[cacheKey] = texture;
        return texture;
    }

    private static Texture2D? LoadTextureFromGodot(string path)
    {
        if (GodotTextureCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var texture = ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
        if (texture == null)
        {
            Log.Warn("[CardsWithAncientSkin] Missing Godot texture resource: " + path);
            return null;
        }

        GodotTextureCache[path] = texture;
        return texture;
    }

    private static void WarnMissingFile(string path)
    {
        if (MissingFileWarnings.Add(path))
        {
            Log.Warn("[CardsWithAncientSkin] Missing file resource: " + path);
        }
    }

    private static Material? LoadMaterial(string path)
    {
        if (MaterialCache.TryGetValue(path, out var cached))
        {
            return cached;
        }

        var material = PreloadManager.Cache.GetMaterial(path);
        if (material == null)
        {
            Log.Warn("[CardsWithAncientSkin] Missing material resource: " + path);
            return null;
        }

        MaterialCache[path] = material;
        return material;
    }
}
