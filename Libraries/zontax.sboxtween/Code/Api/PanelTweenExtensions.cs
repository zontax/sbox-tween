namespace SboxTween;

public static partial class PanelTweenExtensions
{
	#region Translate with units

	/// <inheritdoc cref="Tween.PanelTranslate(Panel, Length?, Length?, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween TweenTranslate( this Panel target, Length? endX, Length? endY, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> Tween.PanelTranslate( target, endX, endY, duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.PanelTranslate(Panel, Length?, Length?, Length?, Length?, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween TweenTranslate( this Panel target, Length? startX, Length? startY, Length? endX, Length? endY, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> Tween.PanelTranslate( target, startX, startY, endX, endY, duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime );

	#endregion

	#region Shakes

	/// <inheritdoc cref="Tween.PanelShakeTranslate(Panel, Vector2, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween ShakeTranslate( this Panel target, Vector2 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> Tween.PanelShakeTranslate( target, strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.PanelPunchTranslate(Panel, Vector2, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PunchTranslate( this Panel target, Vector2 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> Tween.PanelPunchTranslate( target, strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.PanelShakeRotate(Panel, float, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween ShakeRotate( this Panel target, float degrees, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> Tween.PanelShakeRotate( target, degrees, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.PanelPunchRotate(Panel, float, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PunchRotate( this Panel target, float degrees, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> Tween.PanelPunchRotate( target, degrees, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.PanelShakeScale(Panel, Vector2, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween ShakeScale( this Panel target, Vector2 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> Tween.PanelShakeScale( target, strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime );

	/// <inheritdoc cref="Tween.PanelPunchScale(Panel, Vector2, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PunchScale( this Panel target, Vector2 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> Tween.PanelPunchScale( target, strength, duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime );

	#endregion

	#region Intro and outro

	/// <summary>Fades the panel in from fully transparent.</summary>
	public static Tween TweenFadeIn( this Panel panel, float duration = 0.3f, TweenEasing ease = default, float startDelay = 0 )
		=> Tween.PanelOpacity( panel, 0, 1, duration, ease, startDelay: startDelay );

	/// <summary>Fades the panel out from its current opacity.</summary>
	public static Tween TweenFadeOut( this Panel panel, float duration = 0.3f, TweenEasing ease = default, float startDelay = 0 )
		=> Tween.PanelOpacity( panel, 0, duration, ease, startDelay: startDelay );

	/// <summary>
	/// Slides the panel from <paramref name="offset"/> (pixels) to its place while fading in.
	/// <code>
	/// panel.TweenSlideIn( new Vector2( 0, 40 ) ); // from below
	/// </code>
	/// </summary>
	public static Sequence TweenSlideIn( this Panel panel, Vector2 offset, float duration = 0.4f, TweenEasing ease = default, float startDelay = 0 )
		=> Sequence.Create( Tween.PanelTranslate( panel, offset, Vector2.Zero, duration, ease.Ease == Ease.Default && !ease.HasCurve ? Ease.OutCubic : ease, startDelay: startDelay ) )
			.Group( Tween.PanelOpacity( panel, 0, 1, duration * 0.6f, Ease.OutQuad, startDelay: startDelay ) );

	/// <summary>Slides the panel from its place to <paramref name="offset"/> (pixels) while fading out.</summary>
	public static Sequence TweenSlideOut( this Panel panel, Vector2 offset, float duration = 0.3f, TweenEasing ease = default, float startDelay = 0 )
		=> Sequence.Create( Tween.PanelTranslate( panel, Vector2.Zero, offset, duration, ease.Ease == Ease.Default && !ease.HasCurve ? Ease.InCubic : ease, startDelay: startDelay ) )
			.Group( Tween.PanelOpacity( panel, 0, duration, Ease.InQuad, startDelay: startDelay ) );

	/// <summary>Pops the panel in: scales up from <paramref name="fromScale"/> with an overshoot while fading in.</summary>
	public static Sequence TweenPopIn( this Panel panel, float duration = 0.35f, float fromScale = 0.8f, float startDelay = 0 )
		=> Sequence.Create( Tween.PanelScale( panel, fromScale, 1, duration, Ease.OutBack, startDelay: startDelay ) )
			.Group( Tween.PanelOpacity( panel, 0, 1, duration * 0.6f, Ease.OutQuad, startDelay: startDelay ) );

	/// <summary>Pops the panel out: scales down to <paramref name="toScale"/> while fading out.</summary>
	public static Sequence TweenPopOut( this Panel panel, float duration = 0.2f, float toScale = 0.8f, float startDelay = 0 )
		=> Sequence.Create( Tween.PanelScale( panel, 1, toScale, duration, Ease.InBack, startDelay: startDelay ) )
			.Group( Tween.PanelOpacity( panel, 0, duration, Ease.InQuad, startDelay: startDelay ) );

	/// <summary>
	/// Deletes the panel when <paramref name="tween"/> completes. If the tween is stopped, the panel stays.
	/// <code>
	/// panel.DeleteAfter( panel.TweenPopOut() );
	/// </code>
	/// </summary>
	public static Tween DeleteAfter( this Panel panel, Tween tween ) => tween.OnComplete( panel, static p => p.Delete(), warnIfTargetDestroyed: false );

	/// <inheritdoc cref="DeleteAfter(Panel, Tween)"/>
	public static Sequence DeleteAfter( this Panel panel, Sequence sequence ) => sequence.OnComplete( panel, static p => p.Delete(), warnIfTargetDestroyed: false );

	#endregion

	#region Control

	/// <summary>
	/// Clears every inline style property written by tweens on this panel, so CSS controls them again.
	/// Alive tweens keep animating, so stop them first.
	/// </summary>
	public static void ReleaseTweenedStyles( this Panel panel )
	{
		if ( panel.IsValid() )
			PanelTweens.ReleaseAll( panel );
	}

	/// <summary>Stops all animations whose target is this panel. Returns the number of stopped animations.</summary>
	public static int StopTweens( this Panel panel ) => panel is null ? 0 : Tween.StopAll( panel );

	/// <summary>Completes all animations whose target is this panel. Returns the number of completed animations.</summary>
	public static int CompleteTweens( this Panel panel ) => panel is null ? 0 : Tween.CompleteAll( panel );

	/// <summary>Pauses or unpauses all animations whose target is this panel.</summary>
	public static int SetTweensPaused( this Panel panel, bool isPaused ) => panel is null ? 0 : Tween.SetPausedAll( isPaused, panel );

	#endregion
}
