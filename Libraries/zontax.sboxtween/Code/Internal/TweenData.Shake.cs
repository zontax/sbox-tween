namespace SboxTween;

internal struct ShakeState
{
	internal float T;
	internal float PrevInterpolationFactor;
	internal int PrevCyclesDone;
	internal float Frequency;
	internal Vector3 From;
	internal Vector3 To;
	internal float SymmetryFactor;
	/// <summary>Ease.Default means the falloff is disabled.</summary>
	internal Ease FalloffEase;
	internal Ease EaseBetweenShakes;
	internal Curve StrengthOverTime;
	internal bool IsPunch;
	internal bool Sign;
}

internal sealed partial class TweenData
{
	internal ShakeState Shake;

	internal void SetupShake( in ShakeSettings settings )
	{
		Shake = default;
		Shake.IsPunch = settings.IsPunch;
		Shake.SymmetryFactor = Math.Clamp( 1 - settings.Asymmetry, 0, 1 );

		var strength = settings.Strength;
		if ( float.IsNaN( strength.x ) || float.IsNaN( strength.y ) || float.IsNaN( strength.z ) )
		{
			TweenLog.Error( $"Shake's strength is invalid: {strength}. Using 0 instead of NaN components." );
			strength = new Vector3( float.IsNaN( strength.x ) ? 0 : strength.x, float.IsNaN( strength.y ) ? 0 : strength.y, float.IsNaN( strength.z ) ? 0 : strength.z );
		}

		if ( strength == Vector3.Zero )
			TweenLog.Error( "Shake's strength is (0, 0, 0)." );

		// Shakes don't use EndValueOrDiff, so it stores the strength
		EndValueOrDiff = ValueContainer.From( strength );

		var frequency = settings.Frequency;
		if ( !(frequency > 0) || float.IsInfinity( frequency ) )
		{
			TweenLog.Error( $"Shake's frequency should be > 0, but was {frequency}." );
			frequency = ShakeSettings.DefaultFrequency;
		}

		Shake.Frequency = frequency;

		if ( settings.EnableFalloff )
		{
			var falloffEase = settings.FalloffEase;
			if ( falloffEase == Ease.Default )
				falloffEase = Ease.Linear;

			if ( falloffEase == Ease.Custom && !TweenSettings.HasFrames( settings.StrengthOverTime ) )
			{
				TweenLog.Error( $"Shake falloff is Ease.Custom, but {nameof( ShakeSettings.StrengthOverTime )} is not configured correctly. Using Ease.Linear instead." );
				falloffEase = Ease.Linear;
			}

			Shake.FalloffEase = falloffEase;
			Shake.StrengthOverTime = settings.StrengthOverTime;
		}
		else
		{
			Shake.FalloffEase = Ease.Default;
		}

		var easeBetweenShakes = settings.EaseBetweenShakes;
		if ( easeBetweenShakes == Ease.Custom )
		{
			TweenLog.Error( $"{nameof( ShakeSettings.EaseBetweenShakes )} doesn't support Ease.Custom." );
			easeBetweenShakes = Ease.OutQuad;
		}

		if ( easeBetweenShakes == Ease.Default )
			easeBetweenShakes = TweenConfig.DefaultShakeEase;

		Shake.EaseBetweenShakes = easeBetweenShakes;
		OnShakeCycleComplete();
	}

	void OnShakeCycleComplete()
	{
		Shake.T = 0;
		Shake.From = default;
		Shake.Sign = Shake.IsPunch || Manager.Random.NextDouble() < 0.5;
		Shake.To = GenerateShakePoint();
	}

	internal Vector3 GetShakeValue()
	{
		float frequency = Shake.Frequency;
		float fadeInOutFactor = CalcShakeFadeInOutFactor( frequency );
		return GetNextShakeValue( frequency ) * fadeInOutFactor;
	}

	float CalcShakeFadeInOutFactor( float frequency )
	{
		float animationDuration = AnimationDuration;
		float elapsedTimeInterpolating = EasedInterpolationFactor * animationDuration;
		if ( animationDuration == 0 )
			return 0;

		float halfDuration = animationDuration * 0.5f;
		float oneShakeDuration = 1 / frequency;
		if ( oneShakeDuration > halfDuration )
			oneShakeDuration = halfDuration;

		float fadeInDuration = oneShakeDuration * 0.5f;
		if ( elapsedTimeInterpolating < fadeInDuration )
			return MathX.LerpInverse( elapsedTimeInterpolating, 0, fadeInDuration );

		float fadeoutStartTime = animationDuration - oneShakeDuration;
		if ( elapsedTimeInterpolating > fadeoutStartTime )
			return MathX.LerpInverse( elapsedTimeInterpolating, animationDuration, fadeoutStartTime );

		return 1;
	}

