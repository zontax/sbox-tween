using System.IO;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Editor.CodeEditors;

[Title( "Zed" )]
public sealed class Zed : ICodeEditor
{
	void ICodeEditor.OpenFile( string path, int? line, int? column )
	{
		string fileArg = $"{path}:{line ?? 1}:{column ?? 1}";
		string sln = CodeEditor.FindSolutionFromPath( Path.GetDirectoryName( path ) );

		if ( !string.IsNullOrEmpty( sln ) )
		{
			string rootPath = Path.GetDirectoryName( sln );
			Launch( $"\"{rootPath}\" -a \"{fileArg}\"" );
		}
		else
		{
			Launch( $"\"{fileArg}\"" );
		}
	}

	void ICodeEditor.OpenSolution()
	{
		Launch( $"\"{Project.Current.GetRootPath()}\"" );
	}

	void ICodeEditor.OpenAddon( Project addon )
	{
		string projectPath = addon?.GetRootPath() ?? "";
		Launch( $"\"{projectPath}\"" );
	}

	bool ICodeEditor.IsInstalled() => !string.IsNullOrEmpty( GetLocation() );

	static void Launch( string arguments )
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = GetLocation(),
			Arguments = arguments,
			CreateNoWindow = true,
		};
		Process.Start( startInfo );
	}

	[SuppressMessage( "Interoperability", "CA1416:Validate platform compatibility" )]
	static string GetLocation()
	{
		using var key = Registry.ClassesRoot.OpenSubKey( @"Applications\zed.exe\shell\open\command" );
		if ( key?.GetValue( "" ) is not string command ) return null;

		var match = Regex.Match( command, "\"(.*?)\"" );
		if ( !match.Success ) return null;

		string guiPath = match.Groups[1].Value;
		string cliPath = Path.Combine( Path.GetDirectoryName( guiPath )!, "bin", "zed.exe" );

		return File.Exists( cliPath ) ? cliPath : guiPath;
	}
}
