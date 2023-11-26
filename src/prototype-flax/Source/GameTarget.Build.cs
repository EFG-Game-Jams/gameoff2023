using Flax.Build;
using Flax.Build.NativeCpp;
using System.IO;

/// <inheritdoc />
public class GameTarget : GameProjectTarget
{
	/// <inheritdoc />
	public override void Init()
	{
		base.Init();

		// Reference the modules for game
		Modules.Add("SCALE");
	}
	/// <inheritdoc />
	public override void SetupTargetEnvironment(BuildOptions options)
	{
		base.SetupTargetEnvironment(options);
		options.ScriptingAPI.FileReferences.Add(Path.Combine(Globals.Project.ProjectFolderPath, "ThirdParty", "Accord.dll"));
		options.ScriptingAPI.FileReferences.Add(Path.Combine(Globals.Project.ProjectFolderPath, "ThirdParty", "Accord.Math.dll"));
	}
}
