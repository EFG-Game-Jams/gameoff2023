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

	[Header("Status")]
	public bool KillTranslation { get; set; }
	public bool KillRotation { get; set; }

	public override void OnFixedUpdate()
	{
		int controlX = (Input.GetKey(KeyboardKeys.ArrowRight) ? 1 : 0) - (Input.GetKey(KeyboardKeys.ArrowLeft) ? 1 : 0);
		int controlY = (Input.GetKey(KeyboardKeys.W) ? 1 : 0) - (Input.GetKey(KeyboardKeys.S) ? 1 : 0);
		int controlTorque = (Input.GetKey(KeyboardKeys.D) ? 1 : 0) - (Input.GetKey(KeyboardKeys.A) ? 1 : 0);

		if (Input.GetKey(KeyboardKeys.Spacebar))
			KillTranslation = !KillTranslation;
		
		Double2 currentLinearVelocity = (Vector2)vessel.Transform.WorldToLocalVector(vessel.LinearVelocity);
		double currentAngularVelocity = vessel.AngularVelocity.Z;

		Double2 desiredLinearVelocity;
		if (controlX != 0 || controlY != 0)
			desiredLinearVelocity = new Double2(controlX, controlY) * MaxLinearSpeed;
		else if (KillTranslation)
			desiredLinearVelocity = Double2.Zero;
		else
			desiredLinearVelocity = currentLinearVelocity;

		double desiredAngularVelocity;
		if (controlTorque != 0)
			desiredAngularVelocity = controlTorque * MaxAngularSpeed;
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

		thrusters.ApplySolutionToThrusters();
		thrusters.ApplyThrustAndTorque();
	}
}
