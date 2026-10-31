namespace SboxTweenExample;

/// <summary>
/// Stands of the s&amp;box ecosystem: shader attributes, animgraph and morphs, post-processing, typewriter, lights and rigidbodies.
/// </summary>
public sealed partial class TweenDemo
{
	[SkipHotload] static readonly string[] Quotes =
	[
		"Tweens are non-reusable.\nStart a new one,\nit's cheap.",
		"Wait... did it pause?\nYes, after punctuation!",
		"Hello, s&box.\nHello, PrimeTween.",
		"Sequences chain,\ngroup and insert.\nEasy, right?",
	];

	public bool IsCrouching { get; private set; }
	public bool IsWalking { get; private set; }
	public bool IsNoir { get; private set; }
	public bool IsNight { get; private set; }
	public bool IsFastTyping { get; private set; }
	public bool IsFanFast { get; private set; }

	ModelRenderer _shaderRenderer;
	Color _glowColor;
	Sequence _dissolve;
	SkinnedModelRenderer _citizen;
	TextRenderer _typewriterText;
	Tween _typing;
	int _quoteIndex;
	PointLight _lamp;
	Spinner _fan;
	GameObject _physicsStand;
	GameObject _crates;
	Rigidbody _pusher;

	Vignette _vignette;
	ChromaticAberration _aberration;
	ColorAdjustments _adjustments;
	Blur _blur;
	FilmGrain _grain;

	DirectionalLight _sun;
	SkyBox2D _sky;
	(float Intensity, Color Color, Color SkyColor, Color SkyTint) _day;
	bool _isDayCaptured;

	#region Shader Attributes

	void BuildShaderStand( Vector3 position )
	{
		var stand = CreateStand( "Shader Attributes", position );
		var orb = CreateModel( stand, "models/dev/sphere.vmdl", new Vector3( 0, 0, 80 ), 1.3f, Color.White );
		_shaderRenderer = orb.Components.Get<ModelRenderer>();
		// The shader holds the default values of the attributes
		_shaderRenderer.MaterialOverride = Material.Load( "materials/tween_dissolve.vmat" );
		_glowColor = new Color( 1, 0.45f, 0.1f );

		// The attribute is animated by name, like any other value
		_shaderRenderer.TweenAttribute( "Glow", 0, 0.4f, 1.4f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo );
		Tween.LocalAngles( orb, Angles.Zero, new Angles( 0, 360, 0 ), 10, Ease.Linear, cycles: -1 );
	}

	public void Dissolve()
	{
		if ( _dissolve.IsAlive )
			return;

		_dissolve = Sequence.Create( _shaderRenderer.TweenAttribute( "Dissolve", 0, 1, 1.4f, Ease.InQuad ) )
			.ChainDelay( 0.4f )
			.Chain( _shaderRenderer.TweenAttribute( "Dissolve", 1, 0, 1, Ease.OutQuad ) );
		Report( "Dissolve" );
	}

	public void ShiftGlowColor()
	{
		var color = new ColorHsv( Random.Shared.Float( 0, 360 ), 0.85f, 1 ).ToColor();
		// The default value lives in the shader, the renderer attributes can't read it, so the start value is passed explicitly
		_shaderRenderer.TweenAttribute( "GlowColor", _glowColor, color, 0.6f, Ease.OutQuad );
		_glowColor = color;
		Report( "Glow color" );
	}

	#endregion

	#region AnimGraph & Morphs

	void BuildCitizenStand( Vector3 position )
	{
		var stand = CreateStand( "AnimGraph & Morphs", position );
		var go = new GameObject( stand, true, "Citizen" );
		go.LocalPosition = new Vector3( 0, 0, 3 );
		go.LocalRotation = Rotation.FromYaw( 180 );
		go.LocalScale = 1.6f;

		_citizen = go.Components.Create<SkinnedModelRenderer>();
		_citizen.Model = Model.Load( "models/citizen/citizen.vmdl" );
		_citizen.Set( "aim_head_weight", 1f );

		// Look around: a Vector3 animgraph parameter
		_citizen.TweenParameter( "aim_head", new Vector3( 1, -0.7f, 0.1f ), new Vector3( 1, 0.7f, 0.1f ), 2.4f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo );

		IsCrouching = false;
		IsWalking = false;
	}

