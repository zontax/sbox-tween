using System.IO;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Editor.CodeEditors;

[Title( "Cursor" )]
public sealed class Cursor : ICodeEditor
{
	static string Location;

	void ICodeEditor.OpenFile( string path, int? line, int? column )
	{
		string sln = CodeEditor.FindSolutionFromPath( Path.GetDirectoryName( path ) );
		string rootPath = Path.GetDirectoryName( sln );

		Launch( $"-g \"{path}:{line}:{column}\" \"{rootPath}\"" );
	}

	void ICodeEditor.OpenSolution()
	{
		Launch( $"\"{Project.Current.GetRootPath()}\"" );
	}

	void ICodeEditor.OpenAddon( Project addon )
	{
		string projectPath = (addon != null) ? addon.GetRootPath() : "";

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
		if ( Location != null ) return Location;

		string value = null;

		using ( var key = Registry.ClassesRoot.OpenSubKey( @"Applications\\Cursor.exe\\shell\\open\\command" ) )
		{
			value = key?.GetValue( "" ) as string;
		}

		if ( value == null ) return null;

		var rgx = new Regex( "\"(.*)\" \".*\"", RegexOptions.IgnoreCase );
		var matches = rgx.Matches( value );

		if ( matches.Count == 0 || matches[0].Groups.Count < 2 ) return null;

		Location = matches[0].Groups[1].Value;
		return Location;
	}
}
