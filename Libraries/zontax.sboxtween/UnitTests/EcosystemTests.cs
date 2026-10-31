namespace SboxTween.UnitTests;

/// <summary>
/// Component properties of the s&amp;box ecosystem: lights, camera, text, post-processing and fog.
/// </summary>
[TestClass]
public class ComponentPropertyTests : TweenTestBase
{
	/// <summary>
	/// Starts a 1 second linear tween from <paramref name="from"/> to <paramref name="end"/> and checks the value at the middle and at the end.
	/// </summary>
	void CheckFloat( string name, Func<float, Tween> start, Func<float> read, float end, float from = 0 )
	{
		start( end );
		EndFrame();
		AreEqual( from, read(), Epsilon, $"{name} at the start" );
		Tick( 0.5f );
		AreEqual( (from + end) / 2, read(), 0.001f, $"{name} in the middle" );
		Tick( 0.5f );
		AreEqual( end, read(), Epsilon, $"{name} at the end" );
	}

	static TweenSettings Linear => new( 1, Ease.Linear );

	[TestMethod]
	public void FloatPropertiesAnimate()
	{
		var go = CreateObject();
		var light = go.Components.Create<PointLight>();
		var camera = go.Components.Create<CameraComponent>();
		var text = go.Components.Create<TextRenderer>();
		var bloom = go.Components.Create<Bloom>();
		var vignette = go.Components.Create<Vignette>();
		var adjustments = go.Components.Create<ColorAdjustments>();
		var aberration = go.Components.Create<ChromaticAberration>();
		var blur = go.Components.Create<Blur>();
		var pixelate = go.Components.Create<Pixelate>();
		var grain = go.Components.Create<FilmGrain>();
		var dof = go.Components.Create<DepthOfField>();
		var motionBlur = go.Components.Create<MotionBlur>();
		var sharpen = go.Components.Create<Sharpen>();
		var volume = go.Components.Create<PostProcessVolume>();
		var fog = go.Components.Create<Fog>();
		var volumetricFog = go.Components.Create<VolumetricFogVolume>();

		CheckFloat( "Intensity", e => light.TweenIntensity( 0, e, 1, Ease.Linear ), () => light.Intensity, 4 );
		CheckFloat( "Temperature", e => light.TweenTemperature( 1000, e, 1, Ease.Linear ), () => light.Temperature, 9000, 1000 );
		CheckFloat( "ZNear", e => camera.TweenZNear( 0, e, 1, Ease.Linear ), () => camera.ZNear, 20 );
		CheckFloat( "ZFar", e => camera.TweenZFar( 0, e, 1, Ease.Linear ), () => camera.ZFar, 5000 );
		CheckFloat( "FontSize", e => text.TweenFontSize( 0, e, 1, Ease.Linear ), () => text.FontSize, 64 );
		CheckFloat( "TextScale", e => text.TweenScale( 0, e, 1, Ease.Linear ), () => text.Scale, 2 );
		CheckFloat( "BloomStrength", e => bloom.TweenStrength( 0, e, 1, Ease.Linear ), () => bloom.Strength, 3 );
		CheckFloat( "BloomThreshold", e => bloom.TweenThreshold( 0, e, 1, Ease.Linear ), () => bloom.Threshold, 0.5f );
		CheckFloat( "VignetteIntensity", e => vignette.TweenIntensity( 0, e, 1, Ease.Linear ), () => vignette.Intensity, 0.8f );
		CheckFloat( "VignetteSmoothness", e => vignette.TweenSmoothness( 0, e, 1, Ease.Linear ), () => vignette.Smoothness, 0.6f );
		CheckFloat( "Blend", e => adjustments.TweenBlend( 0, e, 1, Ease.Linear ), () => adjustments.Blend, 1 );
		CheckFloat( "Saturation", e => adjustments.TweenSaturation( 0, e, 1, Ease.Linear ), () => adjustments.Saturation, 2 );
		CheckFloat( "HueRotate", e => adjustments.TweenHueRotate( 0, e, 1, Ease.Linear ), () => adjustments.HueRotate, 180 );
		CheckFloat( "Brightness", e => adjustments.TweenBrightness( 0, e, 1, Ease.Linear ), () => adjustments.Brightness, 1.5f );
		CheckFloat( "Contrast", e => adjustments.TweenContrast( 0, e, 1, Ease.Linear ), () => adjustments.Contrast, 1.5f );
		CheckFloat( "Aberration", e => aberration.TweenScale( 0, e, 1, Ease.Linear ), () => aberration.Scale, 1 );
		CheckFloat( "BlurSize", e => blur.TweenSize( 0, e, 1, Ease.Linear ), () => blur.Size, 1 );
		CheckFloat( "Pixelate", e => pixelate.TweenScale( 0, e, 1, Ease.Linear ), () => pixelate.Scale, 0.5f );
		CheckFloat( "FilmGrain", e => grain.TweenIntensity( 0, e, 1, Ease.Linear ), () => grain.Intensity, 0.4f );
		CheckFloat( "FocalDistance", e => dof.TweenFocalDistance( 0, e, 1, Ease.Linear ), () => dof.FocalDistance, 500 );
		CheckFloat( "DofBlurSize", e => dof.TweenBlurSize( 0, e, 1, Ease.Linear ), () => dof.BlurSize, 40 );
		CheckFloat( "MotionBlur", e => motionBlur.TweenScale( 0, e, 1, Ease.Linear ), () => motionBlur.Scale, 0.5f );
		CheckFloat( "Sharpen", e => sharpen.TweenScale( 0, e, 1, Ease.Linear ), () => sharpen.Scale, 3 );
		CheckFloat( "BlendWeight", e => volume.TweenBlendWeight( 0, e, 1, Ease.Linear ), () => volume.BlendWeight, 1 );
		CheckFloat( "FogStart", e => fog.TweenStartDistance( 0, e, 1, Ease.Linear ), () => fog.StartDistance, 300 );
		CheckFloat( "FogAttenuation", e => fog.TweenAttenuationDistance( 0, e, 1, Ease.Linear ), () => fog.AttenuationDistance, 1500 );
		CheckFloat( "FogOpacity", e => fog.TweenMaximumOpacity( 0, e, 1, Ease.Linear ), () => fog.MaximumOpacity, 0.7f );
		CheckFloat( "VolumetricFog", e => volumetricFog.TweenStrength( 0, e, 1, Ease.Linear ), () => volumetricFog.Strength, 0.9f );
	}

