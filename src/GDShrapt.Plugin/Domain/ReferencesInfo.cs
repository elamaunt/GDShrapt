using GDShrapt.Reader;
using System.Collections.Generic;

namespace GDShrapt.Plugin;

internal class ReferencesInfo
    {
        List<GDIdentifier> _references;
        public GDIdentifier Origin { get; private set; }
        public string ExternalName { get; private set; }
        public ReferencesInfo(GDIdentifier origin)
        {
            Origin = origin;
        }

        public ReferencesInfo(string externalName)
        {
            ExternalName = externalName;
        }

        public ReferencesInfo AddReference(GDIdentifier reference)
        {
            if (_references == null)
                _references = new List<GDIdentifier>();

            _references.Add(reference);
            return this;
        }


       /* HashSet<string> _types;
        List<GDIdentifier> _returns;


        public bool IsNumber => (_types?.Any(x => x == "int" || x == "float") ?? false) || (_returns?.Any(x => x.IsNumber) ?? false);
        public bool IsString => (_types?.Any(x => x == "String") ?? false) || (_returns?.Any(x => x.IsString) ?? false);
        public bool IsBool => (_types?.Any(x => x == "bool") ?? false) || (_returns?.Any(x => x.IsBool) ?? false);
        public bool IsNull => (_types?.Any(x => x == "null") ?? false) || (_returns?.Any(x => x.IsNull) ?? false);
        public bool IsVoid => (_types?.Any(x => x == "void") ?? false) || (_returns?.Any(x => x.IsVoid) ?? false);
        public bool IsUndefined => (_types?.Any(x => x == "undefined") ?? false) || (_returns?.Any(x => x.IsUndefined) ?? false);

        public ReferencesInfo(GDIdentifier origin)
        {
            Origin = origin;
        }

        public ReferencesInfo AddType(GDType type) => AddType(type.ToString());
        public ReferencesInfo AddType(GDIdentifier id) => AddType(id.ToString());
        public ReferencesInfo AddType(string type)
        {
            if (_types == null)
                _types = new HashSet<string>();

            _types.Add(type);
            return this;
        }

        public ReferencesInfo AddAssinableType(ReferencesInfo info)
        {
            if (_returns == null)
                _returns = new List<ReferencesInfo>();

            _returns.Add(info);
            return this;
        }

        public static implicit operator ReferencesInfo(GDIdentifier origin)
        {
            return new ReferencesInfo(origin);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            if (_types != null && _types.Count > 0)
                foreach (var item in _types)
                {
                    if (builder.Length > 0)
                        builder.Append(", ");
                    builder.Append(item);
                }

            if (_returns != null && _returns.Count > 0)
                foreach (var item in _returns)
                {
                    if (builder.Length > 0)
                        builder.Append(", ");
                    builder.Append($"{{{item}}}");
                }

            return builder.ToString();
        }*/
}