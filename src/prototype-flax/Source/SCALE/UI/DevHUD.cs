using FlaxEngine;
using FlaxEngine.GUI;
using System.Collections.Generic;

namespace SCALE.UI;

public class DevHUD : Script
{
	public struct StateSnapshot
	{
		public double speed;
		public double maxSpeed;
	}

	public static StateSnapshot State;

	List<Control> controls = new();
	private VerticalPanel statusPanel;
	private Label speedLabel;

	public override void OnEnable()
	{
		statusPanel = new VerticalPanel
		{
			Width = 200,
			AutoSize = true,
			Parent = RootControl.GameRoot,
		};
		controls.Add(statusPanel);

		speedLabel = new Label
		{
			Text = "Speed",
			TextColor = Color.White,
			Parent = statusPanel,
			AnchorPreset = AnchorPresets.TopLeft,			
		};
		controls.Add(speedLabel);
	}

	public override void OnDisable()
	{
		controls.ForEach(c => c.Dispose());
		controls.Clear();
	}

	public override void OnLateUpdate()
	{
		speedLabel.Text = $"{(State.speed / 100f):F1} m/s ({(100 * State.speed / State.maxSpeed):F1} %)";
	}
}