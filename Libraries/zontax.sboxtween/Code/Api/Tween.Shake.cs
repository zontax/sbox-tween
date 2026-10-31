namespace SboxTween;

public readonly partial struct Tween
{
	/// <summary>
	/// Shakes the camera. <paramref name="strengthFactor"/> of 0.5 is a medium shake, 1 is a heavy one.
	/// </summary>
	public static Sequence ShakeCamera( CameraComponent camera, float strengthFactor, float duration = 0.5f, float frequency = ShakeSettings.DefaultFrequency, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
	{
		if ( !camera.IsValid() )
		{
			TweenLog.Error( "'camera' is null or has been destroyed." );
			return default;
		}

		var go = camera.GameObject;
		if ( camera.Orthographic )
		{
			// The camera looks along +X, so the screen plane is Y (left) and Z (up)
			float orthoPosStrength = strengthFactor * camera.OrthographicHeight * 0.03f;
			return Sequence.Create( ShakeLocalPosition( go, new ShakeSettings( new Vector3( 0, orthoPosStrength, orthoPosStrength ), duration, frequency, startDelay: startDelay, endDelay: endDelay, useUnscaledTime: useUnscaledTime ) ) )
				.Group( ShakeLocalRotation( go, new ShakeSettings( new Vector3( 0, 0, strengthFactor * 0.6f ), duration, frequency, startDelay: startDelay, endDelay: endDelay, useUnscaledTime: useUnscaledTime ) ) );
		}

		return Sequence.Create( ShakeLocalRotation( go, new ShakeSettings( strengthFactor * Vector3.One, duration, frequency, startDelay: startDelay, endDelay: endDelay, useUnscaledTime: useUnscaledTime ) ) );
	}

	/// <summary>
	/// Shakes the local position. <paramref name="strength"/> is the amplitude per axis, in inches.
	/// </summary>
	public static Tween ShakeLocalPosition( GameObject target, Vector3 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> ShakeLocalPosition( target, new ShakeSettings( strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="ShakeLocalPosition(GameObject, Vector3, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween ShakeLocalPosition( GameObject target, ShakeSettings settings ) => ShakeTransform( TweenType.ShakeLocalPosition, target, settings );

	/// <summary>
	/// Punches the local position in the direction of <paramref name="strength"/>, then returns back.
	/// </summary>
	public static Tween PunchLocalPosition( GameObject target, Vector3 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> PunchLocalPosition( target, new ShakeSettings( strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="PunchLocalPosition(GameObject, Vector3, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PunchLocalPosition( GameObject target, ShakeSettings settings ) => ShakeLocalPosition( target, settings.WithPunch() );

	/// <summary>
	/// Shakes the local rotation. <paramref name="strength"/> is the amplitude in degrees: (pitch, yaw, roll).
	/// </summary>
	public static Tween ShakeLocalRotation( GameObject target, Vector3 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> ShakeLocalRotation( target, new ShakeSettings( strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="ShakeLocalRotation(GameObject, Vector3, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween ShakeLocalRotation( GameObject target, ShakeSettings settings ) => ShakeTransform( TweenType.ShakeLocalRotation, target, settings );

	/// <summary>
	/// Punches the local rotation. <paramref name="strength"/> is in degrees: (pitch, yaw, roll).
	/// </summary>
	public static Tween PunchLocalRotation( GameObject target, Vector3 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> PunchLocalRotation( target, new ShakeSettings( strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="PunchLocalRotation(GameObject, Vector3, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PunchLocalRotation( GameObject target, ShakeSettings settings ) => ShakeLocalRotation( target, settings.WithPunch() );

	/// <summary>
	/// Shakes the local scale. <paramref name="strength"/> is the amplitude per axis.
	/// </summary>
	public static Tween ShakeLocalScale( GameObject target, Vector3 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> ShakeLocalScale( target, new ShakeSettings( strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="ShakeLocalScale(GameObject, Vector3, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween ShakeLocalScale( GameObject target, ShakeSettings settings ) => ShakeTransform( TweenType.ShakeLocalScale, target, settings );

	/// <summary>
	/// Punches the local scale, e.g. a 'squash and stretch' effect on a button press.
	/// </summary>
	public static Tween PunchLocalScale( GameObject target, Vector3 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> PunchLocalScale( target, new ShakeSettings( strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="PunchLocalScale(GameObject, Vector3, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PunchLocalScale( GameObject target, ShakeSettings settings ) => ShakeLocalScale( target, settings.WithPunch() );

	/// <summary>
	/// Shakes a custom Vector3 value around <paramref name="startValue"/>.
	/// </summary>
	public static Tween ShakeCustom<T>( T target, Vector3 startValue, ShakeSettings settings, Action<T, Vector3> onValueChange ) where T : class
	{
		if ( onValueChange is null )
		{
			TweenLog.Error( Messages.OnValueChangeNull );
			return default;
		}

		var manager = TweenManager.Resolve( target );
		if ( manager is null || manager.IsDisposed )
			return default;

		var tween = manager.Fetch( settings.UpdateType );
		tween.StartValue = ValueContainer.From( startValue );
		tween.SetupShake( settings );
		tween.CustomOnValueChange = onValueChange;
		tween.OnValueChangeInvoker = CustomInvokers<T>.Shake;
		var tweenSettings = settings.TweenSettings;
		tween.Setup( target, ref tweenSettings, false, TweenType.ShakeCustom );
		return manager.Animate( tween );
	}

	/// <summary>
	/// Punches a custom Vector3 value in the direction of the strength.
	/// </summary>
	public static Tween PunchCustom<T>( T target, Vector3 startValue, ShakeSettings settings, Action<T, Vector3> onValueChange ) where T : class
		=> ShakeCustom( target, startValue, settings.WithPunch(), onValueChange );

	static Tween ShakeTransform( TweenType tweenType, object target, ShakeSettings settings )
	{
		var manager = TweenManager.Resolve( target );
		if ( manager is null || manager.IsDisposed )
			return default;

		var tween = manager.Fetch( settings.UpdateType );
		tween.SetupShake( settings );
		var tweenSettings = settings.TweenSettings;
		tween.Setup( target, ref tweenSettings, true, tweenType );
		return manager.Animate( tween );
	}
}
