﻿namespace GDShrapt.Reader
{
    public sealed class GDDualOperatorExression : GDExpression, IExpressionsReceiver, IDualOperatorReceiver
    {
        public override int Priority => GDHelper.GetOperatorPriority(OperatorType);
        public override GDAssociationOrderType AssociationOrder => GDHelper.GetOperatorAssociationOrder(OperatorType);

        enum State
        {
            LeftExpression,
            DualOperator,
            RightExpression,
            Completed
        }

        public GDExpression LeftExpression 
        {
            get => _form.Token0;
            set => _form.Token0 = value;
        }

        public GDDualOperator Operator
        {
            get => _form.Token1;
            set => _form.Token1 = value;
        }

        public GDDualOperatorType OperatorType
        {
            get => _form.Token1 == null ? GDDualOperatorType.Null : _form.Token1.OperatorType;
        }

        public GDExpression RightExpression
        {
            get => _form.Token2;
            set => _form.Token2 = value;
        }

        readonly GDTokensForm<State, GDExpression, GDDualOperator, GDExpression> _form = new GDTokensForm<State, GDExpression, GDDualOperator, GDExpression>();
        internal override GDTokensForm Form => _form;

        internal override void HandleChar(char c, GDReadingState state)
        {
            if (IsSpace(c))
            {
                _form.AddBeforeActiveToken(state.Push(new GDSpace()));
                state.PassChar(c);
                return;
            }

            switch (_form.State)
            {
                case State.LeftExpression:
                    state.Push(new GDExpressionResolver(this));
                    state.PassChar(c);
                    break;
                case State.DualOperator:
                    // Indicates that it isn't a normal expression. The parent should handle the state.
                    if (LeftExpression == null)
                    {
                        state.Pop();
                        state.PassChar(c);
                        return;
                    }

                    state.Push(new GDDualOperatorResolver(this));
                    state.PassChar(c);
                    break;
                case State.RightExpression:
                    state.Push(new GDExpressionResolver(this));
                    state.PassChar(c);
                    break;
                case State.Completed:
                    state.Pop();
                    state.PassChar(c);
                    break;
                default:
                    break;
            }
        }

        internal override void HandleLineFinish(GDReadingState state)
        {
            switch (_form.State)
            {
                case State.LeftExpression:
                case State.DualOperator:
                case State.RightExpression:
                    _form.AddBeforeActiveToken(state.Push(new GDNewLine()));
                    break;
                case State.Completed:
                    state.Pop();
                    state.PassLineFinish();
                    break;
                default:
                    break;
            }
            
        }


        /// <summary>
        /// Rebuilds current node if another inner node has higher priority.
        /// </summary>
        /// <returns>Same node if nothing changed or a new node which now the root</returns>
        protected override GDExpression PriorityRebuildingPass()
        {
            if (IsHigherPriorityThan(LeftExpression, GDSideType.Left))
            {
                var previous = LeftExpression;
                // Remove expression to break cycle
                LeftExpression = null;
                LeftExpression = previous.SwapRight(this).RebuildRootOfPriorityIfNeeded();
                return previous;
            }

            if (IsHigherPriorityThan(RightExpression, GDSideType.Right))
            {
                var previous = RightExpression;
                // Remove expression to break cycle
                RightExpression = null;
                RightExpression = previous.SwapLeft(this).RebuildRootOfPriorityIfNeeded();
                return previous;
            }

            return this;
        }

        public override void RebuildBranchesOfPriorityIfNeeded()
        {
            LeftExpression = LeftExpression.RebuildRootOfPriorityIfNeeded();
            RightExpression = RightExpression.RebuildRootOfPriorityIfNeeded();
        }

        public override GDExpression SwapLeft(GDExpression expression)
        {
            var left = LeftExpression;
            LeftExpression = expression;
            return left;
        }

        public override GDExpression SwapRight(GDExpression expression)
        {
            var right = RightExpression;
            RightExpression = expression;
            return right;
        }

        public override string ToString()
        {
            return $"{LeftExpression} {OperatorType.Print()} {RightExpression}";
        }

        void IExpressionsReceiver.HandleReceivedToken(GDExpression token)
        {
            if (_form.State == State.LeftExpression)
            {
                LeftExpression = token;
                _form.State = State.DualOperator;
                return;
            }

            if (_form.State == State.RightExpression)
            {
                RightExpression = token;
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IExpressionsReceiver.HandleReceivedExpressionSkip()
        {
            if (_form.State == State.LeftExpression)
            {
                _form.State = State.DualOperator;
                return;
            }

            if (_form.State == State.RightExpression)
            {
                _form.State = State.Completed;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IDualOperatorReceiver.HandleReceivedToken(GDDualOperator token)
        {
            if (_form.State == State.DualOperator)
            {
                Operator = token;
                _form.State = State.RightExpression;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IDualOperatorReceiver.HandleDualOperatorSkip()
        {
            if (_form.State == State.DualOperator)
            {
                _form.State = State.RightExpression;
                return;
            }

            throw new GDInvalidReadingStateException();
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDComment token)
        {
            _form.AddBeforeActiveToken(token);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDNewLine token)
        {
            _form.AddBeforeActiveToken(token);
        }

        void IStyleTokensReceiver.HandleReceivedToken(GDSpace token)
        {
            _form.AddBeforeActiveToken(token);
        }

        void ITokenReceiver.HandleReceivedToken(GDInvalidToken token)
        {
             _form.AddBeforeActiveToken(token);
        }
    }
}