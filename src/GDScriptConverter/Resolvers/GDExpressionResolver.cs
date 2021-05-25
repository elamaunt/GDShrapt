using System;
using System.Text;

namespace GDScriptConverter
{
    public class GDExpressionResolver : GDNode
    {
        readonly Action<GDExpression> _handler;

       // StringBuilder _sequenceBuilder = new StringBuilder();
        //bool _facedSpaceChar;
        //bool _sequenceScanMode;
        GDExpression _expression;
        //private char _lastHandledChar;
        //string _previousPattern;

       // Stack<GDExpression> _expressionsStack = new Stack<GDExpression>();
        //Stack<GDExpression> _expressionsPostStack = new Stack<GDExpression>();

        //GDExpression Last => _expressionsStack.PeekOrDefault();

        //GDIdentifier _nextIdentifier;

        public GDExpressionResolver(Action<GDExpression> handler)
        {
            _handler = handler;
        }

       /* static readonly string[] _operators = new string[]
            {
                "and",
                "or",
                "is",
                ">",
                ">=",
                "<=",
                "=",
                "==",
                "!",
                "!=",
                "(",
                "/",
                "/=",
                "*",
                "*=",
                "-",
                "-=",
                "+",
                "+=",
                ".",
            };

        public bool SequenceHasPatterns
        {
            get
            {
                var seq = _sequenceBuilder;

                if (seq == null || seq.Length == 0)
                    return true;

                for (int i = 0; i < _operators.Length; i++)
                {
                    var word = _operators[i];

                    if (word.Length < seq.Length)
                        continue;

                    for (int k = 0; k < seq.Length; k++)
                    {
                        if (seq[k] != word[k])
                            goto CONTINUE;
                    }

                    return true;
                    CONTINUE: continue;
                }

                return false;
            }
        }

        public string MatchedPattern
        {
            get
            {
                var seq = _sequenceBuilder;

                if (seq == null || seq.Length == 0)
                    return null;

                for (int i = 0; i < _operators.Length; i++)
                {
                    var word = _operators[i];

                    if (word.Length != seq.Length)
                        continue;

                    for (int k = 0; k < seq.Length; k++)
                    {
                        if (seq[k] != word[k])
                            goto CONTINUE;
                    }

                    return word;
                    CONTINUE: continue;
                }

                return null;
            }
        }*/

        /*protected override bool CanAppendChar(char c, GDReadingState state)
        {
            _lastHandledChar = c;

            return !IsSpace(c);

            //_facedSpaceChar = IsSpace(c);
            //return !_facedSpaceChar && SequenceHasPatterns;
        }*/

        protected internal override void HandleChar(char c, GDReadingState state)
        {
            //var pattern = MatchedPattern;

            //if (pattern != null)
            //    return;

            if (IsSpace(c))
                return;

            if (c == ',' || c == ')')
            {
                CompleteExpression(state);
                state.HandleChar(c);
                return;
            }

            if (_expression == null)
            {
                if (c == '(')
                {
                    PushAndSave(state, new GDBracketExpression());
                    return;
                }

                if (c == '\"')
                {
                    PushAndSave(state, new GDStringExpression());
                    return;
                }

                if (char.IsDigit(c))
                {
                    PushAndSave(state, new GDNumberExpression());
                    state.HandleChar(c);
                    return;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    PushAndSave(state, new GDIdentifierExpression());
                    state.HandleChar(c);
                    return;
                }

                if (c == '.')
                {
                    PushAndSave(state, new GDMemberOperatorExpression());
                    return;
                }

                if (c == '-' /*||
                    c == '!'*/)
                {
                    PushAndSave(state, new GDSingleOperatorExpression());
                    state.HandleChar(c);
                    return;
                }
            }
            else
            {
                if (c == '(')
                {
                    PushAndSave(state, new GDCallExression()
                    {
                        CallerExpression = _expression
                    });
                    state.HandleChar(c);
                    return;
                }

                if (c == '/' ||
                    c == '*' ||
                    c == '+' ||
                    c == '-' ||
                    c == '!' ||
                    c == '=')
                {
                    if (_expression is GDDualOperatorExression dExpr)
                    {
                    }
                    else
                    {
                        PushAndSave(state, new GDDualOperatorExression()
                        {
                            LeftExpression = _expression
                        });
                        state.HandleChar(c);
                    }
                    return;
                }

                /*if (char.IsDigit(c))
                {

                    return;
                }

                if ()*/

                /*PushAndSave(state, new GDDualOperatorExression()
                {
                    LeftExpression = _expression
                });
                state.HandleChar(c);*/


            }

            /*var pattern = MatchedPattern;

            switch (pattern)
            {
                case "and":
                    break;
                case "or":
                    break;
                case "is":
                    break;
                case ">":
                    break;
                case ">=":
                    break;
                case "<=":
                    break;
                case "=":
                    break;
                case "==":
                    break;
                case "!":
                    break;
                case "!=":
                    break;
                case "(":

                    break;
                case "/":
                    break;
                case "/=":
                    break;
                case "*":
                    break;
                case "*=":
                    break;
                case "-":
                    break;
                case "-=":
                    break;
                case "+":
                    break;
                case "+=":
                    break;
                case ".":
                    break;
                default:
                    break;
            }*/

           // _sequenceBuilder.Append(c);
           // _previousPattern = MatchedPattern;
        }

