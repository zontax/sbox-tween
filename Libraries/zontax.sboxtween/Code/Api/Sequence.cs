using System.Runtime.CompilerServices;
using System.Threading;

namespace SboxTween;

/// <summary>
/// An ordered group of tweens, callbacks and other sequences. Animations can run in parallel with <see cref="Group(Tween)"/>,
/// sequentially with <see cref="Chain(Tween)"/>, or at a specific time with <see cref="Insert(float, Tween)"/>.
/// <code>
/// Sequence.Create()
///     .Group( Tween.LocalPositionX( go, 100, 1.5f ) )
///     .Group( Tween.LocalScale( go, 2, 0.5f ) ) // runs in parallel with the position tween
///     .Chain( Tween.LocalRotation( go, new Angles( 0, 90, 0 ), 1 ) ) // starts after both previous tweens
///     .ChainCallback( () => Log.Info( "Sequence completed" ) );
/// </code>
/// </summary>
public readonly partial struct Sequence : IEquatable<Sequence>
{
	internal readonly Tween Root;

	internal bool IsCreated => Root.IsCreated;

	long Id => Root.Id;

	/// <summary>A Sequence is 'alive' when it has been created and has not been stopped or completed yet.</summary>
	public bool IsAlive => Root.IsAlive;

	/// <summary>Elapsed time of the current cycle.</summary>
	public float ElapsedTime
	{
		get => Root.ElapsedTime;
		set
		{
			var root = Root;
			root.ElapsedTime = value;
		}
	}

	/// <summary>The total number of cycles. Returns -1 to indicate an infinite number of cycles.</summary>
	public int CyclesTotal => Root.CyclesTotal;

	public int CyclesDone => Root.CyclesDone;

	/// <summary>The duration of one cycle.</summary>
	public float Duration => Root.Duration;

	void SetDuration( float value )
	{
		var data = Root.Data;
		if ( value != 0 )
		{
			// Insert( atTime ) is not clamped to the min duration, so a Sequence could cross a huge number of cycles in one update
			value = MathF.Max( TweenSettings.MinDuration, value );
		}

		data.AnimationDuration = value;
		data.CalculateCycleDuration( 0 );
		data.EndValueOrDiff = ValueContainer.From( value );
		data.CacheDiff();
	}

	/// <summary>Elapsed time of all cycles.</summary>
	public float ElapsedTimeTotal
	{
		get => Root.ElapsedTimeTotal;
		set
		{
			var root = Root;
			root.ElapsedTimeTotal = value;
		}
	}

	/// <summary>The duration of all cycles. If cycles == -1, returns <see cref="float.PositiveInfinity"/>.</summary>
	public float DurationTotal => Root.DurationTotal;

	/// <summary>Normalized progress of the current cycle, in the 0..1 range.</summary>
	public float Progress
	{
		get => Root.Progress;
		set
		{
			var root = Root;
			root.Progress = value;
		}
	}

	/// <summary>Normalized progress of all cycles, in the 0..1 range.</summary>
	public float ProgressTotal
	{
		get => Root.ProgressTotal;
		set
		{
			var root = Root;
			root.ProgressTotal = value;
		}
	}

	public bool IsPaused
	{
		get => Root.IsPaused;
		set
		{
			var root = Root;
			root.IsPaused = value;
		}
	}

	/// <summary>Custom timeScale. To smoothly animate the timeScale, use <see cref="Tween.TweenTimeScale(Sequence, TweenSettings{float})"/>.</summary>
	public float TimeScale
	{
		get => Root.TimeScale;
		set
		{
			var root = Root;
			root.TimeScale = value;
		}
	}

	bool TryManipulate( bool checkRecursive = true ) => Root.TryManipulate( checkRecursive );

	/// <summary>
	/// Creates a new Sequence in the active scene.
	/// </summary>
	/// <param name="sequenceEase">Easing applied to the whole timeline of the Sequence.</param>
	public static Sequence Create( int cycles = 1, SequenceCycleMode cycleMode = SequenceCycleMode.Restart, Ease sequenceEase = Ease.Linear, bool useUnscaledTime = false, UpdateType updateType = UpdateType.Default )
		=> CreateIn( TweenManager.Resolve( TweenManager.DummyTarget ), cycles, cycleMode, sequenceEase, useUnscaledTime, updateType );

	/// <summary>
	/// Creates a new Sequence in the given scene.
	/// </summary>
	public static Sequence Create( Scene scene, int cycles = 1, SequenceCycleMode cycleMode = SequenceCycleMode.Restart, Ease sequenceEase = Ease.Linear, bool useUnscaledTime = false, UpdateType updateType = UpdateType.Default )
		=> CreateIn( TweenManager.Resolve( scene ), cycles, cycleMode, sequenceEase, useUnscaledTime, updateType );

	/// <summary>
	/// Creates a new Sequence with <paramref name="firstTween"/> as its first animation.
	/// </summary>
	public static Sequence Create( Tween firstTween )
	{
		var manager = firstTween.IsAliveInternal ? firstTween.Data.Manager : TweenManager.Resolve( TweenManager.DummyTarget );
		return CreateIn( manager ).Group( firstTween );
	}

	static Sequence CreateIn( TweenManager manager, int cycles = 1, SequenceCycleMode cycleMode = SequenceCycleMode.Restart, Ease sequenceEase = Ease.Linear, bool useUnscaledTime = false, UpdateType updateType = UpdateType.Default )
	{
		if ( manager is null || manager.IsDisposed )
			return default;

		if ( cycleMode == (SequenceCycleMode)CycleMode.Incremental )
		{
			TweenLog.Error( "Sequence doesn't support CycleMode.Incremental. The sequence ease is applied to the Sequence's timeline, and incrementing the timeline doesn't make sense." );
			cycleMode = SequenceCycleMode.Restart;
		}

		if ( sequenceEase == Ease.Custom )
		{
			TweenLog.Error( "Sequence doesn't support Ease.Custom." );
			sequenceEase = Ease.Linear;
		}

		if ( sequenceEase == Ease.Default )
			sequenceEase = Ease.Linear;

		var tween = manager.Fetch( updateType );
		var settings = new TweenSettings( 0, sequenceEase, cycles, (CycleMode)cycleMode, 0, 0, useUnscaledTime, updateType );
		tween.IsSequenceEmpty = true;
		tween.Setup( TweenManager.DummyTarget, ref settings, false, TweenType.MainSequence );
		var root = manager.Add( tween );
		return root.IsAliveInternal ? new Sequence( root ) : default;
	}

	internal Sequence( TweenData data ) => Root = new Tween( data );

	Sequence( Tween rootTween )
	{
		Root = rootTween;
		SetSequence( rootTween );
	}

	#region Building

	/// <summary>
	/// Groups <paramref name="tween"/> with the 'previous' animation in this Sequence (the one used in the preceding Group, Chain or Insert call).
	/// Grouped animations start at the same time and run in parallel.
	/// </summary>
	public Sequence Group( Tween tween )
	{
		if ( TryManipulate() )
			Insert( GetLastInSelfOrRoot().WaitDelay, tween );
		return this;
	}

	/// <summary>Places <paramref name="tween"/> after all previously added animations. Chained animations run sequentially.</summary>
	public Sequence Chain( Tween tween )
	{
		if ( TryManipulate() )
			Insert( Duration, tween );
		return this;
	}

	/// <summary>
	/// Places <paramref name="tween"/> at <paramref name="atTime"/>, overlapping with other animations.
	/// The Sequence duration is increased if the inserted tween doesn't fit.
	/// </summary>
	public Sequence Insert( float atTime, Tween tween )
	{
		if ( !ValidateCanAdd( tween ) )
			return this;

		if ( tween.Data.Sequence is not null )
		{
			TweenLog.Error( $"{Messages.NestTwice} Tween: {tween.Data.GetDescription()}" );
			return this;
		}

		SetSequence( tween );
		InsertInternal( atTime, tween );
		return this;
	}

	/// <summary>Places <paramref name="sequence"/> after all previously added animations.</summary>
	public Sequence Chain( Sequence sequence )
	{
		if ( TryManipulate() )
			Insert( Duration, sequence );
		return this;
	}

	/// <summary>Groups <paramref name="sequence"/> with the 'previous' animation in this Sequence.</summary>
	public Sequence Group( Sequence sequence )
	{
		if ( TryManipulate() )
			Insert( GetLastInSelfOrRoot().WaitDelay, sequence );
		return this;
	}

	/// <summary>Places <paramref name="sequence"/> at <paramref name="atTime"/>, overlapping with other animations.</summary>
	public Sequence Insert( float atTime, Sequence sequence )
	{
		if ( !ValidateCanAdd( sequence.Root ) )
			return this;

		var other = sequence.Root.Data;
		if ( other.TweenType != TweenType.MainSequence )
		{
			TweenLog.Error( Messages.NestTwice );
			return this;
		}

		if ( sequence.Root.Id == Id )
		{
			TweenLog.Error( "It's not allowed to add a Sequence to itself." );
			return this;
		}

		if ( other.HasWait )
		{
			if ( !ValidateWaitIsSupported() )
				return this;

			Root.Data.HasWait = true;
		}

		other.TweenType = TweenType.NestedSequence;
		InsertInternal( atTime, sequence.Root );
		return this;
	}

	void InsertInternal( float atTime, Tween other )
	{
		if ( atTime < 0 )
		{
			TweenLog.Error( $"Inserting at negative time ({atTime}) is not allowed." );
			atTime = 0;
		}
		else if ( float.IsNaN( atTime ) )
		{
			TweenLog.Error( "Inserting at NaN time is not allowed. Inserting at 0 instead." );
			atTime = 0;
		}

		var otherData = other.Data;
		otherData.WaitDelay = atTime;
		// ValidateCanAdd() reports that these settings of a child are ignored, so reset them
		otherData.IsPaused = false;
		otherData.TimeScale = 1;
		SetDuration( MathF.Max( Duration, MathF.Min( other.DurationWithWaitDelay, float.MaxValue ) ) );
		AddLinkedReference( other );
	}

	void AddLinkedReference( Tween tween )
	{
		var root = Root.Data;
		TweenData last;
		if ( root.Next is not null )
		{
			last = GetLast();
			var lastInSelf = GetLastInSelfOrRoot();
			lastInSelf.NextSibling = tween.Data;
			tween.Data.PrevSibling = lastInSelf;
		}
		else
		{
			last = root;
		}

		last.Next = tween.Data;
		tween.Data.Prev = last;
		root.IsSequenceEmpty = false;
	}

	TweenData GetLast()
	{
		TweenData result = null;
		foreach ( var current in new TweenData.AllTweensEnumerator( Root.Data ) )
			result = current;
		return result;
	}

	TweenData GetLastInSelfOrRoot()
	{
		var result = Root.Data;
		foreach ( var current in new TweenData.SelfChildrenEnumerator( Root.Data, true ) )
			result = current;
		return result;
	}

	void SetSequence( Tween handle )
	{
		var data = handle.Data;
		data.Sequence = Root.Data;
		data.IsInSequence = true;
	}

	/// <summary>Schedules <paramref name="callback"/> after all previously added animations.</summary>
	public Sequence ChainCallback( Action callback, bool? warnIfTargetDestroyed = null )
	{
		if ( TryManipulate() )
			InsertCallback( Duration, callback, warnIfTargetDestroyed );
		return this;
	}

	/// <summary>Schedules <paramref name="callback"/> after all previously added animations without allocations.</summary>
	public Sequence ChainCallback<T>( T target, Action<T> callback, bool? warnIfTargetDestroyed = null ) where T : class
	{
		if ( TryManipulate() )
			InsertCallback( Duration, target, callback, warnIfTargetDestroyed );
		return this;
	}

	/// <summary>Schedules <paramref name="callback"/> at <paramref name="atTime"/>.</summary>
	public Sequence InsertCallback( float atTime, Action callback, bool? warnIfTargetDestroyed = null )
	{
		// Validate before creating the callback, or it would be invoked even if Insert() fails
		if ( !ValidateCanModifyDuration() )
			return this;

		var delay = Tween.CreateDelay( TweenManager.DummyTarget, 0, false, TweenType.Callback, false, Root.Data.Manager );
		if ( !delay.IsAliveInternal )
			return this;

		delay.Data.OnComplete( callback, warnIfTargetDestroyed );
		return Insert( atTime, delay );
	}

	/// <summary>Schedules <paramref name="callback"/> at <paramref name="atTime"/> without allocations.</summary>
	public Sequence InsertCallback<T>( float atTime, T target, Action<T> callback, bool? warnIfTargetDestroyed = null ) where T : class
	{
		if ( !ValidateCanModifyDuration() )
			return this;

		var delay = Tween.CreateDelay( target, 0, false, TweenType.Callback, false, Root.Data.Manager );
		if ( !delay.IsAliveInternal )
			return this;

		delay.Data.OnComplete( target, callback, warnIfTargetDestroyed );
		return Insert( atTime, delay );
	}

	/// <summary>Schedules a delay after all previously added animations.</summary>
	public Sequence ChainDelay( float duration )
	{
		if ( !TryManipulate() )
			return this;

		return Chain( Tween.CreateDelay( TweenManager.DummyTarget, duration, false, TweenType.Delay, true, Root.Data.Manager ) );
	}

	#region Waits

	/// <summary>True while the Sequence is suspended by a WaitWhile or WaitUntil.</summary>
	public bool IsWaiting => TryManipulate() && Root.Data.WaitPredicateShouldWait is not null;

	/// <summary>
	/// Suspends the Sequence while <paramref name="predicate"/> returns true. Doesn't change <see cref="Duration"/>.
	/// Only supported on sequences with 1 cycle and <see cref="Ease.Linear"/>. Setting <see cref="ElapsedTime"/> or <see cref="Progress"/> directly bypasses the suspension.
	/// <code>
	/// Sequence.Create()
	///     .Chain( Tween.LocalPositionZ( door, 100, 1 ) )
	///     .ChainWaitWhile( player, static p => p.IsInDoorway, timeout: 5 )
	///     .Chain( Tween.LocalPositionZ( door, 0, 1 ) );
	/// </code>
	/// </summary>
	/// <param name="target">Passed to the predicate to avoid closure allocations. If it's destroyed, the wait ends.</param>
	/// <param name="timeout">The wait ends after this time even if the predicate still wants to wait.</param>
	public Sequence ChainWaitWhile<T>( T target, Predicate<T> predicate, float? timeout = null ) where T : class => InsertWait( Duration, target, predicate, false, timeout );

	/// <summary>Suspends the Sequence until <paramref name="predicate"/> returns true. See <see cref="ChainWaitWhile{T}(T, Predicate{T}, float?)"/>.</summary>
	public Sequence ChainWaitUntil<T>( T target, Predicate<T> predicate, float? timeout = null ) where T : class => InsertWait( Duration, target, predicate, true, timeout );

	/// <inheritdoc cref="ChainWaitWhile{T}(T, Predicate{T}, float?)"/>
	public Sequence InsertWaitWhile<T>( float atTime, T target, Predicate<T> predicate, float? timeout = null ) where T : class => InsertWait( atTime, target, predicate, false, timeout );

	/// <inheritdoc cref="ChainWaitUntil{T}(T, Predicate{T}, float?)"/>
	public Sequence InsertWaitUntil<T>( float atTime, T target, Predicate<T> predicate, float? timeout = null ) where T : class => InsertWait( atTime, target, predicate, true, timeout );

	/// <summary>Suspends the Sequence while <paramref name="predicate"/> returns true. A capturing lambda allocates; prefer the overload with a target.</summary>
	public Sequence ChainWaitWhile( Func<bool> predicate, float? timeout = null ) => InsertWait( Duration, predicate, static p => p(), false, timeout );

	/// <summary>Suspends the Sequence until <paramref name="predicate"/> returns true. A capturing lambda allocates; prefer the overload with a target.</summary>
	public Sequence ChainWaitUntil( Func<bool> predicate, float? timeout = null ) => InsertWait( Duration, predicate, static p => p(), true, timeout );

	Sequence InsertWait<T>( float atTime, T target, Predicate<T> predicate, bool isWaitUntil, float? timeout ) where T : class
	{
		if ( !ValidateCanModifyDuration() || !ValidateWaitIsSupported() )
			return this;

		if ( TweenData.IsDestroyed( target ) )
		{
			TweenLog.Error( "'target' has been destroyed." );
			return this;
		}

		if ( predicate is null )
		{
			TweenLog.Error( "'predicate' is null." );
			return this;
		}

		if ( timeout is float t && (float.IsNaN( t ) || t <= 0) )
		{
			TweenLog.Error( $"'timeout' should be greater than 0, but was {t}." );
			timeout = null;
		}

		var wait = Tween.CreateDelay( (object)target ?? TweenManager.DummyTarget, 0, false, TweenType.Delay, false, Root.Data.Manager );
		if ( !wait.IsAliveInternal )
			return this;

		var data = wait.Data;
		Root.Data.HasWait = true;
		data.HasWait = true;
		data.WaitPredicateTarget = target;
		data.WaitPredicate = predicate;
		data.WaitTimeout = timeout ?? float.PositiveInfinity;
		// Two non-capturing lambdas instead of a flag: the invoker is copied to the main sequence when it suspends
		data.WaitPredicateShouldWait = isWaitUntil ? static d => ShouldWait<T>( d, true ) : static d => ShouldWait<T>( d, false );
		return Insert( atTime, wait );
	}

	static bool ShouldWait<T>( TweenData data, bool isWaitUntil ) where T : class
	{
		var predicate = data.WaitPredicate as Predicate<T>;
		var target = data.WaitPredicateTarget as T;
		if ( TweenData.IsDestroyed( target ) )
			return false;

		try
		{
			return predicate( target ) ^ isWaitUntil;
		}
		catch ( Exception e )
		{
			TweenLog.Exception( e, "Wait predicate raised exception, aborting the wait." );
			return false;
		}
	}

	bool ValidateWaitIsSupported()
	{
		var data = Root.Data;
		if ( data.CyclesTotal != 1 || data.Ease != Ease.Linear )
		{
			TweenLog.Error( $"WaitUntil and WaitWhile are only supported on sequences with 'cycles: 1' and 'sequenceEase: Ease.Linear', but this Sequence has 'cycles: {data.CyclesTotal}' and 'sequenceEase: {data.Ease}'." );
			return false;
		}

		return true;
	}

	#endregion

	bool ValidateCanModifyDuration()
	{
		if ( !Tween.ValidateCanModify( Root ) )
			return false;

		// Changing the duration of a moved sequence would desync it from its children
		if ( ElapsedTimeTotal != 0 )
		{
			TweenLog.Error( Messages.ModificationIsNotAllowed );
			return false;
		}

		return true;
	}

	bool ValidateCanAdd( Tween other )
	{
		if ( !ValidateCanModifyDuration() )
			return false;

		if ( !other.IsAliveInternal )
		{
			TweenLog.Error( Messages.AddDeadTweenToSequence );
			return false;
		}

		var otherData = other.Data;
		if ( otherData.Manager != Root.Data.Manager )
		{
			TweenLog.Error( Messages.DifferentManagers );
			return false;
		}

		if ( otherData.IsUpdating )
		{
			// Adding a tween to a Sequence from its own onValueChange would release the tween when it completes in this update
			TweenLog.Error( Messages.RecursiveCall );
			return false;
		}

		// Reparenting would prevent the completing sequence from releasing its children
		if ( !other.ValidateIsNotCompleting() )
			return false;

		if ( otherData.CyclesTotal == -1 )
		{
			TweenLog.Error( Messages.InfiniteTweenInSequence );
			return false;
		}

		if ( otherData.HasCancellationToken )
		{
			TweenLog.Error( "Adding an animation with a CancellationToken to a Sequence is not allowed because the Sequence controls the lifetime of all its children. Call SetCancellationToken() on the Sequence itself instead." );
			return false;
		}

		if ( otherData.Clock is not null )
		{
			TweenLog.Error( "Adding an animation with a Clock to a Sequence is not allowed because the Sequence controls the time of all its children. Call SetClock() on the Sequence itself instead." );
			return false;
		}

		var thisData = Root.Data;
		if ( otherData.IsPaused && otherData.IsPaused != thisData.IsPaused )
			WarnIgnoredChildrenSetting( nameof( IsPaused ), thisData.IsPaused, otherData.IsPaused );

		if ( otherData.TimeScale != 1 && otherData.TimeScale != thisData.TimeScale )
			WarnIgnoredChildrenSetting( nameof( TimeScale ), thisData.TimeScale, otherData.TimeScale );

		if ( otherData.UseUnscaledTime && otherData.UseUnscaledTime != thisData.UseUnscaledTime )
			WarnIgnoredChildrenSetting( nameof( TweenSettings.UseUnscaledTime ), thisData.UseUnscaledTime, otherData.UseUnscaledTime );

		if ( otherData.UpdateType != TweenConfig.DefaultUpdateType && otherData.UpdateType != thisData.UpdateType )
			WarnIgnoredChildrenSetting( nameof( TweenSettings.UpdateType ), thisData.UpdateType, otherData.UpdateType );

		return true;

		static void WarnIgnoredChildrenSetting( string settingName, object sequenceSetting, object childSetting )
		{
			TweenLog.Error( $"'{settingName}' was ignored after adding a child animation to the Sequence (Sequence has '{sequenceSetting}', but the child had '{childSetting}'). " +
				$"The parent Sequence controls '{settingName}' of all its children. Use the default value of '{settingName}' in the child, or the same value as in the Sequence." );
		}
	}

	#endregion

	#region Control

	/// <summary>Stops all animations in the Sequence, ignoring callbacks.</summary>
	public void Stop()
	{
		if ( !IsAlive || !TryManipulate( false ) )
			return;

		ReleaseTweens();
		Root.Data.ReportOnFinally();
	}

	/// <summary>
	/// Immediately completes the Sequence. If it has infinite cycles, completes only the current cycle.
	/// To choose where the Sequence stops, use <see cref="SetRemainingCycles(bool)"/> before calling Complete().
	/// </summary>
	public void Complete()
	{
		if ( !IsAlive || !TryManipulate( false ) )
			return;

		if ( Root.Data.IsResetting )
			return;

		// A callback can reassign the variable this method was called on
		var rootHandle = Root;
		if ( !rootHandle.Data.IsCompleting )
		{
			// Releases a paused sequence that has already completed and invokes its OnFinally()
			rootHandle.IsPaused = false;
			if ( !rootHandle.IsAliveInternal )
				return;

			if ( TimeScale > 0 && rootHandle.Data.State == State.After )
			{
				ReleaseTweens();
				rootHandle.Data.ReportOnFinally();
				return;
			}

			if ( CyclesTotal == -1 || rootHandle.Data.CycleMode == CycleMode.Restart )
			{
				SetRemainingCycles( 1 );
			}
			else
			{
				int cyclesLeft = CyclesTotal - CyclesDone;
				SetRemainingCycles( cyclesLeft % 2 == 1 ? 1 : 2 );
			}
		}

		// Bypass the IsPaused setter: it's not allowed while the sequence is completing
		var data = rootHandle.Data;
		data.IsPaused = false;
		using ( new TweenManager.UpdateDepthScope( data.Manager ) )
		{
			data.UpdateSequence( TimeScale > 0 ? float.MaxValue : TweenData.NegativeElapsedTime, false, true, false, false );
			data.ReportOnFinally();
		}
	}

	internal void EmergencyStop() => ReleaseTweens( static t => t.WarnOnCompleteIgnored( false ) );

	internal void ReleaseTweens( Action<TweenData> beforeKill = null )
	{
		var enumerator = new TweenData.AllTweensEnumerator( Root.Data );
		enumerator.MoveNext();
		var current = enumerator.Current;
		while ( true )
		{
			var next = enumerator.MoveNext() ? enumerator.Current : null;
			var tween = current;
			beforeKill?.Invoke( tween );
			tween.Kill();
			ReleaseTween( tween );
			if ( next is null )
				break;

			current = next;
		}
	}

	static void ReleaseTween( TweenData tween )
	{
		tween.Next = null;
		tween.Prev = null;
		tween.PrevSibling = null;
		tween.NextSibling = null;
		tween.Sequence = null;
		tween.IsInSequence = false;
		if ( tween.IsSequenceRoot() )
			tween.TweenType = TweenType.None;
	}

	/// <summary>
	/// Stops the Sequence when it reaches the end or returns to the start the next time.
	/// </summary>
	public void SetRemainingCycles( bool stopAtEndValue ) => Root.SetRemainingCycles( stopAtEndValue );

	/// <summary>
	/// Sets the number of remaining cycles. Pass -1 to repeat the Sequence indefinitely.
	/// </summary>
	public void SetRemainingCycles( int cycles ) => Root.SetRemainingCycles( cycles );

	public Sequence OnComplete( Action onComplete, bool? warnIfTargetDestroyed = null )
	{
		Root.OnComplete( onComplete, warnIfTargetDestroyed );
		return this;
	}

	public Sequence OnComplete<T>( T target, Action<T> onComplete, bool? warnIfTargetDestroyed = null ) where T : class
	{
		Root.OnComplete( target, onComplete, warnIfTargetDestroyed );
		return this;
	}

	/// <inheritdoc cref="Tween.OnFinally{T}"/>
	public Sequence OnFinally<T>( T target, Action<T> onFinally ) where T : class
	{
		Root.OnFinally( target, onFinally );
		return this;
	}

	/// <summary>Adds a callback invoked every time the Sequence updates, after all its children are updated.</summary>
	public Sequence OnUpdate<T>( T target, Action<T, Sequence> onUpdate ) where T : class
	{
		if ( Root.ValidateIsAlive() )
			Root.Data.SetOnUpdate( target, onUpdate );
		return this;
	}

	/// <inheritdoc cref="Tween.SetCancellationToken"/>
	public Sequence SetCancellationToken( CancellationToken cancellationToken, TweenSettings.CancellationMode? cancellationMode = null )
	{
		Root.SetCancellationToken( cancellationToken, cancellationMode );
		return this;
	}

	/// <inheritdoc cref="Tween.SetClock"/>
	public Sequence SetClock( Tween.Clock clock )
	{
		Root.SetClock( clock );
		return this;
	}

	/// <inheritdoc cref="Tween.GetAwaiter"/>
	public TaskAwaiter GetAwaiter() => Root.GetAwaiter();

	/// <inheritdoc cref="Tween.ToTask"/>
	public Task ToTask() => Root.ToTask();

	internal Sequence ResetOnCompletion()
	{
		Root.ResetOnCompletion();
		return this;
	}

	#endregion

	public override string ToString() => Root.ToString();

	public override int GetHashCode() => Root.GetHashCode();

	public bool Equals( Sequence other ) => Root.Equals( other.Root );

	public override bool Equals( object obj ) => obj is Sequence other && Equals( other );

	public enum SequenceCycleMode : byte
	{
		/// <summary>Restarts the Sequence from the beginning.</summary>
		Restart = CycleMode.Restart,
		/// <summary>Preserves easing of the children when the Sequence moves backward.</summary>
		YoyoChildren = 4,
		/// <summary>Animates forth and back. Use <see cref="YoyoChildren"/> to preserve easing of the children on the backward cycle.</summary>
		Yoyo = CycleMode.Yoyo,
		/// <summary>Rewinds the Sequence as if time was reversed.</summary>
		Rewind = CycleMode.Rewind
	}
}
