namespace Editor;

public sealed class QuickAssetMenu : Menu
{
	public static QuickAssetMenu Current { get; set; }
	public string Filter { get; set; }

	LineEdit _searchInput;
	ListView _list;

	bool UseCloud
	{
		get => EditorCookie.Get( "qam.UseCloud", false );
		set => EditorCookie.Set( "qam.UseCloud", value );
	}

	IReadOnlyList<string> RecentPackages
	{
		get => EditorCookie.Get<List<string>>( "qam.RecentPackages", [] );
		set => EditorCookie.Set( "qam.RecentPackages", value );
	}

	IReadOnlyList<string> RecentAssets
	{
		get => EditorCookie.Get<List<string>>( "qam.RecentAssets", [] );
		set => EditorCookie.Set( "qam.RecentAssets", value );
	}

	public QuickAssetMenu() : base( null )
	{
		IsWindow = false;
		Current = this;

		Layout = Layout.Column();
		FixedWidth = 300;
		FixedHeight = 410;
		Layout.Margin = 8;
		Layout.Spacing = 8;

		var row = Layout.AddRow();

		_searchInput = row.Add( new LineEdit(), 1 );
		_searchInput.TextChanged += v =>
		{
			UpdateList();
		};
		_searchInput.PlaceholderText = "Search for assets...";
		_searchInput.FocusMode = FocusMode.Click;
		_searchInput.Focus();

		var cloudToggle = row.Add( new IconButton( "cloud_off" ) );

		void SetCloudToggle()
		{
			cloudToggle.Icon = UseCloud ? "cloud" : "cloud_off";
			cloudToggle.ToolTip = UseCloud ? "Cloud Enabled" : "Cloud Disabled";
		}

		SetCloudToggle();

		cloudToggle.OnClick = () =>
		{
			UseCloud = !UseCloud;
			SetCloudToggle();
			UpdateList();
		};

		Bind( nameof( Filter ) ).ReadOnly().From( _searchInput, x => x.Text );

		Layout.Add( BuildAssets(), 1 );
	}

	[Shortcut( "quick-asset.toggle", "SHIFT+A", ShortcutType.Widget )]
	public static void ShowQuickAsset()
	{
		var rect = SceneViewWidget.Current.ScreenRect;
		var cursorPos = Application.CursorPosition;

		if ( !rect.IsInside( cursorPos ) )
			return;

		Current?.Destroy();
		Current = new QuickAssetMenu();
		Current.Show();
		Current.CursorPosition = cursorPos;
		Current.Position = cursorPos + 16;
	}

	bool IsSpawnable( AssetType type )
	{
		if ( type == null )
			return false;

		if ( type.IsSimpleAsset )
			return false;

		if ( type.FileExtension == "vmdl" )
			return true;

		if ( type.FileExtension == "prefab" )
			return true;

		if ( type.FileExtension == "sound" )
			return true;

		return false;
	}

	void UpdateList()
	{
		if ( UseCloud )
			_ = SetListCloud();
		else
			SetListAssets();
	}

	void SetListAssets()
	{
		List<object> options = [];

		if ( string.IsNullOrEmpty( Filter ) )
		{
			foreach ( var recent in RecentAssets )
			{
				var asset = AssetSystem.FindByPath( recent );

				if ( asset == null )
					continue;

				if ( !IsSpawnable( asset.AssetType ) )
					continue;

				options.Add( asset );
			}
		}
		else
		{
			var assets = AssetSystem.All
				.Where( x => x.Path.Contains( Filter, StringComparison.OrdinalIgnoreCase ) )
				.Where( x => IsSpawnable( x.AssetType ) )
				.OrderByDescending( x => x.LastOpened )
				.OrderByDescending( x => RecentAssets.Contains( x.Path ) );

			options.AddRange( assets );
		}

		_list.SetItems( options );
	}