        //protected internal override void HandleChar(char c, GDReadingState state)
        //{
        /* if(_sequenceScanMode)
         {
             base.HandleChar(c, state);

             switch (MatchedPattern)
             {
                 case "and":
                     break;
                 case "or":
                     break;
                 case "is":
                     break;
                 default:
                     return;
             }
         }

         if (IsSpace(c))
             return;

         if (char.IsDigit(c))
         {
             state.PushNode(new GDNumberExpression());
             state.HandleChar(c);
             return;
         }

         switch (c)
         {
             case '.':
                 PushAndSave(state, new GDMemberOperatorExpression()
                 { 
                     CallerExpression = _expression
                 });
                 return;
             case '"':
                 PushAndSave(state, new GDStringExpression());
                 return;
             case '>':
             case '<':
             case '=':
             case '-':
             case '+':
             case '/':
             case '*':
             case '!':
                 PushAndSave(state, new GDOperatorExression()
                 {
                     LeftExpression = _expression
                 });
                 state.HandleChar(c);
                 return;
             case '(':
                 switch (_expression)
                 {
                     case GDIdentifierExpression expr:
                         {
                             PushAndSave(state, new GDCallExression() { CallerExpression = expr });
                             return;
                         }
                     case GDMemberOperatorExpression expr2:
                         {
                             PushAndSave(state, new GDCallExression() { CallerExpression = expr2 });
                             return;
                         }
                     default:
                         _expression = new GDBracketExpression();
                         return;
                 }
             case ')':
                 CompleteExpression(state);
                 state.HandleChar(c);
                 return;
             default:
                 break;
         }

         if (_expression != null)
         {
             _sequenceScanMode = true;
         }
         else
         {
             PushAndSave(state, new GDIdentifierExpression());
             state.HandleChar(c);
         }
       */
        // TODO: another expressions
        //}

        protected internal override void HandleLineFinish(GDReadingState state)
        {
            CompleteExpression(state);
        }

        /*protected override void CompleteSequence(GDReadingState state)
        {
            _sequenceScanMode = false;
        }*/

        private void CompleteExpression(GDReadingState state)
        {
            var last = _expression;

            if (last != null)
                _handler(last);

            state.PopNode();
        }

        private void PushAndSave(GDReadingState state, GDExpression node)
        {
            /*var e = _expression;

            if (e != null)
                node = node.CombineLeft(e);
            */
            state.PushNode(_expression = node);



            //state.PushNode(_expressionsStack.PushAndPeek(node));
        }

        /*private void PushPost(GDReadingState state, GDExpression node)
        {
            state.PushNode(_expressionsPostStack.PushAndPeek(node));
        }*/
    }
}