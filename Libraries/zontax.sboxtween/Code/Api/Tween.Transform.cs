namespace SboxTween;

public readonly partial struct Tween
{
	static TweenSettings<Angles> ValidateAnglesData( TweenSettings<Angles> settings )
	{
		if ( settings.StartFromCurrent )
		{
			settings.StartFromCurrent = false;
			TweenLog.Warning( "Animating Euler angles from the current value may produce unexpected results because there is more than one way to represent the current rotation using Euler angles. 'StartFromCurrent' was ignored." );
		}

		return settings;
	}

	static TweenSettings<Rotation> ToRotation( TweenSettings<Angles> s ) => new( s.StartValue.ToRotation(), s.EndValue.ToRotation(), s.Settings ) { StartFromCurrent = s.StartFromCurrent };

	#region Speed based

	/// <summary>
	/// Moves a GameObject in local space at <paramref name="speed"/> inches per second.
	/// The duration is calculated immediately, so to chain speed-based tweens, provide the start value for all subsequent tweens.
	/// </summary>
	public static Tween LocalPositionAtSpeed( GameObject target, Vector3 endValue, float speed, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> PositionAtSpeed( target, new TweenSettings<Vector3>( endValue, new TweenSettings( speed, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) ), false );

	/// <inheritdoc cref="LocalPositionAtSpeed(GameObject, Vector3, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween LocalPositionAtSpeed( GameObject target, Vector3 startValue, Vector3 endValue, float speed, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> PositionAtSpeed( target, new TweenSettings<Vector3>( startValue, endValue, new TweenSettings( speed, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) ), false );

	/// <summary>
	/// Moves a GameObject in world space at <paramref name="speed"/> inches per second.
	/// The duration is calculated immediately, so to chain speed-based tweens, provide the start value for all subsequent tweens.
	/// </summary>
	public static Tween WorldPositionAtSpeed( GameObject target, Vector3 endValue, float speed, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> PositionAtSpeed( target, new TweenSettings<Vector3>( endValue, new TweenSettings( speed, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) ), true );

	/// <inheritdoc cref="WorldPositionAtSpeed(GameObject, Vector3, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween WorldPositionAtSpeed( GameObject target, Vector3 startValue, Vector3 endValue, float speed, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> PositionAtSpeed( target, new TweenSettings<Vector3>( startValue, endValue, new TweenSettings( speed, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) ), true );

	/// <summary>
	/// Rotates a GameObject in local space at <paramref name="averageAngularSpeed"/> degrees per second.
	/// </summary>
	public static Tween LocalRotationAtSpeed( GameObject target, Rotation endValue, float averageAngularSpeed, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> RotationAtSpeed( target, new TweenSettings<Rotation>( endValue, new TweenSettings( averageAngularSpeed, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) ), false );

	/// <inheritdoc cref="LocalRotationAtSpeed(GameObject, Rotation, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween LocalRotationAtSpeed( GameObject target, Rotation startValue, Rotation endValue, float averageAngularSpeed, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> RotationAtSpeed( target, new TweenSettings<Rotation>( startValue, endValue, new TweenSettings( averageAngularSpeed, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) ), false );

	/// <summary>
	/// Rotates a GameObject in world space at <paramref name="averageAngularSpeed"/> degrees per second.
	/// </summary>
	public static Tween WorldRotationAtSpeed( GameObject target, Rotation endValue, float averageAngularSpeed, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> RotationAtSpeed( target, new TweenSettings<Rotation>( endValue, new TweenSettings( averageAngularSpeed, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) ), true );

	/// <inheritdoc cref="WorldRotationAtSpeed(GameObject, Rotation, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween WorldRotationAtSpeed( GameObject target, Rotation startValue, Rotation endValue, float averageAngularSpeed, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> RotationAtSpeed( target, new TweenSettings<Rotation>( startValue, endValue, new TweenSettings( averageAngularSpeed, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) ), true );

	static Tween PositionAtSpeed( GameObject target, TweenSettings<Vector3> settings, bool world )
	{
		float speed = settings.Settings.Duration;
		if ( speed <= 0 )
		{
			TweenLog.Error( $"Invalid speed provided to the Tween.{(world ? nameof( WorldPositionAtSpeed ) : nameof( LocalPositionAtSpeed ))}() method: {speed}." );
			return default;
		}

		if ( settings.StartFromCurrent )
		{
			settings.StartFromCurrent = false;
			// Creating the tween reports the destroyed target
			settings.StartValue = target.IsValid() ? (world ? target.WorldPosition : target.LocalPosition) : Vector3.Zero;
		}

		var s = settings.Settings;
		s.Duration = settings.StartValue.Distance( settings.EndValue ) / speed;
		settings.Settings = s;
		return world ? WorldPosition( target, settings ) : LocalPosition( target, settings );
	}

	static Tween RotationAtSpeed( GameObject target, TweenSettings<Rotation> settings, bool world )
	{
		float speed = settings.Settings.Duration;
		if ( speed <= 0 )
		{
			TweenLog.Error( $"Invalid speed provided to the Tween.{(world ? nameof( WorldRotationAtSpeed ) : nameof( LocalRotationAtSpeed ))}() method: {speed}." );
			return default;
		}

		if ( settings.StartFromCurrent )
		{
			settings.StartFromCurrent = false;
			settings.StartValue = target.IsValid() ? (world ? target.WorldRotation : target.LocalRotation) : Rotation.Identity;
		}

		var s = settings.Settings;
		s.Duration = settings.StartValue.Distance( settings.EndValue ) / speed;
		settings.Settings = s;
		return world ? WorldRotation( target, settings ) : LocalRotation( target, settings );
	}

	#endregion
}
