using System.Runtime.CompilerServices;

namespace SboxTween;

/// <summary>
/// Per-panel bookkeeping. Transform tweens write into channels (translate, rotate, scale), and the manager composes
/// the channels into one <see cref="PanelTransform"/> per frame, so several tweens on one panel don't overwrite each other.
/// </summary>
internal sealed class PanelTweenState
{
	internal readonly Panel Panel;

	internal bool HasTranslate;
	internal Length TranslateX;
	internal Length TranslateY;
	internal bool HasRotate;
	internal float Rotate;
	internal bool HasScale;
	internal Vector2 Scale = Vector2.One;

	internal bool IsTransformDirty;

	/// <summary>Alive tweens per property group, so releasing a style doesn't clear a property another tween still animates.</summary>
	internal readonly int[] AliveCounts = new int[PanelTweens.TypeCount];

	internal PanelTweenState( Panel panel )
	{
		Panel = panel;
	}
}

/// <summary>
/// Reads and writes panel styles for panel tweens.
/// </summary>
internal static class PanelTweens
{
	const TweenType FirstType = TweenType.PanelOpacity;
	const TweenType LastType = TweenType.PanelBorderRadius;
	internal const int TypeCount = LastType - FirstType + 1;

	static readonly ConditionalWeakTable<Panel, PanelTweenState> States = new();

	internal static bool IsPanelType( TweenType type ) => type >= FirstType && type <= LastType;

	internal static bool IsTransformType( TweenType type ) => type is TweenType.PanelTranslate or TweenType.PanelRotate or TweenType.PanelScale or TweenType.PanelScaleUniform
		or TweenType.PanelShakeTranslate or TweenType.PanelShakeRotate or TweenType.PanelShakeScale;

	internal static bool IsLengthType( TweenType type ) => type >= TweenType.PanelWidth && type <= LastType;

	internal static PanelTweenState GetState( Panel panel ) => States.GetValue( panel, static p => new PanelTweenState( p ) );

	internal static PanelTweenState FindState( Panel panel ) => States.TryGetValue( panel, out var state ) ? state : null;

	/// <summary>Tweens that write the same style property share a group.</summary>
	static TweenType Group( TweenType type ) => type switch
	{
		TweenType.PanelShakeTranslate => TweenType.PanelTranslate,
		TweenType.PanelShakeRotate => TweenType.PanelRotate,
		TweenType.PanelScaleUniform or TweenType.PanelShakeScale => TweenType.PanelScale,
		_ => type
	};

	internal static PropType GetPropType( TweenType type )
	{
		switch ( type )
		{
			case TweenType.PanelTranslate:
			case TweenType.PanelScale:
				return PropType.Vector2;
			case TweenType.PanelShakeTranslate:
			case TweenType.PanelShakeRotate:
			case TweenType.PanelShakeScale:
				return PropType.Vector3;
			case TweenType.PanelBackgroundColor:
			case TweenType.PanelFontColor:
			case TweenType.PanelBorderColor:
			case TweenType.PanelBackgroundTint:
				return PropType.Color;
			default:
				return PropType.Float;
		}
	}

	/// <summary>The CSS property name, to detect conflicting CSS transitions.</summary>
	static string CssName( TweenType type ) => type switch
	{
		TweenType.PanelOpacity => "opacity",
		TweenType.PanelBackgroundColor => "background-color",
		TweenType.PanelFontColor => "color",
		TweenType.PanelBorderColor => "border-color",
		TweenType.PanelBackgroundTint => "background-tint",
		TweenType.PanelFilterBlur or TweenType.PanelFilterBrightness or TweenType.PanelFilterSaturate or TweenType.PanelFilterContrast => "filter",
		TweenType.PanelBackdropBlur => "backdrop-filter",
		TweenType.PanelWidth => "width",
		TweenType.PanelHeight => "height",
		TweenType.PanelLeft => "left",
		TweenType.PanelTop => "top",
		TweenType.PanelRight => "right",
		TweenType.PanelBottom => "bottom",
		TweenType.PanelMargin => "margin",
		TweenType.PanelMarginLeft => "margin-left",
		TweenType.PanelMarginTop => "margin-top",
		TweenType.PanelMarginRight => "margin-right",
		TweenType.PanelMarginBottom => "margin-bottom",
		TweenType.PanelPadding => "padding",
		TweenType.PanelPaddingLeft => "padding-left",
		TweenType.PanelPaddingTop => "padding-top",
		TweenType.PanelPaddingRight => "padding-right",
		TweenType.PanelPaddingBottom => "padding-bottom",
		TweenType.PanelGap => "gap",
		TweenType.PanelFontSize => "font-size",
		TweenType.PanelBorderRadius => "border-radius",
		_ => "transform"
	};