	public void ToggleCrouch()
	{
		IsCrouching = !IsCrouching;
		_citizen.TweenParameter( "duck", IsCrouching ? 1 : 0, 0.4f, Ease.OutQuad );
		Report( IsCrouching ? "Crouch" : "Stand up" );
	}

	public void ToggleWalk()
	{
		IsWalking = !IsWalking;
		float speed = IsWalking ? 110 : 0;
		_citizen.TweenParameter( "move_groundspeed", speed, 0.6f, Ease.InOutSine );
		_citizen.TweenParameter( "move_x", speed, 0.6f, Ease.InOutSine );
		_citizen.TweenParameter( "wish_groundspeed", speed, 0.6f, Ease.InOutSine );
		Report( IsWalking ? "Walk in place" : "Stop walking" );
	}

	/// <summary>Face morphs: both mouth corners and the brows go up, hold, and relax.</summary>
	public void Smile()
	{
		Sequence.Create( _citizen.TweenMorph( "lipcornerpullerL", 0, 1, 0.3f, Ease.OutQuad ) )
			.Group( _citizen.TweenMorph( "lipcornerpullerR", 0, 1, 0.3f, Ease.OutQuad ) )
			.Group( _citizen.TweenMorph( "innerbrowraiserL", 0, 0.8f, 0.3f, Ease.OutQuad ) )
			.Group( _citizen.TweenMorph( "innerbrowraiserR", 0, 0.8f, 0.3f, Ease.OutQuad ) )
			.ChainDelay( 1 )
			.Chain( _citizen.TweenMorph( "lipcornerpullerL", 0, 0.5f, Ease.InOutSine ) )
			.Group( _citizen.TweenMorph( "lipcornerpullerR", 0, 0.5f, Ease.InOutSine ) )
			.Group( _citizen.TweenMorph( "innerbrowraiserL", 0, 0.5f, Ease.InOutSine ) )
			.Group( _citizen.TweenMorph( "innerbrowraiserR", 0, 0.5f, Ease.InOutSine ) );
		Report( "Smile" );
	}

	public void Talk()
	{
		// A yoyo on the jaw morphs, with a slightly different rhythm per side
		_citizen.TweenMorph( "openjawL", 0, 0.7f, 0.13f, Ease.InOutSine, cycles: 12, cycleMode: CycleMode.Yoyo );
		_citizen.TweenMorph( "openjawR", 0, 0.6f, 0.15f, Ease.InOutSine, cycles: 10, cycleMode: CycleMode.Yoyo );
		Report( "Talk" );
	}

	#endregion

	#region Post FX

	void BuildPostProcessStand( Vector3 position )
	{
		var stand = CreateStand( "Post FX", position );
		var ring = new GameObject( stand, true, "Ring" );
		ring.LocalPosition = new Vector3( 0, 0, 90 );
		for ( int i = 0; i < 8; i++ )
		{
			float angle = i * MathF.PI / 4;
			var color = new ColorHsv( i * 45, 0.75f, 1 ).ToColor();
			var box = CreateModel( ring, "models/dev/box.vmdl", new Vector3( 0, MathF.Cos( angle ) * 45, MathF.Sin( angle ) * 45 ), 0.3f, color );
			Tween.LocalScale( box, 0.2f, 0.4f, 0.6f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo, startDelay: i * 0.075f );
		}

		Tween.LocalAngles( ring, Angles.Zero, new Angles( 0, 0, 360 ), 6, Ease.Linear, cycles: -1 );
		SetupPostProcess();
	}

	/// <summary>The effects live on the camera, at values that don't change the image.</summary>
	void SetupPostProcess()
	{
		if ( Scene.Camera?.GameObject is not { } camera )
			return;

		_vignette = camera.Components.GetOrCreate<Vignette>();
		_vignette.Intensity = 0;
		_vignette.Color = Color.Black;
		_aberration = camera.Components.GetOrCreate<ChromaticAberration>();
		_aberration.Scale = 0;
		_adjustments = camera.Components.GetOrCreate<ColorAdjustments>();
		_adjustments.Saturation = 1;
		_adjustments.Brightness = 1;
		_adjustments.Contrast = 1;
		_adjustments.HueRotate = 0;
		_blur = camera.Components.GetOrCreate<Blur>();
		_blur.Size = 0;
		_grain = camera.Components.GetOrCreate<FilmGrain>();
		_grain.Intensity = 0;
		IsNoir = false;
	}

