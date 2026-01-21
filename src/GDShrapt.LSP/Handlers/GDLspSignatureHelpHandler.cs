using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GDShrapt.CLI.Core;

namespace GDShrapt.LSP;

/// <summary>
/// Handles textDocument/signatureHelp requests.
/// Thin wrapper over IGDSignatureHelpHandler from CLI.Core.
/// </summary>
public class GDLspSignatureHelpHandler
{
    private readonly IGDSignatureHelpHandler _handler;

    public GDLspSignatureHelpHandler(IGDSignatureHelpHandler handler)
    {
        _handler = handler;
    }

    public Task<GDLspSignatureHelp?> HandleAsync(GDSignatureHelpParams @params, CancellationToken cancellationToken)
    {
        var filePath = GDDocumentManager.UriToPath(@params.TextDocument.Uri);

        // Convert LSP 0-based to CLI.Core 1-based
        var line = @params.Position.Line + 1;
        var column = @params.Position.Character + 1;

        // Delegate to CLI.Core handler
        var result = _handler.GetSignatureHelp(filePath, line, column);
        if (result == null)
            return Task.FromResult<GDLspSignatureHelp?>(null);

        // Convert CLI.Core result to LSP signature help
        var signatures = new List<GDLspSignatureInformation>();
        foreach (var sig in result.Signatures)
        {
            signatures.Add(ConvertToLspSignature(sig));
        }

        var signatureHelp = new GDLspSignatureHelp
        {
            Signatures = signatures.ToArray(),
            ActiveSignature = result.ActiveSignature,
            ActiveParameter = result.ActiveParameter
        };

        return Task.FromResult<GDLspSignatureHelp?>(signatureHelp);
    }

    private static GDLspSignatureInformation ConvertToLspSignature(GDSignatureInfo signature)
    {
        var parameters = new List<GDLspParameterInformation>();
        foreach (var param in signature.Parameters)
        {
            parameters.Add(new GDLspParameterInformation
            {
                Label = param.Label,
                Documentation = param.Documentation != null
                    ? GDLspMarkupContent.Markdown(param.Documentation)
                    : null
            });
        }

        return new GDLspSignatureInformation
        {
            Label = signature.Label,
            Documentation = signature.Documentation != null
                ? GDLspMarkupContent.Markdown(signature.Documentation)
                : null,
            Parameters = parameters.ToArray()
        };
    }
}