	[TestMethod]
	public void ColorPropertiesAnimate()
	{
		var go = CreateObject();
		var ambient = go.Components.Create<AmbientLight>();
		var sun = go.Components.Create<DirectionalLight>();
		var camera = go.Components.Create<CameraComponent>();
		var bloom = go.Components.Create<Bloom>();
		var vignette = go.Components.Create<Vignette>();
		var fog = go.Components.Create<Fog>();
		var volumetricFog = go.Components.Create<VolumetricFogVolume>();

		var start = Color.Black;
		var end = new Color( 1, 0.5f, 0.25f, 1 );
		var middle = Color.Lerp( start, end, 0.5f );
		ambient.TweenColor( start, end, 1, Ease.Linear );
		sun.TweenSkyColor( start, end, 1, Ease.Linear );
		camera.TweenBackgroundColor( start, end, 1, Ease.Linear );
		bloom.TweenTint( start, end, 1, Ease.Linear );
		vignette.TweenColor( start, end, 1, Ease.Linear );
		fog.TweenColor( start, end, 1, Ease.Linear );
		volumetricFog.TweenColor( start, end, 1, Ease.Linear );
		EndFrame();
		Tick( 0.5f );

		AreEqual( middle, ambient.Color, 0.001f );
		AreEqual( middle, sun.SkyColor, 0.001f );
		AreEqual( middle, camera.BackgroundColor, 0.001f );
		AreEqual( middle, bloom.Tint, 0.001f );
		AreEqual( middle, vignette.Color, 0.001f );
		AreEqual( middle, fog.Color, 0.001f );
		AreEqual( middle, volumetricFog.Color, 0.001f );
	}