	async Task SetListCloud()
	{
		List<object> options = [];

		if ( string.IsNullOrEmpty( Filter ) )
		{
			foreach ( var ident in RecentPackages )
			{
				var package = await Package.Fetch( ident, false );

				if ( package == null )
					continue;

				if ( !IsSpawnable( GetAssetType( package.TypeName ) ) )
					continue;

				options.Add( package );
			}
		}
		else
		{
			var tokenSource = new CancellationTokenSource();
			var token = tokenSource.Token;
			var found = await Package.FindAsync( Filter, 200, 0, token );

			if ( found.Packages.Length == 0 )
				return;

			var packages = found.Packages.Where( x => IsSpawnable( GetAssetType( x.TypeName ) ) )
				.OrderByDescending( x => x.Updated )
				.OrderByDescending( x => RecentPackages.Contains( x.FullIdent ) );

			options.AddRange( packages );
		}

		_list.SetItems( options );
	}

	Widget BuildAssets()
	{
		var canvas = new Widget( null )
		{
			Layout = Layout.Row()
		};

		_list = new ListView( canvas );
		UpdateList();
		_list.Margin = 0;
		_list.ItemSize = new Vector2( 0, 40 );
		_list.ItemPaint = PaintListMode;
		_list.ItemSpacing = 0;
		_list.ItemClicked = o =>
		{
			if ( o is Asset a )
				_ = OnSelected( a );

			if ( o is Package p )
				_ = OnSelected( p );
		};
		_list.OnPaintOverride = PaintList;

		canvas.Layout.Add( _list );

		return canvas;
	}

	public static AssetType GetAssetType( string typeName ) => typeName switch
	{
		"map" => AssetType.MapFile,
		"model" => AssetType.Model,
		"material" => AssetType.Material,
		"sound" => AssetType.SoundFile,
		"shader" => AssetType.Shader,
		_ => null
	};

	void PaintListMode( VirtualWidget item )
	{
		bool isRecent;
		string title;
		string subtitle;
		Color color;

		{
			if ( item.Object is Asset asset )
			{
				title = asset.Name;
				subtitle = asset.AssetType.FriendlyName;
				color = asset.AssetType.Color;
				isRecent = RecentAssets.Contains( asset.Path );
			}
			else if ( item.Object is Package package )
			{
				var type = GetAssetType( package.TypeName );

				title = package.Title;
				subtitle = package.Org.Title;
				color = type.Color;
				isRecent = RecentPackages.Contains( package.FullIdent );
			}
			else
			{
				Log.Warning( "Can't paint type" );
				return;
			}
		}

		if ( Paint.HasSelected || Paint.HasPressed )
		{
			Paint.ClearPen();
			Paint.SetBrush( Paint.HasPressed ? Theme.Primary.WithAlpha( 0.4f ) : Theme.Primary.WithAlpha( 0.2f ) );
			Paint.DrawRect( item.Rect.Shrink( 0 ), 3 );

			Paint.SetPen( Theme.Blue );
		}
		else if ( Paint.HasMouseOver )
		{
			Paint.ClearPen();
			Paint.SetBrush( Theme.Blue.Darken( 0.7f ).Desaturate( 0.3f ).WithAlpha( 0.5f ) );
			Paint.DrawRect( item.Rect );
			Paint.SetPen( Theme.Blue );
		}

		var itemRect = item.Rect.Shrink( 1 );
		itemRect = itemRect.Shrink( 0, 0, 8, 0 );

		var rect = itemRect;
		var textRect = rect.Shrink( 40 + 8, 4, 0, 4 );

		{
			Paint.SetPen( Theme.TextControl );
			Paint.SetDefaultFont();
			Paint.DrawText( textRect, title, TextFlag.LeftTop | TextFlag.SingleLine );
		}

		{
			Paint.SetDefaultFont();
			Paint.SetPen( Theme.TextControl.WithAlpha( 0.5f ) );
			Paint.DrawText( textRect, subtitle, TextFlag.LeftBottom | TextFlag.SingleLine );
		}

		if ( isRecent )
		{
			Paint.SetDefaultFont();
			Paint.SetPen( Theme.TextControl.WithAlpha( 0.5f ) );
			Paint.DrawIcon( textRect, "history", 13, TextFlag.RightCenter );
		}

		Paint.ClearPen();

		var ir = itemRect;
		ir.Size = new Vector2( itemRect.Height, itemRect.Height );

		{
			var aPos = rect.TopLeft;
			var bPos = rect.BottomLeft;
			var aColor = color.WithAlpha( 0 );
			var bColor = color.WithAlpha( 0.5f );

			Paint.SetBrushLinear( aPos, bPos, aColor, bColor );
			Paint.DrawRect( ir );

			if ( item.Object is Asset asset )
			{
				Paint.Draw( ir, asset.GetAssetThumb() ?? asset.AssetType.Icon64 );
			}
			else if ( item.Object is Package package )
			{
				Paint.Draw( ir, package.Thumb );
			}
		}

		ir.Top = ir.Bottom - 4;
		Paint.ClearPen();
		Paint.SetBrush( color );
		Paint.DrawRect( ir );
	}

