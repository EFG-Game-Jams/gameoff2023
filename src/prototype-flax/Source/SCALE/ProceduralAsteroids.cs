using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEngine;

namespace SCALE;

public class ProceduralAsteroids : Script
{
	[Serialize, ShowInEditor] private int variantCount;
	[Serialize, ShowInEditor] private Int2 pointCountRange;
	[Serialize, ShowInEditor] private Float2 perAxisScaleRange;
	[Serialize, ShowInEditor] private bool spawnDebugActors;

	public record struct Variant(
		CollisionData CollisionData,
		Model Model);

	private Variant[] variants;

	public Variant GetRandomVariant()
	{
		return variants[Random.Shared.Next(variants.Length)];
	}

	private IEnumerator<Float3> GenerateDebugActorPosition()
	{
		int count = Mathf.NextPowerOfTwo(variantCount);
		int root = (int)Mathf.Sqrt(count);
		for (int i = 0; i < root; i++)
			for (int j = 0; j < root; j++)
				yield return new Float3(i, j, 0) * perAxisScaleRange.Y;
	}

	public override void OnEnable()
	{
		variants = new Variant[variantCount];
		for (int i = 0; i < variants.Length; i++)
			variants[i] = Generate();

		if (!spawnDebugActors)
			return;

		var makePosition = GenerateDebugActorPosition();
		foreach (var variant in variants)
		{
			makePosition.MoveNext();
			var position = makePosition.Current;

			StaticModel model = new StaticModel();
			model.Model = variant.Model;
			model.Position = position;
			model.EulerAngles = new Float3(0, 0, Random.Shared.NextSingle() * 360);
			model.Parent = Actor;

			MeshCollider collider = new MeshCollider();
			collider.CollisionData = variant.CollisionData;
			collider.Parent = model;

			position += Vector3.Right * perAxisScaleRange.Y;
		}
	}
	public override void OnDisable()
	{
		foreach (var variant in variants)
		{
			if (variant.CollisionData)
				Destroy(variant.CollisionData);
			if (variant.Model)
				Destroy(variant.Model);
		}
		variants = null;
	}

	private Variant Generate()
	{
		// select parameters
		int pointCount = Random.Shared.Next(pointCountRange.X, pointCountRange.Y);
		float xScale = Mathf.Lerp(perAxisScaleRange.X, perAxisScaleRange.Y, Random.Shared.NextSingle());
		float yScale = Mathf.Lerp(perAxisScaleRange.X, perAxisScaleRange.Y, Random.Shared.NextSingle());
		float zScale = Mathf.Lerp(perAxisScaleRange.X, perAxisScaleRange.Y, Random.Shared.NextSingle());

		// build collision data
		var collisionData = Content.CreateVirtualAsset<CollisionData>();
		var hullPoints = GenerateHullPoints(pointCount, new Float3(xScale, yScale, zScale));
		var hullTriangles = GenerateTriangles(pointCount);
		collisionData.CookCollision(CollisionDataType.ConvexMesh, hullPoints, hullTriangles);

		// build model
		var model = Content.CreateVirtualAsset<Model>();
		model.SetupLODs(new[] { 1 });
		collisionData.ExtractGeometry(out Float3[] vertexBuffer, out int[] indexBuffer);
		ConvertToFlatShadedMesh(vertexBuffer, indexBuffer, out Float3[] flatVertices, out int[] flatIndices, out Float3[] flatNormals);
		var mesh = model.LODs[0].Meshes[0];
		//mesh.UpdateMesh(flatVertices, flatIndices, flatNormals);

		// use raw mesh
		indexBuffer = indexBuffer.Reverse().ToArray();
		CalculateSmoothNormals(vertexBuffer, indexBuffer, out Float3[] normals);
		mesh.UpdateMesh(vertexBuffer, indexBuffer, normals);

		return new() { CollisionData = collisionData, Model = model };
	}