	[TestMethod]
	public void Vector2PropertyAnimates()
	{
		var fog = CreateObject().Components.Create<Fog>();
		fog.TweenDistanceRange( Vector2.Zero, new Vector2( 100, 1000 ), 1, Ease.Linear );
		EndFrame();
		Tick( 0.5f );
		AreEqual( 50, fog.DistanceRange.x, 0.001f );
		AreEqual( 500, fog.DistanceRange.y, 0.001f );
	}

	[TestMethod]
	public void StartsFromCurrentValue()
	{
		var light = CreateObject().Components.Create<PointLight>();
		light.Intensity = 2;
		Tween.LightIntensity( light, 4, Linear );
		EndFrame();
		Tick( 0.5f );
		AreEqual( 3, light.Intensity, 0.001f );
	}

	[TestMethod]
	public void ExistingComponentTweensHaveExtensions()
	{
		var go = CreateObject();
		var light = go.Components.Create<PointLight>();
		var camera = go.Components.Create<CameraComponent>();
		light.TweenColor( Color.Black, Color.White, 1, Ease.Linear );
		light.TweenRadius( 0, 100, 1, Ease.Linear );
		camera.TweenFieldOfView( 60, 90, 1, Ease.Linear );
		EndFrame();
		Tick( 1 );
		AreEqual( Color.White, light.LightColor );
		AreEqual( 100, light.Radius );
		AreEqual( 90, camera.FieldOfView );
	}

	[TestMethod]
	public void ComponentTweensDontAllocate()
	{
		var go = CreateObject();
		var light = go.Components.Create<PointLight>();
		var vignette = go.Components.Create<Vignette>();
		Manager.SetTweensCapacity( 20 );

		void Run()
		{
			light.TweenIntensity( 0, 2, 0.1f );
			vignette.TweenColor( Color.Black, Color.Red, 0.1f );
			for ( int i = 0; i < 10; i++ )
				Tick( FrameTime );
		}

		Run();
		long before = GC.GetAllocatedBytesForCurrentThread();
		Run();
		long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.AreEqual( 0, bytes, $"Allocated {bytes} bytes" );
	}
}

/// <summary>
/// Values animated by name: shader attributes, animgraph parameters, morphs and properties.
/// </summary>
[TestClass]
public class NamedValueTests : TweenTestBase
{
	static TweenSettings Linear => new( 1, Ease.Linear );

	[TestMethod]
	public void RendererAttributesAnimate()
	{
		var renderer = CreateObject().Components.Create<ModelRenderer>();
		renderer.TweenAttribute( "Glow", 0, 2, 1, Ease.Linear );
		renderer.TweenAttribute( "Offset", Vector3.Zero, new Vector3( 10, 20, 30 ), 1, Ease.Linear );
		renderer.TweenAttribute( "Params", Vector4.Zero, new Vector4( 1, 2, 3, 4 ), 1, Ease.Linear );
		renderer.TweenAttribute( "Emission", Color.Black, Color.White, 1, Ease.Linear );
		EndFrame();
		Tick( 0.5f );

		var attributes = renderer.Attributes;
		AreEqual( 1, attributes.GetFloat( "Glow" ), 0.001f );
		AreEqual( new Vector3( 5, 10, 15 ), attributes.GetVector( "Offset" ), 0.001f );
		var p = attributes.GetVector4( "Params" );
		AreEqual( new Vector3( 0.5f, 1, 1.5f ), new Vector3( p.x, p.y, p.z ), 0.001f );
		AreEqual( 2, p.w, 0.001f );
		var tint = attributes.GetVector4( "Emission" );
		AreEqual( 0.5f, tint.x, 0.001f );
		AreEqual( 1, tint.w, 0.001f );
	}

