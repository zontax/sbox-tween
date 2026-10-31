using System.Diagnostics;
using System.IO;

namespace Editor;

public static class ProjectUtility
{
	[Menu( "Editor", "Utils/Restart Editor", "restart_alt" )]
	public static async void RestartEditor()
	{
		EditorScene.SaveSession();

		string cloudRoot = FileSystem.Cloud.GetFullPath( "" );
		string root = cloudRoot;
		int max = 10;

		while ( !string.IsNullOrEmpty( root ) && max-- > 0 )
		{
			if ( Directory.Exists( Path.Combine( root, "code" ) ) ||
				File.Exists( Path.Combine( root, "addon.json" ) ) )
			{
				break;
			}

			root = Directory.GetParent( root )?.FullName;
		}

		if ( string.IsNullOrEmpty( root ) )
			return;

		string sbproj = null;

		foreach ( var file in Directory.GetFiles( root, "*.sbproj", SearchOption.TopDirectoryOnly ) )
		{
			sbproj = file;
			break;
		}

		if ( string.IsNullOrEmpty( sbproj ) )
			return;

		Process.Start( new ProcessStartInfo
		{
			FileName = sbproj,
			UseShellExecute = true
		} );

		await Task.Delay( 2000 );
		Process.GetCurrentProcess().Kill();
	}

	[Menu( "Editor", "Utils/Open Data Folder", "folder" )]
	public static void OpenDataFolder()
	{
		string fullIdent = Project.Current.Package.FullIdent.Replace( '.', '/' );
		string path = FileSystem.Root.GetFullPath( Path.Combine( "data", fullIdent ) );
		EditorUtility.OpenFolder( path );
	}

	[Menu( "Editor", "Utils/Open Screenshots Folder", "image" )]
	public static void OpenScreenshotsFolder()
	{
		string path = FileSystem.Root.GetFullPath( "screenshots" );
		EditorUtility.OpenFolder( path );
	}
}
