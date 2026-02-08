using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GDShrapt.Semantics.ComponentTests;

/// <summary>
/// Unit tests for GDContainerUsageCollector class.
/// </summary>
[TestClass]
public class ContainerUsageCollectorTests
{
    #region Container Profile Tests

    [TestMethod]
    public void Profile_ComputeInferredType_Array_SingleType()
    {
        // Arrange
        var profile = new GDContainerUsageProfile("arr") { IsDictionary = false };
        profile.ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.Append,
            InferredType = GDSemanticType.FromRuntimeTypeName("int"),
            IsHighConfidence = true
        });
        profile.ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.Append,
            InferredType = GDSemanticType.FromRuntimeTypeName("int"),
            IsHighConfidence = true
        });

        // Act
        var inferredType = profile.ComputeInferredType();

        // Assert
        Assert.IsFalse(inferredType.IsDictionary);
        Assert.IsTrue(inferredType.ElementUnionType.IsSingleType);
        Assert.AreEqual("int", inferredType.EffectiveElementType.DisplayName);
    }

    [TestMethod]
    public void Profile_ComputeInferredType_Array_MultipleTypes()
    {
        // Arrange
        var profile = new GDContainerUsageProfile("arr") { IsDictionary = false };
        profile.ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.Append,
            InferredType = GDSemanticType.FromRuntimeTypeName("int"),
            IsHighConfidence = true
        });
        profile.ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.Append,
            InferredType = GDSemanticType.FromRuntimeTypeName("String"),
            IsHighConfidence = true
        });

        // Act
        var inferredType = profile.ComputeInferredType();

        // Assert
        Assert.IsFalse(inferredType.IsDictionary);
        Assert.IsTrue(inferredType.ElementUnionType.IsUnion);
        Assert.AreEqual(2, inferredType.ElementUnionType.Types.Count);
        Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("int")));
        Assert.IsTrue(inferredType.ElementUnionType.Types.Contains(GDSemanticType.FromRuntimeTypeName("String")));
    }

    [TestMethod]
    public void Profile_ComputeInferredType_Dictionary_KeyAndValue()
    {
        // Arrange
        var profile = new GDContainerUsageProfile("dict") { IsDictionary = true };
        profile.KeyUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.IndexAssign,
            InferredType = GDSemanticType.FromRuntimeTypeName("String"),
            IsHighConfidence = true
        });
        profile.ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.IndexAssign,
            InferredType = GDSemanticType.FromRuntimeTypeName("int"),
            IsHighConfidence = true
        });

        // Act
        var inferredType = profile.ComputeInferredType();

        // Assert
        Assert.IsTrue(inferredType.IsDictionary);
        Assert.IsNotNull(inferredType.KeyUnionType);
        Assert.AreEqual("String", inferredType.EffectiveKeyType?.DisplayName);
        Assert.AreEqual("int", inferredType.EffectiveElementType.DisplayName);
    }

    [TestMethod]
    public void Profile_ComputeInferredType_Empty_ReturnsVariant()
    {
        // Arrange
        var profile = new GDContainerUsageProfile("arr") { IsDictionary = false };

        // Act
        var inferredType = profile.ComputeInferredType();

        // Assert
        Assert.IsTrue(inferredType.ElementUnionType.IsEmpty);
        Assert.AreEqual("Variant", inferredType.EffectiveElementType.DisplayName);
    }

    [TestMethod]
    public void Profile_ComputeInferredType_IgnoresNullTypes()
    {
        // Arrange
        var profile = new GDContainerUsageProfile("arr") { IsDictionary = false };
        profile.ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.Append,
            InferredType = null,
            IsHighConfidence = false
        });
        profile.ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.Append,
            InferredType = GDSemanticType.FromRuntimeTypeName("int"),
            IsHighConfidence = true
        });

        // Act
        var inferredType = profile.ComputeInferredType();

        // Assert
        Assert.IsTrue(inferredType.ElementUnionType.IsSingleType);
        Assert.AreEqual("int", inferredType.EffectiveElementType.DisplayName);
    }

    #endregion

    #region GDContainerElementType Tests

    [TestMethod]
    public void ContainerElementType_IsHomogeneous_SingleType_ReturnsTrue()
    {
        // Arrange
        var containerType = new GDContainerElementType { IsDictionary = false };
        containerType.ElementUnionType.AddTypeName("int", isHighConfidence: true);

        // Assert
        Assert.IsTrue(containerType.IsHomogeneous);
    }

    [TestMethod]
    public void ContainerElementType_IsHomogeneous_MultipleTypes_ReturnsFalse()
    {
        // Arrange
        var containerType = new GDContainerElementType { IsDictionary = false };
        containerType.ElementUnionType.AddTypeName("int", isHighConfidence: true);
        containerType.ElementUnionType.AddTypeName("String", isHighConfidence: true);

        // Assert
        Assert.IsFalse(containerType.IsHomogeneous);
    }

    [TestMethod]
    public void ContainerElementType_EffectiveKeyType_Null_ForArray()
    {
        // Arrange
        var containerType = new GDContainerElementType { IsDictionary = false };
        containerType.ElementUnionType.AddTypeName("int", isHighConfidence: true);

        // Assert
        Assert.IsNull(containerType.KeyUnionType);
        Assert.IsNull(containerType.EffectiveKeyType);
    }

    [TestMethod]
    public void ContainerElementType_Confidence_DelegatesToElementUnion()
    {
        // Arrange
        var containerType = new GDContainerElementType();
        containerType.ElementUnionType.AddTypeName("int", isHighConfidence: true);

        // Assert
        Assert.IsTrue(containerType.ElementUnionType.AllHighConfidence);
    }

    #endregion

    #region Usage Kind Tests

    [TestMethod]
    public void Profile_ValueUsages_TracksDifferentKinds()
    {
        // Arrange
        var profile = new GDContainerUsageProfile("arr") { IsDictionary = false };
        profile.ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.Initialization,
            InferredType = GDSemanticType.FromRuntimeTypeName("int")
        });
        profile.ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.Append,
            InferredType = GDSemanticType.FromRuntimeTypeName("int")
        });
        profile.ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.IndexAssign,
            InferredType = GDSemanticType.FromRuntimeTypeName("int")
        });

        // Assert
        Assert.AreEqual(3, profile.ValueUsageCount);
        Assert.AreEqual(GDContainerUsageKind.Initialization, profile.ValueUsages[0].Kind);
        Assert.AreEqual(GDContainerUsageKind.Append, profile.ValueUsages[1].Kind);
        Assert.AreEqual(GDContainerUsageKind.IndexAssign, profile.ValueUsages[2].Kind);
    }

    [TestMethod]
    public void Profile_KeyUsages_Dictionary_TracksKeys()
    {
        // Arrange
        var profile = new GDContainerUsageProfile("dict") { IsDictionary = true };
        profile.KeyUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.Initialization,
            InferredType = GDSemanticType.FromRuntimeTypeName("String")
        });
        profile.KeyUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.IndexAssign,
            InferredType = GDSemanticType.FromRuntimeTypeName("String")
        });

        // Assert
        Assert.AreEqual(2, profile.KeyUsageCount);
    }

    #endregion

    #region ToString Tests

    [TestMethod]
    public void UsageObservation_ToString_FormatsCorrectly()
    {
        // Arrange
        var observation = new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.Append,
            InferredType = GDSemanticType.FromRuntimeTypeName("int"),
            IsHighConfidence = true,
            Line = 5
        };

        // Act
        var result = observation.ToString();

        // Assert
        Assert.IsTrue(result.Contains("5"));
        Assert.IsTrue(result.Contains("Append"));
        Assert.IsTrue(result.Contains("int"));
        Assert.IsTrue(result.Contains("High"));
    }

    [TestMethod]
    public void Profile_ToString_FormatsCorrectly()
    {
        // Arrange
        var profile = new GDContainerUsageProfile("arr") { IsDictionary = false };
        profile.ValueUsages.Add(new GDContainerUsageObservation
        {
            Kind = GDContainerUsageKind.Append,
            InferredType = GDSemanticType.FromRuntimeTypeName("int")
        });

        // Act
        var result = profile.ToString();

        // Assert
        Assert.IsTrue(result.Contains("arr"));
        Assert.IsTrue(result.Contains("1 values"));
    }

    #endregion
}