	[TestMethod]
	public void RendererAttributeStartsFromCurrentValue()
	{
		var renderer = CreateObject().Components.Create<ModelRenderer>();
		renderer.Attributes.Set( "Glow", 1f );
		Tween.RendererAttribute( renderer, "Glow", 3, Linear );
		EndFrame();
		Tick( 0.5f );
		AreEqual( 2, renderer.Attributes.GetFloat( "Glow" ), 0.001f );
	}

	[TestMethod]
	public void MorphsAnimate()
	{
		var renderer = CreateObject().Components.Create<SkinnedModelRenderer>();
		renderer.Morphs.Set( "smile", 0.2f, 0 );
		renderer.TweenMorph( "smile", 1, Linear );
		EndFrame();
		Tick( 0.5f );
		AreEqual( 0.6f, renderer.Morphs.Get( "smile" ), 0.001f );
		Tick( 0.5f );
		AreEqual( 1, renderer.Morphs.Get( "smile" ) );
	}

	[TestMethod]
	public void AnimParametersCanBeAnimated()
	{
		// Without a SceneModel the parameters can't be read back, so only check that the tweens run without errors
		var renderer = CreateObject().Components.Create<SkinnedModelRenderer>();
		var duck = renderer.TweenParameter( "duck", 0, 1, 1, Ease.Linear );
		var aim = renderer.TweenParameter( "aim", Vector3.Zero, Vector3.Forward, 1, Ease.Linear );
		var look = Tween.AnimParameter( renderer, "look", Rotation.Identity, Rotation.FromYaw( 45 ), 1, Ease.Linear );
		EndFrame();
		Tick( 1.1f );
		Assert.IsFalse( duck.IsAlive || aim.IsAlive || look.IsAlive );
	}

	[TestMethod]
	public void EmptyNameIsRejected()
	{
		var renderer = CreateObject().Components.Create<ModelRenderer>();
		ExpectError( "name of the animated value" );
		Assert.IsFalse( renderer.TweenAttribute( "", 1, 1 ).IsAlive );
	}

	[TestMethod]
	public void PropertiesAnimateByName()
	{
		var go = CreateObject();
		var light = go.Components.Create<PointLight>();
		var vignette = go.Components.Create<Vignette>();
		var aberration = go.Components.Create<ChromaticAberration>();
		var camera = go.Components.Create<CameraComponent>();
		light.TweenProperty( "Radius", 0, 200, 1, Ease.Linear );
		vignette.TweenProperty( "Center", Vector2.Zero, new Vector2( 1, 0.5f ), 1, Ease.Linear );
		aberration.TweenProperty( "Offset", Vector3.Zero, new Vector3( 2, 4, 6 ), 1, Ease.Linear );
		camera.TweenProperty( "BackgroundColor", Color.Black, Color.White, 1, Ease.Linear );
		EndFrame();
		Tick( 0.5f );

		AreEqual( 100, light.Radius, 0.001f );
		AreEqual( 0.5f, vignette.Center.x, 0.001f );
		AreEqual( 0.25f, vignette.Center.y, 0.001f );
		AreEqual( new Vector3( 1, 2, 3 ), aberration.Offset, 0.001f );
		AreEqual( Color.Lerp( Color.Black, Color.White, 0.5f ), camera.BackgroundColor, 0.001f );
	}

	[TestMethod]
	public void PropertyStartsFromCurrentValue()
	{
		var light = CreateObject().Components.Create<PointLight>();
		light.Radius = 100;
		Tween.Property( light, "Radius", 300, Linear );
		EndFrame();
		Tick( 0.5f );
		AreEqual( 200, light.Radius, 0.001f );
	}

	[TestMethod]
	public void MissingPropertyIsRejected()
	{
		var light = CreateObject().Components.Create<PointLight>();
		ExpectError( "'Missing' was not found on PointLight" );
		Assert.IsFalse( light.TweenProperty( "Missing", 1, 1 ).IsAlive );
	}

	[TestMethod]
	public void PropertyOfWrongTypeIsRejected()
	{
		var light = CreateObject().Components.Create<PointLight>();
		ExpectError( "'Radius' of PointLight is Single, but the tween animates Vector3" );
		Assert.IsFalse( light.TweenProperty( "Radius", Vector3.One, 1 ).IsAlive );
	}

