namespace SboxTweenExample;

/// <summary>
/// Builds the SboxTween showcase: a row of stands, each demonstrating a group of features.
/// The UI calls the public methods of this component.
/// </summary>
[Title( "Tween Demo" ), Category( "SboxTween" ), Icon( "animation" )]
public sealed class TweenDemo : Component
{
	const float StandSpacing = 145;

	static readonly Ease[] RaceEases = [Ease.Linear, Ease.InOutSine, Ease.OutBack, Ease.OutElastic, Ease.OutBounce, Ease.InOutExpo];

	static readonly Color[] RaceColors = ["#ff5964", "#ffb84d", "#ffe74c", "#6bf178", "#35a7ff", "#b38cff"];

	/// <summary>Inspector integration: tweak the hover animation of the sequence stand without touching code.</summary>
	[Property, Group( "Settings" )] public TweenSettings<float> HoverSettings { get; set; } = new( 0, 40, new TweenSettings( 1.2f, Ease.InOutSine, -1, CycleMode.Yoyo ) );

	/// <summary>Inspector integration: tweak the punch of the shake stand.</summary>
	[Property, Group( "Settings" )] public ShakeSettings PunchSettings { get; set; } = new( new Vector3( 0, 0, 30 ), 0.6f, 10 );

	public bool IsSlowMotion { get; private set; }
	public bool IsPaused { get; private set; }
	public string LastEvent { get; private set; } = "Ready";
	public int AsyncLoops { get; private set; }
	public int Callbacks { get; private set; }
	public bool IsGateOpen { get; private set; }
	public bool IsRaceSlow { get; private set; }

	/// <summary>True while the gate sequence is suspended by ChainWaitUntil.</summary>
	public bool IsGateWaiting => _gateSequence.IsAlive && _gateSequence.IsWaiting;

	/// <summary>Progress of the showcase sequence, 0..1. Zero when it's not playing.</summary>
	public float SequenceProgress => _sequence.IsAlive ? _sequence.ProgressTotal : 0;

	public TweenManager Manager => TweenManager.Get( Scene );

	GameObject _root;
	GameObject _sequenceBox;
	GameObject _punchBox;
	GameObject _colorSphere;
	PointLight _light;
	GameObject _asyncBox;
	TextRenderer _asyncLabel;
	GameObject _orbitPivot;
	GameObject _speedBox;
	Sequence _sequence;
	GameObject _gateBox;
	Sequence _gateSequence;
	Tween.Clock _raceClock;
	Tween _timeScaleTween;

	protected override void OnStart()
	{
		
		Rebuild();
	}

	/// <summary>Destroys and recreates all stands with their idle animations.</summary>
	public void Rebuild()
	{
		Tween.StopAll();
		_root?.Destroy();
		_root = new GameObject( GameObject, true, "Stands" );

		CreateFloor();
		BuildEaseRace( StandPosition( 0 ) );
		BuildSequenceStand( StandPosition( 1 ) );
		BuildShakeStand( StandPosition( 2 ) );
		BuildColorStand( StandPosition( 3 ) );
		BuildAsyncStand( StandPosition( 4 ) );
		BuildSpeedStand( StandPosition( 5 ) );
		BuildGateStand( StandPosition( 6 ) );

		IsPaused = false;
		IsGateOpen = false;
		IsRaceSlow = false;
		Report( "Demo rebuilt" );
	}

	static Vector3 StandPosition( int index ) => new( 0, (3 - index) * StandSpacing, 0 );

	#region Stands

	void BuildEaseRace( Vector3 position )
	{
		var stand = CreateStand( "Ease Race", position );
		// One clock drives the whole race, so its TimeScale slows down all six tweens at once
		_raceClock = new Tween.Clock();
		for ( int i = 0; i < RaceEases.Length; i++ )
		{
			float y = (RaceEases.Length * 0.5f - 0.5f - i) * 21;
			var box = CreateModel( stand, "models/dev/box.vmdl", new Vector3( 0, y, 70 ), 0.4f, RaceColors[i] );
			box.Name = RaceEases[i].ToString();

			// Infinite yoyo: the same animation with a different ease on every box
			Tween.LocalPositionZ( box, 70, 200, 1.6f, RaceEases[i], cycles: -1, cycleMode: CycleMode.Yoyo ).SetClock( _raceClock );

			// Vertical label under the box
			var label = CreateLabel( stand, RaceEases[i].ToString(), new Vector3( -10, y, 30 ), 0.1f );
			label.GameObject.LocalRotation = Rotation.FromRoll( 90 );
			label.Color = RaceColors[i];
		}
	}

