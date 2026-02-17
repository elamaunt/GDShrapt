using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GDShrapt.Abstractions;

namespace GDShrapt.CLI.Core;

/// <summary>
/// Human-readable text output formatter.
/// </summary>
public class GDTextFormatter : IGDOutputFormatter
{
    public string FormatName => "text";

    public void WriteAnalysisResult(TextWriter output, GDAnalysisResult result)
    {
        output.WriteLine($"Analysis of: {result.ProjectPath}");
        output.WriteLine($"Total files: {result.TotalFiles}");
        output.WriteLine($"Files with errors: {result.FilesWithErrors}");
        output.WriteLine($"Total errors: {result.TotalErrors}");
        output.WriteLine($"Total warnings: {result.TotalWarnings}");
        output.WriteLine($"Total hints: {result.TotalHints}");
        output.WriteLine();

        switch (result.GroupBy)
        {
            case GDGroupBy.Rule:
                WriteGroupedByRule(output, result);
                break;
            case GDGroupBy.Severity:
                WriteGroupedBySeverity(output, result);
                break;
            case GDGroupBy.File:
            default:
                WriteGroupedByFile(output, result);
                break;
        }
    }

    private void WriteGroupedByFile(TextWriter output, GDAnalysisResult result)
    {
        foreach (var file in result.Files)
        {
            if (file.Diagnostics.Count == 0)
                continue;

            output.WriteLine($"--- {GDAnsiColors.Bold(file.FilePath)} ---");

            foreach (var diag in file.Diagnostics)
            {
                var severity = FormatSeverity(diag.Severity);
                var code = GDAnsiColors.Dim(diag.Code);
                output.WriteLine($"  {file.FilePath}:{diag.Line}:{diag.Column}: {severity} {code}: {diag.Message}");
            }

            output.WriteLine();
        }
    }

    private void WriteGroupedByRule(TextWriter output, GDAnalysisResult result)
    {
        var allDiagnostics = result.Files
            .SelectMany(f => f.Diagnostics.Select(d => (File: f.FilePath, Diag: d)))
            .GroupBy(x => x.Diag.Code)
            .OrderBy(g => g.Key);

        foreach (var group in allDiagnostics)
        {
            var count = group.Count();
            output.WriteLine($"--- {group.Key} ({count} occurrence{(count == 1 ? "" : "s")}) ---");

            foreach (var (filePath, diag) in group.OrderBy(x => x.File).ThenBy(x => x.Diag.Line))
            {
                var severity = FormatSeverity(diag.Severity);
                output.WriteLine($"  {filePath}:{diag.Line}:{diag.Column}: {severity} {diag.Message}");
            }

            output.WriteLine();
        }
    }

    private void WriteGroupedBySeverity(TextWriter output, GDAnalysisResult result)
    {
        var allDiagnostics = result.Files
            .SelectMany(f => f.Diagnostics.Select(d => (File: f.FilePath, Diag: d)))
            .GroupBy(x => x.Diag.Severity)
            .OrderBy(g => g.Key); // Error (0) first, then Warning, Info, Hint

        foreach (var group in allDiagnostics)
        {
            var severityName = group.Key switch
            {
                GDSeverity.Error => "Errors",
                GDSeverity.Warning => "Warnings",
                GDSeverity.Information => "Information",
                GDSeverity.Hint => "Hints",
                _ => "Unknown"
            };
            var count = group.Count();
            output.WriteLine($"--- {severityName} ({count}) ---");

            foreach (var (filePath, diag) in group.OrderBy(x => x.File).ThenBy(x => x.Diag.Line))
            {
                output.WriteLine($"  {filePath}:{diag.Line}:{diag.Column}: {diag.Code}: {diag.Message}");
            }

            output.WriteLine();
        }
    }

    private static string FormatSeverity(GDSeverity severity) => severity switch
    {
        GDSeverity.Error => GDAnsiColors.Red("error"),
        GDSeverity.Warning => GDAnsiColors.Yellow("warning"),
        GDSeverity.Information => GDAnsiColors.Cyan("info"),
        GDSeverity.Hint => GDAnsiColors.Blue("hint"),
        _ => "unknown"
    };

