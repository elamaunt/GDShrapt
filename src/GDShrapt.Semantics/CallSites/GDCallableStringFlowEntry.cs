namespace GDShrapt.Semantics;

internal class GDCallableStringFlowEntry
{
    public string ResolvedMethodName { get; }
    public string? ReceiverType { get; }
    public string SourceFilePath { get; }
    public int Line { get; }

    internal GDCallableStringFlowEntry(
        string resolvedMethodName,
        string? receiverType,
        string sourceFilePath,
        int line)
    {
        ResolvedMethodName = resolvedMethodName;
        ReceiverType = receiverType;
        SourceFilePath = sourceFilePath;
        Line = line;
    }
}