	void BuildSequenceStand( Vector3 position )
	{
		var stand = CreateStand( "Sequence", position );
		_sequenceBox = CreateModel( stand, "models/dev/box.vmdl", new Vector3( 0, 0, 40 ), 0.8f, "#35a7ff" );

		// Idle hover driven by inspector settings
		Tween.LocalPositionZ( _sequenceBox, HoverSettings );
	}

	void BuildShakeStand( Vector3 position )
	{
		var stand = CreateStand( "Shake & Punch", position );
		_punchBox = CreateModel( stand, "models/dev/box.vmdl", new Vector3( 0, 0, 50 ), 0.8f, "#ffb84d" );
	}

	void BuildColorStand( Vector3 position )
	{
		var stand = CreateStand( "Color & Light", position );
		_colorSphere = CreateModel( stand, "models/dev/sphere.vmdl", new Vector3( 0, 0, 60 ), 1, Color.White );
		var renderer = _colorSphere.Components.Get<ModelRenderer>();

		Sequence.Create( cycles: -1 )
			.Chain( Tween.Color( renderer, "#ff5964", 1, Ease.InOutSine ) )
			.Chain( Tween.Color( renderer, "#6bf178", 1, Ease.InOutSine ) )
			.Chain( Tween.Color( renderer, "#35a7ff", 1, Ease.InOutSine ) );

		var lightObject = new GameObject( stand, true, "Light" );
		lightObject.LocalPosition = new Vector3( 50, 0, 150 );
		_light = lightObject.Components.Create<PointLight>();
		_light.LightColor = "#ffe74c";
		_light.Radius = 150;

		Tween.LightRadius( _light, 150, 320, 1.5f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo );
		Tween.LocalScale( _colorSphere, 1, 1.25f, 0.75f, Ease.InOutQuad, cycles: -1, cycleMode: CycleMode.Yoyo );
	}

	void BuildAsyncStand( Vector3 position )
	{
		var stand = CreateStand( "Callbacks & Async", position );
		_asyncBox = CreateModel( stand, "models/dev/box.vmdl", new Vector3( 0, 0, 25 ), 0.6f, "#b38cff" );
		_asyncLabel = CreateLabel( stand, "", new Vector3( 0, 0, 190 ), 0.15f );
		_ = RunAsyncLoop( _asyncBox );
	}

	void BuildSpeedStand( Vector3 position )
	{
		var stand = CreateStand( "Speed & Cycles", position );
		_orbitPivot = new GameObject( stand, true, "Orbit Pivot" );
		_orbitPivot.LocalPosition = new Vector3( 0, 0, 80 );
		CreateModel( _orbitPivot, "models/dev/sphere.vmdl", new Vector3( 0, 55, 0 ), 0.3f, "#6bf178" );

		// LocalAngles interpolates Euler angles, so it can spin a full 360 degrees
		Tween.LocalAngles( _orbitPivot, Angles.Zero, new Angles( 0, 360, 0 ), 2, Ease.Linear, cycles: -1 );

		_speedBox = CreateModel( stand, "models/dev/box.vmdl", new Vector3( 0, -40, 20 ), 0.3f, "#ff5964" );
		// Incremental cycles keep stacking the rotation
		Tween.LocalRotation( _speedBox, Rotation.Identity, Rotation.FromYaw( 90 ), 0.5f, Ease.OutBack, cycles: -1, cycleMode: CycleMode.Incremental );
	}

	void BuildGateStand( Vector3 position )
	{
		var stand = CreateStand( "Wait & Additive", position );
		_gateBox = CreateModel( stand, "models/dev/box.vmdl", new Vector3( 0, 0, 25 ), 0.6f, "#6bf178" );
		PlayGateLoop();
	}