	#region Lifetime

	internal static void OnAdded( TweenData tween )
	{
		if ( tween.Target is Panel panel )
			GetState( panel ).AliveCounts[Group( tween.TweenType ) - FirstType]++;
	}

	internal static void OnReleased( TweenData tween )
	{
		if ( tween.Target is not Panel panel || FindState( panel ) is not { } state )
			return;

		int index = Group( tween.TweenType ) - FirstType;
		state.AliveCounts[index] = Math.Max( 0, state.AliveCounts[index] - 1 );
		if ( tween.ReleasePanelStyle && state.AliveCounts[index] == 0 && panel.IsValid )
			Release( state, tween.TweenType, tween.Manager );
	}

	/// <summary>
	/// Clears the inline style property written by a tween, so CSS controls it again.
	/// </summary>
	internal static void Release( PanelTweenState state, TweenType type, TweenManager manager )
	{
		var style = state.Panel.Style;
		switch ( Group( type ) )
		{
			case TweenType.PanelOpacity: style.Opacity = null; break;
			case TweenType.PanelTranslate: state.HasTranslate = false; manager?.MarkPanelDirty( state ); break;
			case TweenType.PanelRotate: state.HasRotate = false; manager?.MarkPanelDirty( state ); break;
			case TweenType.PanelScale: state.HasScale = false; state.Scale = Vector2.One; manager?.MarkPanelDirty( state ); break;
			case TweenType.PanelBackgroundColor: style.BackgroundColor = null; break;
			case TweenType.PanelFontColor: style.FontColor = null; break;
			case TweenType.PanelBorderColor: style.BorderColor = null; break;
			case TweenType.PanelBackgroundTint: style.BackgroundTint = null; break;
			case TweenType.PanelFilterBlur: style.FilterBlur = null; break;
			case TweenType.PanelFilterBrightness: style.FilterBrightness = null; break;
			case TweenType.PanelFilterSaturate: style.FilterSaturate = null; break;
			case TweenType.PanelFilterContrast: style.FilterContrast = null; break;
			case TweenType.PanelBackdropBlur: style.BackdropFilterBlur = null; break;
			default: SetLength( style, type, null ); break;
		}

		if ( manager is null )
			Flush( state );
	}

	/// <summary>
	/// Releases every property tweened on the panel.
	/// </summary>
	internal static void ReleaseAll( Panel panel )
	{
		if ( FindState( panel ) is not { } state )
			return;

		for ( var type = FirstType; type <= LastType; type++ )
		{
			if ( Group( type ) == type )
				Release( state, type, null );
		}
	}

	/// <summary>
	/// Composes the transform channels into the panel's inline transform.
	/// </summary>
	internal static void Flush( PanelTweenState state )
	{
		state.IsTransformDirty = false;
		var panel = state.Panel;
		if ( !panel.IsValid )
			return;

		if ( !state.HasTranslate && !state.HasRotate && !state.HasScale )
		{
			panel.Style.Transform = null;
			return;
		}

		// The same order as 'transform: translate() rotate() scale()'
		var transform = new PanelTransform();
		if ( state.HasTranslate )
			transform.AddTranslate( state.TranslateX, state.TranslateY );
		if ( state.HasRotate )
			transform.AddRotation( 0, 0, state.Rotate );
		if ( state.HasScale )
			transform.AddScale( new Vector3( state.Scale.x, state.Scale.y, 1 ) );

		panel.Style.Transform = transform;
	}

	#endregion

	#region Values

