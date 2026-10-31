using System.Threading;

namespace SboxTween;

/// <summary>
/// Pooled state of a single animation (tween, delay, callback or sequence root).
/// A <see cref="Tween"/> handle stays valid only while <see cref="Id"/> matches.
/// </summary>
internal sealed partial class TweenData
{
	internal const int IniCyclesDone = -1;
	internal const float NegativeElapsedTime = -1000;

	internal TweenManager Manager;
	internal long Id = -1;

	internal bool IsAlive;
	internal bool IsPaused;
	internal bool IsInSequence;
	internal bool UseUnscaledTime;
	internal bool StartFromCurrent;
	internal bool IsUpdating;
	internal bool StoppedEmergently;
	internal bool ResetOnComplete;
	internal bool IsResetting;
	internal bool IsCompleting;
	internal bool WarnEndValueEqualsCurrent;
	internal bool WarnIgnoredOnCompleteIfTargetDestroyed;
	internal bool IsSequenceInverted;
	internal bool IsCustomEaseSameStartEndValues;
	internal bool ModificationIsNotAllowed;
	internal bool HasCancellationToken;
	internal bool CancellationModeComplete;
	internal State State;

	internal float ElapsedTimeTotal;
	internal float TimeScale;
	internal int CyclesDone;
	internal int CyclesTotal;
	internal float WaitDelay;
	internal float CycleDuration;
	internal float StartDelay;
	internal float AnimationDuration;
	internal Ease Ease;
	internal CycleMode CycleMode;
	internal TweenType TweenType;
	internal UpdateType UpdateType;
	internal float EasedInterpolationFactor;

	internal ValueContainer StartValue;
	internal ValueContainer EndValueOrDiff;
	internal object Target;

	internal TweenData Sequence;
	internal TweenData Prev;
	internal TweenData Next;
	internal TweenData PrevSibling;
	internal TweenData NextSibling;
	internal bool IsSequenceEmpty;

	internal Action<TweenData> OnCompleteInvoker;
	internal object OnCompleteCallback;
	internal object OnCompleteTarget;

	internal Action<TweenData> OnValueChangeInvoker;
	internal object CustomOnValueChange;
	internal Action<TweenData> CustomGetterInvoker;
	internal object CustomGetter;

	internal Action<TweenData> OnUpdateInvoker;
	internal object OnUpdateCallback;
	internal object OnUpdateTarget;

	internal Action<TweenData> OnFinallyInvoker;
	internal object OnFinallyCallback;
	internal object OnFinallyTarget;

	internal Curve CustomEase;
	internal bool HasCustomEase;
	internal ParametricEase Parametric;
	internal float ParametricStrength;
	internal float ParametricPeriod;

	internal CancellationToken CancellationToken;

	/// <summary>Id of the animation targeted by TweenTimeScale and awaiters, to detect when the target is recycled.</summary>
	internal long TargetTweenId;

	/// <summary>Set on a wait animation and propagated to all its parent sequences.</summary>
	internal bool HasWait;
	internal object WaitPredicateTarget;
	internal object WaitPredicate;
	/// <summary>On a wait animation: its predicate invoker. On the main sequence root: the active wait, or null if not waiting.</summary>
	internal Predicate<TweenData> WaitPredicateShouldWait;
	/// <summary>On a wait animation: its timeout. On the main sequence root: the remaining timeout of the active wait.</summary>
	internal float WaitTimeout;

	internal SboxTween.Tween.Clock Clock;

	internal bool IsAdditive;
	/// <summary>The eased factor of the previous additive update.</summary>
	internal float AdditivePrevT;
	/// <summary>The part of the previous deltas the target didn't absorb because of rounding.</summary>
	internal ValueContainer AdditiveErr;

	internal bool HasOnUpdate => OnUpdateInvoker is not null;
	internal bool HasOnComplete => OnCompleteInvoker is not null;

	internal PropType PropType => GetPropType( TweenType );

	internal void Setup( object target, ref TweenSettings settings, bool startFromCurrent, TweenType tweenType )
	{
		settings.SetValidValues();
		TweenType = tweenType;

		var ease = settings.Ease;
		if ( ease == Ease.Default )
		{
			ease = TweenConfig.DefaultEase;
		}
		else if ( ease == Ease.Custom && settings.Parametric == ParametricEase.None )
		{
			var curve = settings.CustomEase;
			if ( TweenSettings.HasFrames( curve ) )
			{
				IsCustomEaseSameStartEndValues = curve.Evaluate( 0 ).AlmostEqual( curve.Evaluate( 1 ), 0.0001f );
			}
			else
			{
				TweenLog.Error( $"Ease type is Ease.Custom, but {nameof( TweenSettings.CustomEase )} is not configured correctly." );
				ease = TweenConfig.DefaultEase;
			}
		}

		IsAlive = true;
		WarnIgnoredOnCompleteIfTargetDestroyed = true;
		State = State.Before;
		EasedInterpolationFactor = float.MinValue;
		CyclesDone = IniCyclesDone;
		TimeScale = 1;

		AnimationDuration = settings.Duration;
		Ease = ease;
		CyclesTotal = settings.Cycles;
		CycleMode = settings.CycleMode;
		StartDelay = settings.StartDelay;
		UseUnscaledTime = settings.UseUnscaledTime;
		StartFromCurrent = startFromCurrent;

		if ( settings.Parametric != ParametricEase.None )
		{
			HasCustomEase = false;
			Parametric = settings.Parametric;
			ParametricStrength = settings.ParametricStrength;
			ParametricPeriod = settings.ParametricPeriod;
		}
		else if ( ease == Ease.Custom )
		{
			CustomEase = settings.CustomEase;
			HasCustomEase = true;
		}

		CalculateCycleDuration( settings.EndDelay );
		WarnEndValueEqualsCurrent = TweenConfig.WarnEndValueEqualsCurrent;

		if ( !startFromCurrent )
			CacheDiff();

		Target = target;
	}