	[TestMethod]
	public void DescriptionContainsTheName()
	{
		var renderer = CreateObject().Components.Create<ModelRenderer>();
		var tween = renderer.TweenAttribute( "Glow", 1, 1 );
		StringAssert.Contains( tween.Data.GetDescription(), "RendererAttributeFloat 'Glow'" );
		var entries = new List<TweenDebugEntry>();
		Manager.CollectDebugEntries( entries );
		var entry = entries.Single();
		Assert.AreEqual( "Attribute 'Glow'", entry.Name );
	}

	[TestMethod]
	public void NamedTweensDontAllocate()
	{
		var renderer = CreateObject().Components.Create<SkinnedModelRenderer>();
		Manager.SetTweensCapacity( 20 );

		void Run()
		{
			renderer.TweenAttribute( "Glow", 0, 1, 0.1f );
			renderer.TweenMorph( "smile", 0, 1, 0.1f );
			for ( int i = 0; i < 10; i++ )
				Tick( FrameTime );
		}

		Run();
		long before = GC.GetAllocatedBytesForCurrentThread();
		Run();
		long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.AreEqual( 0, bytes, $"Allocated {bytes} bytes" );
	}
}

[TestClass]
public class RigidbodyTweenTests : TweenTestBase
{
	void FixedTick( float deltaTime ) => Manager.Update( UpdateType.FixedUpdate, deltaTime, deltaTime );

	[TestMethod]
	public void AlwaysUsesFixedUpdate()
	{
		var body = CreateObject().Components.Create<Rigidbody>();
		var tween = body.TweenPosition( new Vector3( 100, 0, 0 ), 1 );
		Assert.AreEqual( UpdateType.FixedUpdate, tween.Data.UpdateType );

		ExpectWarning( "Rigidbody tweens always use UpdateType.FixedUpdate" );
		var rotation = Tween.RigidbodyRotation( body, Rotation.FromYaw( 90 ), new TweenSettings( 1, updateType: UpdateType.Update ) );
		Assert.AreEqual( UpdateType.FixedUpdate, rotation.Data.UpdateType );
	}

	[TestMethod]
	public void CompletionPlacesAndStopsTheBody()
	{
		var body = CreateObject().Components.Create<Rigidbody>();
		var end = new Vector3( 100, 50, 0 );
		var tween = body.TweenPosition( Vector3.Zero, end, 0.5f );
		var rotation = body.TweenRotation( Rotation.Identity, Rotation.FromYaw( 90 ), 0.5f );
		for ( int i = 0; i < 40 && (tween.IsAlive || rotation.IsAlive); i++ )
			FixedTick( FrameTime );

		Assert.IsFalse( tween.IsAlive || rotation.IsAlive );
		AreEqual( end, body.WorldPosition );
		AreEqual( Rotation.FromYaw( 90 ), body.WorldRotation );
		AreEqual( Vector3.Zero, body.Velocity );
		AreEqual( Vector3.Zero, body.AngularVelocity );
	}

	[TestMethod]
	public void CompleteJumpsToTheEnd()
	{
		var body = CreateObject().Components.Create<Rigidbody>();
		var tween = body.TweenPosition( Vector3.Zero, new Vector3( 0, 0, 80 ), 1 );
		FixedTick( FrameTime );
		tween.Complete();
		AreEqual( new Vector3( 0, 0, 80 ), body.WorldPosition );
		AreEqual( Vector3.Zero, body.Velocity );
	}