	/// <summary>A damage flash: red vignette, aberration and a desaturated punch that fade back.</summary>
	public void HitEffect()
	{
		float saturation = IsNoir ? 0 : 1;
		Sequence.Create( _vignette.TweenColor( new Color( 0.6f, 0, 0 ), 0.05f ) )
			.Group( _vignette.TweenIntensity( 0.9f, 0.08f, Ease.OutQuad ) )
			.Group( _aberration.TweenScale( 1, 0.08f, Ease.OutQuad ) )
			.Group( _adjustments.TweenSaturation( 0.2f, 0.08f ) )
			.Chain( _vignette.TweenIntensity( IsNoir ? 0.35f : 0, 0.6f, Ease.OutCubic ) )
			.Group( _aberration.TweenScale( 0, 0.6f, Ease.OutCubic ) )
			.Group( _adjustments.TweenSaturation( saturation, 0.6f, Ease.OutCubic ) )
			.Chain( _vignette.TweenColor( Color.Black, 0.1f ) );

		if ( Scene.Camera is { } camera )
			Tween.ShakeCamera( camera, 0.5f, duration: 0.3f );
		Report( "Hit" );
	}

	/// <summary>Blur in, a full hue rotation, blur out.</summary>
	public void DreamEffect()
	{
		Sequence.Create( _blur.TweenSize( 0, 0.5f, 0.6f, Ease.OutQuad ) )
			.Group( _adjustments.TweenHueRotate( 0, 360, 2.4f, Ease.InOutSine ) )
			.Group( _adjustments.TweenBrightness( 1, 1.25f, 0.6f ) )
			.Insert( 1.8f, _blur.TweenSize( 0, 0.6f, Ease.InQuad ) )
			.Insert( 1.8f, _adjustments.TweenBrightness( 1, 0.6f ) )
			.ChainCallback( _adjustments, static adjustments => adjustments.HueRotate = 0 );
		Report( "Dream" );
	}

	public void ToggleNoir()
	{
		IsNoir = !IsNoir;
		_adjustments.TweenSaturation( IsNoir ? 0 : 1, 0.8f, Ease.InOutSine );
		_adjustments.TweenContrast( IsNoir ? 1.35f : 1, 0.8f, Ease.InOutSine );
		_vignette.TweenIntensity( IsNoir ? 0.35f : 0, 0.8f, Ease.InOutSine );
		_grain.TweenIntensity( IsNoir ? 0.05f : 0, 0.8f, Ease.InOutSine );
		Report( IsNoir ? "Noir on" : "Noir off" );
	}

	#endregion

	#region Typewriter

	void BuildTypewriterStand( Vector3 position )
	{
		var stand = CreateStand( "Typewriter", position );
		var screen = CreateModel( stand, "models/dev/box.vmdl", new Vector3( 8, 0, 100 ), 1, "#1d2230" );
		screen.LocalScale = new Vector3( 0.1f, 2.5f, 1.5f );

		_typewriterText = CreateLabel( stand, "", new Vector3( 0, 0, 100 ), 0.16f );
		_typewriterText.Color = "#6bf178";
		_quoteIndex = 0;
		IsFastTyping = false;
		TypeNextQuote();
	}

	/// <summary>Types a quote, holds it, then starts the next one from OnComplete.</summary>
	void TypeNextQuote()
	{
		if ( !_typewriterText.IsValid() )
			return;

		string quote = Quotes[_quoteIndex++ % Quotes.Length];
		_typing = _typewriterText.TweenTypewriterAtSpeed( quote, IsFastTyping ? 45 : 16, endDelay: 1.6f )
			.OnComplete( this, OnQuoteTyped );
	}