	internal void Reset()
	{
		if ( IsShake( TweenType ) && !StartFromCurrent )
			ResetShakeStartValue();

		Target = null;
		Sequence = null;
		Prev = null;
		Next = null;
		PrevSibling = null;
		NextSibling = null;
		OnCompleteInvoker = null;
		OnCompleteCallback = null;
		OnCompleteTarget = null;
		OnValueChangeInvoker = null;
		CustomOnValueChange = null;
		CustomGetterInvoker = null;
		CustomGetter = null;
		OnUpdateInvoker = null;
		OnUpdateCallback = null;
		OnUpdateTarget = null;
		OnFinallyInvoker = null;
		OnFinallyCallback = null;
		OnFinallyTarget = null;
		CustomEase = default;
		CancellationToken = CancellationToken.None;
		Shake = default;

		IsAlive = false;
		IsPaused = false;
		IsInSequence = false;
		UseUnscaledTime = false;
		StartFromCurrent = false;
		IsUpdating = false;
		StoppedEmergently = false;
		ResetOnComplete = false;
		IsResetting = false;
		IsCompleting = false;
		WarnEndValueEqualsCurrent = false;
		WarnIgnoredOnCompleteIfTargetDestroyed = false;
		IsSequenceInverted = false;
		IsCustomEaseSameStartEndValues = false;
		ModificationIsNotAllowed = false;
		HasCancellationToken = false;
		CancellationModeComplete = false;
		HasCustomEase = false;
		IsSequenceEmpty = false;
		State = State.None;

		ElapsedTimeTotal = 0;
		TimeScale = 0;
		CyclesDone = 0;
		CyclesTotal = 0;
		WaitDelay = 0;
		CycleDuration = 0;
		StartDelay = 0;
		AnimationDuration = 0;
		Ease = Ease.Default;
		CycleMode = CycleMode.Restart;
		TweenType = TweenType.None;
		UpdateType = UpdateType.Default;
		EasedInterpolationFactor = 0;
		StartValue = default;
		EndValueOrDiff = default;
		Parametric = ParametricEase.None;
		ParametricStrength = 0;
		ParametricPeriod = 0;
		TargetTweenId = 0;
		HasWait = false;
		ClearWait();
		WaitTimeout = 0;
		Clock = null;
		IsAdditive = false;
		AdditivePrevT = 0;
		AdditiveErr = default;

		Id = -1;
	}

	#region Timeline

	float CalcTFromElapsedTimeTotal( float elapsedTimeTotal, out State newState )
	{
		// Key timeline points: 0 | startDelay | duration | 1 | endDelay | onComplete
		if ( elapsedTimeTotal == float.MaxValue )
		{
			CyclesDone = CyclesTotal;
			newState = State.After;
			return 1;
		}

		elapsedTimeTotal -= WaitDelay;
		if ( elapsedTimeTotal < 0 )
		{
			CyclesDone = IniCyclesDone;
			newState = State.Before;
			return 0;
		}

		if ( CycleDuration == 0 )
		{
			if ( CyclesTotal == -1 )
			{
				// Add max one cycle per frame
				if ( TimeScale > 0 )
				{
					CyclesDone = CyclesDone == IniCyclesDone ? 1 : CyclesDone + 1;
				}
				else if ( TimeScale != 0 )
				{
					if ( CyclesDone != IniCyclesDone )
						CyclesDone--;

					if ( CyclesDone == IniCyclesDone )
					{
						newState = State.Before;
						return 0;
					}
				}

				newState = State.Running;
				return 1;
			}

			if ( elapsedTimeTotal == 0 )
			{
				CyclesDone = IniCyclesDone;
				newState = State.Before;
				return 0;
			}

			CyclesDone = CyclesTotal;
			newState = State.After;
			return 1;
		}

		float cyclesFloat = elapsedTimeTotal / CycleDuration;
		// Casting a huge float to int overflows
		CyclesDone = CyclesTotal != -1 && cyclesFloat >= CyclesTotal ? CyclesTotal : (int)cyclesFloat;
		if ( CyclesTotal != -1 && CyclesDone == CyclesTotal )
		{
			newState = State.After;
			return 1;
		}

		float elapsedTimeInCycle = elapsedTimeTotal - CycleDuration * CyclesDone - StartDelay;
		if ( elapsedTimeInCycle < 0 )
		{
			newState = State.Before;
			return 0;
		}

		if ( AnimationDuration == 0 )
		{
			newState = State.After;
			return 1;
		}

		float result = elapsedTimeInCycle / AnimationDuration;
		if ( result > 1 )
		{
			newState = State.After;
			return 1;
		}

		newState = State.Running;
		return result;
	}

	internal bool CanManipulate() => !IsInSequence || TweenType == TweenType.MainSequence;
	internal bool IsMainSequenceRoot() => TweenType == TweenType.MainSequence;
	internal bool IsSequenceRoot() => TweenType == TweenType.MainSequence || TweenType == TweenType.NestedSequence;

