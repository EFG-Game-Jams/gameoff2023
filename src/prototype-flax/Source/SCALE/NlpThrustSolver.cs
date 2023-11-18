using Accord.Math.Optimization;
using FlaxEngine;
using FlaxEngine.Assertions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCALE;

/// <summary>
/// NlpThrustSolver Script.
/// </summary>
public class NlpThrustSolver : Script
{
	public float timeMs;
	
	public float[] solution;
	public Float2 result;

	public static void RunTest()
	{
		// Objective function: f(x, y) = (x - 1)^2 + (y - 2)^2
		NonlinearObjectiveFunction objectiveFunction = new(2, (x) =>
		{
			double x1 = x[0];
			double x2 = x[1];
			return Math.Pow(x1 - 1, 2) + Math.Pow(x2 - 2, 2);
		});

		// Constraint functions: x and y should be in the range [-2, 5]
		NonlinearConstraint[] constraints =
		{
			new NonlinearConstraint(objectiveFunction, x => x[0] + 2),  // x[0] >= -2
			new NonlinearConstraint(objectiveFunction, x => 5 - x[0]),  // x[0] <= 5
			new NonlinearConstraint(objectiveFunction, x => x[1] + 2),  // x[1] >= -2
			new NonlinearConstraint(objectiveFunction, x => 5 - x[1])   // x[1] <= 5
		};

		// Initial guess
		double[] initialGuess = new double[] { 0, 0 };

		// Create the Cobyla solver
		var cobyla = new Cobyla(objectiveFunction, constraints)
		{
			Solution = initialGuess
		};

		// Minimize the objective function
		bool success = cobyla.Minimize();

		// Output the results
		Debug.Log("Success: " + success);
		Debug.Log("Solution: x = " + cobyla.Solution[0] + ", y = " + cobyla.Solution[1]);
		Debug.Log("Value of objective function: " + cobyla.Value);
	}


	public class ThrustOptimization
	{
		public struct Thruster
		{
			public Float2 Normal; // Direction of the thruster
			public float MaxThrust; // Maximum thrust this thruster can provide

			public Thruster(Float2 normal, float maxThrust)
			{
				Normal = normal;
				MaxThrust = maxThrust;
			}
		}

		private List<Thruster> thrusters;
		private Float2 desiredDirection;

		public ThrustOptimization(List<Thruster> thrusters, Float2 desiredDirection)
		{
			this.thrusters = thrusters;
			this.desiredDirection = desiredDirection;
		}

		public float[] Optimize()
		{
			int n = thrusters.Count;

			// Define the objective function
			var function = new NonlinearObjectiveFunction(n, x =>
			{
				//Float2 totalThrustVector = CalculateTotalThrustVector(x);
				//double alignment = CalculateAlignment(totalThrustVector, desiredDirection);
				//double totalThrustMagnitude = totalThrustVector.Length;
				//return alignment + totalThrustMagnitude;

				Float2 totalThrustVector = CalculateTotalThrustVector(x);
				Float2 thrustNormal = totalThrustVector.Normalized;
				float alignment = Float2.Dot(thrustNormal, desiredDirection);
				float alignmentScore = Mathf.Pow(alignment, 64);
				return alignmentScore * totalThrustVector.Length;
			});

			// Define constraints
			NonlinearConstraint[] constraints = new NonlinearConstraint[n];
			for (int i = 0; i < n; i++)
			{
				int index = i;
				constraints[index] = new NonlinearConstraint(function, x => Math.Min(x[index], 1.0 - x[index]));
			}

			// select initial guess
			// for the initial guess, we'll go with each thruster's contribution being its dot product with the desired direction
			double[] initialGuess = new double[n];
			for (int i = 0; i < n; i++)
			{
				Thruster thruster = thrusters[i];
				initialGuess[i] = Float2.Dot(thruster.Normal, desiredDirection);
			}

			// Choose a solver
			var solver = new Cobyla(function, constraints) { Solution = initialGuess };
			//var solver = new LevenbergMarquardt(thrusters.Count);
			//solver.Function = function;
			
			// Optimize
			bool ok = solver.Maximize();
			//Assert.IsTrue(ok);
			double[] solution = solver.Solution;

			return solution.Select(s => (float)s).ToArray();
		}

		private Float2 CalculateTotalThrustVector(double[] contributions)
		{
			Float2 totalThrust = new Float2(0, 0);
			for (int i = 0; i < contributions.Length; i++)
			{
				float contribution = (float)contributions[i];
				Thruster thruster = thrusters[i];
				totalThrust += contribution * thruster.MaxThrust * thruster.Normal;
			}
			return totalThrust;
		}

		private double CalculateAlignment(Float2 vectorA, Float2 vectorB)
		{
			double dotProduct = Float2.Dot(vectorA, vectorB);
			double magnitudeProduct = vectorA.Length * vectorB.Length;
			return dotProduct / magnitudeProduct;
		}
	}
	
    /// <inheritdoc/>
    public override void OnUpdate()
    {
		//RunTest();

		List<ThrustOptimization.Thruster> thrusters = new();
		foreach (var child in Actor.Children)
		{
			if (child.OrderInParent == 0) continue;
			Float2 dir = (Float2)child.Transform.Up;
			float maxThrust = 1;
			thrusters.Add(new ThrustOptimization.Thruster(dir, maxThrust));
		}

		Vector3 desired = Actor.Children[0].Position.Normalized;
		var optimiser = new ThrustOptimization(thrusters, (Float2)(Vector2)desired);
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		solution = optimiser.Optimize();
		stopwatch.Stop();
		timeMs = (float)stopwatch.Elapsed.TotalMilliseconds;

		FlaxEngine.DebugDraw.DrawLine(Actor.Position, Actor.Position + Actor.Children[0].LocalPosition.Normalized * 100, FlaxEngine.Color.Red);
		for (int i = 1; i < Actor.Children.Length; i++)
		{
			Actor child = Actor.Children[i];
			Float2 normal = thrusters[i - 1].Normal;
			Vector3 normal3 = new(normal.X, normal.Y, 0);
			FlaxEngine.DebugDraw.DrawLine(child.Position, child.Position + normal3 * thrusters[i-1].MaxThrust * solution[i-1] * 100, FlaxEngine.Color.Green);
		}

		Vector2 sum = Vector2.Zero;
		for (int i = 0; i < thrusters.Count; i++)
			sum += (Vector2)thrusters[i].Normal * thrusters[i].MaxThrust * solution[i];
		result = sum / thrusters.Count;
		FlaxEngine.DebugDraw.DrawLine(Actor.Position, Actor.Position + new Vector3(result.X, result.Y, 0)*100, FlaxEngine.Color.Blue);
	}
}