	void CalculateSmoothNormals(Float3[] vertexBuffer, int[] indexBuffer, out Float3[] normals)
	{
		normals = new Float3[vertexBuffer.Length];

		// Initialize normals array
		for (int i = 0; i < normals.Length; i++)
		{
			normals[i] = new Float3(0, 0, 0);
		}

		// Accumulate normals for each vertex
		for (int i = 0; i < indexBuffer.Length; i += 3)
		{
			int index0 = indexBuffer[i];
			int index1 = indexBuffer[i + 1];
			int index2 = indexBuffer[i + 2];

			Float3 vertex0 = vertexBuffer[index0];
			Float3 vertex1 = vertexBuffer[index1];
			Float3 vertex2 = vertexBuffer[index2];

			// Calculate the normal of the triangle
			Float3 edge1 = vertex1 - vertex0;
			Float3 edge2 = vertex2 - vertex0;
			Float3 triangleNormal = Float3.Cross(edge1, edge2).Normalized;

			// Add the triangle normal to the normals of the vertices of the triangle
			normals[index0] += triangleNormal;
			normals[index1] += triangleNormal;
			normals[index2] += triangleNormal;
		}

		// Normalize each vertex normal
		for (int i = 0; i < normals.Length; i++)
		{
			normals[i] = normals[i].Normalized;
		}
	}

	private Float3[] GenerateHullPoints(int pointCount, Float3 scale)
	{
		Float3 InBox(Float3 scale) => new(
			(Random.Shared.NextSingle() - .5f) * scale.X,
			(Random.Shared.NextSingle() - .5f) * scale.Y,
			(Random.Shared.NextSingle() - .5f) * scale.Z);

		Float3 InEllipsoid(Float3 scale)
		{
			Float3 point;
			do
				point = InBox(scale);
			while ((point / scale).Length > .5f);
			return point;
		}

		Float3 OnEllipsoid(Float3 scale)
		{
			Float3 point = InEllipsoid(scale);
			return point.Normalized * .5f * scale;
		}

		var points = new Float3[pointCount];
		for (int i = 0; i < pointCount; i++)
		{
			points[i] = OnEllipsoid(scale);
		}
		return points;
	}
	private int[] GenerateTriangles(int pointCount)
	{
		var triangles = new int[pointCount * 3];
		for (int i = 0; i < pointCount; i++)
		{
			triangles[i * 3 + 0] = i;
			triangles[i * 3 + 1] = (i + 1) % pointCount;
			triangles[i * 3 + 2] = pointCount;
		}
		return triangles;
	}

	public void ConvertToFlatShadedMesh(Float3[] vertexBuffer, int[] indexBuffer,
									 out Float3[] newVertexBuffer,
									 out int[] newIndexBuffer,
									 out Float3[] normalsArray)
	{
		List<Float3> newVertices = new List<Float3>();
		List<int> newIndices = new List<int>();
		List<Float3> normals = new List<Float3>();

		for (int i = 0; i < indexBuffer.Length; i += 3)
		{
			// Get the vertices of the triangle
			Float3 v0 = vertexBuffer[indexBuffer[i]];
			Float3 v1 = vertexBuffer[indexBuffer[i + 1]];
			Float3 v2 = vertexBuffer[indexBuffer[i + 2]];

			// Calculate the normal
			Float3 edge1 = v1 - v0;
			Float3 edge2 = v2 - v0;
			Float3 normal = -Float3.Cross(edge1, edge2).Normalized;

			// Add vertices and normal for this triangle
			newVertices.Add(v0);
			newVertices.Add(v1);
			newVertices.Add(v2);
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);

			// Add indices for this triangle
			newIndices.Add(newVertices.Count - 1);
			newIndices.Add(newVertices.Count - 2);
			newIndices.Add(newVertices.Count - 3);
		}

		newVertexBuffer = newVertices.ToArray();
		newIndexBuffer = newIndices.ToArray();
		normalsArray = normals.ToArray();
	}
}