	internal bool TrySetPause( bool isPaused )
	{
		if ( IsPaused == isPaused )
			return false;

		IsPaused = isPaused;
		return true;
	}

	internal bool IsDone( int cyclesDiff )
	{
		if ( TimeScale > 0 )
			return cyclesDiff > 0 && CyclesDone == CyclesTotal;

		return cyclesDiff < 0 && CyclesDone == IniCyclesDone;
	}

	float UpdateData( float newElapsedTimeTotal, out bool isValueChanged )
	{
		ElapsedTimeTotal = newElapsedTimeTotal;
		float t = CalcTFromElapsedTimeTotal( ElapsedTimeTotal, out var newState );
		isValueChanged = newState == State.Running || State != newState;
		if ( isValueChanged )
			State = newState;

		return t;
	}

	internal int ClampCyclesDone( int cyclesDone )
	{
		if ( cyclesDone == IniCyclesDone )
			return 0;

		if ( cyclesDone == CyclesTotal )
			return CyclesTotal - 1;

		return cyclesDone;
	}

	internal bool IsForwardCycle( int cycle ) => ClampCyclesDone( cycle ) % 2 == 0;

	internal int GetCyclesDone() => CyclesDone == IniCyclesDone ? 0 : CyclesDone;

	internal float CalcDurationWithWaitDependencies() => WaitDelay + CycleDuration * CyclesTotal;

	internal void CalculateCycleDuration( float endDelay )
	{
		// The sum of finite values can overflow to infinity
		CycleDuration = MathF.Min( StartDelay + AnimationDuration + endDelay, float.MaxValue );
	}

	float EaseInput( float t, bool invertEase )
	{
		if ( invertEase )
			t = 1 - t;

		return CycleMode == CycleMode.Rewind && !IsForwardCycle( CyclesDone ) ? 1 - t : t;
	}

	float EaseOutput( float eased, bool invertEase )
	{
		switch ( CycleMode )
		{
			case CycleMode.Restart:
			case CycleMode.Rewind:
				break;
			case CycleMode.Incremental:
				eased += ClampCyclesDone( CyclesDone );
				break;
			case CycleMode.Yoyo:
			case (CycleMode)SboxTween.Sequence.SequenceCycleMode.YoyoChildren:
				if ( !IsForwardCycle( CyclesDone ) )
					eased = 1 - eased;
				break;
		}

		if ( invertEase )
			return IsCustomEaseSameStartEndValues ? eased : 1 - eased;

		return eased;
	}

	internal float CalcEasedT( float t, bool invertEase ) => EaseOutput( EvaluateEase( EaseInput( t, invertEase ) ), invertEase );

	float EvaluateEase( float t )
	{
		if ( Ease != Ease.Custom )
			return StandardEasing.Evaluate( t, Ease );

		if ( HasCustomEase )
			return CustomEase.Evaluate( t );

		if ( Parametric == ParametricEase.BounceExact )
		{
			float fullAmplitude = PropType == PropType.Rotation
				? ValueContainer.RotationAngle( StartValue, EndValueOrDiff )
				: EndValueOrDiff.Magnitude;
			return StandardEasing.Bounce( t, StandardEasing.BounceExactStrength( ParametricStrength, fullAmplitude ) );
		}

		return StandardEasing.EvaluateParametric( t, Parametric, ParametricStrength, ParametricPeriod, CycleDuration );
	}

	#endregion

	#region Update

	internal bool UpdateAndCheckIfRunning( float dt )
	{
		if ( !IsAlive )
		{
			// Don't release a tween until sequence.ReleaseTweens()
			return IsInSequence;
		}

		// Cancellation should stop paused animations too
		if ( HasCancellationToken && CancellationToken.IsCancellationRequested )
		{
			SboxTween.Tween.CancelTweenOrSequence( new Tween( this ) );
			return IsAlive;
		}

		if ( !IsPaused )
		{
			bool isTicked = true;
			float timeScale = TimeScale;
			if ( Clock is not null )
			{
				if ( Clock.IsManual )
					isTicked = Clock.TryGetDeltaTime( out dt );

				// Clamped to keep the max cycles per update within the limit
				timeScale = Math.Clamp( timeScale * Clock.TimeScale, -SboxTween.Tween.MaxTimeScale, SboxTween.Tween.MaxTimeScale );

				// The TimeScale setter rejects negative values on wait sequences, but the clock doesn't know its animations
				if ( HasWait && dt * timeScale < 0 )
					dt = 0;
			}

			if ( isTicked )
				return SetElapsedTimeTotal( ElapsedTimeTotal + dt * timeScale, true );
		}

		if ( IsTargetDestroyed() )
		{
			EmergencyStop( true );
			return false;
		}

		return IsAlive;
	}

