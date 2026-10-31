using System.IO;

namespace SboxTween.Editor;

/// <summary>
/// Editor → Sbox Tween menu.
/// </summary>
public static class TweenEditorMenu
{
	const string LibraryIdent = "zontax.sbox_tween";

	[Menu( "Editor", "Sbox Tween/Tween Debugger", "animation" )]
	public static void OpenDebugger()
	{
		EditorWindow.DockManager.SetDockState( TweenDebuggerDock.DockName, true );
		EditorWindow.DockManager.RaiseDock( TweenDebuggerDock.DockName );
	}

	[Menu( "Editor", "Sbox Tween/Pause All Tweens", "pause" )]
	public static void PauseAll() => SetPausedAll( true );

	[Menu( "Editor", "Sbox Tween/Resume All Tweens", "play_arrow" )]
	public static void ResumeAll() => SetPausedAll( false );

	[Menu( "Editor", "Sbox Tween/Complete All Tweens", "done_all" )]
	public static void CompleteAll()
	{
		int count = FindManager()?.CompleteAll() ?? 0;
		Log.Info( $"[SboxTween] Completed {count} animations." );
	}

	[Menu( "Editor", "Sbox Tween/Stop All Tweens", "stop" )]
	public static void StopAll()
	{
		int count = FindManager()?.StopAll() ?? 0;
		Log.Info( $"[SboxTween] Stopped {count} animations." );
	}

	[Menu( "Editor", "Sbox Tween/Documentation", "menu_book" )]
	public static void OpenDocumentation()
	{
		var readme = FindLibraryProject() is { } project ? Path.Combine( project.GetRootPath(), "README.md" ) : null;
		if ( readme is null || !File.Exists( readme ) )
		{
			EditorUtility.DisplayDialog( "Sbox Tween", "README.md of the Sbox Tween library was not found." );
			return;
		}

		EditorUtility.OpenFile( readme );
	}

	[Menu( "Editor", "Sbox Tween/Developer/Regenerate Tween API", "code" )]
	public static void RegenerateApi()
	{
		var apiDirectory = FindLibraryProject() is { } project ? Path.Combine( project.GetCodePath(), "Api" ) : null;
		if ( apiDirectory is null || !Directory.Exists( apiDirectory ) )
		{
			EditorUtility.DisplayDialog( "Sbox Tween", "Regenerating is only available when the source code of the Sbox Tween library is part of the open project." );
			return;
		}

		var changed = TweenCodeGenerator.WriteFiles( apiDirectory );
		var message = changed.Count == 0
			? "Generated files are already up to date."
			: $"Updated {string.Join( ", ", changed.Select( Path.GetFileName ) )}. The library will recompile now.";

		Log.Info( $"[SboxTween] {message} ({TweenCodeGenerator.Properties.Length} properties, {TweenCodeGenerator.ValueTypes.Length} custom value types)" );
		EditorUtility.DisplayDialog( "Sbox Tween", message );
	}

	[Menu( "Editor", "Sbox Tween/Developer/Run Benchmark", "speed" )]
	public static void RunBenchmark()
	{
		var report = TweenBenchmark.Format( TweenBenchmark.Run() );
		Log.Info( $"[SboxTween] Benchmark\n{report}" );
		EditorUtility.DisplayDialog( "Sbox Tween Benchmark", report );
	}

	public static void SetPausedAll( bool isPaused )
	{
		int count = FindManager()?.SetPausedAll( isPaused ) ?? 0;
		Log.Info( $"[SboxTween] {(isPaused ? "Paused" : "Resumed")} {count} animations." );
	}

	/// <summary>
	/// The manager of the playing scene, or of the edited scene when not playing.
	/// </summary>
	public static TweenManager FindManager() => TweenManager.Get( Game.IsPlaying ? Game.ActiveScene : SceneEditorSession.Active?.Scene );

	static Project FindLibraryProject() => EditorUtility.Projects.GetAll().FirstOrDefault( x => x.Config?.FullIdent == LibraryIdent );
}
