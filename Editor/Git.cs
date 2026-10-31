namespace Editor;

public static class Git
{
	static readonly Logger Log = new( "git" );

	[ConCmd( "git" )]
	public static void GitCommand( string args = "" )
	{
		var process = new System.Diagnostics.Process();
		process.StartInfo.FileName = "git";
		process.StartInfo.Arguments = args;
		process.StartInfo.WorkingDirectory = Project.Current.GetRootPath();
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.CreateNoWindow = true;

		process.Start();
		string output = process.StandardOutput.ReadToEnd();
		string error = process.StandardError.ReadToEnd();
		process.WaitForExit();

		if ( !string.IsNullOrEmpty( output ) )
		{
			Log.Info( output );
		}

		if ( !string.IsNullOrEmpty( error ) )
		{
			Log.Warning( error );
		}
	}
}
