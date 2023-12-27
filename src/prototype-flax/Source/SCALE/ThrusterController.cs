using System;
using System.Collections.Generic;
using FlaxEngine;

namespace SCALE;

public class ThrusterController : Script
{
	[Header("References")]
	[Serialize, ShowInEditor] private RigidBody vessel;
	[Serialize, ShowInEditor] private ThrusterSystem thrusters;

	[Header("Tuning")]
	public float MaxLinearSpeed { get; set; } = 1000;
	public float MaxAngularSpeed { get; set; } = 10;
	public float LinearSensitivity { get; set; } = 1;
	public float AngularSensitivity { get; set; } = 1;
	public float MaxThrustRequest { get; set; } = 1000;
	public float MaxTorqueRequest { get; set; } = 1000;
	public float LinearVelocityThreshold { get; set; } = 0.1f;
	public float AngularVelocityThreshold { get; set; } = 0.01f;

	[Header("State")]
	public bool KillTranslation { get; set; }
	public bool KillRotation { get; set; }
	public Float2 LinearVelocityControl { get; set; }
	public float AngularVelocityControl { get; set; }

	public override void OnFixedUpdate()
	{
		Double2 currentLinearVelocity = (Vector2)vessel.Transform.WorldToLocalVector(vessel.LinearVelocity);
		double currentAngularVelocity = vessel.AngularVelocity.Z;

		Double2 desiredLinearVelocity;
		if (!LinearVelocityControl.IsZero)
			desiredLinearVelocity = LinearVelocityControl * MaxLinearSpeed;
		else if (KillTranslation)
			desiredLinearVelocity = Double2.Zero;
		else
			desiredLinearVelocity = currentLinearVelocity;

		double desiredAngularVelocity;
		if (AngularVelocityControl != 0)
			desiredAngularVelocity = AngularVelocityControl * MaxAngularSpeed;
		else if (KillRotation)
			desiredAngularVelocity = 0;
		else
			desiredAngularVelocity = currentAngularVelocity;

		Float2 desiredThrust = (Float2)((desiredLinearVelocity - currentLinearVelocity) * LinearSensitivity);
		float requestedThrust = desiredThrust.Length;
		if (requestedThrust > MaxThrustRequest)
			desiredThrust *= MaxThrustRequest / requestedThrust;

		float desiredTorque = (float)((desiredAngularVelocity - currentAngularVelocity) * AngularSensitivity);
		float requestedTorque = Math.Abs(desiredTorque);
		if (requestedTorque > MaxTorqueRequest)
			desiredTorque *= MaxTorqueRequest / requestedTorque;

		thrusters.DesiredThrust = desiredThrust;
		thrusters.DesiredTorque = desiredTorque;
		thrusters.Solve();

		// clear solution if below thresholds
		if (currentLinearVelocity.Length < LinearVelocityThreshold && desiredLinearVelocity.Length < LinearVelocityThreshold
			&& currentAngularVelocity < AngularVelocityThreshold && desiredAngularVelocity < AngularVelocityThreshold)
		{
			for (int i = 0; i < thrusters.Solution.Length; ++i)
				thrusters.Solution[i] = 0;
		}

		thrusters.ApplySolutionToThrusters();
		thrusters.ApplyThrustAndTorque();
	}
}
