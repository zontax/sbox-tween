namespace SboxTween;

/// <summary>
/// Animation settings that don't depend on the animated value type. Can be edited in the inspector.
/// <code>
/// [Property] public TweenSettings Settings { get; set; } = new( 1, Ease.OutBack );
/// Tween.LocalPositionZ( GameObject, 100, Settings );
/// </code>
/// </summary>
public struct TweenSettings
{
	/// <summary>The duration of one cycle, in seconds, not including delays.</summary>
	[Property] public float Duration { get; set; }

	/// <summary>Easing of the animation. Set to <see cref="SboxTween.Ease.Custom"/> to use <see cref="CustomEase"/>.</summary>
	[Property] public Ease Ease { get; set; }

	/// <summary>A curve used as an easing function when <see cref="Ease"/> is <see cref="SboxTween.Ease.Custom"/>.</summary>
	[Property, ShowIf( nameof( Ease ), Ease.Custom )] public Curve CustomEase { get; set; }

	/// <summary>The number of repetitions. -1 repeats the animation indefinitely.</summary>
	[Property] public int Cycles { get; set; }

	/// <summary>Controls how the animation behaves with multiple cycles.</summary>
	[Property] public CycleMode CycleMode { get; set; }

	/// <summary>Delays the start of every cycle.</summary>
	[Property] public float StartDelay { get; set; }

	/// <summary>Delays the completion of every cycle.</summary>
	[Property] public float EndDelay { get; set; }

	/// <summary>Ignore <see cref="Scene.TimeScale"/>.</summary>
	[Property] public bool UseUnscaledTime { get; set; }

	/// <summary>Which scene stage updates the animation.</summary>
	[Property] public UpdateType UpdateType { get; set; }

	internal ParametricEase Parametric;
	internal float ParametricStrength;
	internal float ParametricPeriod;

	internal TweenSettings( float duration, Ease ease, TweenEasing? customEasing, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false, UpdateType updateType = UpdateType.Default )
	{
		Duration = duration;
		var parametric = customEasing?.Parametric ?? ParametricEase.None;
		bool hasCurve = customEasing?.HasCurve ?? false;
		var curve = customEasing?.Curve ?? default;
		if ( ease == Ease.Custom && parametric == ParametricEase.None && (!hasCurve || !ValidateCustomCurve( curve )) )
		{
			TweenLog.Error( Messages.CustomCurveInvalid );
			ease = Ease.Default;
		}

		Ease = ease;
		CustomEase = ease == Ease.Custom && parametric == ParametricEase.None ? curve : default;
		Cycles = cycles;
		CycleMode = cycleMode;
		StartDelay = startDelay;
		EndDelay = endDelay;
		UseUnscaledTime = useUnscaledTime;
		UpdateType = updateType;
		Parametric = parametric;
		ParametricStrength = customEasing?.Strength ?? float.NaN;
		ParametricPeriod = customEasing?.Period ?? float.NaN;
	}

	public TweenSettings( float duration, Ease ease = Ease.Default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false, UpdateType updateType = UpdateType.Default )
		: this( duration, ease, null, cycles, cycleMode, startDelay, endDelay, useUnscaledTime, updateType )
	{
	}

	public TweenSettings( float duration, TweenEasing easing, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false, UpdateType updateType = UpdateType.Default )
		: this( duration, easing.Ease, easing, cycles, cycleMode, startDelay, endDelay, useUnscaledTime, updateType )
	{
	}

	internal const int MaxCycles = int.MaxValue - 1;
	internal const float MinDuration = 0.0001f;

	internal static void SetCyclesTo1If0( ref int cycles )
	{
		if ( cycles == 0 )
			cycles = 1;
	}

	internal void SetValidValues()
	{
		var duration = Duration;
		ValidateFiniteDuration( ref duration );
		if ( duration != 0 )
			duration = MathF.Max( MinDuration, duration );
		Duration = duration;

		var cycles = Cycles;
		if ( cycles < -1 )
		{
			TweenLog.Error( $"Invalid 'cycles' value: {cycles}. Using -1 (infinite cycles) instead." );
			cycles = -1;
		}

		SetCyclesTo1If0( ref cycles );
		if ( cycles > MaxCycles )
			cycles = MaxCycles;
		Cycles = cycles;

		var startDelay = StartDelay;
		SetValidDelay( ref startDelay );
		StartDelay = startDelay;

		var endDelay = EndDelay;
		SetValidDelay( ref endDelay );
		EndDelay = endDelay;
	}

	internal static void SetValidDelay( ref float delay )
	{
		ValidateFiniteDuration( ref delay );
		// A tiny delay with zero duration would cross a huge number of cycles in one update
		delay = delay > 0 ? MathF.Max( MinDuration, delay ) : 0;
	}

