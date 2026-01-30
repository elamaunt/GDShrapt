# CRLF Token Ordering Fix - Continuation Plan

## Status: Nearly Complete (25/26 tests passing)

### Completed Work

1. **Root Cause Identified and Fixed**: `GDCarriageReturnToken.ToString()` returns empty string (by design for Godot compatibility), but `GDExpressionResolver.CompleteExpression` was using `state.PassString(token.ToString())` to repass tokens, which silently dropped CR tokens.

2. **Fix Applied**: Added `PassToken` helper method in `GDExpressionResolver.cs` that handles CR tokens specially by calling `state.PassCarriageReturnChar()` instead of `state.PassString()`.

3. **Files Modified This Session**:
   - `src/GDShrapt.Reader/Resolvers/GDExpressionResolver.cs` - Added `PassToken` helper, fixed 3 call sites

4. **Tests Status**:
   - 25 CRLF tests passing (including `Parse_FunctionWithTypedParams_WithCRLF` and `Parse_ComplexExpression_WithCRLF`)
   - 1 test skipped: `Parse_MultilineString_WithCRLF` (unrelated feature - multiline strings)

### Remaining Task

**Remove `[Ignore]` attributes from the 4 originally failing tests in `CarriageReturnTokenTests.cs`**:

```bash
# Find and edit these tests to remove [Ignore]:
1. Parse_EmptyLineWithCRLF_PreservesStructure
2. Parse_MultilineString_WithCRLF (might still need work - multiline strings)
3. Parse_WindowsStyleFile_FullRoundtrip
4. Parse_TrailingCRLF_Preserved
```

File location: `src/GDShrapt.Reader.Tests/Syntax/CarriageReturnTokenTests.cs`

### Verification Commands

```bash
# Run all CRLF tests
cd "C:\elamaunt\GDShrapt.CLI-Pro\src\GDShrapt"
dotnet test src/GDShrapt.Reader.Tests --filter "Name~_WithCRLF"

# Run all Reader tests to check for regressions
dotnet test src/GDShrapt.Reader.Tests

# Run full solution tests
dotnet test src/GDShrapt.sln
```

### Key Fix Summary

The issue was in `GDExpressionResolver.CompleteExpression`:

```csharp
// OLD (broken for CR tokens):
foreach (var token in dualOperatorExpression.Form.GetAllTokensAfter(0))
    state.PassString(token.ToString());

// NEW (fixed):
foreach (var token in dualOperatorExpression.Form.GetAllTokensAfter(0))
    PassToken(state, token);
```

Where `PassToken` is:
```csharp
private static void PassToken(GDReadingState state, GDSyntaxToken token)
{
    if (token is GDCarriageReturnToken)
        state.PassCarriageReturnChar();
    else if (token is GDNewLine)
        state.PassNewLine();
    else
        state.PassString(token.ToString());
}
```

### Debug Test to Clean Up

A debug test was created and should be deleted:
- `src/GDShrapt.Reader.Tests/Syntax/CRLFDebugTest.cs`

### Previous Session Context

The original plan file is at: `C:\Users\AL003\.claude\plans\adaptive-wondering-flame.md`

It contains detailed root cause analysis of the CR token ordering issues, most of which have now been resolved.
