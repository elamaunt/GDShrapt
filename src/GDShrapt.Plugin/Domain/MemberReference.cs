using GDShrapt.Reader;

namespace GDShrapt.Plugin;

internal class MemberReference
{
    public GDScriptMap Script { get; set; }
    public GDIdentifier Identifier { get; set; }
    public GDClassMember Member { get; set; }
}