	[TestMethod]
	public void AngularVelocityTurnsToTarget()
	{
		var pairs = new (Rotation From, Rotation To)[]
		{
			(Rotation.Identity, Rotation.FromYaw( 90 )),
			(Rotation.From( 10, 20, 30 ), Rotation.From( -40, 100, 5 )),
			(Rotation.FromPitch( 170 ), Rotation.FromPitch( -170 )),
		};

		foreach ( var (from, to) in pairs )
		{
			const float dt = 0.5f;
			var velocity = TweenData.AngularVelocity( from, to, dt );
			float degrees = velocity.Length * dt * 180 / MathF.PI;
			var turned = Rotation.FromAxis( velocity.Normal, degrees ) * from;
			AreEqual( to, turned, 0.05f );
		}

		AreEqual( Vector3.Zero, TweenData.AngularVelocity( Rotation.FromYaw( 30 ), Rotation.FromYaw( 30 ), 0.1f ) );
		AreEqual( MathF.PI / 2, TweenData.AngularVelocity( Rotation.Identity, Rotation.FromYaw( 90 ), 1 ).Length, 0.001f );
	}
}

[TestClass]
public class TypewriterTests : TweenTestBase
{
	[TestMethod]
	public void TypesEvenlyWithoutPauses()
	{
		var text = CreateObject().Components.Create<TextRenderer>();
		text.TweenTypewriter( "Hello world", 1.1f, pauseOnPunctuation: false );
		EndFrame();
		Assert.AreEqual( "", text.Text );
		Tick( 0.5f );
		Assert.AreEqual( "Hello", text.Text );
		Tick( 0.6f );
		Assert.AreEqual( "Hello world", text.Text );
	}

	[TestMethod]
	public void PausesAfterPunctuation()
	{
		// 9 characters plus a sentence pause of 8 after the dot: one character per 0.1 seconds
		var text = CreateObject().Components.Create<TextRenderer>();
		var tween = text.TweenTypewriter( "Hi. There", 1.7f );
		AreEqual( 1.7f, tween.Duration );
		EndFrame();
		Tick( 0.35f );
		Assert.AreEqual( "Hi.", text.Text );
		Tick( 0.7f );
		Assert.AreEqual( "Hi.", text.Text, "Still in the pause" );
		Tick( 0.2f );
		Assert.AreEqual( "Hi. ", text.Text );
		Tick( 0.45f );
		Assert.AreEqual( "Hi. There", text.Text );
	}

	[TestMethod]
	public void DoesntPauseInsideWords()
	{
		Assert.AreEqual( 4, TweenData.CalcTypewriterWeight( "3.14", true ) );
		Assert.AreEqual( 9 + 3, TweenData.CalcTypewriterWeight( "Wait, ...", true ) );
		Assert.AreEqual( 12 + 8 + 8, TweenData.CalcTypewriterWeight( "Yes! No. End", true ) );
	}

	[TestMethod]
	public void AtSpeedUsesCharactersPerSecond()
	{
		var text = CreateObject().Components.Create<TextRenderer>();
		AreEqual( 0.5f, text.TweenTypewriterAtSpeed( "abcde", 10 ).Duration );
		AreEqual( 1.4f, text.TweenTypewriterAtSpeed( "Hi. Yo", 10 ).Duration );
	}

	[TestMethod]
	public void TypesCurrentTextWhenTextIsNull()
	{
		var text = CreateObject().Components.Create<TextRenderer>();
		text.Text = "abcd";
		Tween.Typewriter( text, null, 1 );
		EndFrame();
		Assert.AreEqual( "", text.Text );
		Tick( 0.5f );
		Assert.AreEqual( "ab", text.Text, "The default ease is linear" );
	}

	[TestMethod]
	public void LabelTypewriter()
	{
		var label = new Label();
		var tween = label.TweenTypewriterAtSpeed( "Hello", 10 );
		EndFrame();
		Tick( 0.25f );
		Assert.AreEqual( "He", label.Text );
		TickFor( 0.5f );
		Assert.IsFalse( tween.IsAlive );
		Assert.AreEqual( "Hello", label.Text );
	}

	[TestMethod]
	public void InvalidSpeedIsRejected()
	{
		var label = new Label();
		ExpectError( "Invalid typing speed" );
		Assert.IsFalse( label.TweenTypewriterAtSpeed( "Hello", 0 ).IsAlive );
	}
}