	internal bool SetElapsedTimeTotal( float newElapsedTimeTotal, bool earlyExitSequenceIfPaused )
	{
		if ( IsInSequence )
		{
			if ( IsMainSequenceRoot() )
			{
				// Waits are ignored when the time is set from Progress or ElapsedTime
				if ( HasWait && earlyExitSequenceIfPaused )
				{
					if ( WaitPredicateShouldWait is not null )
					{
						// The sequence is suspended: only the timeout runs
						WaitTimeout -= newElapsedTimeTotal - ElapsedTimeTotal;
						if ( !WaitPredicateShouldWait( this ) || WaitTimeout <= 0 )
							ClearWait();
					}
					else
					{
						// Callbacks of children can't seek the sequence while it iterates them
						IsUpdating = true;
						UpdateSequenceUpToFirstWait( newElapsedTimeTotal );
					}
				}
				else
				{
					IsUpdating = true;
					UpdateSequence( newElapsedTimeTotal, false, earlyExitSequenceIfPaused, true, false );
				}

				IsUpdating = false;
			}
		}
		else
		{
			UpdateAndSetElapsedTimeTotal( newElapsedTimeTotal, out int cyclesDiff, false );
			if ( !StoppedEmergently && IsAlive && IsDone( cyclesDiff ) )
			{
				if ( !IsPaused )
					Kill();

				ReportOnComplete();
			}
		}

		return IsAlive;
	}

	internal void UpdateSequence( float elapsedTimeTotal, bool isRestart, bool earlyExitSequenceIfPaused, bool allowSkipChildrenUpdate, bool invertEase )
	{
		bool isRootBackwardCycle = (ClampCyclesDone( CyclesDone ) % 2 != 0) ^ IsSequenceInverted;
		float prevElapsedTime = FloatVal( StartValue, EasedInterpolationFactor, EndValueOrDiff );
		bool invertRootEase = (CyclesTotal == 1 && isRootBackwardCycle && CycleMode == CycleMode.Rewind) ^ invertEase;
		if ( !UpdateAndSetElapsedTimeTotal( elapsedTimeTotal, out int cyclesDiff, invertRootEase ) && allowSkipChildrenUpdate )
			return;

		if ( CycleMode == (CycleMode)SboxTween.Sequence.SequenceCycleMode.YoyoChildren && isRootBackwardCycle )
			invertEase = !invertEase;

		bool isRestartToBeginning = isRestart && cyclesDiff < 0;
		if ( cyclesDiff != 0 && !isRestartToBeginning )
		{
			if ( isRestart )
			{
				if ( CyclesDone == CyclesTotal )
					cyclesDiff = 1;
				else
					isRestart = false; // A restarting parent moves this sequence to its first cycle, not to the end
			}

			int cyclesDiffAbs = Math.Abs( cyclesDiff );
			if ( IsMainSequenceRoot() && IsDone( cyclesDiff ) )
			{
				// Callbacks invoked by this update can't prevent the sequence from completing
				IsCompleting = true;
			}

			CyclesDone -= cyclesDiff;
			int cyclesDelta = cyclesDiff > 0 ? 1 : -1;
			const int maxSequenceCyclesPerUpdate = 1000;
			if ( cyclesDiffAbs > maxSequenceCyclesPerUpdate )
			{
				LogError( $"Sequence crossed {cyclesDiffAbs} cycles in one update, but max {maxSequenceCyclesPerUpdate} cycles are processed per update. The callbacks of the skipped cycles will not be invoked. Sequence: {GetDescription()}" );
				CyclesDone += (cyclesDiffAbs - maxSequenceCyclesPerUpdate) * cyclesDelta;
				cyclesDiffAbs = maxSequenceCyclesPerUpdate;
			}

			float interpolationFactor = cyclesDelta > 0 ? 1 : 0;
			for ( int i = 0; i < cyclesDiffAbs; i++ )
			{
				if ( CyclesDone == CyclesTotal || CyclesDone == IniCyclesDone )
				{
					// Do nothing when moving backward from the last cycle or forward from the -1 cycle
					CyclesDone += cyclesDelta;
					continue;
				}

				float easedT = CalcEasedT( interpolationFactor, false );
				bool isForwardCycle = (easedT > 0.5f) ^ IsSequenceInverted;

				// Complete the previous cycles by forcing all children to 0 or 1
				float forceChildrenToPosElapsedTime = isForwardCycle ? float.MaxValue : NegativeElapsedTime;
				foreach ( var child in new SelfChildrenEnumerator( Sequence, isForwardCycle ) )
				{
					child.UpdateSequenceChild( forceChildrenToPosElapsedTime, isRestart, invertEase );
					if ( IsEarlyExitAfterChildUpdate( earlyExitSequenceIfPaused ) )
						return;
				}

				CyclesDone += cyclesDelta;
				if ( cyclesDelta > 0 && CyclesDone == CyclesTotal )
				{
					// A callback has shortened the sequence with SetRemainingCycles()
					break;
				}

				if ( CycleMode == CycleMode.Restart && CyclesDone != CyclesTotal && CyclesDone != IniCyclesDone )
				{
					float restartChildrenElapsedTime = !isForwardCycle ? float.MaxValue : NegativeElapsedTime;
					prevElapsedTime = restartChildrenElapsedTime;
					foreach ( var child in new SelfChildrenEnumerator( Sequence, !isForwardCycle ) )
					{
						child.UpdateSequenceChild( restartChildrenElapsedTime, true, invertEase );
						if ( IsEarlyExitAfterChildUpdate( earlyExitSequenceIfPaused ) )
							return;
					}
				}
			}

			if ( IsDone( cyclesDiff ) )
			{
				if ( IsMainSequenceRoot() )
				{
					// The sequence is already at its end, so Complete() called from OnUpdate() can't finish the completion
					IsResetting = true;
					ReportSequenceOnUpdate();
					if ( IsAlive && ResetOnComplete )
						ResetSequence( Sequence );

					IsResetting = false;
					if ( !IsAlive )
						return;

					if ( !IsPaused )
					{
						new Sequence( Sequence ).ReleaseTweens();
					}
					else
					{
						// The sequence was paused before it reached the end, so it stays alive until it's unpaused
						IsCompleting = false;
					}

					// Children are no longer iterated, so the sequence's own OnComplete can seek it
					IsUpdating = false;
				}
				else
				{
					ReportSequenceOnUpdate();
					if ( !IsAlive )
						return;
				}

				ReportOnComplete( false );
				return;
			}
		}

		float encompassingElapsedTime = Math.Clamp( FloatVal( StartValue, EasedInterpolationFactor, EndValueOrDiff ), 0, CycleDuration );
		bool isForward = encompassingElapsedTime > prevElapsedTime;
		foreach ( var child in new SelfChildrenEnumerator( Sequence, isForward ) )
		{
			child.UpdateSequenceChild( encompassingElapsedTime, isRestart, invertEase );
			if ( IsEarlyExitAfterChildUpdate( earlyExitSequenceIfPaused ) )
				return;
		}

		ReportSequenceOnUpdate();
	}

