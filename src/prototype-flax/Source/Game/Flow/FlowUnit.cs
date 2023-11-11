namespace Game.Flow;

[System.Serializable]
public struct FlowUnit
{
	public readonly long Type;
	public readonly long Amount;

	public FlowUnit(long type, long amount)
	{
		Type = type;
		Amount = amount;
	}
}
