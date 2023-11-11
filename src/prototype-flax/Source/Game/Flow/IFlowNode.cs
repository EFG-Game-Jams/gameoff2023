namespace Game.Flow;

public partial interface IFlowNode
{
	
	/// <summary>
	/// Get the flow rules for a 
	/// </summary>
	FlowRule GetFlowRule(long unitType);

	/// <summary>
	/// Called by the solver to at the start of a simulation step
	/// </summary>
	void OnBeginStep() { }

	/// <summary>
	/// Called by the solver to collect the current content	
	/// </summary>
	void CollectContent(FlowUnitStore collector);

	/// <summary>
	/// Called by the solver to apply internal gain (production) to the content
	/// </summary>
	void ProcessGain(FlowUnitStore content) { }

	/// <summary>
	/// Called by the solver to apply internal interaction (mixing, annihilation, etc.) to the content
	/// </summary>
	void ProcessInteraction(FlowUnitStore content) { }

	/// <summary>
	/// Called by the solver to apply internal loss (consumption) to the content
	/// </summary>
	void ProcessLoss(FlowUnitStore content) { }

	/// <summary>
	/// Called by the solver to pass simulation results back to the node
	/// </summary>
	void ApplyResults(FlowUnitStore content);
}
