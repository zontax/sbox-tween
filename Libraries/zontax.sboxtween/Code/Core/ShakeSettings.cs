namespace SboxTween;

/// <summary>
/// Settings of shake and punch animations. Can be edited in the inspector.
/// </summary>
public struct ShakeSettings
{
	internal const float DefaultFrequency = 10;

	/// <summary>
	/// Strength per axis, in local space. Measured in inches for position/scale and in degrees (pitch, yaw, roll) for rotation.
	/// Shakes use the strongest component as the main axis. Punches use the strength as the punch direction.
	/// </summary>
	[Property] public Vector3 Strength { get; set; }

	[Property] public float Duration { get; set; }

	/// <summary>Number of shakes per second.</summary>
	[Property] public float Frequency { get; set; }

	/// <summary>With enabled falloff the shake starts at the highest strength and fades out to the end.</summary>
	[Property] public bool EnableFalloff { get; set; }

	/// <summary>Falloff ease is inverted: it goes from 1 to 0. Default is Ease.Linear. Set to Ease.Custom to use <see cref="StrengthOverTime"/>.</summary>
	[Property, ShowIf( nameof( EnableFalloff ), true )] public Ease FalloffEase { get; set; }

	/// <summary>Shake's strength over time, used when <see cref="FalloffEase"/> is Ease.Custom.</summary>
	[Property, ShowIf( nameof( FalloffEase ), Ease.Custom )] public Curve StrengthOverTime { get; set; }

	/// <summary>
	/// 0 means the shake is symmetrical around the initial value, 1 means it happens between the initial value and the strength.
	/// For punches it's the resistance to recoil.
	/// </summary>
	[Property, Range( 0, 1 )] public float Asymmetry { get; set; }

	/// <summary>Ease between adjacent shake points. Default is Ease.OutQuad.</summary>
	[Property] public Ease EaseBetweenShakes { get; set; }

	[Property] public int Cycles { get; set; }
	[Property] public float StartDelay { get; set; }
	[Property] public float EndDelay { get; set; }
	[Property] public bool UseUnscaledTime { get; set; }
	[Property] public UpdateType UpdateType { get; set; }

	internal bool IsPunch;

	internal ShakeSettings( Vector3 strength, float duration, float frequency, Ease? falloffEase, Curve strengthOverTime, Ease easeBetweenShakes, float asymmetryFactor, int cycles, float startDelay, float endDelay, bool useUnscaledTime, UpdateType updateType )
	{
		Frequency = frequency;
		Strength = strength;
		Duration = duration;
		if ( falloffEase == Ease.Custom && !TweenSettings.HasFrames( strengthOverTime ) )
		{
			TweenLog.Error( $"Shake falloff is Ease.Custom, but {nameof( StrengthOverTime )} is not configured correctly. Using Ease.Linear instead." );
			falloffEase = Ease.Linear;
		}

		FalloffEase = falloffEase ?? default;
		StrengthOverTime = falloffEase == Ease.Custom ? strengthOverTime : default;
		EnableFalloff = falloffEase is not null;
		EaseBetweenShakes = easeBetweenShakes;
		Cycles = cycles;
		StartDelay = startDelay;
		EndDelay = endDelay;
		UseUnscaledTime = useUnscaledTime;
		Asymmetry = asymmetryFactor;
		UpdateType = updateType;
		IsPunch = false;
	}

	public ShakeSettings( Vector3 strength, float duration = 0.5f, float frequency = DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes, UpdateType updateType = UpdateType.Default )
		: this( strength, duration, frequency, enableFalloff ? Ease.Default : null, default, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime, updateType )
	{
	}

	public ShakeSettings( Vector3 strength, float duration, float frequency, Curve strengthOverTime, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes, UpdateType updateType = UpdateType.Default )
		: this( strength, duration, frequency, Ease.Custom, strengthOverTime, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime, updateType )
	{
	}

	internal readonly TweenSettings TweenSettings => new( Duration, Ease.Linear, Cycles, CycleMode.Restart, StartDelay, EndDelay, UseUnscaledTime, UpdateType );

	internal readonly ShakeSettings WithPunch()
	{
		var result = this;
		result.IsPunch = true;
		return result;
	}
}
