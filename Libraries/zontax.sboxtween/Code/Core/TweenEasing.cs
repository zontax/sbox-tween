namespace SboxTween;

/// <summary>
/// Wraps the three available easing methods: a standard <see cref="Ease"/>, a <see cref="Sandbox.Curve"/>, or a parametric ease.
/// <code>
/// Tween.LocalPositionZ( go, 100, 1, TweenEasing.Overshoot( 2 ) );
/// Tween.LocalPositionZ( go, 100, 1, myCurve );
/// </code>
/// </summary>
public readonly struct TweenEasing
{
	internal readonly Ease Ease;
	internal readonly Curve Curve;
	internal readonly bool HasCurve;
	internal readonly ParametricEase Parametric;
	internal readonly float Strength;
	internal readonly float Period;

	TweenEasing( ParametricEase type, float strength, float period = float.NaN )
	{
		Ease = Ease.Custom;
		Curve = default;
		HasCurve = false;
		Parametric = type;
		Strength = strength;
		Period = period;
	}

	TweenEasing( Ease ease, Curve curve, bool hasCurve )
	{
		if ( ease == Ease.Custom && (!hasCurve || !TweenSettings.ValidateCustomCurve( curve )) )
		{
			TweenLog.Error( Messages.CustomCurveInvalid );
			ease = Ease.Default;
			hasCurve = false;
		}

		Ease = ease;
		Curve = curve;
		HasCurve = hasCurve;
		Parametric = ParametricEase.None;
		Strength = float.NaN;
		Period = float.NaN;
	}

	public static implicit operator TweenEasing( Ease ease ) => Standard( ease );
	public static implicit operator TweenEasing( Curve curve ) => FromCurve( curve );

	/// <summary>Standard Robert Penner's easing method. Or simply pass the <see cref="Ease"/> enum instead.</summary>
	public static TweenEasing Standard( Ease ease )
	{
		if ( ease == Ease.Custom )
		{
			TweenLog.Error( $"{nameof( TweenEasing )}.{nameof( Standard )}() doesn't support Ease.Custom, please use {nameof( TweenEasing )}.{nameof( FromCurve )}() instead. Using Ease.Default instead." );
			ease = Ease.Default;
		}

		return new TweenEasing( ease, default, false );
	}

	/// <summary>A <see cref="Sandbox.Curve"/> to use as an easing function. The curve should go from (0, 0) to (1, 1).</summary>
	public static TweenEasing FromCurve( Curve curve ) => new( Ease.Custom, curve, true );

	/// <summary>Customizes the bounce strength of <see cref="Ease.OutBounce"/>.</summary>
	public static TweenEasing Bounce( float strength ) => new( ParametricEase.Bounce, strength );

	/// <summary>Customizes the exact amplitude of the first bounce, in value units (inches, degrees, etc.).</summary>
	public static TweenEasing BounceExact( float amplitude ) => new( ParametricEase.BounceExact, amplitude );

	/// <summary>Customizes the overshoot strength of <see cref="Ease.OutBack"/>.</summary>
	public static TweenEasing Overshoot( float strength ) => new( ParametricEase.Overshoot, strength * StandardEasing.BackEaseConst );

	/// <summary>Customizes the strength and oscillation period of <see cref="Ease.OutElastic"/>.</summary>
	public static TweenEasing Elastic( float strength, float period = StandardEasing.DefaultElasticEasePeriod )
	{
		if ( strength < 1 )
			strength = MathX.Lerp( 0.2f, 1, strength );

		return new TweenEasing( ParametricEase.Elastic, strength, MathF.Max( 0.1f, period ) );
	}

	/// <summary>
	/// Evaluates a standard ease. <see cref="Ease.Default"/> uses <see cref="TweenConfig.DefaultEase"/>.
	/// </summary>
	public static float Evaluate( float t, Ease ease )
	{
		switch ( ease )
		{
			case Ease.Custom:
				TweenLog.Error( "Ease.Custom is an invalid type for TweenEasing.Evaluate(). Please choose another Ease type instead." );
				return t;
			case Ease.Default:
				return StandardEasing.Evaluate( t, TweenConfig.DefaultEase );
			default:
				return StandardEasing.Evaluate( t, ease );
		}
	}
}
