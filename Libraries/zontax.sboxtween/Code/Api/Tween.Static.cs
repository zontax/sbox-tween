namespace SboxTween;

public readonly partial struct Tween
{
	/// <summary>
	/// Returns the number of alive animations in the active scene (on <paramref name="onTarget"/> only, if provided).
	/// </summary>
	public static int GetTweensCount( object onTarget = null ) => TweenManager.Resolve( onTarget, false )?.GetTweensCount( onTarget ) ?? 0;

	/// <summary>
	/// Stops all animations in the active scene. If <paramref name="onTarget"/> is provided, stops only animations on this target
	/// (stopping a tween inside a Sequence is not allowed).
	/// </summary>
	/// <returns>The number of stopped animations.</returns>
	public static int StopAll( object onTarget = null ) => TweenManager.Resolve( onTarget, false )?.StopAll( onTarget ) ?? 0;

	/// <summary>
	/// Completes all animations in the active scene. If <paramref name="onTarget"/> is provided, completes only animations on this target
	/// (completing a tween inside a Sequence is not allowed).
	/// </summary>
	/// <returns>The number of completed animations.</returns>
	public static int CompleteAll( object onTarget = null ) => TweenManager.Resolve( onTarget, false )?.CompleteAll( onTarget ) ?? 0;

	/// <summary>
	/// Pauses or unpauses all animations in the active scene. If <paramref name="onTarget"/> is provided, affects only animations on this target
	/// (pausing a tween inside a Sequence is not allowed).
	/// </summary>
	/// <returns>The number of paused or unpaused animations.</returns>
	public static int SetPausedAll( bool isPaused, object onTarget = null ) => TweenManager.Resolve( onTarget, false )?.SetPausedAll( isPaused, onTarget ) ?? 0;

	#region Delay

	/// <summary>
	/// Creates a delay that behaves like a normal tween: it can be awaited, used in sequences and stopped.
	/// The delay may outlive the caller, consider using the <see cref="Delay{T}(T, float, Action{T}, bool, bool?)"/> overload.
	/// </summary>
	public static Tween Delay( float duration, Action onComplete = null, bool useUnscaledTime = false, bool? warnIfTargetDestroyed = null )
		=> DelayInternal( TweenManager.DummyTarget, duration, onComplete, useUnscaledTime, warnIfTargetDestroyed );

	/// <inheritdoc cref="Delay(float, Action, bool, bool?)"/>
	public static Tween Delay( object target, float duration, Action onComplete = null, bool useUnscaledTime = false, bool? warnIfTargetDestroyed = null )
		=> DelayInternal( target, duration, onComplete, useUnscaledTime, warnIfTargetDestroyed );

	/// <summary>
	/// The preferable overload of all Delay functions: it doesn't invoke <paramref name="onComplete"/> if the target is destroyed,
	/// and calls methods on <paramref name="target"/> without allocations.
	/// <code>
	/// Tween.Delay( this, 1, static self => self.DoSomething() );
	/// </code>
	/// </summary>
	public static Tween Delay<T>( T target, float duration, Action<T> onComplete, bool useUnscaledTime = false, bool? warnIfTargetDestroyed = null ) where T : class
	{
		var delay = CreateDelay( target, duration, useUnscaledTime, TweenType.Delay, true );
		if ( delay.IsAliveInternal )
			delay.Data.OnComplete( target, onComplete, warnIfTargetDestroyed );

		return delay;
	}

	static Tween DelayInternal( object target, float duration, Action onComplete, bool useUnscaledTime, bool? warnIfTargetDestroyed )
	{
		var delay = CreateDelay( target, duration, useUnscaledTime, TweenType.Delay, true );
		if ( onComplete is not null && delay.IsAliveInternal )
			delay.Data.OnComplete( onComplete, warnIfTargetDestroyed );

		return delay;
	}

	internal static Tween CreateDelay( object target, float duration, bool useUnscaledTime, TweenType tweenType, bool checkDuration, TweenManager manager = null )
	{
		manager ??= TweenManager.Resolve( target );
		if ( manager is null || manager.IsDisposed )
			return default;

		var settings = new TweenSettings( duration, Ease.Linear, useUnscaledTime: useUnscaledTime );
		var tween = manager.Fetch( settings.UpdateType );
		tween.Setup( target, ref settings, false, tweenType );
		return checkDuration ? manager.Animate( tween ) : manager.Add( tween );
	}

	#endregion

	#region TimeScale

	/// <summary>
	/// Animates the <see cref="Scene.TimeScale"/> of the active scene. The animation always uses unscaled time.
	/// </summary>
	public static Tween GlobalTimeScale( float endValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0 )
		=> GlobalTimeScale( new TweenSettings<float>( endValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, true ) ) );

	/// <inheritdoc cref="GlobalTimeScale(float, float, TweenEasing, int, CycleMode, float, float)"/>
	public static Tween GlobalTimeScale( float startValue, float endValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0 )
		=> GlobalTimeScale( new TweenSettings<float>( startValue, endValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, true ) ) );

	/// <inheritdoc cref="GlobalTimeScale(float, float, TweenEasing, int, CycleMode, float, float)"/>
	public static Tween GlobalTimeScale( TweenSettings<float> settings ) => GlobalTimeScale( Game.ActiveScene, settings );

	/// <summary>
	/// Animates the <see cref="Scene.TimeScale"/> of <paramref name="scene"/>. The animation always uses unscaled time.
	/// </summary>
	public static Tween GlobalTimeScale( Scene scene, TweenSettings<float> settings )
	{
		settings.StartValue = ClampGlobalTimeScale( settings.StartValue );
		settings.EndValue = ClampGlobalTimeScale( settings.EndValue );
		if ( !settings.Settings.UseUnscaledTime )
		{
			TweenLog.Warning( "Setting UseUnscaledTime to true to animate Scene.TimeScale correctly." );
			var s = settings.Settings;
			s.UseUnscaledTime = true;
			settings.Settings = s;
		}

		return Animate( scene, TweenType.GlobalTimeScale, settings );

		static float ClampGlobalTimeScale( float value )
		{
			if ( value >= 0 )
				return value;

			TweenLog.Error( $"TimeScale should be >= 0, but was {value}." );
			return 0;
		}
	}

	/// <summary>Smoothly animates the <see cref="TimeScale"/> of <paramref name="tween"/>.</summary>
	public static Tween TweenTimeScale( Tween tween, float endValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> TweenTimeScale( tween, new TweenSettings<float>( endValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) ) );

	/// <inheritdoc cref="TweenTimeScale(Tween, float, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween TweenTimeScale( Tween tween, float startValue, float endValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> TweenTimeScale( tween, new TweenSettings<float>( startValue, endValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) ) );

	/// <inheritdoc cref="TweenTimeScale(Tween, float, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween TweenTimeScale( Tween tween, TweenSettings<float> settings ) => AnimateTimeScale( tween, settings, TweenType.TweenTimeScale );

	/// <summary>Smoothly animates the <see cref="Sequence.TimeScale"/> of <paramref name="sequence"/>.</summary>
	public static Tween TweenTimeScale( Sequence sequence, float endValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> TweenTimeScale( sequence, new TweenSettings<float>( endValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) ) );

	/// <inheritdoc cref="TweenTimeScale(Sequence, float, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween TweenTimeScale( Sequence sequence, TweenSettings<float> settings ) => AnimateTimeScale( sequence.Root, settings, TweenType.TweenTimeScaleSequence );

	static Tween AnimateTimeScale( Tween tween, TweenSettings<float> settings, TweenType tweenType )
	{
		if ( !tween.TryManipulate() )
			return default;

		if ( float.IsNaN( settings.EndValue ) || (!settings.StartFromCurrent && float.IsNaN( settings.StartValue )) )
		{
			TweenLog.Error( $"Invalid TimeScale: startValue {settings.StartValue}, endValue {settings.EndValue}." );
			return default;
		}

		float start = settings.StartValue;
		float end = settings.EndValue;
		ClampTimeScale( ref start );
		ClampTimeScale( ref end );
		if ( tween.Data.HasWait && (!ValidateWaitTimeScale( start ) || !ValidateWaitTimeScale( end )) )
			return default;
		settings.StartValue = start;
		settings.EndValue = end;

		return AnimateCore( tween.Data, tweenType, ValueContainer.From( settings.StartValue ), ValueContainer.From( settings.EndValue ), settings.Settings, settings.StartFromCurrent, tween.Data.Manager, tween.Id );
	}

	#endregion

	#region Internal

	internal static void CancelTweenOrSequence( Tween tweenOrSequenceRoot )
	{
		bool shouldStop = !tweenOrSequenceRoot.Data.CancellationModeComplete;
		if ( tweenOrSequenceRoot.Data.TweenType == TweenType.MainSequence )
		{
			var sequence = new Sequence( tweenOrSequenceRoot.Data );
			if ( shouldStop )
				sequence.Stop();
			else
				sequence.Complete();
		}
		else
		{
			if ( shouldStop )
				tweenOrSequenceRoot.Stop();
			else
				tweenOrSequenceRoot.Complete();
		}
	}

	/// <summary>
	/// An infinite helper animation that completes as soon as <paramref name="awaited"/> is no longer alive.
	/// </summary>
	internal static Tween CreateAwaiterTween( TweenData awaited )
	{
		// Always UpdateType.Update, so continuations always happen from the same stage
		var settings = new TweenSettings( float.MaxValue, Ease.Linear, -1, updateType: UpdateType.Update );
		return AnimateCore( awaited, TweenType.TweenAwaiter, default, default, settings, false, awaited.Manager, awaited.Id );
	}

	internal static Tween Animate( object target, TweenType tweenType, TweenSettings<float> s, string parameterName = null ) => AnimateCore( target, tweenType, ValueContainer.From( s.StartValue ), ValueContainer.From( s.EndValue ), s.Settings, s.StartFromCurrent, parameterName: parameterName );
	internal static Tween Animate( object target, TweenType tweenType, TweenSettings<Vector2> s, string parameterName = null ) => AnimateCore( target, tweenType, ValueContainer.From( s.StartValue ), ValueContainer.From( s.EndValue ), s.Settings, s.StartFromCurrent, parameterName: parameterName );
	internal static Tween Animate( object target, TweenType tweenType, TweenSettings<Vector3> s, string parameterName = null ) => AnimateCore( target, tweenType, ValueContainer.From( s.StartValue ), ValueContainer.From( s.EndValue ), s.Settings, s.StartFromCurrent, parameterName: parameterName );
	internal static Tween Animate( object target, TweenType tweenType, TweenSettings<Vector4> s, string parameterName = null ) => AnimateCore( target, tweenType, ValueContainer.From( s.StartValue ), ValueContainer.From( s.EndValue ), s.Settings, s.StartFromCurrent, parameterName: parameterName );
	internal static Tween Animate( object target, TweenType tweenType, TweenSettings<Color> s, string parameterName = null ) => AnimateCore( target, tweenType, ValueContainer.From( s.StartValue ), ValueContainer.From( s.EndValue ), s.Settings, s.StartFromCurrent, parameterName: parameterName );
	internal static Tween Animate( object target, TweenType tweenType, TweenSettings<Rotation> s, string parameterName = null ) => AnimateCore( target, tweenType, ValueContainer.From( s.StartValue ), ValueContainer.From( s.EndValue ), s.Settings, s.StartFromCurrent, parameterName: parameterName );
	internal static Tween Animate( object target, TweenType tweenType, TweenSettings<Angles> s, string parameterName = null ) => AnimateCore( target, tweenType, ValueContainer.From( s.StartValue ), ValueContainer.From( s.EndValue ), s.Settings, s.StartFromCurrent, parameterName: parameterName );

	internal static Tween AnimateCore( object target, TweenType tweenType, ValueContainer startValue, ValueContainer endValue, TweenSettings settings, bool startFromCurrent, TweenManager manager = null, long targetTweenId = 0,
		LengthUnit unitX = LengthUnit.Auto, LengthUnit unitY = LengthUnit.Auto, string parameterName = null )
	{
		// Vector2 translate overloads are in pixels
		if ( tweenType == TweenType.PanelTranslate && unitX == LengthUnit.Auto )
		{
			unitX = LengthUnit.Pixels;
			unitY = LengthUnit.Pixels;
		}

		manager ??= TweenManager.Resolve( target );
		if ( manager is null || manager.IsDisposed )
			return default;

		if ( TweenData.IsRigidbody( tweenType ) )
			settings.UpdateType = ValidateRigidbodyUpdateType( settings.UpdateType );

		object parameterCache = null;
		if ( TweenData.IsNamed( tweenType ) && !TweenData.ValidateParameter( target, tweenType, parameterName, out parameterCache ) )
			return default;

		var tween = manager.Fetch( settings.UpdateType );
		tween.SetParameter( tweenType, parameterName, parameterCache );
		tween.StartValue = startValue;
		tween.EndValueOrDiff = endValue;
		tween.TargetTweenId = targetTweenId;
		tween.LengthUnitX = unitX;
		tween.LengthUnitY = unitY;
		tween.Setup( target, ref settings, startFromCurrent, tweenType );
		return manager.Animate( tween );
	}

	static UpdateType ValidateRigidbodyUpdateType( UpdateType updateType )
	{
		if ( updateType != UpdateType.Default && updateType != UpdateType.FixedUpdate )
			TweenLog.Warning( $"Rigidbody tweens always use UpdateType.FixedUpdate, the requested UpdateType.{updateType} is ignored." );

		return UpdateType.FixedUpdate;
	}

	internal static Tween CustomCore( object target, TweenType tweenType, ValueContainer startValue, ValueContainer endValue, TweenSettings settings, bool startFromCurrent,
		object onValueChange, Action<TweenData> invoker, object getter, Action<TweenData> getterInvoker, bool useDurationAsSpeed )
	{
		if ( onValueChange is null )
		{
			TweenLog.Error( Messages.OnValueChangeNull );
			return default;
		}

		if ( startFromCurrent && getter is null )
		{
			if ( useDurationAsSpeed )
			{
				TweenLog.Error( "'getCurrentValue' is required to calculate the duration when 'useDurationAsSpeed' is true and the start value is not provided." );
				return default;
			}

			TweenLog.Warning( "Custom tweens don't support 'StartFromCurrent' without 'getCurrentValue'. Please provide the getter or the start value. Using the default start value instead." );
			startFromCurrent = false;
		}

		if ( useDurationAsSpeed && settings.Duration <= 0 )
		{
			TweenLog.Error( $"Invalid speed provided to the Tween.Custom() method: {settings.Duration}." );
			return default;
		}

		var manager = TweenManager.Resolve( target );
		if ( manager is null || manager.IsDisposed )
			return default;

		var tween = manager.Fetch( settings.UpdateType );
		tween.TweenType = tweenType;
		tween.Target = target;
		tween.CustomOnValueChange = onValueChange;
		tween.OnValueChangeInvoker = invoker;
		if ( getter is not null )
		{
			tween.CustomGetter = getter;
			tween.CustomGetterInvoker = getterInvoker;
		}

		if ( useDurationAsSpeed )
		{
			float speed = settings.Duration;
			if ( startFromCurrent && !TweenData.IsDestroyed( target ) )
			{
				getterInvoker( tween );
				startValue = tween.StartValue;
				startFromCurrent = false;
			}

			settings.Duration = CalcDistance( tween.PropType, startValue, endValue ) / speed;
		}

		tween.StartValue = startValue;
		tween.EndValueOrDiff = endValue;
		tween.Setup( target, ref settings, startFromCurrent, tweenType );
		return manager.Animate( tween );
	}

	internal static float CalcDistance( PropType propType, ValueContainer a, ValueContainer b )
	{
		switch ( propType )
		{
			case PropType.Float: return MathF.Abs( b.X - a.X );
			case PropType.Vector2: return a.Vector2.Distance( b.Vector2 );
			case PropType.Vector3: return a.Vector3.Distance( b.Vector3 );
			case PropType.Rotation: return ValueContainer.RotationAngle( a, b );
			default: return (b - a).Magnitude;
		}
	}

	#endregion
}