	bool IsEarlyExitAfterChildUpdate( bool earlyExitSequenceIfPaused )
	{
		if ( !IsAlive )
			return true;

		return earlyExitSequenceIfPaused && IsPaused;
	}

	/// <summary>
	/// Updates children forward up to the first wait that suspends the sequence. Returns true if a wait suspended it.
	/// Sequences with waits always have one cycle, linear ease and can't move backward.
	/// </summary>
	bool UpdateSequenceUpToFirstWait( float newElapsedTimeTotal )
	{
		float encompassingElapsedTime = newElapsedTimeTotal - WaitDelay;

		// Deviation from PrimeTween: exactly at the end, zero-duration children (callbacks, waits) are still 'Before',
		// so the sequence would complete without them. Move the children to the end like UpdateSequence() does on completion
		if ( encompassingElapsedTime >= CycleDuration )
			encompassingElapsedTime = float.MaxValue;

		foreach ( var child in new SelfChildrenEnumerator( Sequence, true ) )
		{
			if ( child.IsSequenceRoot() )
			{
				if ( child.HasWait )
				{
					if ( child.UpdateSequenceUpToFirstWait( encompassingElapsedTime ) )
						return true;
				}
				else
				{
					// A sibling's wait doesn't restrict this sequence's cycles, easing, or direction
					child.UpdateSequenceChild( encompassingElapsedTime, false, false );
				}
			}
			else if ( child.ElapsedTimeTotal < encompassingElapsedTime // Update tweens only forward
				&& child.UpdateAndSetElapsedTimeTotal( encompassingElapsedTime, out int cyclesDiff, false ) )
			{
				if ( !IsAlive || IsPaused )
					return false;

				if ( !child.StoppedEmergently && child.IsAlive )
				{
					bool shouldWait = child.HasWait && child.WaitPredicateShouldWait( child );
					if ( !IsAlive )
					{
						// The predicate can stop or complete the sequence and unlink all its children
						return true;
					}

					if ( shouldWait )
					{
						// The main sequence holds the active wait
						var mainSequence = child.FindMainSequence();
						mainSequence.WaitPredicateTarget = child.WaitPredicateTarget;
						mainSequence.WaitPredicate = child.WaitPredicate;
						mainSequence.WaitPredicateShouldWait = child.WaitPredicateShouldWait;
						mainSequence.WaitTimeout = child.WaitTimeout;
						return true;
					}

					if ( cyclesDiff != 0 && child.IsDone( cyclesDiff ) )
						child.ReportOnComplete();
				}
			}

			if ( !IsAlive )
			{
				// A callback has stopped or completed the sequence, so its children can't be enumerated anymore
				return true;
			}
		}

		// Update the sequence root if no child suspended it
		if ( UpdateAndSetElapsedTimeTotal( newElapsedTimeTotal, out int seqCyclesDiff, false ) )
		{
			bool isDone = IsDone( seqCyclesDiff );
			if ( isDone && IsMainSequenceRoot() )
			{
				IsCompleting = true;
				IsResetting = true;
			}

			ReportSequenceOnUpdate();
			if ( isDone )
			{
				if ( IsMainSequenceRoot() )
				{
					if ( IsAlive && ResetOnComplete )
						ResetSequence( Sequence );

					IsResetting = false;
					if ( !IsAlive )
						return false;

					if ( !IsPaused )
						new Sequence( Sequence ).ReleaseTweens();
					else
						IsCompleting = false;

					IsUpdating = false;
				}
				else if ( !IsAlive )
				{
					return false;
				}

				ReportOnComplete( false );
			}
		}

		return false;
	}

	internal void ClearWait()
	{
		WaitPredicateTarget = null;
		WaitPredicate = null;
		WaitPredicateShouldWait = null;
	}

	internal static void ResetSequence( TweenData seq, bool toEnd = false )
	{
		bool isResetToEnd = IsResetToEnd( seq, toEnd );
		foreach ( var child in new SelfChildrenEnumerator( seq, isResetToEnd ) )
		{
			// Setting cyclesDone to cyclesTotal prevents callbacks from firing because isDone requires 'cyclesDiff > 0'
			child.CyclesDone = isResetToEnd ? child.CyclesTotal : IniCyclesDone;

			// Guarantees propagation of the state change
			child.State = State.None;
			float elapsedTimeTotal = isResetToEnd ? float.MaxValue : NegativeElapsedTime;
			child.UpdateAndSetElapsedTimeTotal( elapsedTimeTotal, out _, false );
			if ( !seq.IsAlive )
				return;

			if ( child.IsSequenceRoot() )
			{
				ResetSequence( child, isResetToEnd );
				if ( !seq.IsAlive )
					return;
			}
		}

		seq.ReportSequenceOnUpdate();
	}

