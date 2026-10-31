namespace SboxTween;

internal enum TweenLogLevel
{
	Warning,
	Error
}

internal static class TweenLog
{
	/// <summary>
	/// Replaces the engine logger when set. Used by unit tests to assert on messages.
	/// </summary>
	internal static Action<TweenLogLevel, string> Interceptor;

	public static void Error( string message )
	{
		if ( Interceptor is not null )
		{
			Interceptor( TweenLogLevel.Error, message );
			return;
		}

		Log.Error( $"[SboxTween] {message}" );
	}

	public static void Warning( string message )
	{
		if ( Interceptor is not null )
		{
			Interceptor( TweenLogLevel.Warning, message );
			return;
		}

		Log.Warning( $"[SboxTween] {message}" );
	}

	public static void Exception( Exception e, string message )
	{
		if ( Interceptor is not null )
		{
			Interceptor( TweenLogLevel.Error, $"{message}\n{e}" );
			return;
		}

		Log.Error( e, $"[SboxTween] {message}" );
	}
}

internal static class Messages
{
	internal const string OnCompleteCallbackIgnored = "Tween's OnComplete callback was ignored.";
	internal const string DurationInvalid = "Tween's duration is invalid.";
	internal const string CantManipulateNested = "It's not allowed to manipulate 'nested' animations, please use the parent Sequence instead. When an animation is added to a Sequence, the Sequence controls the lifetime, pause state, timeScale and other settings of its children.";
	internal const string IsDead = "Animation is not alive. Please check the 'IsAlive' property before calling this API.";
	internal const string IsCompleting = "The Sequence is completing, so this call was ignored. Callbacks invoked while a Sequence completes can't pause it, or change its 'TimeScale', remaining cycles, or elapsed time.";
	internal const string DefaultCtor = "Animation is not created. Please check the 'IsAlive' property before calling this API. This error can mean that the animation was created with the default constructor or the target was invalid.";
	internal const string InfiniteTweenInSequence = "It's not allowed to have infinite tweens (cycles == -1) in a Sequence. If you want the Sequence to repeat forever, use Sequence.SetRemainingCycles( -1 ) on the parent Sequence instead.";
	internal const string ModificationIsNotAllowed = "Animation has already been started, it's not allowed to modify it anymore.";
	internal const string RecursiveCall = "Please don't call this API recursively from Tween.Custom() or tween.OnUpdate().";
	internal const string NestTwice = "An animation can be added to a Sequence only once and can only belong to one Sequence.";
	internal const string AddDeadTweenToSequence = "It's not allowed to add 'dead' tweens to a Sequence.";
	internal const string CustomCurveInvalid = "Ease is Ease.Custom, but the Curve is not configured correctly. Using Ease.Default instead.";
	internal const string OnCompleteDuplicate = "Tween already has an OnComplete callback. Adding more callbacks is not allowed. Use a Sequence with ChainCallback() to schedule multiple callbacks.";
	internal const string OnUpdateDuplicate = "Only one OnUpdate() is allowed for one animation.";
	internal const string OnFinallyDuplicate = "Only one OnFinally() is allowed for one animation.";
	internal const string OnValueChangeNull = "'onValueChange' is null.";
	internal const string NoManager = "There is no active scene to run the animation in. Animations need a Scene with a TweenSystem, or an explicit TweenManager scope.";
	internal const string DifferentManagers = "It's not allowed to combine animations from different scenes (TweenManagers) in one Sequence.";

	internal static string CanBeDisabled( string settingName ) => $"To disable this warning, set 'TweenConfig.{settingName} = false'.";
}
