using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GDShrapt.Reader
{

    public class GDTokensForm<STATE, T0> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
        internal GDTokensForm(GDNode owner)
            : base(owner, 1)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
    }

    public class GDTokensForm<STATE, T0, T1> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal GDTokensForm(GDNode owner)
            : base(owner, 2)
        {

        }
        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
    }

    public class GDTokensForm<STATE, T0, T1, T2> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[] 
        {
            typeof(T0),
            typeof(T1),
            typeof(T2)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal GDTokensForm(GDNode owner)
            : base(owner, 3)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
    }

    public class GDTokensForm<STATE, T0, T1, T2, T3> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1),
            typeof(T2),
            typeof(T3)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                case 3: return token is T3;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal GDTokensForm(GDNode owner)
            : base(owner, 4)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => ProtectedSet(value, 3); }
    }

    public class GDTokensForm<STATE, T0, T1, T2, T3, T4> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1),
            typeof(T2),
            typeof(T3),
            typeof(T4)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                case 3: return token is T3;
                case 4: return token is T4;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal GDTokensForm(GDNode owner)
            : base(owner, 5)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => ProtectedSet(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 4);
        public T4 Token4 { get => Get<T4>(4); set => ProtectedSet(value, 4); }
    }

    public class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1),
            typeof(T2),
            typeof(T3),
            typeof(T4),
            typeof(T5)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                case 3: return token is T3;
                case 4: return token is T4;
                case 5: return token is T5;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal GDTokensForm(GDNode owner)
            : base(owner, 6)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => ProtectedSet(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 4);
        public T4 Token4 { get => Get<T4>(4); set => ProtectedSet(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => ProtectedSet(value, 5); }
    }

    public class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1),
            typeof(T2),
            typeof(T3),
            typeof(T4),
            typeof(T5),
            typeof(T6)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                case 3: return token is T3;
                case 4: return token is T4;
                case 5: return token is T5;
                case 6: return token is T6;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal GDTokensForm(GDNode owner)
            : base(owner, 7)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => ProtectedSet(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 4);
        public T4 Token4 { get => Get<T4>(4); set => ProtectedSet(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => ProtectedSet(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => ProtectedSet(value, 6); }
    }

    public class GDTokensForm<STATE, T0,T1,T2,T3,T4,T5,T6,T7> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1),
            typeof(T2),
            typeof(T3),
            typeof(T4),
            typeof(T5),
            typeof(T6),
            typeof(T7)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                case 3: return token is T3;
                case 4: return token is T4;
                case 5: return token is T5;
                case 6: return token is T6;
                case 7: return token is T7;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal GDTokensForm(GDNode owner)
            : base(owner, 8)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => ProtectedSet(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 4);
        public T4 Token4 { get => Get<T4>(4); set => ProtectedSet(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => ProtectedSet(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => ProtectedSet(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => ProtectedSet(value, 7); }
    }

    public class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1),
            typeof(T2),
            typeof(T3),
            typeof(T4),
            typeof(T5),
            typeof(T6),
            typeof(T7),
            typeof(T8)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                case 3: return token is T3;
                case 4: return token is T4;
                case 5: return token is T5;
                case 6: return token is T6;
                case 7: return token is T7;
                case 8: return token is T8;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal GDTokensForm(GDNode owner)
            : base(owner, 9)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => ProtectedSet(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 4);
        public T4 Token4 { get => Get<T4>(4); set => ProtectedSet(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => ProtectedSet(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => ProtectedSet(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => ProtectedSet(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => ProtectedSet(value, 8); }
    }

    public class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
        where T9 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1),
            typeof(T2),
            typeof(T3),
            typeof(T4),
            typeof(T5),
            typeof(T6),
            typeof(T7),
            typeof(T8),
            typeof(T9)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                case 3: return token is T3;
                case 4: return token is T4;
                case 5: return token is T5;
                case 6: return token is T6;
                case 7: return token is T7;
                case 8: return token is T8;
                case 9: return token is T9;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal GDTokensForm(GDNode owner)
            : base(owner, 10)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => ProtectedSet(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 4);
        public T4 Token4 { get => Get<T4>(4); set => ProtectedSet(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => ProtectedSet(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => ProtectedSet(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => ProtectedSet(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => ProtectedSet(value, 8); }
        public void AddBeforeToken9(GDSyntaxToken token) => AddMiddle(token, 9);
        public T9 Token9 { get => Get<T9>(9); set => ProtectedSet(value, 9); }
    }

    public class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
        where T9 : GDSyntaxToken
        where T10 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1),
            typeof(T2),
            typeof(T3),
            typeof(T4),
            typeof(T5),
            typeof(T6),
            typeof(T7),
            typeof(T8),
            typeof(T9),
            typeof(T10)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                case 3: return token is T3;
                case 4: return token is T4;
                case 5: return token is T5;
                case 6: return token is T6;
                case 7: return token is T7;
                case 8: return token is T8;
                case 9: return token is T9;
                case 10: return token is T10;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal GDTokensForm(GDNode owner)
            : base(owner, 11)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => ProtectedSet(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 4);
        public T4 Token4 { get => Get<T4>(4); set => ProtectedSet(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => ProtectedSet(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => ProtectedSet(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => ProtectedSet(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => ProtectedSet(value, 8); }
        public void AddBeforeToken9(GDSyntaxToken token) => AddMiddle(token, 9);
        public T9 Token9 { get => Get<T9>(9); set => ProtectedSet(value, 9); }
        public void AddBeforeToken10(GDSyntaxToken token) => AddMiddle(token, 10);
        public T10 Token10 { get => Get<T10>(10); set => ProtectedSet(value, 10); }
    }

    public class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
        where T9 : GDSyntaxToken
        where T10 : GDSyntaxToken
        where T11 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1),
            typeof(T2),
            typeof(T3),
            typeof(T4),
            typeof(T5),
            typeof(T6),
            typeof(T7),
            typeof(T8),
            typeof(T9),
            typeof(T10),
            typeof(T11)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                case 3: return token is T3;
                case 4: return token is T4;
                case 5: return token is T5;
                case 6: return token is T6;
                case 7: return token is T7;
                case 8: return token is T8;
                case 9: return token is T9;
                case 10: return token is T10;
                case 11: return token is T11;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal GDTokensForm(GDNode owner)
            : base(owner, 12)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => ProtectedSet(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 4);
        public T4 Token4 { get => Get<T4>(4); set => ProtectedSet(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => ProtectedSet(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => ProtectedSet(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => ProtectedSet(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => ProtectedSet(value, 8); }
        public void AddBeforeToken9(GDSyntaxToken token) => AddMiddle(token, 9);
        public T9 Token9 { get => Get<T9>(9); set => ProtectedSet(value, 9); }
        public void AddBeforeToken10(GDSyntaxToken token) => AddMiddle(token, 10);
        public T10 Token10 { get => Get<T10>(10); set => ProtectedSet(value, 10); }
        public void AddBeforeToken11(GDSyntaxToken token) => AddMiddle(token, 11);
        public T11 Token11 { get => Get<T11>(11); set => ProtectedSet(value, 11); }
    }

    public class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
        where T9 : GDSyntaxToken
        where T10 : GDSyntaxToken
        where T11 : GDSyntaxToken
        where T12 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1),
            typeof(T2),
            typeof(T3),
            typeof(T4),
            typeof(T5),
            typeof(T6),
            typeof(T7),
            typeof(T8),
            typeof(T9),
            typeof(T10),
            typeof(T11),
            typeof(T12)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                case 3: return token is T3;
                case 4: return token is T4;
                case 5: return token is T5;
                case 6: return token is T6;
                case 7: return token is T7;
                case 8: return token is T8;
                case 9: return token is T9;
                case 10: return token is T10;
                case 11: return token is T11;
                case 12: return token is T12;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        internal GDTokensForm(GDNode owner)
            : base(owner, 13)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => ProtectedSet(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 4);
        public T4 Token4 { get => Get<T4>(4); set => ProtectedSet(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => ProtectedSet(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => ProtectedSet(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => ProtectedSet(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => ProtectedSet(value, 8); }
        public void AddBeforeToken9(GDSyntaxToken token) => AddMiddle(token, 9);
        public T9 Token9 { get => Get<T9>(9); set => ProtectedSet(value, 9); }
        public void AddBeforeToken10(GDSyntaxToken token) => AddMiddle(token, 10);
        public T10 Token10 { get => Get<T10>(10); set => ProtectedSet(value, 10); }
        public void AddBeforeToken11(GDSyntaxToken token) => AddMiddle(token, 11);
        public T11 Token11 { get => Get<T11>(11); set => ProtectedSet(value, 11); }
        public void AddBeforeToken12(GDSyntaxToken token) => AddMiddle(token, 12);
        public T12 Token12 { get => Get<T12>(12); set => ProtectedSet(value, 12); }
    }

    public class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
        where T9 : GDSyntaxToken
        where T10 : GDSyntaxToken
        where T11 : GDSyntaxToken
        where T12 : GDSyntaxToken
        where T13 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1),
            typeof(T2),
            typeof(T3),
            typeof(T4),
            typeof(T5),
            typeof(T6),
            typeof(T7),
            typeof(T8),
            typeof(T9),
            typeof(T10),
            typeof(T11),
            typeof(T12),
            typeof(T13)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                case 3: return token is T3;
                case 4: return token is T4;
                case 5: return token is T5;
                case 6: return token is T6;
                case 7: return token is T7;
                case 8: return token is T8;
                case 9: return token is T9;
                case 10: return token is T10;
                case 11: return token is T11;
                case 12: return token is T12;
                case 13: return token is T13;
                default:
                    throw new IndexOutOfRangeException();
            }
        }


        internal GDTokensForm(GDNode owner)
            : base(owner, 14)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => ProtectedSet(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 4);
        public T4 Token4 { get => Get<T4>(4); set => ProtectedSet(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => ProtectedSet(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => ProtectedSet(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => ProtectedSet(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => ProtectedSet(value, 8); }
        public void AddBeforeToken9(GDSyntaxToken token) => AddMiddle(token, 9);
        public T9 Token9 { get => Get<T9>(9); set => ProtectedSet(value, 9); }
        public void AddBeforeToken10(GDSyntaxToken token) => AddMiddle(token, 10);
        public T10 Token10 { get => Get<T10>(10); set => ProtectedSet(value, 10); }
        public void AddBeforeToken11(GDSyntaxToken token) => AddMiddle(token, 11);
        public T11 Token11 { get => Get<T11>(11); set => ProtectedSet(value, 11); }
        public void AddBeforeToken12(GDSyntaxToken token) => AddMiddle(token, 12);
        public T12 Token12 { get => Get<T12>(12); set => ProtectedSet(value, 12); }
        public void AddBeforeToken13(GDSyntaxToken token) => AddMiddle(token, 13);
        public T13 Token13 { get => Get<T13>(13); set => ProtectedSet(value, 13); }
    }

    public class GDTokensForm<STATE, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : GDTokensForm<STATE>
        where STATE : struct, System.Enum
        where T0 : GDSyntaxToken
        where T1 : GDSyntaxToken
        where T2 : GDSyntaxToken
        where T3 : GDSyntaxToken
        where T4 : GDSyntaxToken
        where T5 : GDSyntaxToken
        where T6 : GDSyntaxToken
        where T7 : GDSyntaxToken
        where T8 : GDSyntaxToken
        where T9 : GDSyntaxToken
        where T10 : GDSyntaxToken
        where T11 : GDSyntaxToken
        where T12 : GDSyntaxToken
        where T13 : GDSyntaxToken
        where T14 : GDSyntaxToken
    {
        static Type[] GenericTypes = new Type[]
        {
            typeof(T0),
            typeof(T1),
            typeof(T2),
            typeof(T3),
            typeof(T4),
            typeof(T5),
            typeof(T6),
            typeof(T7),
            typeof(T8),
            typeof(T9),
            typeof(T10),
            typeof(T11),
            typeof(T12),
            typeof(T13),
            typeof(T14)
        };

        public override Type[] Types => GenericTypes;

        public override bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            switch (statePoint)
            {
                case 0: return token is T0;
                case 1: return token is T1;
                case 2: return token is T2;
                case 3: return token is T3;
                case 4: return token is T4;
                case 5: return token is T5;
                case 6: return token is T6;
                case 7: return token is T7;
                case 8: return token is T8;
                case 9: return token is T9;
                case 10: return token is T10;
                case 11: return token is T11;
                case 12: return token is T12;
                case 13: return token is T13;
                case 14: return token is T14;
                default:
                    throw new IndexOutOfRangeException();
            }
        }


        internal GDTokensForm(GDNode owner)
            : base(owner, 15)
        {

        }

        public void AddBeforeToken0(GDSyntaxToken token) => AddMiddle(token, 0);
        public T0 Token0 { get => Get<T0>(0); set => ProtectedSet(value, 0); }
        public void AddBeforeToken1(GDSyntaxToken token) => AddMiddle(token, 1);
        public T1 Token1 { get => Get<T1>(1); set => ProtectedSet(value, 1); }
        public void AddBeforeToken2(GDSyntaxToken token) => AddMiddle(token, 2);
        public T2 Token2 { get => Get<T2>(2); set => ProtectedSet(value, 2); }
        public void AddBeforeToken3(GDSyntaxToken token) => AddMiddle(token, 3);
        public T3 Token3 { get => Get<T3>(3); set => ProtectedSet(value, 3); }
        public void AddBeforeToken4(GDSyntaxToken token) => AddMiddle(token, 4);
        public T4 Token4 { get => Get<T4>(4); set => ProtectedSet(value, 4); }
        public void AddBeforeToken5(GDSyntaxToken token) => AddMiddle(token, 5);
        public T5 Token5 { get => Get<T5>(5); set => ProtectedSet(value, 5); }
        public void AddBeforeToken6(GDSyntaxToken token) => AddMiddle(token, 6);
        public T6 Token6 { get => Get<T6>(6); set => ProtectedSet(value, 6); }
        public void AddBeforeToken7(GDSyntaxToken token) => AddMiddle(token, 7);
        public T7 Token7 { get => Get<T7>(7); set => ProtectedSet(value, 7); }
        public void AddBeforeToken8(GDSyntaxToken token) => AddMiddle(token, 8);
        public T8 Token8 { get => Get<T8>(8); set => ProtectedSet(value, 8); }
        public void AddBeforeToken9(GDSyntaxToken token) => AddMiddle(token, 9);
        public T9 Token9 { get => Get<T9>(9); set => ProtectedSet(value, 9); }
        public void AddBeforeToken10(GDSyntaxToken token) => AddMiddle(token, 10);
        public T10 Token10 { get => Get<T10>(10); set => ProtectedSet(value, 10); }
        public void AddBeforeToken11(GDSyntaxToken token) => AddMiddle(token, 11);
        public T11 Token11 { get => Get<T11>(11); set => ProtectedSet(value, 11); }
        public void AddBeforeToken12(GDSyntaxToken token) => AddMiddle(token, 12);
        public T12 Token12 { get => Get<T12>(12); set => ProtectedSet(value, 12); }
        public void AddBeforeToken13(GDSyntaxToken token) => AddMiddle(token, 13);
        public T13 Token13 { get => Get<T13>(13); set => ProtectedSet(value, 13); }
        public void AddBeforeToken14(GDSyntaxToken token) => AddMiddle(token, 14);
        public T14 Token14 { get => Get<T14>(14); set => ProtectedSet(value, 14); }
    }
    public abstract class GDTokensForm<STATE> : GDTokensForm
       where STATE : struct, System.Enum
    {
        internal GDTokensForm(GDNode owner, int size) 
            : base(owner, size)
        {
        }

        public STATE State
        {
            get => (STATE)(object)StateIndex;
            set => StateIndex = Convert.ToInt32(value);
        }

        public bool IsOrLowerState(STATE state) => StateIndex <= Convert.ToInt32(state);
    }

    /// <summary>
    /// Basic class which contains form (like a skeleton) of the specific node
    /// </summary>
    public abstract class GDTokensForm : ICollection<GDSyntaxToken>
    {
        protected LinkedList<GDSyntaxToken> _list;
        protected List<LinkedListNode<GDSyntaxToken>> _statePoints;

        // Freeze mechanism for thread-safe iteration
        protected bool _isFrozen;
        protected GDSyntaxToken[] _frozenSnapshot;
        protected GDSyntaxToken[] _frozenSnapshotReversed;
        protected Dictionary<GDSyntaxToken, int> _frozenTokenIndex;

        public int Count => _list.Count;
        public bool IsReadOnly => false;
        public bool IsFrozen => _isFrozen;

        public int StateIndex { get; set; }
        public bool IsCompleted => StateIndex == _statePoints.Count;

        protected readonly GDNode _owner;
        readonly int _initialSize;

        public abstract Type[] Types { get; }

        public virtual bool IsTokenAppropriateForPoint(GDSyntaxToken token, int statePoint)
        {
            return Types[statePoint].IsAssignableFrom(token.GetType());
        }

        internal GDTokensForm(GDNode owner, int size)
        {
            _owner = owner;

            _initialSize = size;
            _list = new LinkedList<GDSyntaxToken>();
            _statePoints = new List<LinkedListNode<GDSyntaxToken>>(size);

            for (int i = 0; i < size; i++)
                _statePoints.Add(_list.AddLast(default(GDSyntaxToken)));
        }

        internal GDTokensForm(GDNode owner)
        {
            _owner = owner;

            _initialSize = 0;
            _list = new LinkedList<GDSyntaxToken>();
            _statePoints = new List<LinkedListNode<GDSyntaxToken>>();
        }

        public int TokensCount => _list.Count - _statePoints.Count(x => x.Value != null);
        public bool HasTokens => _list.Count > _statePoints.Count || _statePoints.Any(x => x.Value != null);

        /// <summary>
        /// Makes the form immutable and creates a cached snapshot for thread-safe iteration.
        /// Recursively freezes all child nodes.
        /// </summary>
        public void Freeze()
        {
            if (_isFrozen)
                return;

            // Create snapshot before freezing
            _frozenSnapshot = BuildSnapshot();
            _frozenSnapshotReversed = BuildSnapshotReversed();

            // Build position index for fast O(1) lookup
            _frozenTokenIndex = new Dictionary<GDSyntaxToken, int>(_frozenSnapshot.Length);
            for (int i = 0; i < _frozenSnapshot.Length; i++)
            {
                _frozenTokenIndex[_frozenSnapshot[i]] = i;
            }

            _isFrozen = true;

            // Recursively freeze child nodes
            foreach (var token in _frozenSnapshot)
            {
                if (token is GDNode node)
                    node.Form.Freeze();
            }
        }

        private GDSyntaxToken[] BuildSnapshot()
        {
            var result = new List<GDSyntaxToken>();
            var node = _list.First;
            while (node != null)
            {
                if (node.Value != null)
                    result.Add(node.Value);
                node = node.Next;
            }
            return result.ToArray();
        }

        private GDSyntaxToken[] BuildSnapshotReversed()
        {
            var result = new List<GDSyntaxToken>();
            var node = _list.Last;
            while (node != null)
            {
                if (node.Value != null)
                    result.Add(node.Value);
                node = node.Previous;
            }
            return result.ToArray();
        }

        protected void ThrowIfFrozen()
        {
            if (_isFrozen)
                throw new InvalidOperationException(
                    "Cannot modify frozen AST node. Call Clone() to create a mutable copy.");
        }

        public void AddBeforeActiveToken(GDSyntaxToken token)
        {
            AddBeforeToken(token, StateIndex);
        }

        public virtual void AddBeforeToken(GDSyntaxToken newToken, int statePointIndex)
        {
            ThrowIfFrozen();
            if (newToken is null)
                throw new System.ArgumentNullException(nameof(newToken));

            if (statePointIndex < _statePoints.Count)
                AddMiddle(newToken, statePointIndex);
            else
                AddToEnd(newToken);
        }

        public virtual void AddBeforeToken(GDSyntaxToken newToken, GDSyntaxToken beforeThisToken)
        {
            ThrowIfFrozen();
            if (newToken is null)
                throw new System.ArgumentNullException(nameof(newToken));
            if (beforeThisToken is null)
                throw new System.ArgumentNullException(nameof(beforeThisToken));

            var node = _list.Find(beforeThisToken);

            if (node == null)
                throw new NullReferenceException("There is no specific token in the form");

            newToken.Parent = _owner;
            _list.AddBefore(node, newToken);
        }

        public virtual void AddAfterToken(GDSyntaxToken newToken, GDSyntaxToken afterThisToken)
        {
            ThrowIfFrozen();
            if (newToken is null)
                throw new System.ArgumentNullException(nameof(newToken));
            if (afterThisToken is null)
                throw new System.ArgumentNullException(nameof(afterThisToken));

            var node = _list.Find(afterThisToken);

            if (node == null)
                throw new NullReferenceException("There is no specific token in the form");

            newToken.Parent = _owner;
            _list.AddAfter(node, newToken);
        }

        void ICollection<GDSyntaxToken>.Add(GDSyntaxToken item) => AddToEnd(item);
        public virtual void AddToEnd(GDSyntaxToken value)
        {
            ThrowIfFrozen();
            if (value is null)
                throw new System.ArgumentNullException(nameof(value));

            value.Parent = _owner;
            _list.AddLast(value);
        }

        protected void AddMiddle(GDSyntaxToken value, int index)
        {
            ThrowIfFrozen();
            if (value is null)
                throw new System.ArgumentNullException(nameof(value));

            value.Parent = _owner;

            if (index >= _statePoints.Count)
            {
                _list.AddLast(value);
            }
            else
            {
                var node = _statePoints[index];
                node.List.AddBefore(node, value);
            }
        }

        public void Set(GDSyntaxToken value, int index)
        {
            ThrowIfFrozen();
            if (value != null && !IsTokenAppropriateForPoint(value, index))
                throw new InvalidCastException($"Unable to set token {value.TypeName} to State point with type {Types[index]}");

            if (index >= _statePoints.Count) // Only for ListForm
                return;

            var node = _statePoints[index];

            if (node.Value == value)
                return;

            if (node.Value != null)
                node.Value.Parent = null;

            if (value != null)
                value.Parent = _owner;

            node.Value = value;
        }

        protected void ProtectedSet(GDSyntaxToken value, int index)
        {
            var node = _statePoints[index];

            // Allow lazy initialization (setting when current is null) even when frozen.
            // This supports the common pattern: get => _form.Token ?? (_form.Token = new List())
            // When frozen, we only block modifications to existing values.
            if (_isFrozen && node.Value != null)
                throw new InvalidOperationException(
                    "Cannot modify frozen AST node. Call Clone() to create a mutable copy.");

            if (node.Value != null)
                node.Value.Parent = null;

            if (value != null)
                value.Parent = _owner;

            node.Value = value;
        }

        /// <summary>
        /// Used only by cloning methods. <see cref="CloneFrom(GDTokensForm)"/>
        /// </summary>
        void SetOrAdd(GDSyntaxToken value, int index)
        {
            if (index >= _statePoints.Count)
            {
                // Only for ListForms
                _statePoints.Add(_list.AddLast(value));

                if (value != null)
                    value.Parent = _owner;
            }
            else
            {
                var node = _statePoints[index];

                if (node.Value == value)
                    return;

                if (node.Value != null)
                    node.Value.Parent = null;

                if (value != null)
                    value.Parent = _owner;

                node.Value = value;
            }
        }

        public T Get<T>(int statePointIndex) where T : GDSyntaxToken => (T)_statePoints[statePointIndex].Value;
        public GDSyntaxToken Get(int index) => _statePoints[index].Value;

        public void SetFormUnsafe(params GDSyntaxToken[] tokens)
        {
            ThrowIfFrozen();
            Clear();

            if (tokens == null || tokens.Length == 0)
                return;

            if (_statePoints.Count == 0) // For List forms
            {
                for (int i = 0; i < tokens.Length; i++)
                {
                    var token = tokens[i];

                    if (token == null || IsTokenAppropriateForPoint(token, StateIndex))
                    {
                        var node = _list.AddLast(token);
                        _statePoints.Add(node);
                        StateIndex++;
                    }
                    else
                    {
                        _list.AddLast(token);
                    }
                }
            }
            else  // For typed forms
            {
                for (int i = 0; i < tokens.Length; i++)
                {
                    var token = tokens[i];

                    if (StateIndex < _statePoints.Count)
                    {
                        if (token == null)
                        {
                            _statePoints[StateIndex++].Value = token;
                        }
                        else
                        {
                            bool inserted = false;
                            for (int index = StateIndex; index < _statePoints.Count; index++)
                            {
                                if (IsTokenAppropriateForPoint(token, index))
                                {
                                    _statePoints[index].Value = token;
                                    StateIndex = index + 1;
                                    inserted = true;
                                    break;
                                }
                            }

                            if (!inserted)
                                _list.AddBefore(_statePoints[StateIndex], token);
                        }
                    }
                    else
                    {
                        if (token == null)
                            throw new GDInvalidStateException("Cant add a null token when the node state is Completed");

                        _list.AddLast(token);
                    }
                }
            }
        }


        public void Clear()
        {
            ThrowIfFrozen();
            StateIndex = 0;
            foreach (var token in _list)
            {
                if (token != null)
                    token.Parent = null;
            }

            _list = new LinkedList<GDSyntaxToken>();
            _statePoints = new List<LinkedListNode<GDSyntaxToken>>(_initialSize);

            if (_initialSize > 0)
                for (int i = 0; i < _initialSize; i++)
                    _statePoints.Add(_list.AddLast((GDSyntaxToken)null));
        }

        public bool Contains(GDSyntaxToken item)
        {
            if (item is null)
                throw new System.ArgumentNullException(nameof(item));

            if (_isFrozen && _frozenTokenIndex != null)
                return _frozenTokenIndex.ContainsKey(item);

            return _list.Contains(item);
        }

        public void CopyTo(GDSyntaxToken[] array, int arrayIndex)
        {
            if (_isFrozen && _frozenSnapshot != null)
            {
                _frozenSnapshot.CopyTo(array, arrayIndex);
                return;
            }

            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(GDSyntaxToken item)
        {
            ThrowIfFrozen();
            if (item is null)
                throw new System.ArgumentNullException(nameof(item));

            for (int i = 0; i < _statePoints.Count; i++)
            {
                if (_statePoints[i].Value == item)
                {
                    _statePoints[i].Value = null;
                    item.Parent = null;
                    return true;
                }
            }

            if (_list.Remove(item))
            {
                item.Parent = null;
                return true;
            }

            return false;
        }

        public IEnumerator<GDSyntaxToken> GetEnumerator()
        {
            // If frozen - return snapshot iterator for thread safety
            if (_isFrozen && _frozenSnapshot != null)
                return ((IEnumerable<GDSyntaxToken>)_frozenSnapshot).GetEnumerator();

            return GetEnumeratorLazy();
        }

        private IEnumerator<GDSyntaxToken> GetEnumeratorLazy()
        {
            var node = _list.First;

            if (node == null)
                yield break;

            do
            {
                if (node.Value != null)
                    yield return node.Value;
                node = node.Next;
            } while (node != null);

            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<GDSyntaxToken> Direct()
        {
            // If frozen - return cached snapshot for thread safety
            if (_isFrozen && _frozenSnapshot != null)
                return _frozenSnapshot;

            return DirectLazy();
        }

        private IEnumerable<GDSyntaxToken> DirectLazy()
        {
            var node = _list.First;

            if (node == null)
                yield break;

            do
            {
                if (node.Value != null)
                    yield return node.Value;
                node = node.Next;
            } while (node != null);
        }

        public IEnumerable<GDSyntaxToken> Reversed()
        {
            // If frozen - return cached snapshot for thread safety
            if (_isFrozen && _frozenSnapshotReversed != null)
                return _frozenSnapshotReversed;

            return ReversedLazy();
        }

        private IEnumerable<GDSyntaxToken> ReversedLazy()
        {
            var node = _list.Last;

            if (node == null)
                yield break;

            do
            {
                if (node.Value != null)
                    yield return node.Value;
                node = node.Previous;
            } while (node != null);
        }

        public IEnumerable<GDSyntaxToken> GetAllTokensAfter(int statePointIndex)
        {
            if (_isFrozen && _frozenSnapshot != null && _frozenTokenIndex != null)
            {
                var token = _statePoints[statePointIndex].Value;
                if (token != null && _frozenTokenIndex.TryGetValue(token, out int index))
                {
                    for (int i = index + 1; i < _frozenSnapshot.Length; i++)
                        yield return _frozenSnapshot[i];
                }
                yield break;
            }

            var node = _statePoints[statePointIndex];

            var next = node.Next;
            while (next != null)
            {
                if (next.Value != null)
                    yield return next.Value;
                next = next.Next;
            }
        }

        public IEnumerable<GDSyntaxToken> GetAllTokensAfter(GDSyntaxToken token)
        {
            if (_isFrozen && _frozenTokenIndex != null && _frozenSnapshot != null)
            {
                if (_frozenTokenIndex.TryGetValue(token, out int index))
                {
                    for (int i = index + 1; i < _frozenSnapshot.Length; i++)
                        yield return _frozenSnapshot[i];
                }
                yield break;
            }

            var node = _list.Find(token);

            if (node == null)
                throw new NullReferenceException("There is no specific token in the form");

            var next = node.Next;
            while (next != null)
            {
                if (next.Value != null)
                    yield return next.Value;
                next = next.Next;
            }
        }

        public IEnumerable<GDSyntaxToken> GetTokensBefore(int statePointIndex)
        {
            if (_isFrozen && _frozenSnapshot != null && _frozenTokenIndex != null)
            {
                var token = _statePoints[statePointIndex].Value;
                if (token != null && _frozenTokenIndex.TryGetValue(token, out int index))
                {
                    for (int i = index - 1; i >= 0; i--)
                        yield return _frozenSnapshot[i];
                }
                yield break;
            }

            var node = _statePoints[statePointIndex];

            var previous = node.Previous;
            while (previous != null)
            {
                if (previous.Value != null)
                    yield return previous.Value;
                previous = previous.Previous;
            }
        }

        public IEnumerable<GDSyntaxToken> GetTokensBefore(GDSyntaxToken token)
        {
            if (_isFrozen && _frozenTokenIndex != null && _frozenSnapshot != null)
            {
                if (_frozenTokenIndex.TryGetValue(token, out int index))
                {
                    for (int i = index - 1; i >= 0; i--)
                        yield return _frozenSnapshot[i];
                }
                yield break;
            }

            var node = _list.Find(token);

            if (node == null)
                throw new NullReferenceException("There is no specific token in the form");

            var previous = node.Previous;
            while (previous != null)
            {
                if (previous.Value != null)
                    yield return previous.Value;
                previous = previous.Previous;
            }
        }

        public int CountTokensBetween(int statePointStartIndex, int statePointEndIndex)
        {
            var s = _statePoints[statePointStartIndex];
            var e = _statePoints[statePointEndIndex];

            int counter = 0;
            var next = s.Next;

            while (next != e)
            {
                counter++;
                next = next.Next;
            }

            return counter;
        }

        /// <summary>
        /// Main nodes cloning method. Current form must be empty
        /// </summary>
        /// <param name="form">The form to be cloned</param>
        internal void CloneFrom(GDTokensForm form)
        {
            if ((_initialSize != 0 || form._initialSize != 0) && _initialSize != form._initialSize)
                throw new InvalidOperationException("Forms must have same size or zero");

            if (StateIndex > 0)
                throw new InvalidOperationException("The form must be at initial state");

            if (form._list.Count == 0)
                return;

            var node = form._list.First;
            // FIX: Use ElementAtOrDefault to handle ListForms with 0 initial state points
            var point = form._statePoints.Count > StateIndex
                ? form._statePoints[StateIndex]
                : null;

            while (node != null)
            {
                if (point != null && point == node)
                {
                    SetOrAdd(node.Value?.Clone(), StateIndex++);
                    point = form._statePoints.ElementAtOrDefault(StateIndex);
                }
                else
                {
                    var clone = node.Value?.Clone();
                    AddBeforeActiveToken(clone);
                }

                node = node.Next;
            }
        }

        public T PreviousBefore<T>(GDSyntaxToken token)
            where T : GDSyntaxToken
        {
            if (_isFrozen && _frozenTokenIndex != null && _frozenSnapshot != null)
            {
                if (_frozenTokenIndex.TryGetValue(token, out int index))
                {
                    for (int i = index - 1; i >= 0; i--)
                    {
                        if (_frozenSnapshot[i] is T result)
                            return result;
                    }
                }
                return default;
            }

            var node = _list.Find(token);

            if (node == null)
                throw new NullReferenceException("There is no specific token in the form");

            do
            {
                node = node.Previous;
                if (node?.Value is T value)
                    return value;
            }
            while (node != null);

            return null;
        }

        public GDSyntaxToken PreviousTokenBefore(GDSyntaxToken token)
        {
            if (_isFrozen && _frozenTokenIndex != null && _frozenSnapshot != null)
            {
                if (_frozenTokenIndex.TryGetValue(token, out int index))
                {
                    if (index - 1 >= 0)
                        return _frozenSnapshot[index - 1];
                }
                return null;
            }

            var node = _list.Find(token);

            if (node == null)
                throw new NullReferenceException("There is no specific token in the form");

            do
            {
                node = node.Previous;
                if (node?.Value != null)
                    return node.Value;
            }
            while (node != null);

            return null;
        }

        public GDSyntaxToken NextTokenAfter(GDSyntaxToken token)
        {
            if (_isFrozen && _frozenTokenIndex != null && _frozenSnapshot != null)
            {
                if (_frozenTokenIndex.TryGetValue(token, out int index))
                {
                    if (index + 1 < _frozenSnapshot.Length)
                        return _frozenSnapshot[index + 1];
                }
                return null;
            }

            var node = _list.Find(token);

            if (node == null)
                throw new NullReferenceException("There is no specific token in the form");

            do
            {
                node = node.Next;
                if (node?.Value != null)
                    return node.Value;
            }
            while (node != null);

            return null;
        }

        public T NextAfter<T>(GDSyntaxToken token)
            where T : GDSyntaxToken
        {
            if (_isFrozen && _frozenTokenIndex != null && _frozenSnapshot != null)
            {
                if (_frozenTokenIndex.TryGetValue(token, out int index))
                {
                    for (int i = index + 1; i < _frozenSnapshot.Length; i++)
                    {
                        if (_frozenSnapshot[i] is T result)
                            return result;
                    }
                }
                return default;
            }

            var node = _list.Find(token);

            if (node == null)
                throw new NullReferenceException("There is no specific token in the form");

            do
            {
                node = node.Next;
                if (node?.Value is T value)
                    return value;
            }
            while (node != null);

            return null;
        }

        public GDSyntaxToken FirstToken
        {
            get
            {
                if (_isFrozen && _frozenSnapshot != null)
                {
                    return _frozenSnapshot.Length > 0 ? _frozenSnapshot[0] : null;
                }

                var node = _list.First;

                if (node == null)
                    return null;

                if (node.Value != null)
                    return node.Value;

                do
                {
                    node = node.Next;
                    if (node?.Value != null)
                        return node.Value;
                }
                while (node != null);

                return null;
            }
        }

        public GDSyntaxToken LastToken
        {
            get
            {
                if (_isFrozen && _frozenSnapshotReversed != null)
                {
                    return _frozenSnapshotReversed.Length > 0 ? _frozenSnapshotReversed[0] : null;
                }

                var node = _list.Last;

                if (node == null)
                    return null;

                if (node.Value != null)
                    return node.Value;

                do
                {
                    node = node.Previous;
                    if (node?.Value != null)
                        return node.Value;
                }
                while (node != null);

                return null;
            }
        }

        public GDNode FirstNode => FindFirst<GDNode>();

        private T FindFirst<T>()
            where T : GDSyntaxToken
        {
            if (_isFrozen && _frozenSnapshot != null)
            {
                foreach (var token in _frozenSnapshot)
                {
                    if (token is T result)
                        return result;
                }
                return default;
            }

            var node = _list.First;

            if (node == null)
                return null;

            if (node.Value is T value)
                return value;

            do
            {
                node = node.Next;
                if (node?.Value is T v)
                    return v;
            }
            while (node != null);

            return null;
        }

        public GDNode LastNode => FindLast<GDNode>();

        private T FindLast<T>()
            where T : GDSyntaxToken
        {
            if (_isFrozen && _frozenSnapshotReversed != null)
            {
                foreach (var token in _frozenSnapshotReversed)
                {
                    if (token is T result)
                        return result;
                }
                return default;
            }

            var node = _list.Last;

            if (node == null)
                return null;

            if (node.Value is T value)
                return value;

            do
            {
                node = node.Previous;
                if (node?.Value is T v)
                    return v;
            }
            while (node != null);

            return null;
        }
    }
}