	// Endless loops use named methods: a hotload can't always match the lambdas of a running tween
	static void OnQuoteTyped( TweenDemo demo ) => demo.TypeNextQuote();

	/// <summary>Completing the tween shows the whole text and starts the next quote.</summary>
	public void SkipQuote()
	{
		_typing.Complete();
		Report( "Skip quote" );
	}

	public void ToggleTypingSpeed()
	{
		IsFastTyping = !IsFastTyping;
		Report( IsFastTyping ? "Fast typing" : "Slow typing" );
	}

	#endregion

	#region Lights

	void BuildLightsStand( Vector3 position )
	{
		var stand = CreateStand( "Lights", position );
		var post = CreateModel( stand, "models/dev/box.vmdl", new Vector3( 0, 0, 60 ), 1, "#4c566a" );
		post.LocalScale = new Vector3( 0.12f, 0.12f, 2.4f );
		var bulb = CreateModel( stand, "models/dev/sphere.vmdl", new Vector3( 0, 0, 130 ), 0.35f, "#fff1c1" );

		var lampObject = new GameObject( bulb, true, "Lamp" );
		_lamp = lampObject.Components.Create<PointLight>();
		_lamp.Radius = 260;
		_lamp.LightColor = Color.White;

		// Warm to cold and back: the temperature is in Kelvin
		_lamp.TweenTemperature( 1800, 9000, 3, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo );

		var fanObject = new GameObject( stand, true, "Fan" );
		fanObject.LocalPosition = new Vector3( -30, 0, 60 );
		for ( int i = 0; i < 3; i++ )
		{
			var blade = CreateModel( fanObject, "models/dev/box.vmdl", Vector3.Zero, 1, "#88c0d0" );
			blade.LocalScale = new Vector3( 0.05f, 0.9f, 0.12f );
			blade.LocalRotation = Rotation.FromRoll( i * 60 );
		}

		fanObject.LocalRotation = Rotation.FromYaw( 90 );
		_fan = fanObject.Components.Create<Spinner>();
		_fan.Speed = 120;
		IsFanFast = false;

		CaptureDay();
	}

	void CaptureDay()
	{
		_sun ??= Scene.GetAllComponents<DirectionalLight>().FirstOrDefault();
		_sky ??= Scene.GetAllComponents<SkyBox2D>().FirstOrDefault();
		if ( !_isDayCaptured && _sun.IsValid() )
		{
			_day = (_sun.Intensity, _sun.LightColor, _sun.SkyColor, _sky?.Tint ?? Color.White);
			_isDayCaptured = true;
		}

		// Rebuild during the night goes back to the day
		if ( IsNight && _sun.IsValid() )
		{
			_sun.Intensity = _day.Intensity;
			_sun.LightColor = _day.Color;
			_sun.SkyColor = _day.SkyColor;
			if ( _sky.IsValid() )
				_sky.Tint = _day.SkyTint;
		}

		IsNight = false;
	}

	public void ToggleDayNight()
	{
		if ( !_sun.IsValid() )
			return;

		IsNight = !IsNight;
		const float duration = 1.5f;
		var night = (Intensity: 0.08f, Color: new Color( 0.5f, 0.6f, 1 ), SkyColor: new Color( 0.02f, 0.03f, 0.08f ), SkyTint: new Color( 0.12f, 0.14f, 0.25f ));
		_sun.TweenIntensity( IsNight ? night.Intensity : _day.Intensity, duration, Ease.InOutSine );
		_sun.TweenColor( IsNight ? night.Color : _day.Color, duration, Ease.InOutSine );
		_sun.TweenSkyColor( IsNight ? night.SkyColor : _day.SkyColor, duration, Ease.InOutSine );
		// SkyBox2D.Tint has no dedicated tween, so it's animated by name through the TypeLibrary
		if ( _sky.IsValid() )
			_sky.TweenProperty( "Tint", IsNight ? night.SkyTint : _day.SkyTint, duration, Ease.InOutSine );

		_lamp.TweenIntensity( IsNight ? 3 : 1, duration, Ease.InOutSine );
		Report( IsNight ? "Night" : "Day" );
	}