	internal static ValueContainer Get( TweenData tween )
	{
		var panel = tween.Target as Panel;
		var style = panel.Style;
		var computed = panel.ComputedStyle;
		var state = GetState( panel );

		switch ( tween.TweenType )
		{
			case TweenType.PanelOpacity: return ValueContainer.From( style.Opacity ?? computed?.Opacity ?? 1 );
			case TweenType.PanelTranslate:
				return new ValueContainer(
					state.HasTranslate && state.TranslateX.Unit == tween.LengthUnitX ? state.TranslateX.Value : 0,
					state.HasTranslate && state.TranslateY.Unit == tween.LengthUnitY ? state.TranslateY.Value : 0 );
			case TweenType.PanelRotate: return ValueContainer.From( state.HasRotate ? state.Rotate : 0 );
			case TweenType.PanelScale: return ValueContainer.From( state.Scale );
			case TweenType.PanelScaleUniform: return ValueContainer.From( state.Scale.x );
			case TweenType.PanelShakeTranslate:
				return new ValueContainer(
					state.HasTranslate && state.TranslateX.Unit == LengthUnit.Pixels ? state.TranslateX.Value : 0,
					state.HasTranslate && state.TranslateY.Unit == LengthUnit.Pixels ? state.TranslateY.Value : 0 );
			case TweenType.PanelShakeRotate: return ValueContainer.From( state.HasRotate ? state.Rotate : 0 );
			case TweenType.PanelShakeScale: return new ValueContainer( state.Scale.x, state.Scale.y );
			case TweenType.PanelBackgroundColor: return ValueContainer.From( style.BackgroundColor ?? computed?.BackgroundColor ?? Color.Transparent );
			case TweenType.PanelFontColor: return ValueContainer.From( style.FontColor ?? computed?.FontColor ?? Color.White );
			case TweenType.PanelBorderColor: return ValueContainer.From( style.BorderLeftColor ?? computed?.BorderLeftColor ?? Color.Transparent );
			case TweenType.PanelBackgroundTint: return ValueContainer.From( style.BackgroundTint ?? computed?.BackgroundTint ?? Color.White );
			case TweenType.PanelFilterBlur: return ValueContainer.From( Multiplier( style.FilterBlur ?? computed?.FilterBlur, 0 ) );
			case TweenType.PanelFilterBrightness: return ValueContainer.From( Multiplier( style.FilterBrightness ?? computed?.FilterBrightness, 1 ) );
			case TweenType.PanelFilterSaturate: return ValueContainer.From( Multiplier( style.FilterSaturate ?? computed?.FilterSaturate, 1 ) );
			case TweenType.PanelFilterContrast: return ValueContainer.From( Multiplier( style.FilterContrast ?? computed?.FilterContrast, 1 ) );
			case TweenType.PanelBackdropBlur: return ValueContainer.From( Multiplier( style.BackdropFilterBlur ?? computed?.BackdropFilterBlur, 0 ) );
			default: return ValueContainer.From( GetLength( tween, panel ) );
		}
	}

	static float Multiplier( Length? value, float fallback ) => value?.GetPixels( 1 ) ?? fallback;

