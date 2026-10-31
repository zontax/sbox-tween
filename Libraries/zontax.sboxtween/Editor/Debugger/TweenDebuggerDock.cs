using Button = global::Editor.Button;
using Label = global::Editor.Label;

namespace SboxTween.Editor;

/// <summary>
/// Shows all alive animations of the playing scene (or the edited scene when not playing) and lets you control them.
/// </summary>
[Dock( "Editor", DockName, "animation" )]
public class TweenDebuggerDock : Widget
{
	public const string DockName = "Tweens";

	readonly Label _stats;
	readonly Button _pauseButton;
	readonly TweenListWidget _list;
	bool _isPaused;

	public TweenDebuggerDock( Widget parent ) : base( parent )
	{
		MinimumSize = new Vector2( 360, 160 );
		Layout = Layout.Column();

		var header = Layout.AddRow();
		header.Margin = 4;
		header.Spacing = 4;
		_stats = header.Add( new Label( this ), 1 );
		_pauseButton = header.Add( new Button( "Pause All", "pause", this ) { Clicked = TogglePauseAll } );
		header.Add( new Button( "Complete All", "done_all", this ) { Clicked = () => TweenEditorMenu.CompleteAll() } );
		header.Add( new Button( "Stop All", "stop", this ) { Clicked = () => TweenEditorMenu.StopAll() } );

		var scroll = Layout.Add( new ScrollArea( this ), 1 );
		_list = new TweenListWidget( scroll );
		scroll.Canvas = _list;
	}

	void TogglePauseAll()
	{
		_isPaused = !_isPaused;
		TweenEditorMenu.SetPausedAll( _isPaused );
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		if ( !Visible )
			return;

		var manager = TweenEditorMenu.FindManager();
		if ( manager is null )
		{
			_list.Entries.Clear();
			_stats.Text = "No scene";
		}
		else
		{
			manager.CollectDebugEntries( _list.Entries );
			string scene = Game.IsPlaying ? "Playing" : "Editor scene";
			_stats.Text = $"{scene}  ·  Alive: {manager.TweensCount}  ·  Max alive: {manager.MaxAliveTweens}";
		}

		_pauseButton.Text = _isPaused ? "Resume All" : "Pause All";
		_pauseButton.Icon = _isPaused ? "play_arrow" : "pause";
		_list.Refresh();
	}
}

/// <summary>
/// A custom painted list of <see cref="TweenDebugEntry"/> rows.
/// </summary>
class TweenListWidget : Widget
{
	const float RowHeight = 22;
	const float Indent = 14;
	const float NameWidth = 170;
	const float TargetWidth = 170;
	const float InfoWidth = 150;
	const float ButtonSize = 20;
	const int ButtonCount = 3;

	public List<TweenDebugEntry> Entries { get; } = [];

	public TweenListWidget( Widget parent ) : base( parent )
	{
		MouseTracking = true;
	}

	public void Refresh()
	{
		MinimumHeight = MathF.Max( Entries.Count, 1 ) * RowHeight;
		Update();
	}

	float TargetX => 8 + NameWidth;
	float ProgressX => TargetX + TargetWidth + 8;
	float ButtonsX => Width - ButtonCount * ButtonSize - 6;
	float InfoX => ButtonsX - InfoWidth - 8;
	float ProgressWidth => MathF.Max( 40, InfoX - ProgressX - 8 );

	protected override void OnPaint()
	{
		Paint.SetDefaultFont();

		if ( Entries.Count == 0 )
		{
			Paint.SetPen( Theme.TextDisabled );
			Paint.DrawText( new Rect( 8, 0, Width - 16, RowHeight ), "No alive animations", TextFlag.LeftCenter );
			return;
		}

		for ( int i = 0; i < Entries.Count; i++ )
			PaintRow( Entries[i], i * RowHeight, i % 2 == 1 );
	}

