using NUnit.Framework;
using UnityEngine;

namespace HabitGarden.Tests
{
    public sealed class GeometryTests
    {
        [Test]public void LeavesHaveFiniteVerticesAndNonzeroNormals(){var mesh=PlantGeometry.Leaf(.8f);foreach(var v in mesh.vertices)Assert.IsFalse(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z));foreach(var n in mesh.normals)Assert.Greater(n.sqrMagnitude,.5f);Object.DestroyImmediate(mesh);}
        [Test]public void StemContainsCapsAndExpectedHeight(){var mesh=PlantGeometry.Stem(Vector3.up, .03f);Assert.AreEqual(74,mesh.vertexCount);Assert.AreEqual(1,mesh.bounds.max.y,.001f);Assert.AreEqual(0,mesh.bounds.min.y,.001f);Object.DestroyImmediate(mesh);}
    }
}