	static float GetLength( TweenData tween, Panel panel )
	{
		var style = panel.Style;
		var computed = panel.ComputedStyle;
		var rect = panel.Box.Rect;
		return tween.TweenType switch
		{
			TweenType.PanelWidth => ReadLength( tween, panel, style.Width, computed?.Width, rect.Width ),
			TweenType.PanelHeight => ReadLength( tween, panel, style.Height, computed?.Height, rect.Height ),
			TweenType.PanelLeft => ReadLength( tween, panel, style.Left, computed?.Left, null ),
			TweenType.PanelTop => ReadLength( tween, panel, style.Top, computed?.Top, null ),
			TweenType.PanelRight => ReadLength( tween, panel, style.Right, computed?.Right, null ),
			TweenType.PanelBottom => ReadLength( tween, panel, style.Bottom, computed?.Bottom, null ),
			TweenType.PanelMargin or TweenType.PanelMarginLeft => ReadLength( tween, panel, style.MarginLeft, computed?.MarginLeft, 0 ),
			TweenType.PanelMarginTop => ReadLength( tween, panel, style.MarginTop, computed?.MarginTop, 0 ),
			TweenType.PanelMarginRight => ReadLength( tween, panel, style.MarginRight, computed?.MarginRight, 0 ),
			TweenType.PanelMarginBottom => ReadLength( tween, panel, style.MarginBottom, computed?.MarginBottom, 0 ),
			TweenType.PanelPadding or TweenType.PanelPaddingLeft => ReadLength( tween, panel, style.PaddingLeft, computed?.PaddingLeft, 0 ),
			TweenType.PanelPaddingTop => ReadLength( tween, panel, style.PaddingTop, computed?.PaddingTop, 0 ),
			TweenType.PanelPaddingRight => ReadLength( tween, panel, style.PaddingRight, computed?.PaddingRight, 0 ),
			TweenType.PanelPaddingBottom => ReadLength( tween, panel, style.PaddingBottom, computed?.PaddingBottom, 0 ),
			TweenType.PanelGap => ReadLength( tween, panel, style.RowGap, computed?.RowGap, 0 ),
			TweenType.PanelFontSize => ReadLength( tween, panel, style.FontSize, computed?.FontSize, null ),
			TweenType.PanelBorderRadius => ReadLength( tween, panel, style.BorderTopLeftRadius, computed?.BorderTopLeftRadius, 0 ),
			_ => throw new InvalidOperationException( $"Tween type {tween.TweenType} doesn't support reading the current value." )
		};
	}

	/// <summary>
	/// Reads a length in the unit of the tween. The inline style is in design pixels, while the computed style is already scaled to the screen.
	/// </summary>
	static float ReadLength( TweenData tween, Panel panel, Length? inline, Length? computed, float? screenPixelsFallback )
	{
		var unit = tween.LengthUnitX;
		if ( inline is { } i && i.Unit == unit )
			return i.Value;

		float scale = panel.ScaleToScreen > 0 ? panel.ScaleToScreen : 1;
		if ( computed is { } c && c.Unit == unit )
			return unit == LengthUnit.Pixels ? c.Value / scale : c.Value;

		if ( unit == LengthUnit.Pixels && screenPixelsFallback is { } fallback )
			return fallback / scale;

		TweenLog.Warning( $"Can't read the current value of {tween.GetDescription()} in '{unit}' units, starting from 0. Provide a start value to avoid this." );
		return 0;
	}

	internal static void Set( TweenData tween )
	{
		var panel = tween.Target as Panel;
		var style = panel.Style;
		if ( !tween.PanelTransitionChecked )
		{
			tween.PanelTransitionChecked = true;
			CheckTransitions( tween, panel );
		}

		var start = tween.StartValue;
		var delta = tween.EndValueOrDiff;
		float t = tween.EasedInterpolationFactor;
		float value = start.X + delta.X * t;

		switch ( tween.TweenType )
		{
			case TweenType.PanelOpacity: style.Opacity = value; break;
			case TweenType.PanelTranslate:
				SetTranslate( tween, new Length { Value = value, Unit = tween.LengthUnitX }, new Length { Value = start.Y + delta.Y * t, Unit = tween.LengthUnitY } );
				break;
			case TweenType.PanelRotate: SetRotate( tween, value ); break;
			case TweenType.PanelScale: SetScale( tween, tween.CurrentVector2 ); break;
			case TweenType.PanelScaleUniform: SetScale( tween, new Vector2( value, value ) ); break;
			case TweenType.PanelShakeTranslate:
			{
				var shake = start.Vector3 + tween.GetShakeValue();
				SetTranslate( tween, new Length { Value = shake.x, Unit = LengthUnit.Pixels }, new Length { Value = shake.y, Unit = LengthUnit.Pixels } );
				break;
			}
			case TweenType.PanelShakeRotate: SetRotate( tween, start.X + tween.GetShakeValue().x ); break;
			case TweenType.PanelShakeScale:
			{
				var shake = start.Vector3 + tween.GetShakeValue();
				SetScale( tween, new Vector2( shake.x, shake.y ) );
				break;
			}
			case TweenType.PanelBackgroundColor: style.BackgroundColor = tween.CurrentColor; break;
			case TweenType.PanelFontColor: style.FontColor = tween.CurrentColor; break;
			case TweenType.PanelBorderColor: style.BorderColor = tween.CurrentColor; break;
			case TweenType.PanelBackgroundTint: style.BackgroundTint = tween.CurrentColor; break;
			case TweenType.PanelFilterBlur: style.FilterBlur = value; break;
			case TweenType.PanelFilterBrightness: style.FilterBrightness = value; break;
			case TweenType.PanelFilterSaturate: style.FilterSaturate = value; break;
			case TweenType.PanelFilterContrast: style.FilterContrast = value; break;
			case TweenType.PanelBackdropBlur: style.BackdropFilterBlur = value; break;
			default: SetLength( style, tween.TweenType, new Length { Value = value, Unit = tween.LengthUnitX } ); break;
		}
	}

