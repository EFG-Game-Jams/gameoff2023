namespace Game.Flow;

/// <summary>
/// Node rule for a specific fluid type
///	Equilibirum = 0 : overflow
///	Equilibrium > 0 : balance
/// </summary>
[System.Serializable]
public struct FlowRule
{
	public bool allowInflow;
	public bool allowOutflow;
	public long capacity;
	public long equilibrium;
}

