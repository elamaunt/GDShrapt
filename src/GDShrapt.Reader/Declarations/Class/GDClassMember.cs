using System.Collections.Generic;

namespace GDShrapt.Reader
{
    public abstract class GDClassMember : GDIntendedNode
    {
        internal GDClassMember(int intendation)
            : base(intendation)
        {
        }

        internal GDClassMember()
            : base()
        {
        }

        public IEnumerable<GDCustomAttribute> AttributesDeclaredBefore
        {
            get 
            {
                var @class = ClassDeclaration;

                if (@class == null)
                    yield break;

                bool foundThis = false;

                foreach (var item in @class.Members.NodesReversed)
                {
                    if (!foundThis)
                    {
                        if (ReferenceEquals(item, this))
                            foundThis = true;
                    }
                    else
                    {
                        if (item is GDCustomAttribute attr)
                            yield return attr;
                        else
                            yield break;

                    }
                }
            }
        }

        public IEnumerable<GDCustomAttribute> AttributesDeclaredBeforeFromStartOfTheClass
        {
            get
            {
                var @class = ClassDeclaration;

                if (@class == null)
                    yield break;

                bool foundThis = false;

                foreach (var item in @class.Members.NodesReversed)
                {
                    if (!foundThis)
                    {
                        if (ReferenceEquals(item, this))
                            foundThis = true;
                    }
                    else
                    {
                        if (item is GDCustomAttribute attr)
                            yield return attr;
                    }
                }
            }
        }
    }
}