    public void WriteSymbols(TextWriter output, IEnumerable<GDSymbolInfo> symbols)
    {
        foreach (var symbol in symbols)
        {
            var type = string.IsNullOrEmpty(symbol.Type) ? "" : $" : {symbol.Type}";
            var container = string.IsNullOrEmpty(symbol.ContainerName) ? "" : $" in {symbol.ContainerName}";
            output.WriteLine($"  {symbol.Kind,-12} {symbol.Name}{type} at line {symbol.Line}{container}");
        }
    }

    public void WriteReferences(TextWriter output, IEnumerable<GDReferenceInfo> references)
    {
        var grouped = references
            .GroupBy(r => r.FilePath)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            output.WriteLine($"  --- {GDAnsiColors.Bold(group.Key)} ---");

            foreach (var reference in group.OrderBy(r => r.Line).ThenBy(r => r.Column))
            {
                WriteReferenceLine(output, reference, "  ");
            }
        }
    }

    public void WriteReferenceGroups(TextWriter output, IEnumerable<GDReferenceGroupInfo> groups)
    {
        var groupList = groups.ToList();
        var regularGroups = groupList.Where(g => !g.IsCrossFile && !g.IsSignalConnection).ToList();
        var crossFileGroups = groupList.Where(g => g.IsCrossFile).ToList();
        var signalGroups = groupList.Where(g => g.IsSignalConnection).ToList();

        foreach (var group in regularGroups)
        {
            if (!HasNonContractReferences(group))
                continue;

            WriteGroupTree(output, group, indent: "");
            output.WriteLine();
        }

        WriteDuckTypedSection(output, crossFileGroups);
        WriteContractStringsSection(output, crossFileGroups, regularGroups);
        WriteSignalConnectionsSection(output, signalGroups);
    }

    public void WriteFindRefsResult(TextWriter output, GDFindRefsResultInfo result)
    {
        var allPrimary = result.PrimaryGroups;

        // 1. Symbol header
        output.WriteLine($"Symbol: {GDAnsiColors.Bold(result.SymbolName)}");
        output.WriteLine($"Kind: {result.SymbolKind}");
        if (!string.IsNullOrEmpty(result.DeclaredInClassName))
        {
            var declPath = !string.IsNullOrEmpty(result.DeclaredInFilePath)
                ? $"{result.DeclaredInFilePath}:{result.DeclaredAtLine}"
                : "";
            output.WriteLine($"Declared in: {result.DeclaredInClassName} ({declPath})");
        }
        output.WriteLine();

        var regularGroups = allPrimary.Where(g => !g.IsCrossFile && !g.IsSignalConnection).ToList();
        var crossFileGroups = allPrimary.Where(g => g.IsCrossFile).ToList();
        var signalGroups = allPrimary.Where(g => g.IsSignalConnection).ToList();

        // 2. Root group (primary declaration + calls in same class)
        var rootGroups = SortGroups(regularGroups.Where(g => !g.IsOverride && !g.IsInherited));
        foreach (var group in rootGroups)
        {
            if (!HasNonContractReferences(group))
                continue;

            WriteGroupHeader(output, group, "");
            foreach (var reference in group.References.Where(r => !r.IsContractString).OrderBy(r => r.Line).ThenBy(r => r.Column))
            {
                WriteNewReferenceLine(output, reference, "  ", group.IsInherited, group.SymbolName);
            }
        }

        // 3. Overrides section
        var allOverrides = rootGroups.SelectMany(g => g.Overrides).ToList();
        var dependentGroups = regularGroups.Where(g => g.IsOverride || g.IsInherited).ToList();
        allOverrides.AddRange(dependentGroups);
        allOverrides = SortGroups(allOverrides);

        if (allOverrides.Count > 0)
        {
            output.WriteLine();
            output.WriteLine(GDAnsiColors.Bold($"Overrides ({allOverrides.Count}):"));
            for (int i = 0; i < allOverrides.Count; i++)
            {
                var isLast = i == allOverrides.Count - 1;
                var branch = isLast ? "\u2514\u2500\u2500 " : "\u251c\u2500\u2500 ";
                var childIndent = isLast ? "    " : "\u2502   ";

                var ovr = allOverrides[i];
                var ovrHeader = !string.IsNullOrEmpty(ovr.ClassName)
                    ? GDAnsiColors.Bold(ovr.ClassName)
                    : GDAnsiColors.Bold(ovr.DeclarationFilePath);

                output.WriteLine($"{branch}{ovrHeader} ({ovr.DeclarationFilePath}:{ovr.DeclarationLine})");

                foreach (var reference in ovr.References.Where(r => !r.IsContractString).OrderBy(r => r.Line).ThenBy(r => r.Column))
                {
                    WriteNewReferenceLine(output, reference, childIndent, ovr.IsInherited, ovr.SymbolName);
                }

                if (ovr.Overrides.Count > 0)
                {
                    foreach (var nested in ovr.Overrides)
                    {
                        WriteGroupTree(output, nested, childIndent);
                    }
                }
            }
        }

        // 5. Cross-file references
        WriteDuckTypedSection(output, crossFileGroups);

        // 6. Signal connections (split Code/Scene)
        WriteSignalConnectionsSplit(output, signalGroups);

        // 7. Contract strings
        WriteContractStringsSection(output, crossFileGroups, regularGroups);

        // 8. Unrelated symbols with same name
        var sortedUnrelated = SortGroups(result.UnrelatedGroups);
        if (sortedUnrelated.Count > 0)
        {
            output.WriteLine();
            output.WriteLine(GDAnsiColors.Yellow($"Unrelated symbols with same name ({sortedUnrelated.Count}):"));
            output.WriteLine(GDAnsiColors.Dim("  (independent type hierarchy, not affected by rename)"));
            foreach (var group in sortedUnrelated)
            {
                WriteGroupTree(output, group, "  ");
            }
        }

        // 9. Summary block
        WriteSummary(output, result);
    }

    private static List<GDReferenceGroupInfo> SortGroups(IEnumerable<GDReferenceGroupInfo> groups)
        => groups.OrderBy(g => g.DeclarationFilePath, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(g => g.ClassName, StringComparer.OrdinalIgnoreCase)
                 .ToList();

    private static void WriteGroupHeader(TextWriter output, GDReferenceGroupInfo group, string indent)
    {
        var header = !string.IsNullOrEmpty(group.ClassName)
            ? GDAnsiColors.Bold(group.ClassName)
            : GDAnsiColors.Bold(group.DeclarationFilePath);

        if (group.IsInherited)
            output.WriteLine($"{indent}{header} ({group.DeclarationFilePath})");
        else
            output.WriteLine($"{indent}{header} ({group.DeclarationFilePath}:{group.DeclarationLine})");
    }

    private static void WriteNewReferenceLine(TextWriter output, GDReferenceInfo reference,
        string indent = "", bool isInherited = false, string? symbolName = null)
    {
        string marker;
        if (reference.Confidence.HasValue)
        {
            var conf = reference.Confidence.Value.ToString().ToLowerInvariant();
            marker = reference.IsContractString ? $"[contract-{conf}]" : $"[{conf}]";
        }
        else if (reference.IsOverride)
            marker = "[override]";
        else if (isInherited)
            marker = reference.IsWrite ? "[write]" : "[call]";
        else if (reference.IsDeclaration)
            marker = "[def]";
        else if (reference.IsSuperCall)
            marker = "[call-super]";
        else if (reference.IsWrite)
            marker = "[write]";
        else
            marker = "[call]";

        var reason = !string.IsNullOrEmpty(reference.Reason) ? $" {reference.Reason}" : "";
        output.WriteLine($"{indent}{FormatPosition(reference)} {marker}{reason}");

        if (!string.IsNullOrEmpty(reference.Context))
        {
            var contextLine = reference.Context.TrimStart();
            if (!string.IsNullOrEmpty(symbolName))
                contextLine = HighlightSymbol(contextLine, symbolName);
            output.WriteLine($"{indent}  {GDAnsiColors.Dim(contextLine)}");
        }
    }

    private static void WriteDuckTypedSection(TextWriter output, List<GDReferenceGroupInfo> crossFileGroups)
    {
        var crossFileRefs = crossFileGroups
            .SelectMany(g => g.References.Where(r => !r.IsContractString).Select(r => (Group: g, Ref: r)))
            .ToList();

        if (crossFileRefs.Count > 0)
        {
            output.WriteLine();
            output.WriteLine(GDAnsiColors.Yellow($"Potential references (duck-typed) ({crossFileRefs.Count}):"));

            var confidenceLevels = crossFileRefs
                .Select(x => x.Ref.Confidence?.ToString().ToLowerInvariant() ?? "potential")
                .Distinct()
                .ToList();

            if (confidenceLevels.Count > 1)
            {
                var byConfidence = crossFileRefs
                    .GroupBy(x => x.Ref.Confidence?.ToString().ToLowerInvariant() ?? "potential")
                    .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var confGroup in byConfidence)
                {
                    output.WriteLine($"  {confGroup.Key} ({confGroup.Count()}):");
                    WriteCrossFileRefs(output, confGroup, indent: "    ");
                }
            }
            else
            {
                WriteCrossFileRefs(output, crossFileRefs, indent: "  ");
            }
        }
    }

    private static void WriteCrossFileRefs(TextWriter output,
        IEnumerable<(GDReferenceGroupInfo Group, GDReferenceInfo Ref)> refs, string indent)
    {
        var byFile = refs.GroupBy(x => x.Ref.FilePath)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
        var renderedProvenance = new HashSet<string>();

        foreach (var fileGroup in byFile)
        {
            output.WriteLine($"{indent}{fileGroup.Key}");
            foreach (var (group, reference) in fileGroup.OrderBy(x => x.Ref.Line).ThenBy(x => x.Ref.Column))
            {
                var conf = reference.Confidence.HasValue
                    ? $"[{reference.Confidence.Value.ToString().ToLowerInvariant()}]"
                    : "[potential]";
                var reason = !string.IsNullOrEmpty(reference.Reason) ? $" {reference.Reason}" : "";
                output.WriteLine($"{indent}  {FormatPosition(reference)} {GDAnsiColors.Yellow(conf)}{reason}");
                if (!string.IsNullOrEmpty(reference.Context))
                {
                    var ctx = reference.Context.TrimStart();
                    if (!string.IsNullOrEmpty(group.SymbolName))
                        ctx = HighlightSymbol(ctx, group.SymbolName);
                    output.WriteLine($"{indent}    {GDAnsiColors.Dim(ctx)}");
                }

                WriteProvenanceBlock(output, reference, indent + "    ", renderedProvenance);
            }
        }
    }

    private static void WriteProvenanceBlock(TextWriter output, GDReferenceInfo reference,
        string indent, HashSet<string> renderedProvenance)
    {
        if (!string.IsNullOrEmpty(reference.PromotionLabel))
        {
            output.WriteLine($"{indent}{GDAnsiColors.Green(reference.PromotionLabel)}");
            if (reference.PromotionProofParts?.Count > 0)
            {
                output.WriteLine($"{indent}{GDAnsiColors.Dim("Proof:")}");
                for (int i = 0; i < reference.PromotionProofParts.Count; i++)
                    output.WriteLine($"{indent}  {GDAnsiColors.Dim($"{i + 1}.")} {reference.PromotionProofParts[i]}");
            }
            if (!string.IsNullOrEmpty(reference.PromotionFilter))
                output.WriteLine($"{indent}  {GDAnsiColors.Dim($"({reference.PromotionFilter})")}");
        }

        if (reference.DetailedProvenance?.Count > 0)
        {
            var varName = reference.ProvenanceVariableName;
            if (varName != null && renderedProvenance.Contains(varName))
            {
                output.WriteLine(GDAnsiColors.Dim($"{indent}Evidence: (same as above)"));
            }
            else
            {
                if (varName != null)
                    renderedProvenance.Add(varName);

                if (!string.IsNullOrEmpty(varName))
                    output.WriteLine(GDAnsiColors.Dim($"{indent}Evidence for '{varName}':"));

                for (int i = 0; i < reference.DetailedProvenance.Count; i++)
                {
                    var entry = reference.DetailedProvenance[i];
                    var lineInfo = entry.SourceLine.HasValue ? $":{entry.SourceLine}" : "";
                    var location = !string.IsNullOrEmpty(entry.SourceFilePath)
                        ? $"{entry.SourceFilePath}{lineInfo}" : "";

                    output.WriteLine();
                    if (!string.IsNullOrEmpty(location))
                        output.WriteLine($"{indent}  {GDAnsiColors.Dim($"#{i + 1}")} {GDAnsiColors.Dim(location)}  {GDAnsiColors.Cyan(entry.TypeName)} ({entry.SourceReason})");
                    else
                        output.WriteLine($"{indent}  {GDAnsiColors.Dim($"#{i + 1}")} {GDAnsiColors.Cyan(entry.TypeName)} ({entry.SourceReason})");

                    if (entry.CallSites.Count > 0)
                        WriteCallSiteChain(output, entry.CallSites, indent.Length + 4);
                }
            }
        }
    }

    private static void WriteCallSiteChain(TextWriter output, List<GDCallSiteInfo> callSites, int indent)
    {
        var pad = new string(' ', indent);
        foreach (var cs in callSites)
        {
            output.WriteLine($"{pad}{GDAnsiColors.Dim(cs.FilePath + ":")}{GDAnsiColors.Cyan($"{cs.Line}")}  {cs.Expression}");
            if (cs.InnerChain.Count > 0)
                WriteCallSiteChain(output, cs.InnerChain, indent + 2);
        }
    }

    private static void WriteContractStringsSection(TextWriter output,
        List<GDReferenceGroupInfo> crossFileGroups, List<GDReferenceGroupInfo> regularGroups)
    {
        var contractRefs = crossFileGroups
            .Concat(regularGroups)
            .SelectMany(g => g.References.Where(r => r.IsContractString).Select(r => (Group: g, Ref: r)))
            .ToList();

        if (contractRefs.Count > 0)
        {
            output.WriteLine();
            output.WriteLine(GDAnsiColors.Magenta($"Contract strings ({contractRefs.Count}):"));
            output.WriteLine(GDAnsiColors.Dim("  (string-based API contracts, not auto-applied in rename; use rename --include-contract-strings)"));
            var byFile = contractRefs.GroupBy(x => x.Ref.FilePath)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var fileGroup in byFile)
            {
                output.WriteLine($"  {fileGroup.Key}");
                foreach (var (group, reference) in fileGroup.OrderBy(x => x.Ref.Line).ThenBy(x => x.Ref.Column))
                {
                    output.WriteLine($"    {FormatPosition(reference)}");
                    if (!string.IsNullOrEmpty(reference.Context))
                    {
                        var ctx = reference.Context.TrimStart();
                        if (!string.IsNullOrEmpty(group.SymbolName))
                            ctx = HighlightSymbol(ctx, group.SymbolName);
                        output.WriteLine($"      {GDAnsiColors.Dim(ctx)}");
                    }
                }
            }
        }
    }

    private static void WriteSignalConnectionsSection(TextWriter output, List<GDReferenceGroupInfo> signalGroups)
    {
        var signalRefs = signalGroups
            .SelectMany(g => g.References.Select(r => (Group: g, Ref: r)))
            .ToList();

        if (signalRefs.Count > 0)
        {
            output.WriteLine();
            output.WriteLine(GDAnsiColors.Cyan($"Signal connections ({signalRefs.Count}):"));
            var byFile = signalRefs.GroupBy(x => x.Ref.FilePath)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var fileGroup in byFile)
            {
                output.WriteLine($"  {fileGroup.Key}");
                foreach (var (group, reference) in fileGroup.OrderBy(x => x.Ref.Line).ThenBy(x => x.Ref.Column))
                {
                    var source = reference.IsSceneSignal ? "scene" : "code";
                    var signal = !string.IsNullOrEmpty(reference.SignalName) ? reference.SignalName : "?";
                    var receiver = !string.IsNullOrEmpty(reference.ReceiverTypeName)
                        ? $" -> {reference.ReceiverTypeName}" + (!string.IsNullOrEmpty(group.SymbolName) ? $".{group.SymbolName}" : "")
                        : "";
                    output.WriteLine($"    {FormatPosition(reference)} [{source}] {signal}.connect(){receiver}");
                    if (!string.IsNullOrEmpty(reference.Context))
                    {
                        var ctx = reference.Context.TrimStart();
                        if (!string.IsNullOrEmpty(group.SymbolName))
                            ctx = HighlightSymbol(ctx, group.SymbolName);
                        output.WriteLine($"      {GDAnsiColors.Dim(ctx)}");
                    }
                }
            }
        }
    }

    private static void WriteSignalConnectionsSplit(TextWriter output, List<GDReferenceGroupInfo> signalGroups)
    {
        var signalRefs = signalGroups
            .SelectMany(g => g.References.Select(r => (Group: g, Ref: r)))
            .ToList();

        if (signalRefs.Count == 0)
            return;

        var codeSignals = signalRefs.Where(x => !x.Ref.IsSceneSignal).ToList();
        var sceneSignals = signalRefs.Where(x => x.Ref.IsSceneSignal).ToList();

        output.WriteLine();
        output.WriteLine(GDAnsiColors.Cyan($"Signal connections ({signalRefs.Count}):"));

        if (codeSignals.Count > 0)
        {
            output.WriteLine($"  Code ({codeSignals.Count}):");
            var byFile = codeSignals.GroupBy(x => x.Ref.FilePath)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var fileGroup in byFile)
            {
                output.WriteLine($"    {fileGroup.Key}");
                foreach (var (group, reference) in fileGroup.OrderBy(x => x.Ref.Line).ThenBy(x => x.Ref.Column))
                {
                    var signal = !string.IsNullOrEmpty(reference.SignalName) ? reference.SignalName : "?";
                    var receiver = !string.IsNullOrEmpty(reference.ReceiverTypeName)
                        ? $" -> {reference.ReceiverTypeName}" + (!string.IsNullOrEmpty(group.SymbolName) ? $".{group.SymbolName}" : "")
                        : "";
                    output.WriteLine($"      {FormatPosition(reference)} {signal}.connect(){receiver}");
                    if (!string.IsNullOrEmpty(reference.Context))
                    {
                        var ctx = reference.Context.TrimStart();
                        if (!string.IsNullOrEmpty(group.SymbolName))
                            ctx = HighlightSymbol(ctx, group.SymbolName);
                        output.WriteLine($"        {GDAnsiColors.Dim(ctx)}");
                    }
                }
            }
        }

        if (sceneSignals.Count > 0)
        {
            output.WriteLine($"  Scene ({sceneSignals.Count}):");
            var byFile = sceneSignals.GroupBy(x => x.Ref.FilePath)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var fileGroup in byFile)
            {
                output.WriteLine($"    {fileGroup.Key}");
                foreach (var (group, reference) in fileGroup.OrderBy(x => x.Ref.Line).ThenBy(x => x.Ref.Column))
                {
                    var signal = !string.IsNullOrEmpty(reference.SignalName) ? reference.SignalName : "?";
                    var sceneReceiver = !string.IsNullOrEmpty(reference.ReceiverTypeName)
                        ? $" -> {reference.ReceiverTypeName}" + (!string.IsNullOrEmpty(group.SymbolName) ? $".{group.SymbolName}" : "")
                        : "";
                    output.WriteLine($"      {FormatPosition(reference)} {signal}.connect(){sceneReceiver}");
                    if (!string.IsNullOrEmpty(reference.Context))
                    {
                        var ctx = reference.Context.TrimStart();
                        if (!string.IsNullOrEmpty(group.SymbolName))
                            ctx = HighlightSymbol(ctx, group.SymbolName);
                        output.WriteLine($"        {GDAnsiColors.Dim(ctx)}");
                    }
                }
            }
        }
    }

    private static void WriteSummary(TextWriter output, GDFindRefsResultInfo result)
    {
        var allPrimary = result.PrimaryGroups;
        var regularGroups = allPrimary.Where(g => !g.IsCrossFile && !g.IsSignalConnection).ToList();
        var crossFileGroups = allPrimary.Where(g => g.IsCrossFile).ToList();
        var signalGroups = allPrimary.Where(g => g.IsSignalConnection).ToList();

        // Collect all primary refs (root + overrides) for counting
        var allOverrides = CollectAllOverrides(regularGroups);
        var allRegularRefs = regularGroups.SelectMany(g => g.References)
            .Concat(allOverrides.SelectMany(g => g.References))
            .ToList();

        int defCount = allRegularRefs.Count(r => r.IsDeclaration && !r.IsOverride);
        int overrideCount = allRegularRefs.Count(r => r.IsOverride);
        int superCallCount = allRegularRefs.Count(r => r.IsSuperCall);
        int callCount = allRegularRefs
            .Count(r => !r.IsDeclaration && !r.IsOverride && !r.IsSuperCall && !r.IsContractString && !r.IsSignalConnection);

        var crossFileRefs = crossFileGroups
            .SelectMany(g => g.References.Where(r => !r.IsContractString))
            .ToList();
        int crossFileCount = crossFileRefs.Count;
        var signalRefs = signalGroups.SelectMany(g => g.References).ToList();
        int signalCodeCount = signalRefs.Count(r => !r.IsSceneSignal);
        int signalSceneCount = signalRefs.Count(r => r.IsSceneSignal);
        int signalTotal = signalRefs.Count;
        int contractCount = allPrimary
            .SelectMany(g => g.References)
            .Count(r => r.IsContractString);
        int sameNameCount = result.UnrelatedGroups.Count;
        int sameNameRefs = 0;
        foreach (var ug in result.UnrelatedGroups)
            sameNameRefs += ug.References.Count + CountOverrideRefs(ug);

        int total = defCount + overrideCount + superCallCount + callCount + crossFileCount + signalTotal + contractCount + sameNameRefs;

        output.WriteLine();
        output.WriteLine(GDAnsiColors.Bold("Summary:"));
        output.WriteLine($"  Definition{(defCount == 1 ? "" : "s")}: {defCount}");
        if (overrideCount > 0) output.WriteLine($"  Overrides: {overrideCount}");
        if (superCallCount > 0) output.WriteLine($"  Super calls: {superCallCount}");
        if (callCount > 0) output.WriteLine($"  Calls: {callCount}");
        if (crossFileCount > 0)
        {
            var confidenceCounts = crossFileRefs
                .GroupBy(r => r.Confidence?.ToString().ToLowerInvariant() ?? "potential")
                .ToDictionary(g => g.Key, g => g.Count());

            if (confidenceCounts.Count > 1)
            {
                var parts = confidenceCounts
                    .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kv => $"{kv.Key}: {kv.Value}");
                output.WriteLine($"  Potential (duck-typed): {crossFileCount} ({string.Join(", ", parts)})");
            }
            else
            {
                output.WriteLine($"  Potential (duck-typed): {crossFileCount}");
            }
        }
        if (signalTotal > 0) output.WriteLine($"  Signals: {signalTotal} (code: {signalCodeCount}, scene: {signalSceneCount})");
        if (contractCount > 0) output.WriteLine($"  Contract strings: {contractCount}");
        if (sameNameCount > 0)
        {
            output.WriteLine($"  Unrelated: {sameNameCount} ({sameNameRefs} ref{(sameNameRefs == 1 ? "" : "s")})");
            foreach (var ug in result.UnrelatedGroups)
            {
                var ugName = !string.IsNullOrEmpty(ug.ClassName) ? ug.ClassName : ug.DeclarationFilePath;
                var ugRefCount = ug.References.Count + CountOverrideRefs(ug);
                var kinds = FormatRefKinds(ug);
                output.WriteLine($"    {ugName}: {ugRefCount} ref{(ugRefCount == 1 ? "" : "s")}{kinds}");
            }
        }
        output.WriteLine($"  Total references: {total}");
    }

    private static string FormatRefKinds(GDReferenceGroupInfo group)
    {
        var allRefs = group.References.ToList();
        CollectOverrideRefs(group, allRefs);

        int def = allRefs.Count(r => r.IsDeclaration || r.IsOverride);
        int call = allRefs.Count(r => !r.IsDeclaration && !r.IsOverride && !r.IsSuperCall && !r.IsWrite && !r.IsContractString && !r.IsSignalConnection);
        int write = allRefs.Count(r => r.IsWrite);
        int superCall = allRefs.Count(r => r.IsSuperCall);

        var parts = new List<string>();
        if (def > 0) parts.Add($"def: {def}");
        if (call > 0) parts.Add($"call: {call}");
        if (write > 0) parts.Add($"write: {write}");
        if (superCall > 0) parts.Add($"super: {superCall}");

        return parts.Count > 0 ? $" ({string.Join(", ", parts)})" : "";
    }

    private static void CollectOverrideRefs(GDReferenceGroupInfo group, List<GDReferenceInfo> allRefs)
    {
        foreach (var ovr in group.Overrides)
        {
            allRefs.AddRange(ovr.References);
            CollectOverrideRefs(ovr, allRefs);
        }
    }

    private static List<GDReferenceGroupInfo> CollectAllOverrides(List<GDReferenceGroupInfo> groups)
    {
        var result = new List<GDReferenceGroupInfo>();
        foreach (var g in groups)
        {
            result.AddRange(g.Overrides);
            if (g.IsOverride || g.IsInherited)
                result.Add(g);
        }
        return result;
    }

    private static int CountOverrideRefs(GDReferenceGroupInfo group)
    {
        int count = 0;
        foreach (var ovr in group.Overrides)
        {
            count += ovr.References.Count;
            count += CountOverrideRefs(ovr);
        }
        return count;
    }

    private static void WriteGroupTree(TextWriter output, GDReferenceGroupInfo group, string indent)
    {
        WriteGroupHeader(output, group, indent);

        foreach (var reference in group.References.Where(r => !r.IsContractString).OrderBy(r => r.Line).ThenBy(r => r.Column))
        {
            WriteNewReferenceLine(output, reference, indent + "  ", group.IsInherited, group.SymbolName);
        }

        var overrides = group.Overrides;
        for (int i = 0; i < overrides.Count; i++)
        {
            var isLast = i == overrides.Count - 1;
            var branch = isLast ? "\u2514\u2500\u2500 " : "\u251c\u2500\u2500 ";
            var childIndent = indent + (isLast ? "    " : "\u2502   ");

            var ovr = overrides[i];
            var ovrHeader = !string.IsNullOrEmpty(ovr.ClassName)
                ? GDAnsiColors.Bold(ovr.ClassName)
                : GDAnsiColors.Bold(ovr.DeclarationFilePath);

            if (ovr.IsInherited)
                output.WriteLine($"{indent}{branch}{ovrHeader} ({ovr.DeclarationFilePath})");
            else
                output.WriteLine($"{indent}{branch}{ovrHeader} ({ovr.DeclarationFilePath}:{ovr.DeclarationLine})");

            foreach (var reference in ovr.References.Where(r => !r.IsContractString).OrderBy(r => r.Line).ThenBy(r => r.Column))
            {
                WriteNewReferenceLine(output, reference, childIndent, ovr.IsInherited, ovr.SymbolName);
            }

            if (ovr.Overrides.Count > 0)
            {
                WriteGroupTree(output, ovr, childIndent);
            }
        }
    }

    private static void WriteReferenceLine(TextWriter output, GDReferenceInfo reference,
        string indent = "", bool isInherited = false, string? symbolName = null)
    {
        // Legacy markers for backward compatibility with WriteReferences
        string marker;
        if (reference.Confidence.HasValue)
        {
            var conf = reference.Confidence.Value.ToString().ToLowerInvariant();
            marker = reference.IsContractString ? $"[contract-{conf}]" : $"[{conf}]";
        }
        else if (reference.IsOverride)
            marker = "[override]";
        else if (isInherited)
            marker = reference.IsWrite ? "[write]" : "[call]";
        else if (reference.IsDeclaration)
            marker = "[def]";
        else if (reference.IsSuperCall)
            marker = "[call-super]";
        else if (reference.IsWrite)
            marker = "[write]";
        else
            marker = "[call]";

        output.WriteLine($"{indent}  {FormatPosition(reference)} {marker}");

        if (!string.IsNullOrEmpty(reference.Context))
        {
            var contextLine = reference.Context.TrimStart();
            if (!string.IsNullOrEmpty(symbolName))
                contextLine = HighlightSymbol(contextLine, symbolName);
            output.WriteLine($"{indent}    {GDAnsiColors.Dim(contextLine)}");
        }
    }

    private static bool HasNonContractReferences(GDReferenceGroupInfo group)
    {
        if (group.References.Any(r => !r.IsContractString))
            return true;
        return group.Overrides.Any(HasNonContractReferences);
    }

    private static string FormatPosition(GDReferenceInfo r)
    {
        if (r.EndColumn.HasValue && r.EndColumn.Value > r.Column)
            return $"{r.Line}:{r.Column}..{r.EndColumn.Value}";
        return $"{r.Line}:{r.Column}";
    }

    private static string HighlightSymbol(string line, string symbol)
    {
        var idx = line.IndexOf(symbol, StringComparison.Ordinal);
        if (idx < 0)
            return line;

        var sb = new StringBuilder();
        int pos = 0;
        while (pos < line.Length)
        {
            idx = line.IndexOf(symbol, pos, StringComparison.Ordinal);
            if (idx < 0)
            {
                sb.Append(line, pos, line.Length - pos);
                break;
            }
            sb.Append(line, pos, idx - pos);
            if (GDAnsiColors.Enabled)
            {
                sb.Append("\x1b[0m");
                sb.Append(GDAnsiColors.Green(symbol));
                sb.Append("\x1b[2m");
            }
            else
            {
                sb.Append(symbol);
            }
            pos = idx + symbol.Length;
        }
        return sb.ToString();
    }

    public void WriteMessage(TextWriter output, string message)
    {
        output.WriteLine(message);
    }

    public void WriteError(TextWriter output, string error)
    {
        output.WriteLine($"{GDAnsiColors.Red("Error")}: {error}");
    }
}
