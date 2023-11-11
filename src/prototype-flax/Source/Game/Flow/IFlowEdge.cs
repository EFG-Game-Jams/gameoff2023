namespace Game.Flow;

public interface IFlowEdge
{
	IFlowNode Input { get; }
	IFlowNode Output { get; }

	/// <summary>
	/// Called by the solver to at the start of a simulation step
	/// </summary>
	void OnBeginStep();

	/// <summary>
	/// Called by the solver to collect the current content
	/// </summary>
	void CollectContent(FlowUnitStore collector);

	/// <summary>
	/// Called repeatedly by the solver to consume any units which are ready to exit the edge
	/// </summary>
	bool TryPull(out FlowUnit unit);

	/// <summary>
	/// Called by the solver to determine if a flow is possible (not required, just an optimisation)
	/// </summary>
	bool CanFlow(long unitType) => true;

	/// <summary>
	/// Called by the solver to attempt to send a unit into the edge
	/// </summary>
	/// <param name="unit">Unit to send</param>
	/// <returns>Unit which was actually sent</returns>
	FlowUnit TryPush(in FlowUnit unit);
}