	void PaintRow( TweenDebugEntry entry, float y, bool odd )
	{
		if ( odd )
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.SurfaceLightBackground.WithAlpha( 0.3f ) );
			Paint.DrawRect( new Rect( 0, y, Width, RowHeight ) );
		}

		float nameX = 8 + entry.Depth * Indent;
		Paint.SetDefaultFont( 8, entry.IsSequence ? 700 : 400 );
		Paint.SetPen( entry.Depth > 0 ? Theme.TextLight : Theme.Text );
		Paint.DrawText( new Rect( nameX, y, NameWidth - (nameX - 8), RowHeight ), entry.Name, TextFlag.LeftCenter );

		Paint.SetDefaultFont();
		var targetName = GetTargetName( entry.Target );
		Paint.SetPen( GetGameObject( entry.Target ).IsValid() ? Theme.TextLink : Theme.TextDisabled );
		Paint.DrawText( new Rect( TargetX, y, TargetWidth, RowHeight ), targetName, TextFlag.LeftCenter );

		var bar = new Rect( ProgressX, y + 7, ProgressWidth, RowHeight - 14 );
		Paint.ClearPen();
		Paint.SetBrush( Theme.ControlBackground );
		Paint.DrawRect( bar, 2 );
		Paint.SetBrush( entry.IsPaused ? Theme.Yellow : (entry.IsSequence ? Theme.Blue : Theme.Green) );
		Paint.DrawRect( new Rect( bar.Left, bar.Top, bar.Width * entry.Progress, bar.Height ), 2 );

		Paint.SetPen( Theme.TextLight );
		Paint.DrawText( new Rect( InfoX, y, InfoWidth, RowHeight ), GetInfo( entry ), TextFlag.LeftCenter );

		if ( !entry.CanControl )
			return;

		Paint.SetPen( Theme.Text );
		Paint.DrawIcon( ButtonRect( 0, y ), entry.IsPaused ? "play_arrow" : "pause", 14 );
		Paint.DrawIcon( ButtonRect( 1, y ), "done_all", 14 );
		Paint.DrawIcon( ButtonRect( 2, y ), "stop", 14 );
	}

	Rect ButtonRect( int index, float y ) => new( ButtonsX + index * ButtonSize, y + 1, ButtonSize, RowHeight - 2 );

	static string GetInfo( TweenDebugEntry entry )
	{
		string cycles = entry.CyclesTotal == -1 ? $"{entry.CyclesDone}/∞" : $"{entry.CyclesDone}/{entry.CyclesTotal}";
		string info = $"{entry.Duration:0.00}s  {cycles}";
		if ( entry.TimeScale != 1 )
			info += $"  ×{entry.TimeScale:0.##}";
		if ( entry.IsPaused )
			info += "  paused";
		return info;
	}

	static GameObject GetGameObject( object target ) => target switch
	{
		GameObject go => go,
		Component component => component.GameObject,
		_ => null
	};

	static string GetTargetName( object target ) => target switch
	{
		null => "—",
		GameObject go when go.IsValid() => go.Name,
		Component component when component.IsValid() => $"{component.GameObject.Name} ({component.GetType().Name})",
		_ => target.GetType().Name
	};

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		if ( !e.LeftMouseButton )
			return;

		int row = (int)(e.LocalPosition.y / RowHeight);
		if ( row < 0 || row >= Entries.Count )
			return;

		var entry = Entries[row];
		float x = e.LocalPosition.x;

		if ( entry.CanControl && x >= ButtonsX )
		{
			int button = (int)((x - ButtonsX) / ButtonSize);
			switch ( button )
			{
				case 0:
					entry.SetPaused( !entry.IsPaused );
					break;
				case 1:
					entry.Complete();
					break;
				case 2:
					entry.Stop();
					break;
			}

			return;
		}

		if ( x >= TargetX && x < TargetX + TargetWidth )
			SelectTarget( entry.Target );
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		base.OnMouseMove( e );

		int row = (int)(e.LocalPosition.y / RowHeight);
		float x = e.LocalPosition.x;
		bool clickable = row >= 0 && row < Entries.Count
			&& ((Entries[row].CanControl && x >= ButtonsX) || (x >= TargetX && x < TargetX + TargetWidth && GetGameObject( Entries[row].Target ).IsValid()));

		Cursor = clickable ? CursorShape.Finger : CursorShape.Arrow;
	}

	static void SelectTarget( object target )
	{
		var go = GetGameObject( target );
		if ( !go.IsValid() )
			return;

		if ( go.Scene.Editor is SceneEditorSession session )
			session.Selection.Set( go );
	}
}