	// 'toEnd' is relative to the playback direction. A sequence with an even number of Yoyo or Rewind cycles ends where it starts
	static bool IsResetToEnd( TweenData seq, bool toEnd ) => seq.IsSequenceInverted ^ (toEnd && (seq.CycleMode == CycleMode.Restart || seq.CyclesTotal % 2 != 0));

	void UpdateSequenceChild( float encompassingElapsedTime, bool isRestart, bool invertEase )
	{
		if ( IsSequenceRoot() )
		{
			UpdateSequence( encompassingElapsedTime, isRestart, true, true, invertEase );
			return;
		}

		UpdateAndSetElapsedTimeTotal( encompassingElapsedTime, out int cyclesDiff, invertEase );
		if ( !StoppedEmergently && IsAlive && cyclesDiff != 0 && IsDone( cyclesDiff ) )
			ReportOnComplete();
	}

	internal bool UpdateAndSetElapsedTimeTotal( float newElapsedTimeTotal, out int cyclesDiff, bool invertEase )
	{
		int oldCyclesDone = CyclesDone;
		float t = UpdateData( newElapsedTimeTotal, out bool isValueChanged );
		cyclesDiff = CyclesDone - oldCyclesDone;
		if ( !isValueChanged )
			return false;

		EasedInterpolationFactor = CalcEasedT( t, invertEase );
		if ( TryCacheDiff() )
			ReportOnValueChange();

		return true;
	}

	internal void ForceComplete()
	{
		// Protects from a recursive call
		Kill();

		int cyclesTotal;
		if ( TimeScale > 0 )
		{
			cyclesTotal = CyclesTotal;
			if ( cyclesTotal == -1 )
			{
				// Same as SetRemainingCycles( 1 )
				cyclesTotal = GetCyclesDone() + 1;
				CyclesTotal = cyclesTotal;
			}
		}
		else
		{
			cyclesTotal = IniCyclesDone;
		}

		CyclesDone = cyclesTotal;
		EasedInterpolationFactor = CalcEasedT( 1, false );

		TryCacheDiff();
		ReportOnValueChange();

		if ( StoppedEmergently )
			return;

		ReportOnComplete();
	}

	internal void EmergencyStop( bool isTargetDestroyed )
	{
		if ( Sequence is not null )
		{
			var mainSequence = FindMainSequence();
			new Sequence( mainSequence ).EmergencyStop();
		}
		else if ( IsAlive )
		{
			// EmergencyStop() can be called after ForceComplete() and a caught exception in Tween.Custom()
			Kill();
		}

		StoppedEmergently = true;
		WarnOnCompleteIgnored( isTargetDestroyed );
	}

	internal void Kill()
	{
		IsAlive = false;
	}

	internal TweenData FindMainSequence()
	{
		var mainSequence = Sequence;
		while ( true )
		{
			var previous = mainSequence.Prev;
			if ( previous is null )
				break;

			var parent = previous.Sequence;
			if ( parent is null )
				break;

			mainSequence = parent;
		}

		return mainSequence;
	}

	#endregion

	#region Values

	// Returns false if Tween.Custom's getter has stopped or completed the tween, so the value shouldn't be reported
	bool TryCacheDiff()
	{
		if ( !StartFromCurrent )
			return true;

		if ( !TryTakeStartValueFromOtherShake() && !IsTargetDestroyed() )
		{
			bool wasAlive = IsAlive;
			IsUpdating = true;
			try
			{
				if ( CustomGetterInvoker is not null )
					CustomGetterInvoker( this );
				else
					StartValue = GetAnimatedValue();
			}
			catch ( Exception e )
			{
				TweenLog.Exception( e, $"Getting the current value raised exception, so the tween starts from the 'startValue' instead of the current value, tween: {GetDescription()}" );
			}
			finally
			{
				IsUpdating = false;
			}

			// The custom getter can call Stop() or Complete()
			if ( wasAlive && !IsAlive )
				return false;
		}

		StartFromCurrent = false;
		if ( WarnEndValueEqualsCurrent && !IsShake( TweenType ) && EndValueEqualsCurrent() )
			TweenLog.Warning( $"Tween's 'endValue' equals the current animated value: {FormatValue( StartValue )}, tween: {GetDescription()}. {Messages.CanBeDisabled( nameof( TweenConfig.WarnEndValueEqualsCurrent ) )}" );

		CacheDiff();
		return true;
	}

	bool EndValueEqualsCurrent()
	{
		// q and -q represent the same rotation, so compare by angle instead of by raw components
		return PropType == PropType.Rotation
			? ValueContainer.RotationAngle( StartValue, EndValueOrDiff ) == 0
			: StartValue == EndValueOrDiff;
	}

	internal void CacheDiff()
	{
		var propType = PropType;
		if ( IsShake( TweenType ) )
		{
			// Shakes store the strength in EndValueOrDiff
			if ( propType == PropType.Rotation )
				StartValue.NormalizeRotation();
			return;
		}

		if ( propType == PropType.Rotation )
		{
			StartValue.NormalizeRotation();
			EndValueOrDiff.NormalizeRotation();
			return;
		}

		EndValueOrDiff -= StartValue;
	}

