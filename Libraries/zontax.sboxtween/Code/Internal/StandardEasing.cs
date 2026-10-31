// Credits: http://robertpenner.com/easing/
// Copyright © 2001 Robert Penner. Released under the MIT license.

namespace SboxTween;

internal static class StandardEasing
{
	const float HalfPi = MathF.PI / 2;
	internal const float BackEaseConst = 1.70158f;
	internal const float DefaultElasticEasePeriod = 0.3f;

	static float InElastic( float t ) => 1 - OutElastic( 1 - t );

	static float OutElastic( float t )
	{
		float decay = MathF.Pow( 2, -10 * t );
		const float phase = DefaultElasticEasePeriod / 4;
		const float twoPi = MathF.PI * 2;
		return t > 0.9999f ? 1 : decay * MathF.Sin( (t - phase) * twoPi / DefaultElasticEasePeriod ) + 1;
	}

	static float OutBounce( float x )
	{
		const float n1 = 7.5625f;
		const float d1 = 2.75f;
		if ( x < 1 / d1 )
			return n1 * x * x;

		if ( x < 2 / d1 )
			return n1 * (x -= 1.5f / d1) * x + 0.75f;

		if ( x < 2.5f / d1 )
			return n1 * (x -= 2.25f / d1) * x + 0.9375f;

		return n1 * (x -= 2.625f / d1) * x + 0.984375f;
	}

	internal static float Evaluate( float t, Ease ease )
	{
		switch ( ease )
		{
			case Ease.Linear:
				return t;
			case Ease.InSine:
				return 1 - MathF.Cos( t * HalfPi );
			case Ease.OutSine:
				return MathF.Sin( t * HalfPi );
			case Ease.InOutSine:
				return -0.5f * (MathF.Cos( MathF.PI * t ) - 1);
			case Ease.InQuad:
				return t * t;
			case Ease.OutQuad:
				return -t * (t - 2);
			case Ease.InOutQuad:
				t *= 2;
				if ( t < 1 )
					return 0.5f * t * t;
				return -0.5f * (--t * (t - 2) - 1);
			case Ease.InCubic:
				return t * t * t;
			case Ease.OutCubic:
				return (t -= 1) * t * t + 1;
			case Ease.InOutCubic:
				t *= 2;
				if ( t < 1 )
					return 0.5f * t * t * t;
				return 0.5f * ((t -= 2) * t * t + 2);
			case Ease.InQuart:
				return t * t * t * t;
			case Ease.OutQuart:
				return -((t -= 1) * t * t * t - 1);
			case Ease.InOutQuart:
				t *= 2;
				if ( t < 1 )
					return 0.5f * t * t * t * t;
				return -0.5f * ((t -= 2) * t * t * t - 2);
			case Ease.InQuint:
				return t * t * t * t * t;
			case Ease.OutQuint:
				return (t -= 1) * t * t * t * t + 1;
			case Ease.InOutQuint:
				t *= 2;
				if ( t < 1 )
					return 0.5f * t * t * t * t * t;
				return 0.5f * ((t -= 2) * t * t * t * t + 2);
			case Ease.InExpo:
				return t == 0 ? 0 : MathF.Pow( 2, 10 * (t - 1) );
			case Ease.OutExpo:
				return t == 1 ? 1 : -MathF.Pow( 2, -10 * t ) + 1;
			case Ease.InOutExpo:
				if ( t == 0 )
					return 0;
				if ( t == 1 )
					return 1;
				t *= 2;
				if ( t < 1 )
					return 0.5f * MathF.Pow( 2, 10 * (t - 1) );
				return 0.5f * (-MathF.Pow( 2, -10 * --t ) + 2);
			case Ease.InCirc:
				return -(MathF.Sqrt( 1 - t * t ) - 1);
			case Ease.OutCirc:
				return MathF.Sqrt( 1 - (t -= 1) * t );
			case Ease.InOutCirc:
				t *= 2;
				if ( t < 1 )
					return -0.5f * (MathF.Sqrt( 1 - t * t ) - 1);
				return 0.5f * (MathF.Sqrt( 1 - (t -= 2) * t ) + 1);
			case Ease.InBack:
				return t * t * ((BackEaseConst + 1) * t - BackEaseConst);
			case Ease.OutBack:
				return (t -= 1) * t * ((BackEaseConst + 1) * t + BackEaseConst) + 1;
			case Ease.InOutBack:
				t *= 2;
				const float c1 = BackEaseConst * 1.525f;
				if ( t < 1 )
					return 0.5f * (t * t * ((c1 + 1) * t - c1));
				return 0.5f * ((t -= 2) * t * ((c1 + 1) * t + c1) + 2);
			case Ease.InElastic:
				return InElastic( t );
			case Ease.OutElastic:
				return OutElastic( t );
			case Ease.InOutElastic:
				if ( t < 0.5f )
					return InElastic( t * 2 ) * 0.5f;
				return 0.5f + OutElastic( (t - 0.5f) * 2 ) * 0.5f;
			case Ease.InBounce:
				return 1 - OutBounce( 1 - t );
			case Ease.OutBounce:
				return OutBounce( t );
			case Ease.InOutBounce:
				return t < 0.5f
					? (1 - OutBounce( 1 - 2 * t )) / 2
					: (1 + OutBounce( 2 * t - 1 )) / 2;
			default:
				TweenLog.Error( $"Invalid ease type: {ease}." );
				return t;
		}
	}

	const float FirstBounceAmplitude = 0.75f;

	internal static float Bounce( float t, float strength )
	{
		const float n1 = 7.5625f;
		const float d1 = 2.75f;
		if ( t < 1 / d1 )
			return n1 * t * t;

		float bounce;
		if ( t < 2 / d1 )
			bounce = n1 * (t -= 1.5f / d1) * t + FirstBounceAmplitude;
		else if ( t < 2.5f / d1 )
			bounce = n1 * (t -= 2.25f / d1) * t + 0.9375f;
		else
			bounce = n1 * (t -= 2.625f / d1) * t + 0.984375f;

		return 1 - (1 - bounce) * strength;
	}

	internal static float EvaluateParametric( float t, ParametricEase ease, float strength, float period, float duration )
	{
		switch ( ease )
		{
			case ParametricEase.Overshoot:
				t -= 1;
				return t * t * ((strength + 1) * t + strength) + 1;
			case ParametricEase.Elastic:
				const float twoPi = 2 * MathF.PI;
				float decayFactor;
				if ( strength >= 1 )
				{
					decayFactor = 1;
				}
				else
				{
					decayFactor = 1 / strength;
					strength = 1;
				}

				float decay = MathF.Pow( 2, -10 * t * decayFactor );
				if ( duration == 0 )
					return 1;

				period /= duration;
				float phase = period / twoPi * MathF.Asin( 1 / strength );
				return t > 0.9999f ? 1 : strength * decay * MathF.Sin( (t - phase) * twoPi / period ) + 1;
			case ParametricEase.Bounce:
				return Bounce( t, strength );
			default:
				TweenLog.Error( $"Invalid parametric ease: {ease}." );
				return t;
		}
	}

	/// <summary>
	/// Strength factor for <see cref="ParametricEase.BounceExact"/> so the first bounce has the exact amplitude in value units.
	/// </summary>
	internal static float BounceExactStrength( float amplitude, float fullAmplitude )
	{
		float strengthFactor = fullAmplitude < 0.0001f ? 1 : 1 / (fullAmplitude * (1 - FirstBounceAmplitude));
		return amplitude * strengthFactor;
	}
}
