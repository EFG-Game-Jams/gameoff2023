﻿using System;
using System.Collections.Generic;
using FlaxEngine;

namespace SCALE;

public class ProceduralAsteroids : Script
{
	[Serialize, ShowInEditor] private int variantCount;
	[Serialize, ShowInEditor] private Int2 pointCountRange;
	[Serialize, ShowInEditor] private Float2 perAxisScaleRange;
	[Serialize, ShowInEditor] private bool spawnDebugActors;

	public struct Variant
	{
		public CollisionData CollisionData;
		public Model Model;
	}
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
		mesh.UpdateMesh(flatVertices, flatIndices, flatNormals);

		return new() { CollisionData = collisionData, Model = model };
	}

	private Float3[] GenerateHullPoints(int pointCount, Float3 scale)
	{
		var points = new Float3[pointCount];
		for (int i = 0; i < pointCount; i++)
		{
			points[i].X = (Random.Shared.NextSingle() - .5f) * scale.X;
			points[i].Y = (Random.Shared.NextSingle() - .5f) * scale.Y;
			points[i].Z = (Random.Shared.NextSingle() - .5f) * scale.Z;
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