	internal static void ValidateFiniteDuration( ref float f )
	{
		if ( float.IsNaN( f ) || float.IsInfinity( f ) )
		{
			TweenLog.Error( Messages.DurationInvalid );
			f = 0;
		}
	}

	internal static bool HasFrames( in Curve curve ) => !curve.Frames.IsDefaultOrEmpty && curve.Frames.Length >= 2;

	internal static bool ValidateCustomCurve( in Curve curve )
	{
		if ( !HasFrames( curve ) )
		{
			TweenLog.Error( "Custom ease curve should have at least 2 frames, please edit the curve in the inspector." );
			return false;
		}

		if ( TweenConfig.ValidateCustomCurves )
		{
			var error = GetCurveError( curve );
			if ( error is not null )
				TweenLog.Warning( $"Custom ease curve is not configured correctly which may have unexpected results: {error}. {Messages.CanBeDisabled( nameof( TweenConfig.ValidateCustomCurves ) )}" );
		}

		return true;
	}

	static string GetCurveError( in Curve curve )
	{
		float start = curve.Evaluate( 0 );
		if ( !IsZeroOrOne( start ) )
			return $"start value is not 0 or 1, but {start}";

		float end = curve.Evaluate( 1 );
		if ( !IsZeroOrOne( end ) )
			return $"end value is not 0 or 1, but {end}";

		return null;
	}

	static bool IsZeroOrOne( float value ) => value.AlmostEqual( 0, 0.001f ) || value.AlmostEqual( 1, 0.001f );

	/// <summary>
	/// What happens to an animation when its CancellationToken is canceled.
	/// </summary>
	public enum CancellationMode : byte
	{
		/// <summary>Stops the animation as if Stop() was called.</summary>
		Stop,
		/// <summary>Completes the animation as if Complete() was called.</summary>
		Complete
	}
}

/// <summary>
/// Animation settings together with start and end values. Can be edited in the inspector.
/// </summary>
public struct TweenSettings<T> where T : struct
{
	/// <summary>If true, the current value of the animated property is used instead of <see cref="StartValue"/>.</summary>
	[Property] public bool StartFromCurrent { get; set; }

	[Property, HideIf( nameof( StartFromCurrent ), true )] public T StartValue { get; set; }

	[Property] public T EndValue { get; set; }

	[Property, InlineEditor] public TweenSettings Settings { get; set; }

	public TweenSettings( T endValue, TweenSettings settings )
	{
		StartFromCurrent = true;
		StartValue = default;
		EndValue = endValue;
		Settings = settings;
	}

	public TweenSettings( T startValue, T endValue, TweenSettings settings )
	{
		StartFromCurrent = false;
		StartValue = startValue;
		EndValue = endValue;
		Settings = settings;
	}

	public TweenSettings( T endValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false, UpdateType updateType = UpdateType.Default )
		: this( endValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime, updateType ) )
	{
	}

	public TweenSettings( T startValue, T endValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false, UpdateType updateType = UpdateType.Default )
		: this( startValue, endValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime, updateType ) )
	{
	}

	/// <summary>
	/// Returns settings that animate towards <see cref="EndValue"/> if <paramref name="toEndValue"/> is true, or towards <see cref="StartValue"/> otherwise.
	/// Useful for show/hide animations.
	/// </summary>
	public readonly TweenSettings<T> WithDirection( bool toEndValue, bool startFromCurrent = true )
	{
		if ( StartFromCurrent )
			TweenLog.Warning( $"{nameof( StartFromCurrent )} is already enabled on this TweenSettings. {nameof( WithDirection )}() should be called on the TweenSettings once to choose the direction." );

		var result = this;
		result.StartFromCurrent = startFromCurrent;
		if ( toEndValue )
			return result;

		(result.StartValue, result.EndValue) = (result.EndValue, result.StartValue);
		return result;
	}

	/// <summary>
	/// Returns settings that animate from <paramref name="currentValue"/> towards <see cref="EndValue"/> or <see cref="StartValue"/>.
	/// </summary>
	public readonly TweenSettings<T> WithDirection( bool toEndValue, T currentValue )
	{
		var result = this;
		if ( result.StartFromCurrent )
		{
			result.StartFromCurrent = false;
			TweenLog.Warning( $"{nameof( StartFromCurrent )} is already enabled on this TweenSettings. {nameof( WithDirection )}( bool toEndValue, T currentValue ) should be called once on the TweenSettings created with both start and end values." );
		}

		result.StartValue = currentValue;
		result.EndValue = toEndValue ? EndValue : StartValue;
		return result;
	}
}
