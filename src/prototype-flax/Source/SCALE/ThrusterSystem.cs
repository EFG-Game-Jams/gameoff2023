using System;
using System.Collections.Generic;
using System.Reflection;
using Accord.Math.Optimization;
using FlaxEngine;
using FlaxEngine.Assertions;

namespace SCALE;

/// <summary>
/// ThrusterSystem Script.
/// </summary>
public class ThrusterSystem : Script
{
	[Header("References")]
	[Serialize, ShowInEditor] private RigidBody vessel;
	[Serialize, ShowInEditor] private Thruster[] thrusters;

	[Header("Tuning")]
	[Serialize, ShowInEditor] private double torqueExponent = 1;
	[Serialize, ShowInEditor] private double magnitudeExponent = 1;
	[Serialize, ShowInEditor] private double alignmentExponent = 1;
	[Serialize, ShowInEditor] private double maxSolverTimeMs = 1;

	[Header("Status")]
	[ShowInEditor, ReadOnly] public Float2 DesiredThrust { get; set; }
	[ShowInEditor, ReadOnly] public float DesiredTorque { get; set; }
	[ShowInEditor, ReadOnly] public Float2 RealisedThrust { get; private set; }
	[ShowInEditor, ReadOnly] public float RealisedTorque { get; private set; }
	[ShowInEditor, ReadOnly] public float ObjectiveResult { get; private set; }
	[ShowInEditor, ReadOnly] public int SolverIterationsLastStep { get; private set; }
	[ShowInEditor, ReadOnly] public ThrusterState[] ThrusterStates { get; private set; }
	[ShowInEditor, ReadOnly] public double[] Solution { get; private set; }

	private Cobyla solver;

	public struct ThrusterState
	{
		public Double2 directionLocal;
		public Double2 throttleToThrust;
		public double throttleToTorque;
		public double maxThrust;
	}	

	public override void OnStart()
	{
		ThrusterStates = new ThrusterState[thrusters.Length];
		Solution = new double[thrusters.Length];

		var function = new NonlinearObjectiveFunction(thrusters.Length, x =>
		{
			ComputeResultingThrustAndTorque(x, out var totalThrust, out var totalTorque);

			double torqueScore = Math.Pow(Math.Abs(totalTorque - DesiredTorque), torqueExponent);
			double magnitudeScore = Math.Pow(Double2.Distance(totalThrust, DesiredThrust), magnitudeExponent);

			double alignmentScore = 0;
			if (!DesiredThrust.IsZero)
			{
				Double2 thrustDir = totalThrust;
				thrustDir.Normalize();
				alignmentScore = 1.0 - Double2.Dot(thrustDir, DesiredThrust.Normalized);
				alignmentScore = Math.Pow(Math.Abs(alignmentScore), alignmentExponent);
			}

			double efficiencyScore = 0;
			if (!totalThrust.IsZero)
			{
				Double2 totalThrustDir = totalThrust;
				totalThrustDir.Normalize();
				for (int i = 0; i < x.Length; i++)
				{
					var thrust = ThrusterStates[i].directionLocal;
					var misaligned = Math.Max(0, 1 - Double2.Dot(thrust, totalThrustDir));
					efficiencyScore += misaligned * x[i];
				}
			}
			efficiencyScore *= 10;

			return torqueScore + magnitudeScore + alignmentScore + efficiencyScore;
		});

		var constraints = new NonlinearConstraint[thrusters.Length];
		for (int i = 0; i < thrusters.Length; i++)
		{
			int index = i;
			var thruster = thrusters[i];
			constraints[i] = new NonlinearConstraint(function, x => Math.Min(x[index], 1.0 - x[index])); // constrain [0;1]
		}

		solver = new Cobyla(function, constraints)
		{
			Solution = Solution
		};
	}

	public void Solve()
	{
		RefreshThrusterStates();

		// set initial guess
		// each thruster's contribution is its dot product with the desired direction
		Double2 desiredDirection = DesiredThrust.Normalized;
		for (int i = 0; i < ThrusterStates.Length; i++)
		{
			var thruster = ThrusterStates[i];
			Solution[i] = Double2.Dot(thruster.directionLocal, desiredDirection);
		}
		solver.Solution = Solution;

		RunSolver(maxSolverTimeMs);

		ApplySolution();
	}

	public void ApplySolutionToThrusters()
	{
		for (int i = 0; i < thrusters.Length; i++)
			thrusters[i].SetThrottle((float)Solution[i]);
	}

	public void ApplyThrustAndTorque()
	{
		vessel.AddRelativeForce((Float3)RealisedThrust);
		vessel.AddRelativeTorque(Vector3.UnitZ * RealisedTorque);
	}

	private void ComputeResultingThrustAndTorque(double[] x, out Double2 totalThrust, out double totalTorque)
	{
		totalThrust = Double2.Zero;
		totalTorque = 0;
		for (int i = 0; i < ThrusterStates.Length; i++)
		{
			var state = ThrusterStates[i];
			totalThrust += state.throttleToThrust * x[i];
			totalTorque += state.throttleToTorque * x[i];
		}
	}

	private void RefreshThrusterStates()
	{
		for (int i = 0; i < thrusters.Length; i++)
		{
			var thruster = thrusters[i];
			Double2 direction = thruster.GetRelativeThrustDirection(vessel.Transform);
			ThrusterStates[i] = new ThrusterState()
			{
				directionLocal = direction,
				throttleToThrust = thruster.maxThrust * direction,
				throttleToTorque = thruster.maxThrust * thruster.GetTorqueContribution(vessel.Transform),
				maxThrust = thruster.maxThrust,
			};
		}
	}
	private void RunSolver(double maxTimeMs)
	{
		SolverIterationsLastStep = 0;
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		while (stopwatch.Elapsed.TotalMilliseconds < maxTimeMs)
		{
			++SolverIterationsLastStep;
			if (solver.Minimize())
				break;
		}
	}
	private void ApplySolution()
	{
		// clamp solution
		Solution = solver.Solution;
		for (int i = 0; i < Solution.Length; i++)
			Solution[i] = Math.Clamp(Solution[i], 0, 1);

		// compute results
		ComputeResultingThrustAndTorque(Solution, out var realisedThrust, out var realisedTorque);
		this.RealisedThrust = (Float2)realisedThrust;
		this.RealisedTorque = (float)realisedTorque;
		ObjectiveResult = (float)solver.Function(solver.Solution);
	}

	public override void OnDebugDrawSelected()
	{
		foreach (var thruster in thrusters)
		{
			var worldPos = thruster.Actor.Position;
			DebugDraw.DrawSphere(new(worldPos, 2f), Color.Red);
			DebugDraw.DrawLine(worldPos, worldPos + thruster.Transform.Down * 100 * thruster.throttle, Color.Blue);
		}
	}
}
