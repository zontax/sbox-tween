namespace SboxTween;

/// <summary>
/// Additive tweens animate delta changes instead of absolute values, so several additive tweens can animate the same value
/// at the same time, blending their changes. Don't rely on exact values animated by additive tweens.
/// </summary>
public readonly partial struct Tween
{
	#region Transform

	/// <summary>Moves a GameObject by <paramref name="deltaValue"/> in local space, blending with other additive tweens.</summary>
	public static Tween LocalPositionAdditive( GameObject target, Vector3 deltaValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> LocalPositionAdditive( target, deltaValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="LocalPositionAdditive(GameObject, Vector3, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween LocalPositionAdditive( GameObject target, Vector3 deltaValue, TweenSettings settings )
		=> CustomAdditive( target, deltaValue, settings, AddLocalPosition, GetLocalPosition );

	/// <summary>Moves a GameObject by <paramref name="deltaValue"/> in world space, blending with other additive tweens.</summary>
	public static Tween WorldPositionAdditive( GameObject target, Vector3 deltaValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> WorldPositionAdditive( target, deltaValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="WorldPositionAdditive(GameObject, Vector3, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween WorldPositionAdditive( GameObject target, Vector3 deltaValue, TweenSettings settings )
		=> CustomAdditive( target, deltaValue, settings, AddWorldPosition, GetWorldPosition );

	/// <summary>Changes the local scale of a GameObject by <paramref name="deltaValue"/>, blending with other additive tweens.</summary>
	public static Tween LocalScaleAdditive( GameObject target, Vector3 deltaValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> LocalScaleAdditive( target, deltaValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="LocalScaleAdditive(GameObject, Vector3, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween LocalScaleAdditive( GameObject target, Vector3 deltaValue, TweenSettings settings )
		=> CustomAdditive( target, deltaValue, settings, AddLocalScale, GetLocalScale );

	/// <summary>
	/// Rotates a GameObject by <paramref name="deltaValue"/> Euler angles relative to its current rotation, blending with other additive tweens.
	/// Angles can exceed 180 degrees, e.g. (0, 720, 0) spins twice.
	/// </summary>
	public static Tween LocalRotationAdditive( GameObject target, Angles deltaValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> LocalRotationAdditive( target, deltaValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="LocalRotationAdditive(GameObject, Angles, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween LocalRotationAdditive( GameObject target, Angles deltaValue, TweenSettings settings )
		=> AdditiveCore( target, TweenType.CustomAngles, ValueContainer.From( deltaValue ), settings, (Action<GameObject, Rotation>)LocalRotate, (Func<GameObject, Rotation>)GetLocalRotation, AdditiveInvokers<GameObject>.EulerRotation, false );

	/// <summary>Rotates a GameObject by <paramref name="deltaValue"/> relative to its current rotation, blending with other additive tweens.</summary>
	public static Tween LocalRotationAdditive( GameObject target, Rotation deltaValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> LocalRotationAdditive( target, deltaValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="LocalRotationAdditive(GameObject, Rotation, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween LocalRotationAdditive( GameObject target, Rotation deltaValue, TweenSettings settings )
		=> CustomAdditive( target, deltaValue, settings, LocalRotate, GetLocalRotation );

	/// <summary>
	/// Rotates a GameObject by <paramref name="deltaValue"/> Euler angles relative to its current world rotation, blending with other additive tweens.
	/// </summary>
	public static Tween WorldRotationAdditive( GameObject target, Angles deltaValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> WorldRotationAdditive( target, deltaValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="WorldRotationAdditive(GameObject, Angles, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween WorldRotationAdditive( GameObject target, Angles deltaValue, TweenSettings settings )
		=> AdditiveCore( target, TweenType.CustomAngles, ValueContainer.From( deltaValue ), settings, (Action<GameObject, Rotation>)WorldRotate, (Func<GameObject, Rotation>)GetWorldRotation, AdditiveInvokers<GameObject>.EulerRotation, false );

	/// <summary>Rotates a GameObject by <paramref name="deltaValue"/> relative to its current world rotation, blending with other additive tweens.</summary>
	public static Tween WorldRotationAdditive( GameObject target, Rotation deltaValue, float duration, TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false )
		=> WorldRotationAdditive( target, deltaValue, new TweenSettings( duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime ) );

	/// <inheritdoc cref="WorldRotationAdditive(GameObject, Rotation, float, TweenEasing, int, CycleMode, float, float, bool)"/>
	public static Tween WorldRotationAdditive( GameObject target, Rotation deltaValue, TweenSettings settings )
		=> CustomAdditive( target, deltaValue, settings, WorldRotate, GetWorldRotation );

	static void AddLocalPosition( GameObject go, Vector3 delta ) => go.LocalPosition += delta;
	static void AddWorldPosition( GameObject go, Vector3 delta ) => go.WorldPosition += delta;
	static void AddLocalScale( GameObject go, Vector3 delta ) => go.LocalScale += delta;
	static Vector3 GetLocalPosition( GameObject go ) => go.LocalPosition;
	static Vector3 GetWorldPosition( GameObject go ) => go.WorldPosition;
	static Vector3 GetLocalScale( GameObject go ) => go.LocalScale;

	// The delta is applied from the right, so it rotates around the object's own axes
	static void LocalRotate( GameObject go, Rotation delta ) => go.LocalRotation *= delta;
	static void WorldRotate( GameObject go, Rotation delta ) => go.WorldRotation *= delta;
	static Rotation GetLocalRotation( GameObject go ) => go.LocalRotation;
	static Rotation GetWorldRotation( GameObject go ) => go.WorldRotation;

	#endregion

	#region Custom

	/// <summary>
	/// Animates a custom value additively: <paramref name="onDeltaChange"/> receives the change since the previous update.
	/// <code>
	/// Tween.CustomAdditive( this, 10f, new TweenSettings( 1 ),
	///     onDeltaChange: static ( self, delta ) => self.Value += delta,
	///     getCurrentValue: static self => self.Value );
	/// </code>
	/// </summary>
	/// <param name="getCurrentValue">
	/// Applying a delta rounds it to the precision of the value. With the getter, the tween measures how much of each delta the value actually received
	/// and carries the remainder to the next update, so the value doesn't drift over time.
	/// </param>
	/// <param name="useDurationAsSpeed">Treat the duration as speed (units per second) and calculate the real duration from the delta.</param>
	public static Tween CustomAdditive<T>( T target, float deltaValue, TweenSettings settings, Action<T, float> onDeltaChange, Func<T, float> getCurrentValue = null, bool useDurationAsSpeed = false ) where T : class
		=> AdditiveCore( target, TweenType.CustomFloat, ValueContainer.From( deltaValue ), settings, onDeltaChange, getCurrentValue, AdditiveInvokers<T>.FloatDelta, useDurationAsSpeed );

	/// <inheritdoc cref="CustomAdditive{T}(T, float, TweenSettings, Action{T, float}, Func{T, float}, bool)"/>
	public static Tween CustomAdditive<T>( T target, Vector2 deltaValue, TweenSettings settings, Action<T, Vector2> onDeltaChange, Func<T, Vector2> getCurrentValue = null, bool useDurationAsSpeed = false ) where T : class
		=> AdditiveCore( target, TweenType.CustomVector2, ValueContainer.From( deltaValue ), settings, onDeltaChange, getCurrentValue, AdditiveInvokers<T>.Vector2Delta, useDurationAsSpeed );

	/// <inheritdoc cref="CustomAdditive{T}(T, float, TweenSettings, Action{T, float}, Func{T, float}, bool)"/>
	public static Tween CustomAdditive<T>( T target, Vector3 deltaValue, TweenSettings settings, Action<T, Vector3> onDeltaChange, Func<T, Vector3> getCurrentValue = null, bool useDurationAsSpeed = false ) where T : class
		=> AdditiveCore( target, TweenType.CustomVector3, ValueContainer.From( deltaValue ), settings, onDeltaChange, getCurrentValue, AdditiveInvokers<T>.Vector3Delta, useDurationAsSpeed );

	/// <inheritdoc cref="CustomAdditive{T}(T, float, TweenSettings, Action{T, float}, Func{T, float}, bool)"/>
	public static Tween CustomAdditive<T>( T target, Vector4 deltaValue, TweenSettings settings, Action<T, Vector4> onDeltaChange, Func<T, Vector4> getCurrentValue = null, bool useDurationAsSpeed = false ) where T : class
		=> AdditiveCore( target, TweenType.CustomVector4, ValueContainer.From( deltaValue ), settings, onDeltaChange, getCurrentValue, AdditiveInvokers<T>.Vector4Delta, useDurationAsSpeed );

	/// <inheritdoc cref="CustomAdditive{T}(T, float, TweenSettings, Action{T, float}, Func{T, float}, bool)"/>
	public static Tween CustomAdditive<T>( T target, Color deltaValue, TweenSettings settings, Action<T, Color> onDeltaChange, Func<T, Color> getCurrentValue = null, bool useDurationAsSpeed = false ) where T : class
		=> AdditiveCore( target, TweenType.CustomColor, ValueContainer.From( deltaValue ), settings, onDeltaChange, getCurrentValue, AdditiveInvokers<T>.ColorDelta, useDurationAsSpeed );

	/// <summary>
	/// Animates a custom rotation additively: <paramref name="onDeltaChange"/> receives the rotation since the previous update.
	/// The delta assumes it's applied from the right: <c>rotation *= delta</c>.
	/// </summary>
	public static Tween CustomAdditive<T>( T target, Rotation deltaValue, TweenSettings settings, Action<T, Rotation> onDeltaChange, Func<T, Rotation> getCurrentValue = null, bool useDurationAsSpeed = false ) where T : class
		=> AdditiveCore( target, TweenType.CustomRotation, ValueContainer.From( deltaValue ), settings, onDeltaChange, getCurrentValue, AdditiveInvokers<T>.RotationDelta, useDurationAsSpeed );

	static Tween AdditiveCore( object target, TweenType tweenType, ValueContainer deltaValue, TweenSettings settings, object onDeltaChange, object getCurrentValue, Action<TweenData> invoker, bool useDurationAsSpeed )
	{
		if ( onDeltaChange is null )
		{
			TweenLog.Error( Messages.OnValueChangeNull );
			return default;
		}

		var propType = TweenData.GetPropType( tweenType );
		bool isRotation = propType == PropType.Rotation || tweenType == TweenType.CustomAngles;
		var startValue = propType == PropType.Rotation ? ValueContainer.From( Rotation.Identity ) : default;

		if ( useDurationAsSpeed )
		{
			float speed = settings.Duration;
			if ( speed <= 0 )
			{
				TweenLog.Error( $"Invalid speed provided to the Tween.CustomAdditive() method: {speed}." );
				return default;
			}

			settings.Duration = CalcDistance( propType, startValue, deltaValue ) / speed;
		}

		if ( getCurrentValue is null && TweenConfig.WarnCustomAdditiveWithoutGetter )
			TweenLog.Warning( $"CustomAdditive() is created without 'getCurrentValue'. Without the getter, the animated value accumulates floating-point rounding errors over time. {Messages.CanBeDisabled( nameof( TweenConfig.WarnCustomAdditiveWithoutGetter ) )}" );

		var manager = TweenManager.Resolve( target );
		if ( manager is null || manager.IsDisposed )
			return default;

		var tween = manager.Fetch( settings.UpdateType );
		tween.IsAdditive = true;
		tween.CustomOnValueChange = onDeltaChange;
		tween.OnValueChangeInvoker = invoker;
		// The getter only measures how much of each delta the target received; additive tweens never start from the current value
		tween.CustomGetter = getCurrentValue;
		tween.AdditiveErr = isRotation ? ValueContainer.From( Rotation.Identity ) : default;
		tween.StartValue = startValue;
		tween.EndValueOrDiff = deltaValue;
		tween.Setup( target, ref settings, false, tweenType );
		return manager.Animate( tween );
	}

	#endregion
}

/// <summary>
/// Value change handlers of additive tweens. They report the difference between the current and the previous update.
/// </summary>
static class AdditiveInvokers<T> where T : class
{
	// Named methods instead of lambdas, so hotload can upgrade the invokers of running tweens
	internal static readonly Action<TweenData> FloatDelta = InvokeFloat;
	internal static readonly Action<TweenData> Vector2Delta = InvokeVector2;
	internal static readonly Action<TweenData> Vector3Delta = InvokeVector3;
	internal static readonly Action<TweenData> Vector4Delta = InvokeVector4;
	internal static readonly Action<TweenData> ColorDelta = InvokeColor;
	internal static readonly Action<TweenData> RotationDelta = InvokeRotation;
	internal static readonly Action<TweenData> EulerRotation = InvokeEulerRotation;

	static void InvokeFloat( TweenData d ) => Linear( d, ToFloat, FromFloat );
	static void InvokeVector2( TweenData d ) => Linear( d, ToVector2, FromVector2 );
	static void InvokeVector3( TweenData d ) => Linear( d, ToVector3, FromVector3 );
	static void InvokeVector4( TweenData d ) => Linear( d, ToVector4, FromVector4 );
	static void InvokeColor( TweenData d ) => Linear( d, ToColor, FromColor );

	static float ToFloat( ValueContainer c ) => c.X;
	static Vector2 ToVector2( ValueContainer c ) => c.Vector2;
	static Vector3 ToVector3( ValueContainer c ) => c.Vector3;
	static Vector4 ToVector4( ValueContainer c ) => c.Vector4;
	static Color ToColor( ValueContainer c ) => c.Color;
	static ValueContainer FromFloat( float v ) => ValueContainer.From( v );
	static ValueContainer FromVector2( Vector2 v ) => ValueContainer.From( v );
	static ValueContainer FromVector3( Vector3 v ) => ValueContainer.From( v );
	static ValueContainer FromVector4( Vector4 v ) => ValueContainer.From( v );
	static ValueContainer FromColor( Color v ) => ValueContainer.From( v );

	static void InvokeRotation( TweenData d )
	{
		// The rotation since the previous update, plus the rotation the target missed because of rounding
		var start = d.StartValue.Rotation;
		var end = d.EndValueOrDiff.Rotation;
		var previous = Rotation.Slerp( start, end, d.AdditivePrevT, false );
		var current = Rotation.Slerp( start, end, d.EasedInterpolationFactor, false );
		ApplyRotation( d, d.AdditiveErr.Rotation * previous.Inverse * current );
	}

	static void InvokeEulerRotation( TweenData d )
	{
		// Composing Euler( delta ) of each update is not Euler( sum of deltas ) when more than one axis is animated, so the delta is a rotation
		var previous = new Angles( (d.StartValue + d.EndValueOrDiff * d.AdditivePrevT).Vector3 ).ToRotation();
		var current = new Angles( (d.StartValue + d.EndValueOrDiff * d.EasedInterpolationFactor).Vector3 ).ToRotation();
		ApplyRotation( d, d.AdditiveErr.Rotation * previous.Inverse * current );
	}

	static void ApplyRotation( TweenData d, Rotation delta )
	{
		if ( !d.BeginCustomCallback() )
			return;

		try
		{
			var target = d.Target as T;
			var getter = d.CustomGetter as Func<T, Rotation>;
			var before = getter is not null ? getter( target ) : default;
			(d.CustomOnValueChange as Action<T, Rotation>)( target, delta );
			if ( getter is not null )
				d.AdditiveErr = ValueContainer.From( RotationError( before, getter( target ), delta ) );

			d.AdditivePrevT = d.EasedInterpolationFactor;
		}
		finally
		{
			d.IsUpdating = false;
		}
	}

	static void Linear<TValue>( TweenData d, Func<ValueContainer, TValue> toValue, Func<TValue, ValueContainer> fromValue )
	{
		if ( !d.BeginCustomCallback() )
			return;

		try
		{
			// Calculated in double: the product and the difference of two floats are (nearly) exact in double,
			// so the only rounding left is the conversion to float, which is carried to the next update
			float t = d.EasedInterpolationFactor;
			float prevT = d.AdditivePrevT;
			var diff = d.EndValueOrDiff;
			var err = d.AdditiveErr;
			double dx = (double)diff.X * t - (double)diff.X * prevT + err.X;
			double dy = (double)diff.Y * t - (double)diff.Y * prevT + err.Y;
			double dz = (double)diff.Z * t - (double)diff.Z * prevT + err.Z;
			double dw = (double)diff.W * t - (double)diff.W * prevT + err.W;
			var delta = new ValueContainer( (float)dx, (float)dy, (float)dz, (float)dw );

			var target = d.Target as T;
			var getter = d.CustomGetter as Func<T, TValue>;
			var before = getter is not null ? fromValue( getter( target ) ) : default;
			(d.CustomOnValueChange as Action<T, TValue>)( target, toValue( delta ) );

			if ( getter is not null )
			{
				var after = fromValue( getter( target ) );
				d.AdditiveErr = new ValueContainer( LinearError( dx, before.X, after.X ), LinearError( dy, before.Y, after.Y ), LinearError( dz, before.Z, after.Z ), LinearError( dw, before.W, after.W ) );
			}
			else
			{
				// Without the getter, assume the target absorbed the float delta exactly
				d.AdditiveErr = new ValueContainer( (float)(dx - delta.X), (float)(dy - delta.Y), (float)(dz - delta.Z), (float)(dw - delta.W) );
			}

			d.AdditivePrevT = t;
		}
		finally
		{
			d.IsUpdating = false;
		}
	}

	/// <summary>
	/// The error only compensates the rounding, which is below 2 ulps of the values. A bigger error means the setter didn't apply the delta right away,
	/// clamped it, or the getter returned a non-finite value. Carrying such an error would apply the delta again, so it's discarded.
	/// </summary>
	static float LinearError( double delta, float before, float after )
	{
		double err = delta - ((double)after - before);
		double bound = Math.Max( Math.Max( Math.Abs( (double)before ), Math.Abs( (double)after ) ), Math.Abs( delta ) ) * 2.4e-7;
		return Math.Abs( err ) <= bound && bound <= float.MaxValue ? (float)err : 0;
	}

	/// <summary>
	/// The rotation the target missed, so applying 'target * err * step' catches up. The rounding is a rotation of ~1e-7 radians.
	/// </summary>
	static Rotation RotationError( Rotation before, Rotation after, Rotation delta )
	{
		var err = (after.Inverse * before * delta).Normal;
		return MathF.Abs( err.x ) + MathF.Abs( err.y ) + MathF.Abs( err.z ) <= 1e-5f ? err : Rotation.Identity;
	}
}
