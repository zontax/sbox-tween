namespace SboxTween;

/// <summary>
/// Panel tweens animate the inline style of a UI panel (<c>panel.Style</c>), which has priority over stylesheets.
/// <list type="bullet">
/// <item>Prefer transform (translate, scale, rotate) and opacity: they don't recalculate the layout. Width, margins and other layout properties do, every frame.</item>
/// <item>Don't set an animated property in a razor <c>style="..."</c> attribute: razor overwrites it whenever the attribute string changes. Classes are fine.</item>
/// <item>A CSS <c>transition</c> on the same property makes the tween lag behind; see <see cref="TweenConfig.WarnPanelTransitions"/>.</item>
/// <item>The animated value stays after the tween ends. Use <see cref="ReleaseStyleOnComplete"/> to give the property back to CSS.</item>
/// </list>
/// </summary>
public readonly partial struct Tween
{
	/// <summary>
	/// Panel tweens only: when the tween ends in any way (completed, stopped, or its panel deleted), clears the inline style property it animated,
	/// so the stylesheet controls the property again. Useful for effects like hover punches.
	/// </summary>
	public Tween ReleaseStyleOnComplete()
	{
		if ( !ValidateIsAlive() )
			return this;

		if ( !PanelTweens.IsPanelType( Data.TweenType ) )
		{
			TweenLog.Error( $"{nameof( ReleaseStyleOnComplete )}() only works with panel tweens, tween: {this}." );
			return this;
		}

		Data.ReleasePanelStyle = true;
		return this;
	}

	#region Translate with units

	/// <summary>
	/// Moves a panel with <c>transform: translate()</c> in any units, e.g. <c>Length.Percent( -100 )</c> moves it by its own width.
	/// Doesn't recalculate the layout.
	/// </summary>
	public static Tween PanelTranslate( Panel target, Length? endX, Length? endY, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> PanelTranslateLength( target, null, null, endX, endY, true, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="PanelTranslate(Panel, Length?, Length?, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween PanelTranslate( Panel target, Length? startX, Length? startY, Length? endX, Length? endY, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> PanelTranslateLength( target, startX, startY, endX, endY, false, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) );

	static Tween PanelTranslateLength( Panel target, Length? startX, Length? startY, Length? endX, Length? endY, bool startFromCurrent, TweenSettings settings )
	{
		if ( endX is not { } ex || endY is not { } ey || !ValidateUnit( ex.Unit ) || !ValidateUnit( ey.Unit ) )
		{
			TweenLog.Error( "Translate needs both end values in pixels, percents, view or em units." );
			return default;
		}

		var start = default( ValueContainer );
		if ( !startFromCurrent )
		{
			if ( startX is not { } sx || startY is not { } sy || sx.Unit != ex.Unit || sy.Unit != ey.Unit )
			{
				TweenLog.Error( "Start and end values of a translate must use the same units on each axis." );
				return default;
			}

			start = new ValueContainer( sx.Value, sy.Value );
		}

		return AnimateCore( target, TweenType.PanelTranslate, start, new ValueContainer( ex.Value, ey.Value ), settings, startFromCurrent, unitX: ex.Unit, unitY: ey.Unit );
	}

	#endregion

	#region Shakes

	/// <summary>Shakes a panel with <c>transform: translate()</c>. <paramref name="strength"/> is in pixels.</summary>
	public static Tween PanelShakeTranslate( Panel target, Vector2 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> PanelShakeTranslate( target, new ShakeSettings( new Vector3( strength.x, strength.y, 0 ), duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="PanelShakeTranslate(Panel, Vector2, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PanelShakeTranslate( Panel target, ShakeSettings settings ) => ShakeTransform( TweenType.PanelShakeTranslate, target, settings );

	/// <summary>Punches a panel in the direction of <paramref name="strength"/> (pixels), then returns it back.</summary>
	public static Tween PanelPunchTranslate( Panel target, Vector2 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> PanelShakeTranslate( target, new ShakeSettings( new Vector3( strength.x, strength.y, 0 ), duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime ).WithPunch() );

	/// <inheritdoc cref="PanelPunchTranslate(Panel, Vector2, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PanelPunchTranslate( Panel target, ShakeSettings settings ) => PanelShakeTranslate( target, settings.WithPunch() );

	/// <summary>Shakes the rotation of a panel. <paramref name="degrees"/> is the amplitude.</summary>
	public static Tween PanelShakeRotate( Panel target, float degrees, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> PanelShakeRotate( target, new ShakeSettings( new Vector3( degrees, 0, 0 ), duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime ) );

	/// <summary>Shakes the rotation of a panel. The X component of <see cref="ShakeSettings.Strength"/> is the amplitude in degrees.</summary>
	public static Tween PanelShakeRotate( Panel target, ShakeSettings settings ) => ShakeTransform( TweenType.PanelShakeRotate, target, settings );

	/// <summary>Punches the rotation of a panel by <paramref name="degrees"/>, then returns it back.</summary>
	public static Tween PanelPunchRotate( Panel target, float degrees, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> PanelShakeRotate( target, new ShakeSettings( new Vector3( degrees, 0, 0 ), duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime ).WithPunch() );

	/// <inheritdoc cref="PanelPunchRotate(Panel, float, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PanelPunchRotate( Panel target, ShakeSettings settings ) => PanelShakeRotate( target, settings.WithPunch() );

	/// <summary>Shakes the scale of a panel. <paramref name="strength"/> is added to the scale.</summary>
	public static Tween PanelShakeScale( Panel target, Vector2 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> PanelShakeScale( target, new ShakeSettings( new Vector3( strength.x, strength.y, 0 ), duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="PanelShakeScale(Panel, Vector2, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PanelShakeScale( Panel target, ShakeSettings settings ) => ShakeTransform( TweenType.PanelShakeScale, target, settings );

	/// <summary>Punches the scale of a panel, e.g. a 'squash and stretch' effect on a button press.</summary>
	public static Tween PanelPunchScale( Panel target, Vector2 strength, float duration, float frequency = ShakeSettings.DefaultFrequency, bool enableFalloff = true, Ease easeBetweenShakes = Ease.Default, float asymmetryFactor = 0, int cycles = 1,
		float startDelay = 0, float endDelay = 0, bool useUnscaledTime = TweenConfig.DefaultUseUnscaledTimeForShakes )
		=> PanelShakeScale( target, new ShakeSettings( new Vector3( strength.x, strength.y, 0 ), duration, frequency, enableFalloff, easeBetweenShakes, asymmetryFactor, cycles, startDelay, endDelay, useUnscaledTime ).WithPunch() );

	/// <inheritdoc cref="PanelPunchScale(Panel, Vector2, float, float, bool, Ease, float, int, float, float, bool)"/>
	public static Tween PanelPunchScale( Panel target, ShakeSettings settings ) => PanelShakeScale( target, settings.WithPunch() );

	#endregion

	internal static Tween Animate( object target, TweenType tweenType, TweenSettings<Length> s )
	{
		var unit = s.EndValue.Unit;
		if ( !ValidateUnit( unit ) )
		{
			TweenLog.Error( $"Can't animate a length in '{unit}' units. Use pixels, percents, view or em units." );
			return default;
		}

		if ( !s.StartFromCurrent && s.StartValue.Unit != unit )
		{
			TweenLog.Error( $"Start and end values must use the same units, but the start is in '{s.StartValue.Unit}' and the end is in '{unit}'." );
			return default;
		}

		return AnimateCore( target, tweenType, ValueContainer.From( s.StartValue.Value ), ValueContainer.From( s.EndValue.Value ), s.Settings, s.StartFromCurrent, unitX: unit );
	}

	static bool ValidateUnit( LengthUnit unit ) => unit is LengthUnit.Pixels or LengthUnit.Percentage or LengthUnit.ViewWidth or LengthUnit.ViewHeight
		or LengthUnit.ViewMin or LengthUnit.ViewMax or LengthUnit.Em or LengthUnit.RootEm;
}
