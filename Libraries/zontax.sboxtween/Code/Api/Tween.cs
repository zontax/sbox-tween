using System.Runtime.CompilerServices;
using System.Threading;

namespace SboxTween;

/// <summary>
/// The main API of SboxTween. Use static Tween methods to start animations, then use the returned struct to control them.
/// <br/><br/>
/// Tweens are non-reusable: when a tween completes or is stopped, it becomes 'dead' (<see cref="IsAlive"/> == false).
/// To play the animation again or in the opposite direction, simply start a new Tween. Starting tweens is fast and doesn't allocate.
/// <code>
/// var tween = Tween.LocalPositionX( GameObject, endValue: 100, duration: 1 );
/// if ( tween.IsAlive )
///     Log.Info( $"Animation is still running, elapsed time: {tween.ElapsedTime}." );
/// </code>
/// </summary>
public readonly partial struct Tween : IEquatable<Tween>
{
	/// <summary>
	/// Same limit as Unity's Time.timeScale. A huge timeScale makes an animation cross a huge number of cycles in one update.
	/// </summary>
	internal const float MaxTimeScale = 100;

	internal readonly long Id;
	internal readonly TweenData Data;

	internal Tween( TweenData data )
	{
		Id = data.Id;
		Data = data;
	}

	internal bool IsCreated => Id != 0;

	bool IsReferenceValid => Id != 0 && Data is not null && Data.Id == Id;

	internal bool IsAliveInternal => IsReferenceValid && Data.IsAlive;

	/// <summary>
	/// A tween is 'alive' when it has been created and has not been stopped or completed yet. A paused tween is also alive.
	/// </summary>
	public bool IsAlive => IsAliveInternal;

	/// <summary>Elapsed time of the current cycle.</summary>
	public float ElapsedTime
	{
		get
		{
			if ( !ValidateIsAlive() )
				return 0;

			if ( CyclesDone == CyclesTotal )
				return Duration;

			float result = ElapsedTimeTotal - Duration * CyclesDone;
			return result < 0 ? 0 : result;
		}
		set => SetElapsedTime( value );
	}

	void SetElapsedTime( float value )
	{
		if ( !TryManipulate() )
			return;

		if ( value < 0 || float.IsNaN( value ) )
		{
			TweenLog.Error( $"Invalid ElapsedTime value: {value}, tween: {this}" );
			return;
		}

		float cycleDuration = Duration;
		if ( value > cycleDuration )
			value = cycleDuration;

		int cyclesDone = CyclesDone;
		if ( cyclesDone == CyclesTotal )
			cyclesDone -= 1;

		SetElapsedTimeTotal( value + cycleDuration * cyclesDone );
	}

	/// <summary>The total number of cycles. Returns -1 to indicate an infinite number of cycles.</summary>
	public int CyclesTotal => ValidateIsAlive() ? Data.CyclesTotal : 0;

	/// <summary>The number of completed cycles.</summary>
	public int CyclesDone => ValidateIsAlive() ? Data.GetCyclesDone() : 0;

	/// <summary>The duration of one cycle, including start and end delays.</summary>
	public float Duration
	{
		get
		{
			if ( !ValidateIsAlive() )
				return 0;

			float result = Data.CycleDuration;
			TweenSettings.ValidateFiniteDuration( ref result );
			return result;
		}
	}

	/// <summary>Elapsed time of all cycles.</summary>
	public float ElapsedTimeTotal
	{
		get
		{
			if ( !ValidateIsAlive() )
				return 0;

			if ( Data.ElapsedTimeTotal == float.MaxValue )
				return DurationTotal;

			return Math.Clamp( Data.ElapsedTimeTotal - Data.WaitDelay, 0, DurationTotal );
		}
		set => SetElapsedTimeTotal( value );
	}

	void SetElapsedTimeTotal( float value )
	{
		if ( !TryManipulate() )
			return;

		if ( value < 0 || float.IsNaN( value ) || (CyclesTotal == -1 && value >= float.MaxValue) )
		{
			TweenLog.Error( $"Invalid ElapsedTimeTotal value: {value}, tween: {this}" );
			return;
		}

		if ( CyclesTotal == -1 )
		{
			float cycleDuration = Data.CycleDuration;
			// float can't represent the time inside a cycle after 2^24 cycles
			if ( cycleDuration != 0 && value / cycleDuration >= 1 << 24 )
			{
				TweenLog.Error( $"Invalid ElapsedTimeTotal value: {value}. An infinite animation can't be moved further than {1 << 24} cycles because of the float precision, tween: {this}" );
				return;
			}
		}
		else if ( value >= DurationTotal )
		{
			// float.MaxValue moves the animation to the end without calculating cyclesDone
			value = float.MaxValue;
		}

		if ( !ValidateIsNotCompleting() )
			return;

		if ( Data.IsUpdating )
		{
			// A callback of a Sequence's child can't seek the Sequence while it's updating its children
			TweenLog.Error( Messages.RecursiveCall );
			return;
		}

		var data = Data;
		// A callback can reassign the variable this method was called on
		var self = this;
		using ( new TweenManager.UpdateDepthScope( data.Manager ) )
		{
			data.SetElapsedTimeTotal( value, false );

			// SetElapsedTimeTotal may complete the tween
			if ( data.IsAlive )
			{
				// Not 'value > durationTotal': OnComplete() may have moved the animation to another time
				if ( data.ElapsedTimeTotal == float.MaxValue )
					data.ElapsedTimeTotal = self.DurationTotal;
			}
			else
			{
				data.ReportOnFinally();
			}
		}
	}

	/// <summary>The duration of all cycles. If cycles == -1, returns <see cref="float.PositiveInfinity"/>.</summary>
	public float DurationTotal
	{
		get
		{
			if ( !ValidateIsAlive() )
				return 0;

			int cycles = Data.CyclesTotal;
			if ( cycles == -1 )
				return float.PositiveInfinity;

			// The product of finite values can overflow to infinity
			return MathF.Min( Data.CycleDuration * cycles, float.MaxValue );
		}
	}

	/// <summary>Normalized progress of the current cycle, in the 0..1 range.</summary>
	public float Progress
	{
		get
		{
			if ( !ValidateIsAlive() )
				return 0;

			if ( Duration == 0 )
				return GetProgressFromState();

			return MathF.Min( ElapsedTime / Duration, 1 );
		}
		set
		{
			value = Math.Clamp( value, 0, 1 );
			if ( value == 1 && CyclesDone == CyclesTotal - 1 )
			{
				SetElapsedTimeTotal( float.MaxValue );
				return;
			}

			SetElapsedTime( value * Duration );
		}
	}

	/// <summary>Normalized progress of all cycles, in the 0..1 range.</summary>
	public float ProgressTotal
	{
		get
		{
			if ( !ValidateIsAlive() )
				return 0;

			if ( CyclesTotal == -1 )
				return 0;

			float totalDuration = DurationTotal;
			if ( totalDuration == 0 )
				return GetProgressFromState();

			return MathF.Min( ElapsedTimeTotal / totalDuration, 1 );
		}
		set
		{
			if ( !ValidateIsAlive() )
				return;

			if ( CyclesTotal == -1 )
			{
				TweenLog.Error( $"It's not allowed to set ProgressTotal on an infinite tween (CyclesTotal == -1), tween: {this}." );
				return;
			}

			value = Math.Clamp( value, 0, 1 );
			if ( value == 1 )
			{
				SetElapsedTimeTotal( float.MaxValue );
				return;
			}

			SetElapsedTimeTotal( value * DurationTotal );
		}
	}

	float GetProgressFromState() => Data.State == State.After ? 1 : 0;

	/// <summary>The current eased percentage of change between start and end values.</summary>
	public float InterpolationFactor => ValidateIsAlive() ? MathF.Max( 0, Data.EasedInterpolationFactor ) : 0;

	public bool IsPaused
	{
		get => TryManipulate() && Data.IsPaused;
		set
		{
			if ( !TryManipulate() || !ValidateIsNotCompleting() )
				return;

			var data = Data;
			if ( !data.TrySetPause( value ) || value )
				return;

			if ( (TimeScale > 0 && ProgressTotal >= 1) || (TimeScale < 0 && ProgressTotal == 0) )
			{
				if ( data.IsMainSequenceRoot() )
					new Sequence( data ).ReleaseTweens();
				else
					data.Kill();

				data.ReportOnFinally();
			}
		}
	}

	/// <summary>Custom timeScale. To smoothly animate the timeScale, use <see cref="Tween.TweenTimeScale(Tween, TweenSettings{float})"/>.</summary>
	public float TimeScale
	{
		get => TryManipulate() ? Data.TimeScale : 1;
		set
		{
			if ( !TryManipulate() )
				return;

			if ( float.IsNaN( value ) || float.IsInfinity( value ) )
				throw new ArgumentException( $"Invalid TimeScale: {value}." );

			ClampTimeScale( ref value );
			if ( Data.HasWait && !ValidateWaitTimeScale( value ) )
				return;

			if ( ValidateIsNotCompleting() )
				Data.TimeScale = value;
		}
	}

	internal static bool ValidateWaitTimeScale( float timeScale )
	{
		if ( timeScale < 0 )
		{
			TweenLog.Error( "Negative 'TimeScale' is not supported on a Sequence that contains a ChainWaitWhile() or ChainWaitUntil() element." );
			return false;
		}

		return true;
	}

	internal static void ClampTimeScale( ref float timeScale )
	{
		if ( MathF.Abs( timeScale ) > MaxTimeScale )
		{
			TweenLog.Error( $"'TimeScale' ({timeScale}) should be in the [-{MaxTimeScale}, {MaxTimeScale}] range." );
			timeScale = Math.Clamp( timeScale, -MaxTimeScale, MaxTimeScale );
		}
	}

	/// <summary>Interrupts the tween, ignoring the OnComplete callback.</summary>
	public void Stop()
	{
		if ( !IsAlive || !TryManipulate( false ) )
			return;

		Data.Kill();
		Data.ReportOnFinally();
	}

	/// <summary>
	/// Immediately completes the tween. If the tween has infinite cycles, completes only the current cycle.
	/// To choose between start and end values in the case of infinite cycles, use <see cref="SetRemainingCycles(bool)"/> before calling Complete().
	/// </summary>
	public void Complete()
	{
		// Don't warn that a tween is dead because a dead tween is already 'completed'
		if ( !IsAlive || !TryManipulate( false ) )
			return;

		var data = Data;
		if ( data.IsPaused && data.TimeScale > 0 && data.CyclesDone == data.CyclesTotal )
		{
			// The tween has completed while paused and its OnComplete has already been called
			data.Kill();
		}
		else
		{
			using ( new TweenManager.UpdateDepthScope( data.Manager ) )
				data.ForceComplete();
		}

		data.ReportOnFinally();
	}

	/// <summary>
	/// Stops the tween when it reaches the start or end value the next time.
	/// For example, to stop an infinite Yoyo animation at its end value, pass true.
	/// </summary>
	public void SetRemainingCycles( bool stopAtEndValue )
	{
		if ( !TryManipulate() )
			return;

		var data = Data;
		if ( data.CycleMode == CycleMode.Restart || data.CycleMode == CycleMode.Incremental )
			TweenLog.Warning( $"{nameof( SetRemainingCycles )}( bool stopAtEndValue ) is meant to be used with CycleMode.Yoyo or Rewind. Please consider using the overload that accepts int instead." );

		bool isOneCycleLeft = data.GetCyclesDone() % 2 == 0 == stopAtEndValue;
		if ( data.IsSequenceInverted )
			isOneCycleLeft = !isOneCycleLeft;

		SetRemainingCycles( isOneCycleLeft ? 1 : 2 );
	}

	/// <summary>
	/// Sets the number of remaining cycles. Modifies <see cref="CyclesTotal"/> so the tween completes after the given number of cycles.
	/// Pass -1 to repeat the tween indefinitely.
	/// </summary>
	public void SetRemainingCycles( int cycles )
	{
		if ( !TryManipulate() )
			return;

		if ( cycles < -1 )
		{
			TweenLog.Error( $"Invalid 'cycles' value: {cycles}, tween: {this}" );
			return;
		}

		var data = Data;
		if ( data.HasWait )
		{
			// A Sequence with a wait always has 1 cycle
			if ( cycles != 1 )
				TweenLog.Error( $"'{nameof( SetRemainingCycles )}()' with cycles other than 1 is not supported on a Sequence that contains a ChainWaitWhile() or ChainWaitUntil() element." );
			return;
		}

		if ( data.TweenType == TweenType.Delay && data.HasOnComplete )
		{
			TweenLog.Error( "Applying cycles to Delay will not repeat the OnComplete() callback, but instead will increase the Delay duration. " +
				"OnComplete() is called only once when ALL tween cycles complete. To repeat the callback, put the tween inside Sequence.Create( cycles: numCycles )." );
		}

		if ( !ValidateIsNotCompleting() )
			return;

		if ( cycles == -1 )
		{
			if ( data.TimeScale > 0 )
				data.CyclesTotal = -1;
			else
				TweenLog.Error( $"'{nameof( SetRemainingCycles )}()' doesn't work with negative 'TimeScale' and infinite (-1) 'cycles'." );
			return;
		}

		TweenSettings.SetCyclesTo1If0( ref cycles );
		if ( data.TimeScale > 0 )
		{
			long newCyclesTotal = (long)data.GetCyclesDone() + cycles;
			data.CyclesTotal = newCyclesTotal < TweenSettings.MaxCycles ? (int)newCyclesTotal : TweenSettings.MaxCycles;
		}
		else
		{
			int targetCyclesDone = cycles - 1;
			if ( data.CycleDuration != 0 && targetCyclesDone >= 1 << 24 )
			{
				TweenLog.Error( $"Invalid 'cycles' value: {cycles}. With negative 'TimeScale', an animation can't be moved further than {1 << 24} cycles because of the float precision, tween: {this}" );
				return;
			}

			data.ElapsedTimeTotal = targetCyclesDone * data.CycleDuration + ElapsedTime;
			data.CyclesDone = targetCyclesDone;
			if ( data.CyclesTotal < targetCyclesDone )
				data.CyclesTotal = targetCyclesDone + 1;
		}
	}

	/// <summary>
	/// Adds a completion callback. Consider using <see cref="OnComplete{T}"/> to prevent capturing variables into a closure.
	/// </summary>
	/// <param name="warnIfTargetDestroyed">Set to false to disable the error about the target's destruction. The callback is silently ignored if the target is destroyed.</param>
	public Tween OnComplete( Action onComplete, bool? warnIfTargetDestroyed = null )
	{
		if ( ValidateIsAlive() )
			Data.OnComplete( onComplete, warnIfTargetDestroyed );
		return this;
	}

	/// <summary>
	/// Adds a completion callback without allocations. The callback is not invoked if <paramref name="target"/> is destroyed.
	/// <code>
	/// Tween.LocalPositionX( GameObject, 100, 1 ).OnComplete( GameObject, static go => go.Destroy() );
	/// </code>
	/// </summary>
	public Tween OnComplete<T>( T target, Action<T> onComplete, bool? warnIfTargetDestroyed = null ) where T : class
	{
		if ( ValidateIsAlive() )
			Data.OnComplete( target, onComplete, warnIfTargetDestroyed );
		return this;
	}

	/// <summary>
	/// Adds a callback invoked every time the animated value changes.
	/// </summary>
	public Tween OnUpdate<T>( T target, Action<T, Tween> onUpdate ) where T : class
	{
		if ( ValidateIsAlive() )
			Data.SetOnUpdate( target, onUpdate );
		return this;
	}

	/// <summary>
	/// Adds a callback invoked once when the tween is no longer alive, similar to a 'finally' block:
	/// it was completed, stopped, its target was destroyed, or it was stopped because of an exception.
	/// If the tween completes, OnFinally is invoked after OnComplete.
	/// </summary>
	public Tween OnFinally<T>( T target, Action<T> onFinally ) where T : class
	{
		if ( ValidateIsAlive() )
			Data.SetOnFinally( target, onFinally );
		return this;
	}

	/// <summary>Creates a Sequence and groups this tween with <paramref name="tween"/>.</summary>
	public Sequence Group( Tween tween ) => TryManipulate() ? Sequence.Create( this ).Group( tween ) : default;

	/// <summary>Creates a Sequence and chains <paramref name="tween"/> after this tween.</summary>
	public Sequence Chain( Tween tween ) => TryManipulate() ? Sequence.Create( this ).Chain( tween ) : default;

	/// <summary>Creates a Sequence and groups this tween with <paramref name="sequence"/>.</summary>
	public Sequence Group( Sequence sequence ) => TryManipulate() ? Sequence.Create( this ).Group( sequence ) : default;

	/// <summary>Creates a Sequence and chains <paramref name="sequence"/> after this tween.</summary>
	public Sequence Chain( Sequence sequence ) => TryManipulate() ? Sequence.Create( this ).Chain( sequence ) : default;

	/// <summary>
	/// Stops the animation when <paramref name="cancellationToken"/> is canceled.
	/// By default, cancellation stops the animation as if Stop() was called; pass <see cref="TweenSettings.CancellationMode.Complete"/> to complete it instead.
	/// If the animation is awaited, the await throws <see cref="OperationCanceledException"/>.
	/// </summary>
	public Tween SetCancellationToken( CancellationToken cancellationToken, TweenSettings.CancellationMode? cancellationMode = null )
	{
		if ( !ValidateCanModify( this ) || !cancellationToken.CanBeCanceled )
			return this;

		var data = Data;
		if ( data.HasCancellationToken )
		{
			TweenLog.Error( $"{nameof( SetCancellationToken )}() was already called on this animation. Applying more than one CancellationToken to the same animation is not supported." );
			return this;
		}

		data.CancellationToken = cancellationToken;
		data.CancellationModeComplete = (cancellationMode ?? TweenConfig.DefaultCancellationMode) == TweenSettings.CancellationMode.Complete;
		data.HasCancellationToken = true;
		if ( cancellationToken.IsCancellationRequested )
			CancelTweenOrSequence( this );

		return this;
	}

	/// <summary>
	/// Allows <c>await tween;</c>. The continuation runs after the tween is no longer alive.
	/// </summary>
	public TaskAwaiter GetAwaiter() => ToTask().GetAwaiter();

	/// <summary>
	/// Returns a Task that completes when the tween is no longer alive.
	/// </summary>
	public Task ToTask() => AwaiterSource.Create( this );

	/// <summary>
	/// Controls the animation's time with a <see cref="Clock"/>. One clock can control many animations, but all of them must have the same update type.
	/// </summary>
	public Tween SetClock( Clock clock )
	{
		if ( clock is null )
		{
			TweenLog.Error( "'clock' is null." );
			return this;
		}

		if ( !ValidateCanModify( this ) )
			return this;

		var updateType = Data.UpdateType;
		if ( clock.UpdateType == UpdateType.Default )
		{
			clock.UpdateType = updateType;
		}
		else if ( clock.UpdateType != updateType )
		{
			TweenLog.Error( $"The clock is bound to the '{clock.UpdateType}' update type by the first {nameof( SetClock )}() call, and the binding is permanent. This animation has '{updateType}'. Please use a separate Clock for each update type." );
			return this;
		}

		Data.Clock = clock;
		return this;
	}

	/// <summary>
	/// A shared time source for animations, assigned with <see cref="SetClock"/>.
	/// <code>
	/// var clock = new Tween.Clock();
	/// Tween.LocalPositionX( go, 100, 1 ).SetClock( clock );
	/// Sequence.Create().Chain( Tween.LocalScale( go, 2, 1 ) ).SetClock( clock );
	/// clock.TimeScale = 0.5f; // slows down both animations
	/// </code>
	/// </summary>
	public sealed class Clock
	{
		const float MaxManualDeltaTime = 10;

		/// <summary>
		/// When true, animations advance only by the time passed to <see cref="Tick"/>, and don't update in frames without a Tick.
		/// Changing this value discards the ticks that haven't been applied yet.
		/// </summary>
		public bool IsManual
		{
			get => _isManual;
			set
			{
				if ( _isManual == value )
					return;

				_isManual = value;
				_pendingDeltaTime = 0;
				_hasPendingTick = false;
			}
		}

		/// <summary>Multiplies the delta time of all animations that use this clock, on top of their own TimeScale.</summary>
		public float TimeScale
		{
			get => _timeScale;
			set
			{
				if ( float.IsNaN( value ) || float.IsInfinity( value ) )
					throw new ArgumentException( $"Invalid TimeScale: {value}." );

				ClampTimeScale( ref value );
				_timeScale = value;
			}
		}

		internal UpdateType UpdateType;
		bool _isManual;
		float _timeScale = 1;
		float _pendingDeltaTime;
		bool _hasPendingTick;
		float _passDeltaTime;
		bool _isPassTicked;
		int _updatePass = -1;

		/// <summary>
		/// Advances the animations of a manual clock (<see cref="IsManual"/>). Several calls before the next update accumulate.
		/// </summary>
		public void Tick( float deltaTime )
		{
			if ( float.IsNaN( deltaTime ) )
			{
				TweenLog.Error( $"Invalid deltaTime: {deltaTime}." );
				return;
			}

			if ( MathF.Abs( deltaTime ) > MaxManualDeltaTime )
			{
				TweenLog.Error( $"deltaTime ({deltaTime}) should be in the [-{MaxManualDeltaTime}, {MaxManualDeltaTime}] range." );
				deltaTime = Math.Clamp( deltaTime, -MaxManualDeltaTime, MaxManualDeltaTime );
			}

			_pendingDeltaTime += deltaTime;
			_hasPendingTick = true;
		}

		/// <summary>
		/// Pending ticks are consumed once per update pass, so all animations sharing the clock receive the same delta time.
		/// </summary>
		internal bool TryGetDeltaTime( out float deltaTime )
		{
			int pass = TweenManager.UpdatePassCount;
			if ( _updatePass != pass )
			{
				_updatePass = pass;
				// Clamped silently because ticks legitimately accumulate while all animations of the clock are paused
				_passDeltaTime = Math.Clamp( _pendingDeltaTime, -MaxManualDeltaTime, MaxManualDeltaTime );
				_isPassTicked = _hasPendingTick;
				_pendingDeltaTime = 0;
				_hasPendingTick = false;
			}

			deltaTime = _passDeltaTime;
			return _isPassTicked;
		}
	}

	/// <summary>Instantly resets the animation to the beginning upon completion.</summary>
	internal Tween ResetOnCompletion()
	{
		if ( ValidateIsAlive() )
			Data.ResetOnComplete = true;
		return this;
	}

	#region Validation

	internal bool ValidateIsAlive()
	{
		if ( !IsCreated )
		{
			TweenLog.Error( Messages.DefaultCtor );
			return false;
		}

		if ( !IsAliveInternal )
		{
			TweenLog.Error( Messages.IsDead );
			return false;
		}

		return true;
	}

	internal bool ValidateIsNotCompleting()
	{
		if ( Data.IsCompleting )
		{
			TweenLog.Error( Messages.IsCompleting );
			return false;
		}

		return true;
	}

	internal static bool ValidateCanModify( Tween t )
	{
		if ( !t.TryManipulate() )
			return false;

		if ( t.Data.ModificationIsNotAllowed )
		{
			TweenLog.Error( Messages.ModificationIsNotAllowed );
			return false;
		}

		return true;
	}

	internal bool TryManipulate( bool checkRecursive = true )
	{
		if ( !ValidateIsAlive() )
			return false;

		var data = Data;
		if ( !data.CanManipulate() )
		{
			TweenLog.Error( Messages.CantManipulateNested );
			return false;
		}

		if ( data.IsInSequence )
		{
			// The root's IsUpdating only blocks seeking
			foreach ( var child in new TweenData.AllTweensEnumerator( data ) )
			{
				if ( child == data )
					continue;

				if ( checkRecursive )
				{
					if ( child.IsUpdating )
					{
						TweenLog.Error( Messages.RecursiveCall );
						return false;
					}
				}
				else
				{
					child.IsUpdating = false;
				}
			}
		}
		else if ( checkRecursive )
		{
			if ( data.IsUpdating )
			{
				TweenLog.Error( Messages.RecursiveCall );
				return false;
			}
		}
		else
		{
			data.IsUpdating = false;
		}

		return true;
	}

	#endregion

	internal float DurationWithWaitDelay => Data.CalcDurationWithWaitDependencies();

	public override string ToString() => IsAliveInternal ? Data.GetDescription() : $"DEAD / id {Id}";

	public override int GetHashCode() => Id.GetHashCode();

	public bool Equals( Tween other ) => IsAlive && other.IsAlive && Id == other.Id;

	public override bool Equals( object obj ) => obj is Tween other && Equals( other );
}
