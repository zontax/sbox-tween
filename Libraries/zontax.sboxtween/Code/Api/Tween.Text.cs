namespace SboxTween;

public readonly partial struct Tween
{
	/// <summary>
	/// Types <paramref name="text"/> into a TextRenderer character by character. The default ease is linear.
	/// <code>
	/// Tween.Typewriter( sign, "Hello, world!", 1.5f );
	/// </code>
	/// </summary>
	/// <param name="text">The text to type. If null, the current text of the renderer is typed.</param>
	/// <param name="pauseOnPunctuation">Pause after a sentence (. ! ?) and a bit shorter after a clause (, ; :).</param>
	public static Tween Typewriter( TextRenderer target, string text, float duration, bool pauseOnPunctuation = true, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> Typewriter( target, text, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ), pauseOnPunctuation );

	/// <inheritdoc cref="Typewriter(TextRenderer, string, float, bool, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween Typewriter( TextRenderer target, string text, TweenSettings settings, bool pauseOnPunctuation = true )
		=> TypewriterCore( target, TweenType.TextTypewriter, text ?? target?.Text, settings, pauseOnPunctuation, false );

	/// <summary>
	/// Types <paramref name="text"/> into a TextRenderer at a constant speed. Punctuation pauses add to the duration.
	/// </summary>
	/// <param name="text">The text to type. If null, the current text of the renderer is typed.</param>
	/// <param name="charactersPerSecond">Typing speed.</param>
	/// <param name="pauseOnPunctuation">Pause after a sentence (. ! ?) and a bit shorter after a clause (, ; :).</param>
	public static Tween TypewriterAtSpeed( TextRenderer target, string text, float charactersPerSecond, bool pauseOnPunctuation = true, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> TypewriterCore( target, TweenType.TextTypewriter, text ?? target?.Text, new TweenSettings( charactersPerSecond, Ease.Linear, startDelay: startDelay, endDelay: endDelay, useUnscaledTime: useUnscaledTime ), pauseOnPunctuation, true );

	/// <summary>
	/// Types <paramref name="text"/> into a UI Label character by character. The default ease is linear.
	/// </summary>
	/// <param name="text">The text to type. If null, the current text of the label is typed.</param>
	/// <param name="pauseOnPunctuation">Pause after a sentence (. ! ?) and a bit shorter after a clause (, ; :).</param>
	public static Tween Typewriter( Label target, string text, float duration, bool pauseOnPunctuation = true, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> Typewriter( target, text, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ), pauseOnPunctuation );

	/// <inheritdoc cref="Typewriter(Label, string, float, bool, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween Typewriter( Label target, string text, TweenSettings settings, bool pauseOnPunctuation = true )
		=> TypewriterCore( target, TweenType.LabelTypewriter, text ?? target?.Text, settings, pauseOnPunctuation, false );

	/// <summary>
	/// Types <paramref name="text"/> into a UI Label at a constant speed. Punctuation pauses add to the duration.
	/// </summary>
	/// <param name="text">The text to type. If null, the current text of the label is typed.</param>
	/// <param name="charactersPerSecond">Typing speed.</param>
	/// <param name="pauseOnPunctuation">Pause after a sentence (. ! ?) and a bit shorter after a clause (, ; :).</param>
	public static Tween TypewriterAtSpeed( Label target, string text, float charactersPerSecond, bool pauseOnPunctuation = true, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> TypewriterCore( target, TweenType.LabelTypewriter, text ?? target?.Text, new TweenSettings( charactersPerSecond, Ease.Linear, startDelay: startDelay, endDelay: endDelay, useUnscaledTime: useUnscaledTime ), pauseOnPunctuation, true );

	static Tween TypewriterCore( object target, TweenType tweenType, string text, TweenSettings settings, bool pauseOnPunctuation, bool useDurationAsSpeed )
	{
		text ??= "";
		float weight = TweenData.CalcTypewriterWeight( text, pauseOnPunctuation );
		if ( useDurationAsSpeed )
		{
			if ( settings.Duration <= 0 )
			{
				TweenLog.Error( $"Invalid typing speed: {settings.Duration} characters per second." );
				return default;
			}

			settings.Duration = weight / settings.Duration;
		}

		if ( settings.Ease == Ease.Default )
			settings.Ease = Ease.Linear;

		var tween = AnimateCore( target, tweenType, ValueContainer.From( 0 ), ValueContainer.From( 1 ), settings, false, parameterName: text );
		if ( tween.IsAliveInternal )
		{
			tween.Data.TypewriterWeight = weight;
			tween.Data.TypewriterPauses = pauseOnPunctuation;
			tween.Data.TypewriterVisible = -1;
		}

		return tween;
	}
}