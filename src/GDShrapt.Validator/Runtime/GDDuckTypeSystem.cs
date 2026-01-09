using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{
    /// <summary>
    /// Represents a duck type - a type defined by its capabilities rather than its name.
    /// Used when we know what methods/properties an object has without knowing the exact type.
    /// </summary>
    public class GDDuckType
    {
        /// <summary>
        /// Methods that this duck type must have.
        /// </summary>
        public HashSet<string> RequiredMethods { get; } = new HashSet<string>();

        /// <summary>
        /// Properties that this duck type must have.
        /// </summary>
        public HashSet<string> RequiredProperties { get; } = new HashSet<string>();

        /// <summary>
        /// Signals that this duck type must have.
        /// </summary>
        public HashSet<string> RequiredSignals { get; } = new HashSet<string>();

        /// <summary>
        /// Known base types this could be (from 'is' checks).
        /// </summary>
        public HashSet<string> PossibleTypes { get; } = new HashSet<string>();

        /// <summary>
        /// Types that this is definitely NOT (from failed 'is' checks in else branches).
        /// </summary>
        public HashSet<string> ExcludedTypes { get; } = new HashSet<string>();

        /// <summary>
        /// Checks if this duck type is compatible with a concrete type.
        /// </summary>
        public bool IsCompatibleWith(string typeName, IGDRuntimeProvider provider)
        {
            if (string.IsNullOrEmpty(typeName))
                return true; // Unknown type is potentially compatible

            // Check excluded types
            if (ExcludedTypes.Contains(typeName))
                return false;

            // Check if type is in possible types (if any defined)
            if (PossibleTypes.Count > 0 && !PossibleTypes.Any(pt =>
                pt == typeName || provider.IsAssignableTo(typeName, pt)))
                return false;

            // Check required methods
            foreach (var method in RequiredMethods)
            {
                var member = provider.GetMember(typeName, method);
                if (member == null || member.Kind != GDRuntimeMemberKind.Method)
                    return false;
            }

            // Check required properties
            foreach (var prop in RequiredProperties)
            {
                var member = provider.GetMember(typeName, prop);
                if (member == null || member.Kind != GDRuntimeMemberKind.Property)
                    return false;
            }

            // Check required signals
            foreach (var signal in RequiredSignals)
            {
                var member = provider.GetMember(typeName, signal);
                if (member == null || member.Kind != GDRuntimeMemberKind.Signal)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Merges another duck type into this one.
        /// </summary>
        public void MergeWith(GDDuckType other)
        {
            if (other == null)
                return;

            foreach (var m in other.RequiredMethods)
                RequiredMethods.Add(m);
            foreach (var p in other.RequiredProperties)
                RequiredProperties.Add(p);
            foreach (var s in other.RequiredSignals)
                RequiredSignals.Add(s);
            foreach (var t in other.PossibleTypes)
                PossibleTypes.Add(t);
            foreach (var t in other.ExcludedTypes)
                ExcludedTypes.Add(t);
        }

        /// <summary>
        /// Creates intersection of possible types (for type narrowing).
        /// </summary>
        public GDDuckType IntersectWith(GDDuckType other)
        {
            if (other == null)
                return this;

            var result = new GDDuckType();

            // Merge all requirements
            foreach (var m in RequiredMethods)
                result.RequiredMethods.Add(m);
            foreach (var m in other.RequiredMethods)
                result.RequiredMethods.Add(m);

            foreach (var p in RequiredProperties)
                result.RequiredProperties.Add(p);
            foreach (var p in other.RequiredProperties)
                result.RequiredProperties.Add(p);

            foreach (var s in RequiredSignals)
                result.RequiredSignals.Add(s);
            foreach (var s in other.RequiredSignals)
                result.RequiredSignals.Add(s);

            // Intersect possible types
            if (PossibleTypes.Count > 0 && other.PossibleTypes.Count > 0)
            {
                foreach (var t in PossibleTypes)
                {
                    if (other.PossibleTypes.Contains(t))
                        result.PossibleTypes.Add(t);
                }
            }
            else if (PossibleTypes.Count > 0)
            {
                foreach (var t in PossibleTypes)
                    result.PossibleTypes.Add(t);
            }
            else if (other.PossibleTypes.Count > 0)
            {
                foreach (var t in other.PossibleTypes)
                    result.PossibleTypes.Add(t);
            }

            // Union excluded types
            foreach (var t in ExcludedTypes)
                result.ExcludedTypes.Add(t);
            foreach (var t in other.ExcludedTypes)
                result.ExcludedTypes.Add(t);

            return result;
        }

        public override string ToString()
        {
            var parts = new List<string>();

            if (PossibleTypes.Count > 0)
                parts.Add($"is:{string.Join("|", PossibleTypes)}");
            if (RequiredMethods.Count > 0)
                parts.Add($"methods:{string.Join(",", RequiredMethods)}");
            if (RequiredProperties.Count > 0)
                parts.Add($"props:{string.Join(",", RequiredProperties)}");
            if (RequiredSignals.Count > 0)
                parts.Add($"signals:{string.Join(",", RequiredSignals)}");

            return parts.Count > 0 ? $"DuckType({string.Join(" ", parts)})" : "DuckType(any)";
        }
    }

    /// <summary>
    /// Tracks type narrowing information within a scope.
    /// Used for flow-sensitive type analysis (if obj is Player: ...).
    /// </summary>
    public class GDTypeNarrowingContext
    {
        private readonly Dictionary<string, GDDuckType> _narrowedTypes;
        private readonly GDTypeNarrowingContext _parent;

        public GDTypeNarrowingContext(GDTypeNarrowingContext parent = null)
        {
            _narrowedTypes = new Dictionary<string, GDDuckType>();
            _parent = parent;
        }

        /// <summary>
        /// Narrows the type of a variable within this context.
        /// </summary>
        public void NarrowType(string variableName, string toType)
        {
            if (!_narrowedTypes.TryGetValue(variableName, out var duckType))
            {
                duckType = new GDDuckType();
                _narrowedTypes[variableName] = duckType;
            }
            duckType.PossibleTypes.Add(toType);
        }

        /// <summary>
        /// Excludes a type from a variable (for else branches).
        /// </summary>
        public void ExcludeType(string variableName, string type)
        {
            if (!_narrowedTypes.TryGetValue(variableName, out var duckType))
            {
                duckType = new GDDuckType();
                _narrowedTypes[variableName] = duckType;
            }
            duckType.ExcludedTypes.Add(type);
        }

        /// <summary>
        /// Records that a variable must have a method (from has_method check).
        /// </summary>
        public void RequireMethod(string variableName, string methodName)
        {
            if (!_narrowedTypes.TryGetValue(variableName, out var duckType))
            {
                duckType = new GDDuckType();
                _narrowedTypes[variableName] = duckType;
            }
            duckType.RequiredMethods.Add(methodName);
        }

        /// <summary>
        /// Records that a variable must have a signal (from has_signal check).
        /// </summary>
        public void RequireSignal(string variableName, string signalName)
        {
            if (!_narrowedTypes.TryGetValue(variableName, out var duckType))
            {
                duckType = new GDDuckType();
                _narrowedTypes[variableName] = duckType;
            }
            duckType.RequiredSignals.Add(signalName);
        }

        /// <summary>
        /// Records that a variable must have a property (from property access).
        /// </summary>
        public void RequireProperty(string variableName, string propertyName)
        {
            if (!_narrowedTypes.TryGetValue(variableName, out var duckType))
            {
                duckType = new GDDuckType();
                _narrowedTypes[variableName] = duckType;
            }
            duckType.RequiredProperties.Add(propertyName);
        }

        /// <summary>
        /// Gets the narrowed type information for a variable.
        /// </summary>
        public GDDuckType GetNarrowedType(string variableName)
        {
            if (_narrowedTypes.TryGetValue(variableName, out var duckType))
                return duckType;
            return _parent?.GetNarrowedType(variableName);
        }

        /// <summary>
        /// Gets the most specific concrete type for a variable, if determinable.
        /// </summary>
        public string GetConcreteType(string variableName)
        {
            var duckType = GetNarrowedType(variableName);
            if (duckType == null)
                return null;

            // If there's exactly one possible type, return it
            if (duckType.PossibleTypes.Count == 1)
                return duckType.PossibleTypes.First();

            return null;
        }

        /// <summary>
        /// Creates a child context for nested scopes (if branches, loops, etc.).
        /// </summary>
        public GDTypeNarrowingContext CreateChild()
        {
            return new GDTypeNarrowingContext(this);
        }

        /// <summary>
        /// Gets all variable names that have narrowing information in this context.
        /// Does not include parent context variables.
        /// </summary>
        public IEnumerable<string> GetNarrowedVariables()
        {
            return _narrowedTypes.Keys;
        }

        /// <summary>
        /// Merges type information from two branches (for if/else convergence).
        /// </summary>
        public static GDTypeNarrowingContext MergeBranches(
            GDTypeNarrowingContext ifBranch,
            GDTypeNarrowingContext elseBranch)
        {
            // For merged branches, we can only keep type info that holds in both
            var merged = new GDTypeNarrowingContext();

            if (ifBranch == null || elseBranch == null)
                return merged;

            var allVars = new HashSet<string>();
            foreach (var kv in ifBranch._narrowedTypes)
                allVars.Add(kv.Key);
            foreach (var kv in elseBranch._narrowedTypes)
                allVars.Add(kv.Key);

            foreach (var varName in allVars)
            {
                var ifType = ifBranch.GetNarrowedType(varName);
                var elseType = elseBranch.GetNarrowedType(varName);

                if (ifType != null && elseType != null)
                {
                    // Both branches have info - keep common requirements
                    var mergedDuck = new GDDuckType();

                    // Methods/properties/signals must be present in BOTH branches
                    foreach (var m in ifType.RequiredMethods)
                    {
                        if (elseType.RequiredMethods.Contains(m))
                            mergedDuck.RequiredMethods.Add(m);
                    }
                    foreach (var p in ifType.RequiredProperties)
                    {
                        if (elseType.RequiredProperties.Contains(p))
                            mergedDuck.RequiredProperties.Add(p);
                    }
                    foreach (var s in ifType.RequiredSignals)
                    {
                        if (elseType.RequiredSignals.Contains(s))
                            mergedDuck.RequiredSignals.Add(s);
                    }

                    // Possible types: union (could be either)
                    foreach (var t in ifType.PossibleTypes)
                        mergedDuck.PossibleTypes.Add(t);
                    foreach (var t in elseType.PossibleTypes)
                        mergedDuck.PossibleTypes.Add(t);

                    if (mergedDuck.RequiredMethods.Count > 0 ||
                        mergedDuck.RequiredProperties.Count > 0 ||
                        mergedDuck.RequiredSignals.Count > 0 ||
                        mergedDuck.PossibleTypes.Count > 0)
                    {
                        merged._narrowedTypes[varName] = mergedDuck;
                    }
                }
            }

            return merged;
        }
    }

    /// <summary>
    /// Analyzes control flow to extract type narrowing information.
    /// Detects patterns like: if obj is Player, if obj.has_method("foo"), etc.
    /// </summary>
    public class GDTypeNarrowingAnalyzer
    {
        private readonly IGDRuntimeProvider _runtimeProvider;

        public GDTypeNarrowingAnalyzer(IGDRuntimeProvider runtimeProvider)
        {
            _runtimeProvider = runtimeProvider;
        }

        /// <summary>
        /// Analyzes a condition expression and returns type narrowing information.
        /// </summary>
        /// <param name="condition">The condition expression</param>
        /// <param name="isNegated">True if in else branch (condition is false)</param>
        /// <returns>Type narrowing context for the branch</returns>
        public GDTypeNarrowingContext AnalyzeCondition(GDExpression condition, bool isNegated = false)
        {
            var context = new GDTypeNarrowingContext();

            if (condition == null)
                return context;

            AnalyzeConditionInto(condition, context, isNegated);
            return context;
        }

        private void AnalyzeConditionInto(GDExpression condition, GDTypeNarrowingContext context, bool isNegated)
        {
            switch (condition)
            {
                // obj is Type
                case GDDualOperatorExpression dualOp when dualOp.Operator?.OperatorType == GDDualOperatorType.Is:
                    AnalyzeIsExpression(dualOp, context, isNegated);
                    break;

                // obj.has_method("name") / obj.has_signal("name")
                case GDCallExpression callExpr:
                    AnalyzeCallCondition(callExpr, context, isNegated);
                    break;

                // not condition
                case GDSingleOperatorExpression singleOp
                    when singleOp.Operator?.OperatorType == GDSingleOperatorType.Not ||
                         singleOp.Operator?.OperatorType == GDSingleOperatorType.Not2:
                    AnalyzeConditionInto(singleOp.TargetExpression, context, !isNegated);
                    break;

                // condition and condition (both && and 'and' keyword)
                case GDDualOperatorExpression andOp when andOp.Operator?.OperatorType == GDDualOperatorType.And ||
                                                         andOp.Operator?.OperatorType == GDDualOperatorType.And2:
                    if (!isNegated)
                    {
                        // Both must be true
                        AnalyzeConditionInto(andOp.LeftExpression, context, false);
                        AnalyzeConditionInto(andOp.RightExpression, context, false);
                    }
                    // In negated case (else branch), we can't conclude much
                    break;

                // condition or condition (both || and 'or' keyword)
                case GDDualOperatorExpression orOp when orOp.Operator?.OperatorType == GDDualOperatorType.Or ||
                                                        orOp.Operator?.OperatorType == GDDualOperatorType.Or2:
                    if (isNegated)
                    {
                        // Both must be false
                        AnalyzeConditionInto(orOp.LeftExpression, context, true);
                        AnalyzeConditionInto(orOp.RightExpression, context, true);
                    }
                    // In non-negated case, we can't conclude much (either could be true)
                    break;

                // (condition)
                case GDBracketExpression bracket:
                    AnalyzeConditionInto(bracket.InnerExpression, context, isNegated);
                    break;
            }
        }

        private void AnalyzeIsExpression(GDDualOperatorExpression isExpr, GDTypeNarrowingContext context, bool isNegated)
        {
            // Get the variable name
            var varName = GetVariableName(isExpr.LeftExpression);
            if (varName == null)
                return;

            // Get the type name
            var typeName = GetTypeName(isExpr.RightExpression);
            if (typeName == null)
                return;

            if (isNegated)
            {
                // In else branch: obj is NOT this type
                context.ExcludeType(varName, typeName);
            }
            else
            {
                // In if branch: obj IS this type
                context.NarrowType(varName, typeName);
            }
        }

        private void AnalyzeCallCondition(GDCallExpression callExpr, GDTypeNarrowingContext context, bool isNegated)
        {
            // Check for has_method/has_signal pattern
            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                var methodName = memberOp.Identifier?.Sequence;
                var varName = GetVariableName(memberOp.CallerExpression);

                if (varName == null)
                    return;

                // has_method("name")
                if (methodName == "has_method" && !isNegated)
                {
                    var argName = GetStringArgument(callExpr);
                    if (argName != null)
                        context.RequireMethod(varName, argName);
                }
                // has_signal("name")
                else if (methodName == "has_signal" && !isNegated)
                {
                    var argName = GetStringArgument(callExpr);
                    if (argName != null)
                        context.RequireSignal(varName, argName);
                }
                // has("property")
                else if (methodName == "has" && !isNegated)
                {
                    var argName = GetStringArgument(callExpr);
                    if (argName != null)
                        context.RequireProperty(varName, argName);
                }
            }

            // Check for is_instance_valid(obj)
            if (callExpr.CallerExpression is GDIdentifierExpression funcIdent)
            {
                var funcName = funcIdent.Identifier?.Sequence;
                if (funcName == "is_instance_valid" && !isNegated)
                {
                    // Object is valid, but we don't learn much about type
                }
            }
        }

        private string GetVariableName(GDExpression expr)
        {
            if (expr is GDIdentifierExpression ident)
                return ident.Identifier?.Sequence;
            return null;
        }

        private string GetTypeName(GDExpression expr)
        {
            if (expr is GDIdentifierExpression ident)
                return ident.Identifier?.Sequence;
            return null;
        }

        private string GetStringArgument(GDCallExpression callExpr)
        {
            var args = callExpr.Parameters;
            if (args == null || args.Count == 0)
                return null;

            var firstArg = args.FirstOrDefault();
            if (firstArg is GDStringExpression strExpr)
            {
                // GDStringNode.Sequence returns content without quotes
                return strExpr.String?.Sequence;
            }

            return null;
        }

        /// <summary>
        /// Finds all types that could potentially satisfy a duck type.
        /// </summary>
        public IEnumerable<string> FindCompatibleTypes(GDDuckType duckType, IEnumerable<string> knownTypes)
        {
            foreach (var typeName in knownTypes)
            {
                if (duckType.IsCompatibleWith(typeName, _runtimeProvider))
                    yield return typeName;
            }
        }
    }

    /// <summary>
    /// Collects duck type information by analyzing member accesses and method calls.
    /// </summary>
    public class GDDuckTypeCollector : GDVisitor
    {
        private readonly Dictionary<string, GDDuckType> _variableDuckTypes;
        private readonly GDScopeStack _scopes;

        public IReadOnlyDictionary<string, GDDuckType> VariableDuckTypes => _variableDuckTypes;

        public GDDuckTypeCollector(GDScopeStack scopes)
        {
            _variableDuckTypes = new Dictionary<string, GDDuckType>();
            _scopes = scopes;
        }

        /// <summary>
        /// Collects duck type information from an AST.
        /// </summary>
        public void Collect(GDNode node)
        {
            if (node != null)
                node.WalkIn(this);
        }

        public override void Visit(GDMemberOperatorExpression memberOp)
        {
            var varName = GetRootVariableName(memberOp.CallerExpression);
            if (varName == null)
                return;

            // Check if variable is untyped
            var symbol = _scopes?.Lookup(varName);
            if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName))
                return; // Already has a known type

            var memberName = memberOp.Identifier?.Sequence;
            if (string.IsNullOrEmpty(memberName))
                return;

            EnsureDuckType(varName).RequiredProperties.Add(memberName);
        }

        public override void Visit(GDCallExpression callExpr)
        {
            if (callExpr.CallerExpression is GDMemberOperatorExpression memberOp)
            {
                var varName = GetRootVariableName(memberOp.CallerExpression);
                if (varName == null)
                    return;

                // Check if variable is untyped
                var symbol = _scopes?.Lookup(varName);
                if (symbol != null && !string.IsNullOrEmpty(symbol.TypeName))
                    return;

                var methodName = memberOp.Identifier?.Sequence;
                if (string.IsNullOrEmpty(methodName))
                    return;

                EnsureDuckType(varName).RequiredMethods.Add(methodName);
            }
        }

        private string GetRootVariableName(GDExpression expr)
        {
            while (expr is GDMemberOperatorExpression member)
                expr = member.CallerExpression;
            while (expr is GDIndexerExpression indexer)
                expr = indexer.CallerExpression;

            if (expr is GDIdentifierExpression ident)
                return ident.Identifier?.Sequence;

            return null;
        }

        private GDDuckType EnsureDuckType(string varName)
        {
            if (!_variableDuckTypes.TryGetValue(varName, out var duckType))
            {
                duckType = new GDDuckType();
                _variableDuckTypes[varName] = duckType;
            }
            return duckType;
        }
    }
}