	Vector3 GetNextShakeValue( float frequency )
	{
		float interpolationFactor = EasedInterpolationFactor;

		int cyclesDiff = GetCyclesDone() - Shake.PrevCyclesDone;
		Shake.PrevCyclesDone = GetCyclesDone();
		if ( interpolationFactor == 0 || (cyclesDiff > 0 && GetCyclesDone() != CyclesTotal) )
		{
			OnShakeCycleComplete();
			Shake.PrevInterpolationFactor = interpolationFactor;
		}

		float animationDuration = AnimationDuration;
		float dt = (interpolationFactor - Shake.PrevInterpolationFactor) * animationDuration;
		Shake.PrevInterpolationFactor = interpolationFactor;

		float strengthOverTime = CalcShakeStrengthOverTime( interpolationFactor );
		// Handpicked formula that describes the relationship between strength and frequency
		float frequencyFactor = Math.Clamp( strengthOverTime * 3, 0, 1 );

		// The initial velocity should be twice as big because the first shake starts from zero
		float elapsedTimeInterpolating = interpolationFactor * animationDuration;
		float halfShakeDuration = 0.5f / frequency;
		float iniVelFactor = elapsedTimeInterpolating < halfShakeDuration ? 2 : 1;

		Shake.T += frequency * dt * frequencyFactor * iniVelFactor;
		if ( Shake.T < 0 || Shake.T >= 1 )
		{
			Shake.Sign = !Shake.Sign;
			if ( Shake.T < 0 )
			{
				Shake.T = 1;
				Shake.To = Shake.From;
				Shake.From = GenerateShakePoint();
			}
			else
			{
				Shake.T = 0;
				Shake.From = Shake.To;
				Shake.To = GenerateShakePoint();
			}
		}

		var strengthPerAxis = EndValueOrDiff.Vector3;
		float easedT = StandardEasing.Evaluate( Shake.T, Shake.EaseBetweenShakes );
		return new Vector3(
			MathX.Lerp( Shake.From.x, Shake.To.x, easedT, false ) * strengthPerAxis.x * strengthOverTime,
			MathX.Lerp( Shake.From.y, Shake.To.y, easedT, false ) * strengthPerAxis.y * strengthOverTime,
			MathX.Lerp( Shake.From.z, Shake.To.z, easedT, false ) * strengthPerAxis.z * strengthOverTime );
	}

	Vector3 GenerateShakePoint()
	{
		var strengthPerAxis = EndValueOrDiff.Vector3;
		int mainAxisIndex = GetMainAxisIndex( strengthPerAxis );
		float symmetryFactor = Shake.SymmetryFactor;
		float signFloat = Shake.Sign ? 1 : -1;
		var result = Vector3.Zero;
		for ( int i = 0; i < 3; i++ )
		{
			float strength = strengthPerAxis[i];
			float val;
			if ( Shake.IsPunch )
				val = ClampBySymmetryFactor( strength * signFloat, strength, symmetryFactor );
			else
				val = i == mainAxisIndex ? CalcMainAxisEndVal( signFloat, strength, symmetryFactor ) : CalcNonMainAxisEndVal( strength, symmetryFactor );

			result[i] = strength == 0 ? 0 : val / strength;
		}

		return result;
	}

	static int GetMainAxisIndex( Vector3 strengthByAxis )
	{
		int mainAxisIndex = -1;
		float maxStrength = float.NegativeInfinity;
		for ( int i = 0; i < 3; i++ )
		{
			float strength = MathF.Abs( strengthByAxis[i] );
			if ( strength > maxStrength )
			{
				maxStrength = strength;
				mainAxisIndex = i;
			}
		}

		return mainAxisIndex;
	}

	float CalcShakeStrengthOverTime( float interpolationFactor )
	{
		if ( Shake.FalloffEase == Ease.Default )
			return 1;

		if ( Shake.FalloffEase != Ease.Custom )
			return 1 - StandardEasing.Evaluate( interpolationFactor, Shake.FalloffEase );

		return Shake.StrengthOverTime.Evaluate( interpolationFactor );
	}

	float CalcMainAxisEndVal( float velocity, float strength, float symmetryFactor )
	{
		// Strength or its abs doesn't matter because velocity alternates
		float result = MathF.Sign( velocity ) * strength * RandomRange( 0.6f, 1 );
		return ClampBySymmetryFactor( result, strength, symmetryFactor );
	}

	static float ClampBySymmetryFactor( float val, float strength, float symmetryFactor )
	{
		if ( strength > 0 )
			return Math.Clamp( val, -strength * symmetryFactor, strength );

		return Math.Clamp( val, strength, -strength * symmetryFactor );
	}

	float CalcNonMainAxisEndVal( float strength, float symmetryFactor )
	{
		if ( strength > 0 )
			return RandomRange( -strength * symmetryFactor, strength );

		return RandomRange( strength, -strength * symmetryFactor );
	}

	float RandomRange( float minInclusive, float max )
	{
		double val = Manager.Random.NextDouble();
		return (float)(minInclusive + val * (max - minInclusive));
	}

	/// <summary>
	/// Several shakes on the same target share the start value of the first one, so they don't drift.
	/// </summary>
	bool TryTakeStartValueFromOtherShake()
	{
		if ( !IsShake( TweenType ) || Target is not (GameObject or Panel) )
			return false;

		var shakes = Manager.Shakes;
		var key = (Target, TweenType);
		if ( !shakes.TryGetValue( key, out var data ) )
		{
			var startValue = GetAnimatedValue();
			shakes.Add( key, (startValue, 1) );
			return false;
		}

		StartValue = data.StartValue;
		data.Count++;
		shakes[key] = data;
		return true;
	}

	void ResetShakeStartValue()
	{
		if ( Target is not (GameObject or Panel) || Manager is null )
			return;

		var key = (Target, TweenType);
		var shakes = Manager.Shakes;
		if ( !shakes.TryGetValue( key, out var data ) )
			return;

		data.Count--;
		if ( data.Count == 0 )
			shakes.Remove( key );
		else
			shakes[key] = data;
	}
}
