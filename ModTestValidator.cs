using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace CardsWithAncientSkin;

internal sealed class ModTestValidator : Node
{
    private const int OutputWidth = 768;
    private const int OutputHeight = 1024;
    private const string ValidatorArg = "--cardswithancientskin-validate";

    private readonly List<CardModel> _jobModels = new();
    private readonly List<string> _jobSuffixes = new();
    private readonly List<CardPreviewMode> _jobPreviewModes = new();
    private readonly List<string> _renderedFiles = new();
    private readonly List<string> _summaryLines = new();

    private SubViewport? _viewport;
    private Control? _root;
    private PackedScene? _cardScene;
    private NCard? _card;
    private int _jobIndex = -1;
    private int _frames;
    private string _outputDir = "";
    private string _outputPath = "";
    private bool _started;

    public static bool ShouldRun()
    {
        return OS.GetCmdlineUserArgs().Any(arg =>
            string.Equals(arg, ValidatorArg, StringComparison.OrdinalIgnoreCase));
    }

    public override void _Ready()
    {
        StartValidator();
    }

    public void StartValidator()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        var workspaceRoot = ProjectSettings.GlobalizePath("res://");
        _outputDir = Path.Combine(workspaceRoot, "CardsWithAncientSkin", "test_output");
        Directory.CreateDirectory(_outputDir);
        ClearPreviousOutputs();
        BuildJobs(workspaceRoot);

        Log.Info("[CardsWithAncientSkin] ModTestValidator ready. Jobs=" + _jobModels.Count);

        _viewport = new SubViewport
        {
            Size = new Vector2I(OutputWidth, OutputHeight),
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        AddChild(_viewport);

        _root = new Control
        {
            Size = new Vector2(OutputWidth, OutputHeight)
        };
        _viewport.AddChild(_root);

        _cardScene = GD.Load<PackedScene>("res://scenes/cards/card.tscn");
        SetProcess(true);
        RenderNextCard();
    }

    public override void _Process(double delta)
    {
        _frames++;
        if (_frames == 2 && _card != null)
        {
            _card.UpdateVisuals(PileType.Deck, _jobPreviewModes[_jobIndex]);
        }

        if (_frames < 8 || _viewport == null || _card == null)
        {
            return;
        }

        var image = _viewport.GetTexture().GetImage();
        var error = image.SavePng(_outputPath);
        Log.Info("[CardsWithAncientSkin] ModTestValidator saved " + _outputPath + " error=" + error);
        _renderedFiles.Add(_outputPath);
        RenderNextCard();
    }

    private void BuildJobs(string workspaceRoot)
    {
        var portraitDir = Path.Combine(workspaceRoot, "CardsWithAncientSkin", "resources", "mod_card_portraits_ancient_form");
        var portraitIds = Directory.Exists(portraitDir)
            ? Directory.GetFiles(portraitDir, "*.png")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!.ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var idEntry in AncientSkinConfig.GetEnabledCardIds())
        {
            var original = ModelDb.AllCards.FirstOrDefault(card =>
                string.Equals(card.Id.Entry, idEntry, StringComparison.OrdinalIgnoreCase));
            if (original == null)
            {
                _summaryLines.Add($"{idEntry}: missing card model");
                continue;
            }

            var hasPortrait = portraitIds.Contains(idEntry);
            _summaryLines.Add($"{idEntry}: portrait={(hasPortrait ? "custom" : "fallback")}, upgraded={original.IsUpgradable}");

            _jobModels.Add(original);
            _jobSuffixes.Add("mod_base_godot_render");
            _jobPreviewModes.Add(CardPreviewMode.Normal);

            if (!original.IsUpgradable)
            {
                continue;
            }

            var upgraded = original.ToMutable();
            upgraded.UpgradeInternal();
            _jobModels.Add(upgraded);
            _jobSuffixes.Add("mod_upgraded_godot_render");
            _jobPreviewModes.Add(CardPreviewMode.Upgrade);
        }
    }

    private void ClearPreviousOutputs()
    {
        foreach (var pattern in new[] { "*.png", "*.txt", "*.log" })
        {
            foreach (var file in Directory.GetFiles(_outputDir, pattern))
            {
                File.Delete(file);
            }
        }
    }

    private void RenderNextCard()
    {
        _jobIndex++;
        _frames = 0;

        if (_jobIndex >= _jobModels.Count)
        {
            var manifestPath = Path.Combine(_outputDir, "validator_manifest.txt");
            var summaryPath = Path.Combine(_outputDir, "validator_summary.txt");
            File.WriteAllLines(manifestPath, _renderedFiles);
            File.WriteAllLines(summaryPath, _summaryLines);
            Log.Info("[CardsWithAncientSkin] ModTestValidator complete. rendered=" + _renderedFiles.Count);
            SetProcess(false);
            GetTree().Quit();
            return;
        }

        if (_root == null || _cardScene == null)
        {
            Log.Error("[CardsWithAncientSkin] ModTestValidator root or card scene missing.");
            GetTree().Quit(1);
            return;
        }

        if (_card != null)
        {
            _card.QueueFree();
            _card = null;
        }

        var model = _jobModels[_jobIndex];
        var suffix = _jobSuffixes[_jobIndex];
        _outputPath = Path.Combine(_outputDir, model.Id.Entry.ToLowerInvariant() + "_" + suffix + ".png");

        _card = _cardScene.Instantiate<NCard>();
        _card.Position = new Vector2(OutputWidth / 2f, OutputHeight / 2f);
        _card.Scale = new Vector2(2f, 2f);
        _root.AddChild(_card);
        _card.Model = model;

        Log.Info("[CardsWithAncientSkin] ModTestValidator rendering " + model.Id + " -> " + _outputPath);
    }
}

[HarmonyPatch]
internal static class CiCoreRunnerValidatorRedirectPatch
{
    private static MethodBase? TargetMethod()
    {
        var targetType = AccessTools.TypeByName("RiderTestRunner.CiCoreRunner");
        return targetType == null ? null : AccessTools.Method(targetType, "_Ready");
    }

    public static bool Prefix(Node __instance)
    {
        if (!ModTestValidator.ShouldRun())
        {
            return true;
        }

        Log.Info("[CardsWithAncientSkin] Redirecting CiCoreRunner to ModTestValidator.");
        ModTestValidatorBootstrap.TryStart(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(NOneTimeInitialization), nameof(NOneTimeInitialization._Ready))]
internal static class ModTestValidatorBootstrap
{
    private static bool _started;

    public static void Postfix(NOneTimeInitialization __instance)
    {
        TryStart(__instance);
    }

    public static void TryStart(Node host)
    {
        if (_started || !ModTestValidator.ShouldRun())
        {
            return;
        }

        if (host == null)
        {
            Log.Error("[CardsWithAncientSkin] ModTestValidator bootstrap failed: host missing.");
            return;
        }

        _started = true;
        Log.Info("[CardsWithAncientSkin] ModTestValidator bootstrap starting.");
        var validator = new ModTestValidator();
        host.AddChild(validator);
        validator.StartValidator();
    }
}
