using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace CardsWithAncientSkin;

internal static class AncientSkinApplicator
{
    public static void ApplyToCard(NCard card)
    {
        var model = card.Model;
        if (model == null || !AncientSkinResources.ShouldApply(model))
        {
            return;
        }

        var portrait = card.GetNode<TextureRect>("%Portrait");
        var ancientPortrait = card.GetNode<TextureRect>("%AncientPortrait");
        var frame = card.GetNode<TextureRect>("%Frame");
        var ancientBorder = card.GetNode<TextureRect>("%AncientBorder");
        var ancientTextBg = card.GetNode<TextureRect>("%AncientTextBg");
        var ancientBanner = card.GetNode<Control>("%AncientBanner");
        var ancientHighlight = card.GetNode<TextureRect>("%AncientHighlight");
        var portraitBorder = card.GetNode<TextureRect>("%PortraitBorder");
        var titleBanner = card.GetNode<TextureRect>("%TitleBanner");
        var portraitCanvasGroup = card.GetNode<CanvasGroup>("%PortraitCanvasGroup");

        portrait.Visible = false;
        frame.Visible = false;
        portraitBorder.Visible = false;

        ancientPortrait.Visible = true;
        ancientBorder.Visible = true;
        ancientTextBg.Visible = true;
        ancientHighlight.Visible = true;

        ancientBanner.Visible = false;
        titleBanner.Visible = true;
        titleBanner.Texture = model.BannerTexture;
        titleBanner.Material = model.BannerMaterial;

        // This is the missing step from the native Ancient path:
        // without the canvas-group mask, the portrait won't be clipped to the rounded card shape.
        if (card.Visibility == MegaCrit.Sts2.Core.Entities.UI.ModelVisibility.Visible)
        {
            var maskMaterial = AncientSkinResources.GetAncientMaskMaterial();
            if (maskMaterial != null)
            {
                portraitCanvasGroup.Material = maskMaterial;
            }
        }
        else
        {
            var blurMaskMaterial = AncientSkinResources.GetAncientBlurMaskMaterial();
            if (blurMaskMaterial != null)
            {
                portraitCanvasGroup.Material = blurMaskMaterial;
            }
        }

        var portraitTexture = AncientSkinResources.GetPortraitTexture(model);
        if (portraitTexture != null)
        {
            ancientPortrait.Texture = portraitTexture;
        }

        var borderTexture = AncientSkinResources.GetBorderTexture(model);
        if (borderTexture != null)
        {
            ancientBorder.Texture = borderTexture;
        }

        var textBgTexture = AncientSkinResources.GetAncientTextBackground(model);
        if (textBgTexture != null)
        {
            ancientTextBg.Texture = textBgTexture;
        }
    }

    public static void LogAppliedCard(NCard card)
    {
        var model = card.Model;
        if (model == null || !AncientSkinResources.ShouldApply(model))
        {
            return;
        }

        Log.Info(
            "[CardsWithAncientSkin] Applied ancient skin: " +
            $"id={model.Id}, title={model.Title}, rarity={model.Rarity}, upgrade={model.CurrentUpgradeLevel}");
    }
}