	internal static float FloatVal( ValueContainer startValue, float t, ValueContainer delta ) => startValue.X + delta.X * t;

	string FormatValue( ValueContainer value )
	{
		return PropType switch
		{
			PropType.Float => value.X.ToString(),
			PropType.Vector2 => value.Vector2.ToString(),
			PropType.Vector3 => value.Vector3.ToString(),
			PropType.Color => value.Color.ToString(),
			PropType.Rotation => value.Rotation.Angles().ToString(),
			_ => value.ToString()
		};
	}

	internal bool ReportOnValueChange()
	{
		try
		{
			// The setter can fail even if the target is valid, and the user-provided setter of a custom tween can throw
			if ( !SetAnimatedValue() )
				return false;

			// A sequence root updates before its children, so its OnUpdate() is invoked by UpdateSequence() after the children are updated
			if ( HasOnUpdate && !StoppedEmergently && IsAlive && !IsSequenceRoot() )
			{
				IsUpdating = true;
				OnUpdateInvoker?.Invoke( this );
				IsUpdating = false;
			}
		}
		catch ( Exception e )
		{
			IsUpdating = false;
			TweenLog.Exception( e, $"Tween was stopped because of exception in 'onValueChange', tween: {GetDescription()}" );
			EmergencyStop( false );
			return false;
		}

		return true;
	}

	#endregion

	#region Callbacks

	void ReportOnComplete( bool canResetOnComplete = true )
	{
		if ( CyclesDone != CyclesTotal )
			return;

		if ( canResetOnComplete && ResetOnComplete && !IsInSequence )
			UpdateAndSetElapsedTimeTotal( NegativeElapsedTime, out _, false );

		OnCompleteInvoker?.Invoke( this );
	}

	internal void OnComplete( Action onComplete, bool? warnIfTargetDestroyed )
	{
		if ( onComplete is null || !ValidateOnCompleteAssignment() )
			return;

		WarnIgnoredOnCompleteIfTargetDestroyed = warnIfTargetDestroyed ?? TweenConfig.WarnIfTargetDestroyed;
		OnCompleteCallback = onComplete;
		OnCompleteInvoker = static tween =>
		{
			var callback = tween.OnCompleteCallback as Action;
			try
			{
				callback();
			}
			catch ( Exception e )
			{
				tween.HandleOnCompleteException( e );
			}
		};
	}

	internal void OnComplete<T>( T target, Action<T> onComplete, bool? warnIfTargetDestroyed ) where T : class
	{
		if ( target is null || IsDestroyed( target ) )
		{
			TweenLog.Error( $"'target' is null or has been destroyed. {Messages.OnCompleteCallbackIgnored}" );
			return;
		}

		if ( onComplete is null || !ValidateOnCompleteAssignment() )
			return;

		WarnIgnoredOnCompleteIfTargetDestroyed = warnIfTargetDestroyed ?? TweenConfig.WarnIfTargetDestroyed;
		OnCompleteTarget = target;
		OnCompleteCallback = onComplete;
		OnCompleteInvoker = static tween =>
		{
			var callback = tween.OnCompleteCallback as Action<T>;
			var callbackTarget = tween.OnCompleteTarget as T;
			if ( IsDestroyed( callbackTarget ) )
			{
				tween.WarnOnCompleteIgnored( true );
				return;
			}

			try
			{
				callback( callbackTarget );
			}
			catch ( Exception e )
			{
				tween.HandleOnCompleteException( e );
			}
		};
	}

	void HandleOnCompleteException( Exception e )
	{
		// Design decision: if a tween inside a Sequence throws in OnComplete(), the Sequence should continue
		TweenLog.Exception( e, $"Tween's onComplete callback raised exception, tween: {GetDescription()}" );
	}

	bool ValidateOnCompleteAssignment()
	{
		if ( OnCompleteTarget is not null || OnCompleteCallback is not null || OnCompleteInvoker is not null )
		{
			LogError( Messages.OnCompleteDuplicate );
			return false;
		}

		return true;
	}

	internal void WarnOnCompleteIgnored( bool isTargetDestroyed )
	{
		if ( !HasOnComplete || !WarnIgnoredOnCompleteIfTargetDestroyed )
			return;

		OnCompleteInvoker = null;
		var msg = $"{Messages.OnCompleteCallbackIgnored} Tween: {GetDescription()}.";
		if ( isTargetDestroyed )
			msg += "\nIf you use tween.OnComplete(), Tween.Delay(), or sequence.ChainDelay() only for cosmetic purposes, you can turn off this error by passing 'warnIfTargetDestroyed: false' to the method.";

		LogError( msg );
	}

	internal void SetOnUpdate<T>( T target, Delegate onUpdate ) where T : class
	{
		if ( OnUpdateInvoker is not null )
		{
			LogError( Messages.OnUpdateDuplicate );
			return;
		}

		if ( onUpdate is null )
		{
			LogError( "'onUpdate' is null." );
			return;
		}

		OnUpdateTarget = target;
		OnUpdateCallback = onUpdate;
		OnUpdateInvoker = static tween => tween.InvokeOnUpdate<T>();
	}

