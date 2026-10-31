namespace SboxTween;

public static partial class ComponentTweenExtensions
{
	/// <inheritdoc cref="Tween.Typewriter(TextRenderer, string, float, bool, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween TweenTypewriter( this TextRenderer target, string text, float duration, bool pauseOnPunctuation = true, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> Tween.Typewriter( target, text, duration, pauseOnPunctuation, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.TypewriterAtSpeed(TextRenderer, string, float, bool, float, float, bool)"/>
	public static Tween TweenTypewriterAtSpeed( this TextRenderer target, string text, float charactersPerSecond, bool pauseOnPunctuation = true, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> Tween.TypewriterAtSpeed( target, text, charactersPerSecond, pauseOnPunctuation, startDelay, endDelay, useUnscaledTime );
}