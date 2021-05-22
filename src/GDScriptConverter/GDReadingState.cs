using System.Collections.Generic;

namespace GDScriptConverter
{
    public class GDReadingState
    {
        readonly Stack<GDNode> _nodesStack = new Stack<GDNode>();


        public int LineIntendation;
        public bool LineIntendationEnded;

        public GDProject Project { get; }
        public GDTypeDeclaration Type { get; private set; }

        GDNode CurrentNode => _nodesStack.Peek();

        public GDReadingState(GDProject project)
        {
            Project = project;
        }

        public void LineStarted()
        {
            LineIntendation = 0;
            LineIntendationEnded = false;
        }

        public void FileStarted()
        {
            Type = PushNode(new GDTypeDeclaration());
            Project.Types.Add(Type);

        }

        public void FileFinished()
        {
            
        }

        public void LineFinished()
        {
            CurrentNode.HandleLineFinish(this);
        }

        public void HandleChar(char c)
        {
            if (c == '#')
            {
                CurrentNode.HandleSharpChar(this);
                return;
            }


            if (!LineIntendationEnded)
            {
                if (c == '\t')
                {
                    LineIntendation++;
                    return;
                }
                else
                {
                    LineIntendationEnded = true;
                }
            }

            CurrentNode.HandleChar(c, this);
        }

        public T PushNode<T>(T node)
            where T : GDNode
        {
            _nodesStack.Push(node);
            return node;
        }

        public GDNode PopNode()
        {
            return _nodesStack.Pop();
        }
    }
}