	/// <summary>Rises, waits until the gate is opened from the UI, falls down and repeats.</summary>
	void PlayGateLoop()
	{
		var renderer = _gateBox.Components.Get<ModelRenderer>();
		_gateSequence = Sequence.Create()
			.Chain( Tween.LocalPositionZ( _gateBox, 25, 150, 0.6f, Ease.OutBack ) )
			.Group( Tween.Color( renderer, "#ffb84d", 0.3f ) )
			.ChainWaitUntil( this, static demo => demo.IsGateOpen )
			.Chain( Tween.Color( renderer, "#6bf178", 0.2f ) )
			.Chain( Tween.LocalPositionZ( _gateBox, 150, 25, 0.5f, Ease.OutBounce ) )
			.ChainCallback( this, static demo => demo.IsGateOpen = false )
			.ChainDelay( 0.3f )
			.OnComplete( this, static demo => demo.PlayGateLoop() );
	}

	async Task RunAsyncLoop( GameObject box )
	{
		try
		{
			while ( box.IsValid() )
			{
				_asyncLabel.Text = "await move up";
				await Tween.LocalPositionZ( box, 140, 0.6f, Ease.OutCubic );
				_asyncLabel.Text = "await spin";
				await Tween.LocalAngles( box, Angles.Zero, new Angles( 0, 180, 0 ), 0.6f, Ease.InOutBack );
				_asyncLabel.Text = "await Delay";
				await Tween.Delay( this, 0.3f, static _ => { } );
				_asyncLabel.Text = "await fall";
				await Tween.LocalPositionZ( box, 25, 0.5f, TweenEasing.BounceExact( 25 ) );
				AsyncLoops++;
			}
		}
		catch ( OperationCanceledException )
		{
			// The scene was unloaded while waiting
		}
	}

	#endregion

	#region Actions

	/// <summary>A sequence that groups, chains, inserts and calls back.</summary>
	public void PlaySequence()
	{
		if ( _sequence.IsAlive )
			_sequence.Complete();

		var box = _sequenceBox;
		var renderer = box.Components.Get<ModelRenderer>();
		Tween.StopAll( box );

		// Spin relative to the current yaw, so every play rotates the box by another half turn
		float yaw = box.LocalRotation.Angles().yaw;

		_sequence = Sequence.Create()
			.Chain( Tween.LocalPositionZ( box, 140, 0.5f, Ease.OutBack ) )
			.Group( Tween.LocalScale( box, 0.5f, 0.5f, Ease.OutQuad ) )
			.ChainCallback( this, static demo => demo.OnSequenceCallback( "Moved up" ) )
			.Chain( Tween.LocalAngles( box, new Angles( 0, yaw, 0 ), new Angles( 0, yaw + 180, 0 ), 0.6f, Ease.InOutSine ) )
			.Insert( 0.5f, Tween.Color( renderer, "#ff5964", 0.6f ) )
			.ChainDelay( 0.2f )
			.Chain( Tween.LocalPositionZ( box, 40, 0.6f, Ease.OutBounce ) )
			.Group( Tween.LocalScale( box, 0.8f, 0.6f, Ease.OutElastic ) )
			.Group( Tween.Color( renderer, "#35a7ff", 0.6f ) )
			.ChainCallback( this, static demo => demo.OnSequenceCallback( "Landed" ) )
			.OnComplete( this, static demo => demo.ResumeHover() );

		Report( "Sequence started" );
	}

	void OnSequenceCallback( string message )
	{
		Callbacks++;
		Report( $"Sequence callback: {message}" );
	}

	void ResumeHover()
	{
		Tween.LocalPositionZ( _sequenceBox, HoverSettings );
		Report( "Sequence completed" );
	}

	public void Punch()
	{
		Tween.PunchLocalScale( _punchBox, new Vector3( 0.4f, 0.4f, -0.3f ), 0.5f, 8 );
		Tween.PunchLocalPosition( _punchBox, PunchSettings );
		Report( "Punch" );
	}

