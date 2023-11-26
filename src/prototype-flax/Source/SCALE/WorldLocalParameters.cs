using FlaxEngine;
using System.Collections.Generic;
using System.Data.Common;

namespace SCALE;

[System.Serializable]
public struct WorldLocalParameters
{
	[HideInEditor] public Vector2 Position;
	public float AsteroidDensity;
	public Float2 AsteroidSize;
	public Float2 AsteroidLinearSpeed;
	public Float2 AsteroidAngularSpeed;

	public static WorldLocalParameters Factor(WorldLocalParameters parameters, float scale)
	{
		return new WorldLocalParameters
		{
			Position = parameters.Position * scale,
			AsteroidDensity = parameters.AsteroidDensity * scale,
			AsteroidSize = parameters.AsteroidSize * scale,
			AsteroidLinearSpeed = parameters.AsteroidLinearSpeed * scale,
			AsteroidAngularSpeed = parameters.AsteroidAngularSpeed * scale,
		};
	}

	public static WorldLocalParameters Sum(WorldLocalParameters a, WorldLocalParameters b)
	{
		return new WorldLocalParameters
		{
			Position = a.Position + b.Position,
			AsteroidDensity = a.AsteroidDensity + b.AsteroidDensity,
			AsteroidSize = a.AsteroidSize + b.AsteroidSize,
			AsteroidLinearSpeed = a.AsteroidLinearSpeed + b.AsteroidLinearSpeed,
			AsteroidAngularSpeed = a.AsteroidAngularSpeed + b.AsteroidAngularSpeed,
		};
	}

	public class Interpolator
	{
		private List<WorldLocalParameters> samples = new();
		private List<float> weights = new();

		public void Clear() => samples.Clear();
		public void Add(WorldLocalParameters sample) => samples.Add(sample);

		public WorldLocalParameters Sample(Vector2 position)
		{
			// compute weights
			float totalWeight = 0;
			weights.Clear();
			foreach (var sample in samples)
			{
				float weight = (float)(1.0 / Vector2.Distance(position, sample.Position));
				weights.Add(weight);
				totalWeight += weight;
			}

			// normalise weights
			for (int i = 0; i < weights.Count; i++)
				weights[i] /= totalWeight;

			// interpolate
			WorldLocalParameters result = default;
			for (int i = 0; i < samples.Count; i++)
				result = Sum(result, Factor(samples[i], weights[i]));

			return result;
		}
	}
}
