using System;
using System.Collections.Generic;
using FlaxEngine;

namespace SCALE;

/// <summary>
/// Thruster Script.
/// </summary>
public class Thruster : Script
{
	public float maxThrust;
	public float throttle;

	public Double2 GetRelativeThrustDirection(Transform vessel)
	{
		Vector3 relativeUp = vessel.WorldToLocalVector(Transform.Up);
		return (Vector2)relativeUp;
	}

	public double GetTorqueContribution(Transform vessel)
	{
		Double3 forcePosition = vessel.WorldToLocal(Actor.Position);
		Double3 forceDirection = vessel.WorldToLocalVector(Transform.Up);
		return Double3.Cross(forcePosition, forceDirection).Z;
	}
}