	public void ShakeObject()
	{
		Tween.ShakeLocalRotation( _punchBox, new Vector3( 15, 15, 15 ), 0.8f, 12 );
		Tween.ShakeLocalPosition( _punchBox, new Vector3( 0, 15, 10 ), 0.8f, 12 );
		Report( "Shake object" );
	}

	public void ShakeCamera()
	{
		if ( Scene.Camera is { } camera )
			Tween.ShakeCamera( camera, 0.8f );
		Report( "Shake camera" );
	}

	public void ToggleSlowMotion()
	{
		IsSlowMotion = !IsSlowMotion;
		_timeScaleTween.Stop();
		_timeScaleTween = Tween.GlobalTimeScale( Scene, new TweenSettings<float>( IsSlowMotion ? 0.2f : 1, new TweenSettings( 0.4f, Ease.OutQuad, useUnscaledTime: true ) ) );
		Report( IsSlowMotion ? "Slow motion on" : "Slow motion off" );
	}

	public void TogglePause()
	{
		IsPaused = !IsPaused;
		int count = Tween.SetPausedAll( IsPaused );
		Report( $"{(IsPaused ? "Paused" : "Resumed")} {count} animations" );
	}

	public void CompleteAll()
	{
		int count = Tween.CompleteAll();
		Report( $"Completed {count} animations" );
	}

	public void StopAll()
	{
		int count = Tween.StopAll();
		Report( $"Stopped {count} animations" );
	}

	/// <summary>Lets the waiting gate sequence continue.</summary>
	public void OpenGate()
	{
		IsGateOpen = true;
		Report( "Gate opened" );
	}

	/// <summary>Each click adds a quarter turn; additive tweens blend, so fast clicks stack smoothly.</summary>
	public void AdditiveSpin()
	{
		Tween.LocalRotationAdditive( _gateBox, new Angles( 0, 90, 0 ), 0.6f, Ease.OutBack );
		Report( "Additive spin" );
	}

	/// <summary>Slows down all race tweens at once through their shared clock.</summary>
	public void ToggleRaceClock()
	{
		IsRaceSlow = !IsRaceSlow;
		_raceClock.TimeScale = IsRaceSlow ? 0.25f : 1;
		Report( IsRaceSlow ? "Race clock x0.25" : "Race clock x1" );
	}

	void Report( string message )
	{
		LastEvent = message;
	}

	#endregion

	#region Building helpers

	void CreateFloor()
	{
		var floor = CreateModel( _root, "models/dev/box.vmdl", new Vector3( 0, 0, -5 ), 1, "#2a2f3a" );
		floor.Name = "Floor";
		floor.LocalScale = new Vector3( 8, 22, 0.2f );
	}

	GameObject CreateStand( string title, Vector3 position )
	{
		var stand = new GameObject( _root, true, title );
		stand.LocalPosition = position;

		var pad = CreateModel( stand, "models/dev/box.vmdl", new Vector3( 0, 0, 1 ), 1, "#3b4252" );
		pad.Name = "Pad";
		pad.LocalScale = new Vector3( 2.4f, 2.6f, 0.08f );

		CreateLabel( stand, title, new Vector3( 0, 0, 230 ), 0.22f );
		return stand;
	}

	static GameObject CreateModel( GameObject parent, string model, Vector3 position, float scale, Color tint )
	{
		var go = new GameObject( parent, true, System.IO.Path.GetFileNameWithoutExtension( model ) );
		go.LocalPosition = position;
		go.LocalScale = scale;
		var renderer = go.Components.Create<ModelRenderer>();
		renderer.Model = Model.Load( model );
		renderer.Tint = tint;
		return go;
	}

	static TextRenderer CreateLabel( GameObject parent, string text, Vector3 position, float scale )
	{
		var go = new GameObject( parent, true, "Label" );
		go.LocalPosition = position;
		var label = go.Components.Create<TextRenderer>();
		label.Text = text;
		label.Scale = scale;
		label.FontSize = 64;
		label.Color = Color.White;
		return label;
	}

	#endregion
}
