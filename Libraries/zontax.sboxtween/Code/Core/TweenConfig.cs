namespace SboxTween;

/// <summary>
/// Global settings shared by all <see cref="TweenManager"/> instances.
/// </summary>
public static class TweenConfig
{
	internal const Ease DefaultShakeEase = Ease.OutQuad;
	internal const bool DefaultUseUnscaledTimeForShakes = false;

	static Ease _defaultEase = Ease.OutQuad;

	/// <summary>The ease used when an animation is created with <see cref="Ease.Default"/>.</summary>
	public static Ease DefaultEase
	{
		get => _defaultEase;
		set
		{
			if ( value == Ease.Custom || value == Ease.Default )
			{
				TweenLog.Error( "Only standard ease types are allowed as a default ease." );
				return;
			}

			_defaultEase = value;
		}
	}

	static UpdateType _defaultUpdateType = UpdateType.Update;

	/// <summary>The update type used when an animation is created with <see cref="UpdateType.Default"/>.</summary>
	public static UpdateType DefaultUpdateType
	{
		get => _defaultUpdateType;
		set
		{
			if ( value == UpdateType.Default )
			{
				TweenLog.Error( "UpdateType.Default can't be used as a default update type." );
				return;
			}

			_defaultUpdateType = value;
		}
	}

	/// <summary>What happens to an animation when its CancellationToken is canceled.</summary>
	public static TweenSettings.CancellationMode DefaultCancellationMode { get; set; } = TweenSettings.CancellationMode.Stop;

	/// <summary>Warn when an animation is started on a GameObject that is not active in the hierarchy.</summary>
	public static bool WarnTweenOnDisabledTarget { get; set; } = true;

	/// <summary>Warn when an animation is created with a duration of zero or less.</summary>
	public static bool WarnZeroDuration { get; set; } = true;

	/// <summary>Warn when a curve passed as a custom ease doesn't start at (0, 0|1) and end at (1, 0|1).</summary>
	public static bool ValidateCustomCurves { get; set; } = true;

	/// <summary>Warn when an animation starts from the current value and the current value already equals the end value.</summary>
	public static bool WarnEndValueEqualsCurrent { get; set; } = true;

	/// <summary>Log an error when an OnComplete callback is ignored because its target was destroyed.</summary>
	public static bool WarnIfTargetDestroyed { get; set; } = true;
}
