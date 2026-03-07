namespace GDShrapt.Semantics;

public enum GDTypeConflictKind
{
    Widening,
    IncompatibleAssignment,
    UnreachableType,
    PotentialNull,
    CollisionLayerMismatch,
    RemovedNodeAccess
}