	void InvokeOnUpdate<T>() where T : class
	{
		var callback = OnUpdateCallback;
		var target = OnUpdateTarget as T;
		if ( IsDestroyed( target ) )
		{
			LogError( $"OnUpdate() will not be called again because OnUpdate()'s target has been destroyed, tween: {GetDescription()}" );
			ClearOnUpdate();
			return;
		}

		try
		{
			if ( callback is Action<T, Tween> tweenCallback )
				tweenCallback( target, new Tween( this ) );
			else
				(callback as Action<T, Sequence>)( target, new Sequence( this ) );
		}
		catch ( Exception e )
		{
			TweenLog.Exception( e, $"OnUpdate() will not be called again because it threw an exception, tween: {GetDescription()}" );
			ClearOnUpdate();
		}
	}

	void ClearOnUpdate()
	{
		OnUpdateTarget = null;
		OnUpdateCallback = null;
		OnUpdateInvoker = null;
	}

	internal void ReportSequenceOnUpdate()
	{
		if ( HasOnUpdate && IsAlive )
			OnUpdateInvoker( this );
	}

	internal void SetOnFinally<T>( T target, Action<T> onFinally ) where T : class
	{
		if ( OnFinallyInvoker is not null )
		{
			LogError( Messages.OnFinallyDuplicate );
			return;
		}

		if ( onFinally is null )
		{
			LogError( "'onFinally' is null." );
			return;
		}

		OnFinallyTarget = target;
		OnFinallyCallback = onFinally;
		OnFinallyInvoker = static tween =>
		{
			var callback = tween.OnFinallyCallback as Action<T>;
			var target = tween.OnFinallyTarget as T;
			// The callback can trigger the manager update that recycles this animation, so clear the fields before the invocation
			tween.OnFinallyTarget = null;
			tween.OnFinallyCallback = null;
			long id = tween.Id;
			try
			{
				callback( target );
			}
			catch ( Exception e )
			{
				string description = tween.Id == id ? tween.GetDescription() : "<released>";
				TweenLog.Exception( e, $"Animation's OnFinally() callback raised exception, animation: {description}" );
			}
		};
	}

	internal void ReportOnFinally()
	{
		var onFinally = OnFinallyInvoker;
		if ( onFinally is null )
			return;

		// Cleared before the invocation so that a later stop path doesn't invoke it twice
		OnFinallyInvoker = null;
		onFinally( this );
	}

	#endregion

	#region Utils

	internal bool IsTargetDestroyed() => IsDestroyed( Target );

	internal static bool IsDestroyed( object obj ) => obj is IValid valid && !valid.IsValid;

	internal static bool IsShake( TweenType tweenType ) => tweenType is TweenType.ShakeLocalPosition or TweenType.ShakeLocalRotation or TweenType.ShakeLocalScale or TweenType.ShakeCustom;

	internal void LogError( string message ) => TweenLog.Error( message );

	internal string GetDescription()
	{
		var sb = new System.Text.StringBuilder();
		if ( !IsAlive )
			sb.Append( " - " );

		switch ( TweenType )
		{
			case TweenType.Delay:
				sb.Append( "Delay" );
				break;
			case TweenType.Callback:
				sb.Append( "Callback" );
				break;
			case TweenType.MainSequence:
			case TweenType.NestedSequence:
				sb.Append( "Sequence" );
				break;
			default:
				sb.Append( TweenType );
				break;
		}

		const string separator = "  /  ";
		if ( Target is not null && Target != TweenManager.DummyTarget )
		{
			sb.Append( separator );
			sb.Append( Target switch
			{
				GameObject go when go.IsValid() => go.Name,
				Component c when c.IsValid() => $"{c.GameObject.Name} ({c.GetType().Name})",
				_ => Target.GetType().Name
			} );
		}

		if ( TweenType != TweenType.Callback )
			sb.Append( separator ).Append( $"{AnimationDuration:0.0#}s" );

		return sb.ToString();
	}

	public override string ToString() => GetDescription();

	#endregion

	/// <summary>
	/// Enumerates direct children of a sequence (siblings), forward or backward.
	/// </summary>
	internal struct SelfChildrenEnumerator
	{
		TweenData _current;
		readonly bool _isForward;
		bool _isStarted;

		internal SelfChildrenEnumerator( TweenData sequenceRoot, bool isForward )
		{
			_isForward = isForward;
			_isStarted = false;
			if ( sequenceRoot.IsSequenceEmpty )
			{
				_current = null;
				return;
			}

			_current = sequenceRoot.Next;
			if ( !isForward )
			{
				while ( _current.NextSibling is not null )
					_current = _current.NextSibling;
			}
		}

		public readonly SelfChildrenEnumerator GetEnumerator() => this;

		public readonly TweenData Current => _current;

		public bool MoveNext()
		{
			if ( _current is null )
				return false;

			if ( !_isStarted )
			{
				_isStarted = true;
				return true;
			}

			_current = _isForward ? _current.NextSibling : _current.PrevSibling;
			return _current is not null;
		}
	}

	/// <summary>
	/// Enumerates the main sequence root and all its nested animations, in the order they were added.
	/// </summary>
	internal struct AllTweensEnumerator
	{
		readonly TweenData _root;
		TweenData _current;
		bool _isStarted;

		internal AllTweensEnumerator( TweenData mainSequenceRoot )
		{
			_root = mainSequenceRoot;
			_current = null;
			_isStarted = false;
		}

		public readonly AllTweensEnumerator GetEnumerator() => this;

		public readonly TweenData Current => _current;

		public bool MoveNext()
		{
			if ( !_isStarted )
			{
				_current = _root;
				_isStarted = true;
				return true;
			}

			_current = _current.Next;
			return _current is not null;
		}
	}
}