	static void SetTranslate( TweenData tween, Length x, Length y )
	{
		var state = GetState( tween.Target as Panel );
		state.HasTranslate = true;
		state.TranslateX = x;
		state.TranslateY = y;
		tween.Manager.MarkPanelDirty( state );
	}

	static void SetRotate( TweenData tween, float degrees )
	{
		var state = GetState( tween.Target as Panel );
		state.HasRotate = true;
		state.Rotate = degrees;
		tween.Manager.MarkPanelDirty( state );
	}

	static void SetScale( TweenData tween, Vector2 scale )
	{
		var state = GetState( tween.Target as Panel );
		state.HasScale = true;
		state.Scale = scale;
		tween.Manager.MarkPanelDirty( state );
	}

	static void SetLength( Styles style, TweenType type, Length? value )
	{
		switch ( type )
		{
			case TweenType.PanelWidth: style.Width = value; break;
			case TweenType.PanelHeight: style.Height = value; break;
			case TweenType.PanelLeft: style.Left = value; break;
			case TweenType.PanelTop: style.Top = value; break;
			case TweenType.PanelRight: style.Right = value; break;
			case TweenType.PanelBottom: style.Bottom = value; break;
			case TweenType.PanelMargin: style.Margin = value; break;
			case TweenType.PanelMarginLeft: style.MarginLeft = value; break;
			case TweenType.PanelMarginTop: style.MarginTop = value; break;
			case TweenType.PanelMarginRight: style.MarginRight = value; break;
			case TweenType.PanelMarginBottom: style.MarginBottom = value; break;
			case TweenType.PanelPadding: style.Padding = value; break;
			case TweenType.PanelPaddingLeft: style.PaddingLeft = value; break;
			case TweenType.PanelPaddingTop: style.PaddingTop = value; break;
			case TweenType.PanelPaddingRight: style.PaddingRight = value; break;
			case TweenType.PanelPaddingBottom: style.PaddingBottom = value; break;
			case TweenType.PanelGap:
				style.RowGap = value;
				style.ColumnGap = value;
				break;
			case TweenType.PanelFontSize: style.FontSize = value; break;
			case TweenType.PanelBorderRadius:
				style.BorderTopLeftRadius = value;
				style.BorderTopRightRadius = value;
				style.BorderBottomRightRadius = value;
				style.BorderBottomLeftRadius = value;
				break;
			default:
				throw new InvalidOperationException( $"Tween type {type} doesn't support setting the value." );
		}
	}

	#endregion

	/// <summary>
	/// A CSS transition on the same property restarts on every tween update and makes the animation lag behind.
	/// </summary>
	static void CheckTransitions( TweenData tween, Panel panel )
	{
		var transitions = panel.ComputedStyle?.Transitions?.List;
		if ( transitions is null )
			return;

		var css = CssName( tween.TweenType );
		foreach ( var transition in transitions )
		{
			var property = transition.Property;
			if ( string.IsNullOrEmpty( property ) )
				continue;

			if ( property == "all" || property == css || css.StartsWith( property + "-" ) || property.StartsWith( css + "-" ) )
			{
				tween.PanelTransitionConflict = true;
				if ( TweenConfig.WarnPanelTransitions )
					TweenLog.Warning( $"The panel has a CSS 'transition: {property}' that conflicts with the tween, so the animation will lag behind: {tween.GetDescription()}. Remove the transition for '{css}' from the panel's style. {Messages.CanBeDisabled( nameof( TweenConfig.WarnPanelTransitions ) )}" );
				return;
			}
		}
	}
}
