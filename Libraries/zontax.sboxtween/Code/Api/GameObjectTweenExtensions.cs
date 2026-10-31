namespace SboxTween;

public static partial class GameObjectTweenExtensions
{
	/// <inheritdoc cref="Tween.ShakeLocalPosition(GameObject, Vector3, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween ShakeLocalPosition( this GameObject target, Vector3 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> Tween.ShakeLocalPosition( target, strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.ShakeLocalPosition(GameObject, ShakeSettings)"/>
	public static Tween ShakeLocalPosition( this GameObject target, ShakeSettings settings ) => Tween.ShakeLocalPosition( target, settings );

	/// <inheritdoc cref="Tween.PunchLocalPosition(GameObject, Vector3, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PunchLocalPosition( this GameObject target, Vector3 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> Tween.PunchLocalPosition( target, strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.PunchLocalPosition(GameObject, ShakeSettings)"/>
	public static Tween PunchLocalPosition( this GameObject target, ShakeSettings settings ) => Tween.PunchLocalPosition( target, settings );

	/// <inheritdoc cref="Tween.ShakeLocalRotation(GameObject, Vector3, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween ShakeLocalRotation( this GameObject target, Vector3 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> Tween.ShakeLocalRotation( target, strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.ShakeLocalRotation(GameObject, ShakeSettings)"/>
	public static Tween ShakeLocalRotation( this GameObject target, ShakeSettings settings ) => Tween.ShakeLocalRotation( target, settings );

	/// <inheritdoc cref="Tween.PunchLocalRotation(GameObject, Vector3, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PunchLocalRotation( this GameObject target, Vector3 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> Tween.PunchLocalRotation( target, strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.PunchLocalRotation(GameObject, ShakeSettings)"/>
	public static Tween PunchLocalRotation( this GameObject target, ShakeSettings settings ) => Tween.PunchLocalRotation( target, settings );

	/// <inheritdoc cref="Tween.ShakeLocalScale(GameObject, Vector3, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween ShakeLocalScale( this GameObject target, Vector3 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> Tween.ShakeLocalScale( target, strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.ShakeLocalScale(GameObject, ShakeSettings)"/>
	public static Tween ShakeLocalScale( this GameObject target, ShakeSettings settings ) => Tween.ShakeLocalScale( target, settings );

	/// <inheritdoc cref="Tween.PunchLocalScale(GameObject, Vector3, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PunchLocalScale( this GameObject target, Vector3 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> Tween.PunchLocalScale( target, strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.PunchLocalScale(GameObject, ShakeSettings)"/>
	public static Tween PunchLocalScale( this GameObject target, ShakeSettings settings ) => Tween.PunchLocalScale( target, settings );

	/// <summary>Stops all animations whose target is this GameObject. Returns the number of stopped animations.</summary>
	public static int StopTweens( this GameObject target ) => target is null ? 0 : Tween.StopAll( target );

	/// <summary>Completes all animations whose target is this GameObject. Returns the number of completed animations.</summary>
	public static int CompleteTweens( this GameObject target ) => target is null ? 0 : Tween.CompleteAll( target );

	/// <summary>Pauses or unpauses all animations whose target is this GameObject.</summary>
	public static int SetTweensPaused( this GameObject target, bool isPaused ) => target is null ? 0 : Tween.SetPausedAll( isPaused, target );
}