	/// <summary>A few quick random intensity steps, then back to normal.</summary>
	public void FlickerLamp()
	{
		float normal = IsNight ? 3 : 1;
		var sequence = Sequence.Create();
		for ( int i = 0; i < 6; i++ )
			sequence.Chain( _lamp.TweenIntensity( Random.Shared.Float( 0, normal * 1.5f ), Random.Shared.Float( 0.03f, 0.08f ), Ease.Linear ) );
		sequence.Chain( _lamp.TweenIntensity( normal, 0.2f ) );
		Report( "Flicker" );
	}

	/// <summary>Spinner.Speed is a plain [Property]: Tween.Property finds it by name.</summary>
	public void ToggleFanSpeed()
	{
		IsFanFast = !IsFanFast;
		_fan.TweenProperty( "Speed", IsFanFast ? 900 : 120, 1.2f, Ease.InOutCubic );
		Report( IsFanFast ? "Fan fast" : "Fan slow" );
	}

	#endregion

	#region Rigidbody

	void BuildPhysicsStand( Vector3 position )
	{
		_physicsStand = CreateStand( "Rigidbody", position );

		var pusher = CreateModel( _physicsStand, "models/dev/box.vmdl", PusherStart, 1, "#ff5964" );
		pusher.LocalScale = new Vector3( 0.8f, 0.25f, 0.6f );
		AddBoxCollider( pusher );
		_pusher = pusher.Components.Create<Rigidbody>();
		_pusher.Gravity = false;
		_pusher.MassOverride = 2000;
		// Only slides along Y: the tween drives the velocity, the locks keep it from tipping over
		_pusher.Locking = new PhysicsLock { X = true, Z = true, Pitch = true, Yaw = true, Roll = true };

		// A turnstile in the corner, out of the pusher's way
		var paddlePivot = CreateModel( _physicsStand, "models/dev/box.vmdl", new Vector3( 38, -36, 12 ), 1, "#ffe74c" );
		paddlePivot.LocalScale = new Vector3( 0.7f, 0.12f, 0.3f );
		AddBoxCollider( paddlePivot );
		var paddle = paddlePivot.Components.Create<Rigidbody>();
		paddle.Gravity = false;
		paddle.MassOverride = 2000;
		paddle.Locking = new PhysicsLock { X = true, Y = true, Z = true, Pitch = true, Roll = true };
		// Incremental cycles keep turning the paddle a quarter turn after another
		paddle.TweenRotation( paddle.WorldRotation, paddle.WorldRotation * Rotation.FromYaw( 90 ), 0.8f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Incremental );

		ResetCrates();
		PlayPusherLoop();
	}

	static Vector3 PusherStart => new( 0, 52, 19 );

	/// <summary>The rigidbody tweens update in FixedUpdate, so the sequence uses it too.</summary>
	void PlayPusherLoop()
	{
		var start = _physicsStand.WorldPosition + PusherStart;
		var end = _physicsStand.WorldPosition + new Vector3( 0, -38, 19 );
		Sequence.Create( cycles: -1, updateType: UpdateType.FixedUpdate )
			.ChainDelay( 1.2f )
			.Chain( _pusher.TweenPosition( start, end, 1.2f, Ease.InOutCubic ) )
			.ChainDelay( 0.4f )
			.Chain( _pusher.TweenPosition( end, start, 1.1f, Ease.InOutSine ) )
			.ChainDelay( 1.5f );
	}

	public void ResetCrates()
	{
		_crates?.Destroy();
		_crates = new GameObject( _physicsStand, true, "Crates" );
		for ( int row = 0; row < 3; row++ )
		{
			for ( int column = 0; column < 3 - row; column++ )
			{
				var crate = CreateModel( _crates, "models/dev/box.vmdl", new Vector3( -10, column * 17 - 17 + row * 8.5f, 12 + row * 17 ), 0.32f, new ColorHsv( 30 + row * 25, 0.6f, 0.9f ).ToColor() );
				AddBoxCollider( crate );
				var body = crate.Components.Create<Rigidbody>();
				body.MassOverride = 20;
			}
		}

		Report( "Crates reset" );
	}

	#endregion
}