	public override void Hide()
	{
		base.Hide();
		WindowOpacity = 0;
	}

	protected override void OnKeyPress( KeyEvent e )
	{
		if ( e.KeyboardModifiers.HasFlag( KeyboardModifiers.Ctrl ) && e.Key is KeyCode.K )
			_searchInput.Focus();
	}

	Ray GetRay( Vector2 cursorPosition, Vector2 screenSize )
	{
		var state = SceneViewWidget.Current.LastSelectedViewportWidget.State;
		var fov = 80;

		var aspect = screenSize.x / screenSize.y;
		var posNormalized = new Vector2( (2 * cursorPosition.x / screenSize.x) - 1, (2 * cursorPosition.y / screenSize.y) - 1 ) * -1;

		float halfWidth = MathF.Tan( fov * MathF.PI / 360 );
		float halfHeight = halfWidth / aspect;

		var ray = new Vector3( 1, posNormalized.x / (1 / halfWidth), posNormalized.y / (1 / halfHeight) ) * state.CameraRotation;
		return new Ray( state.CameraPosition, ray.Normal );
	}

	async Task CreateGameObject( string path )
	{
		using var a = SceneEditorSession.Scope();
		using var b = SceneEditorSession.Active.UndoScope( "Quick Add Asset" ).Push();

		var drop = await BaseDropObject.CreateDropFor( path );

		var lastSelectedViewport = SceneViewWidget.Current.LastSelectedViewportWidget;
		var rect = lastSelectedViewport.ScreenRect;
		var cursorPos = CursorPosition - rect.TopLeft;
		var state = lastSelectedViewport.State;
		var ray = GetRay( cursorPos, rect.Size );

		var tr = SceneEditorSession.Active.Scene.Trace
			.WithoutTags( "trigger" )
			.Ray( ray, 1000 )
			.Run();

		if ( drop is not null )
		{
			await drop.StartInitialize( path );
			drop.UpdateDrag( tr, EditorScene.GizmoSettings );

			using ( var sc = SceneEditorSession.Active.Scene.Push() )
			{
				await drop.OnDrop();
				var go = drop.GameObject;

				if ( go.IsValid() )
				{
					EditorScene.Selection.Add( go );
				}
			}

			drop.Delete();
		}
	}

	async Task OnSelected( Package package )
	{
		Close();
		RecentPackages = [package.FullIdent, .. RecentPackages.Where( x => x != package.FullIdent ).Take( 8 )];

		await CreateGameObject( package.Url );
	}

	async Task OnSelected( Asset asset )
	{
		Close();
		RecentAssets = [asset.Path, .. RecentAssets.Where( x => x != asset.Path ).Take( 8 )];

		await CreateGameObject( asset.Path );
	}

	bool PaintList()
	{
		if ( _list.Items.Any() )
			return false;

		Paint.ClearBrush();
		Paint.ClearPen();
		Paint.SetPen( Theme.TextControl.WithAlpha( 0.5f ) );
		Paint.SetDefaultFont();

		if ( string.IsNullOrEmpty( Filter ) )
		{
			Paint.DrawText( _list.ContentRect, "Type to start searching for assets" );
		}
		else
		{
			Paint.DrawText( _list.ContentRect, "Nothing found" );
		}

		return true;
	}

	Vector2 CursorPosition;
}
