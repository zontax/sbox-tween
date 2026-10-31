namespace SboxTween.UnitTests;

[TestClass]
public class TweenBasicsTests : TweenTestBase
{
	[TestMethod]
	public void CustomFloatReachesEndValueAndDies()
	{
		float value = -1;
		int completed = 0;
		var tween = Tween.Custom( 0, 10, 1, v => value = v, Ease.Linear ).OnComplete( () => completed++ );
		Assert.IsTrue( tween.IsAlive );

		// The start value is applied at the end of the frame the tween was created in
		EndFrame();
		AreEqual( 0, value );

		Tick( 0.5f );
		AreEqual( 5, value );
		Assert.IsTrue( tween.IsAlive );
		AreEqual( 0.5f, tween.Progress );
		AreEqual( 0.5f, tween.ElapsedTime );

		Tick( 0.5f );
		AreEqual( 10, value );
		Assert.IsFalse( tween.IsAlive );
		Assert.AreEqual( 1, completed );

		Tick( 0.5f );
		Assert.AreEqual( 1, completed );
		Assert.AreEqual( 0, Manager.TweensCount );
	}

	[TestMethod]
	public void EaseIsApplied()
	{
		float value = 0;
		Tween.Custom( 0, 1, 1, v => value = v, Ease.InQuad );
		Tick( 0.5f );
		AreEqual( 0.25f, value );
	}

	[TestMethod]
	public void DefaultEaseIsUsed()
	{
		float value = 0;
		Tween.Custom( 0, 1, 1, v => value = v );
		Tick( 0.5f );
		AreEqual( TweenEasing.Evaluate( 0.5f, TweenConfig.DefaultEase ), value );
	}

	[TestMethod]
	public void StartAndEndDelays()
	{
		float value = -1;
		bool completed = false;
		var tween = Tween.Custom( 0, 1, 1, v => value = v, Ease.Linear, startDelay: 0.5f, endDelay: 0.5f ).OnComplete( () => completed = true );
		AreEqual( 2, tween.Duration );

		EndFrame();
		// Tweens with a start delay don't apply the start value immediately
		AreEqual( -1, value );

		// The value is not touched during the start delay
		Tick( 0.25f );
		AreEqual( -1, value );

		Tick( 0.75f );
		AreEqual( 0.5f, value );

		Tick( 0.5f );
		AreEqual( 1, value );
		Assert.IsTrue( tween.IsAlive, "End delay should keep the tween alive" );
		Assert.IsFalse( completed );

		Tick( 0.5f );
		Assert.IsFalse( tween.IsAlive );
		Assert.IsTrue( completed );
	}

	[TestMethod]
	public void StopKeepsCurrentValueAndSkipsOnComplete()
	{
		float value = 0;
		bool completed = false;
		bool finalized = false;
		var tween = Tween.Custom( 0, 10, 1, v => value = v, Ease.Linear )
			.OnComplete( () => completed = true )
			.OnFinally( this, _ => finalized = true );

		Tick( 0.3f );
		tween.Stop();
		Assert.IsFalse( tween.IsAlive );
		AreEqual( 3, value );
		Assert.IsFalse( completed );
		Assert.IsTrue( finalized );

		Tick( 1 );
		AreEqual( 3, value );
	}

	[TestMethod]
	public void CompleteJumpsToEnd()
	{
		float value = 0;
		int completed = 0;
		int finalized = 0;
		var tween = Tween.Custom( 0, 10, 1, v => value = v )
			.OnComplete( () => completed++ )
			.OnFinally( this, _ => finalized++ );

		Tick( 0.1f );
		tween.Complete();
		AreEqual( 10, value );
		Assert.AreEqual( 1, completed );
		Assert.AreEqual( 1, finalized );
		Assert.IsFalse( tween.IsAlive );

		// Completing a dead tween is a no-op without errors
		tween.Complete();
		Tick( 1 );
		Assert.AreEqual( 1, completed );
		Assert.AreEqual( 1, finalized );
	}

	[TestMethod]
	public void CompleteBeforeFirstUpdate()
	{
		float value = 0;
		var tween = Tween.Custom( 0, 10, 1, v => value = v );
		tween.Complete();
		AreEqual( 10, value );
		Assert.IsFalse( tween.IsAlive );
	}

	[TestMethod]
	public void OnFinallyIsInvokedAfterOnComplete()
	{
		var order = new List<string>();
		Tween.Delay( 0.1f )
			.OnComplete( () => order.Add( "complete" ) )
			.OnFinally( order, o => o.Add( "finally" ) );

		TickFor( 0.2f );
		CollectionAssert.AreEqual( new[] { "complete", "finally" }, order );
	}

	[TestMethod]
	public void PauseFreezesTween()
	{
		float value = 0;
		var tween = Tween.Custom( 0, 10, 1, v => value = v, Ease.Linear );
		Tick( 0.2f );
		tween.IsPaused = true;
		Assert.IsTrue( tween.IsPaused );
		Tick( 0.5f );
		AreEqual( 2, value );

		tween.IsPaused = false;
		Tick( 0.3f );
		AreEqual( 5, value );
	}

	[TestMethod]
	public void TimeScaleSpeedsUpTween()
	{
		float value = 0;
		var tween = Tween.Custom( 0, 10, 1, v => value = v, Ease.Linear );
		tween.TimeScale = 2;
		Tick( 0.25f );
		AreEqual( 5, value );
	}

	[TestMethod]
	public void InvalidTimeScaleIsClamped()
	{
		var tween = Tween.Delay( 1 );
		ExpectError( "TimeScale" );
		tween.TimeScale = 1000;
		AreEqual( Tween.MaxTimeScale, tween.TimeScale );
	}

	[TestMethod]
	public void UnscaledTimeUsesUnscaledDelta()
	{
		float scaled = 0;
		float unscaled = 0;
		Tween.Custom( 0, 1, 1, v => scaled = v, Ease.Linear );
		Tween.Custom( 0, 1, 1, v => unscaled = v, Ease.Linear, useUnscaledTime: true );

		Tick( 0.1f, 0.5f );
		AreEqual( 0.1f, scaled );
		AreEqual( 0.5f, unscaled );
	}

	[TestMethod]
	public void ElapsedTimeAndProgressSetters()
	{
		float value = 0;
		var tween = Tween.Custom( 0, 10, 2, v => value = v, Ease.Linear );
		Tick( 0 );

		tween.ElapsedTime = 1;
		AreEqual( 5, value );
		AreEqual( 0.5f, tween.Progress );

		tween.Progress = 0.25f;
		AreEqual( 2.5f, value );

		tween.ProgressTotal = 1;
		AreEqual( 10, value );
		Assert.IsFalse( tween.IsAlive );
	}

	[TestMethod]
	public void ZeroDurationCompletesOnFirstUpdate()
	{
		float value = 0;
		bool completed = false;
		ExpectWarning( "duration" );
		Tween.Custom( 0, 10, 0, v => value = v ).OnComplete( () => completed = true );
		Tick( 0.01f );
		AreEqual( 10, value );
		Assert.IsTrue( completed );
	}

	[TestMethod]
	public void DelayInvokesCallback()
	{
		int calls = 0;
		var delay = Tween.Delay( 0.5f, () => calls++ );
		TickFor( 0.4f );
		Assert.AreEqual( 0, calls );
		TickFor( 0.2f );
		Assert.AreEqual( 1, calls );
		Assert.IsFalse( delay.IsAlive );
	}

	[TestMethod]
	public void DelayWithTargetSkipsCallbackWhenTargetDestroyed()
	{
		var go = CreateObject();
		int calls = 0;
		Tween.Delay( go, 0.1f, _ => calls++ );
		go.DestroyImmediate();

		ExpectError( "OnComplete callback was ignored" );
		TickFor( 0.2f );
		Assert.AreEqual( 0, calls );
	}

	[TestMethod]
	public void DelayWithTargetCanDisableDestroyedWarning()
	{
		var go = CreateObject();
		Tween.Delay( go, 0.1f, _ => { }, warnIfTargetDestroyed: false );
		go.DestroyImmediate();
		TickFor( 0.2f );
	}

	[TestMethod]
	public void OnUpdateIsInvokedOnValueChange()
	{
		var factors = new List<float>();
		Tween.Custom( 0, 1, 1, _ => { }, Ease.Linear ).OnUpdate( factors, static ( list, t ) => list.Add( t.InterpolationFactor ) );
		EndFrame();
		Tick( 0.5f );
		Tick( 0.5f );
		CollectionAssert.AreEqual( new float[] { 0, 0.5f, 1 }, factors );
	}

	[TestMethod]
	public void OnUpdateWithMismatchedDelegateIsCleared()
	{
		var factors = new List<float>();
		var tween = Tween.Custom( 0, 1, 1, _ => { } ).OnUpdate( factors, static ( list, t ) => list.Add( t.InterpolationFactor ) );
		// Simulates a callback from an assembly that wasn't hotloaded together with the library
		tween.Data.OnUpdateCallback = new Action<List<float>, int>( ( _, _ ) => { } );
		ExpectError( "no longer matches" );
		Tick( 0.5f );
		Tick( 0.5f );
		Assert.IsFalse( tween.Data.HasOnUpdate );
	}

	[TestMethod]
	public void SecondOnCompleteIsRejected()
	{
		var tween = Tween.Delay( 1 ).OnComplete( () => { } );
		ExpectError( "already has an OnComplete" );
		tween.OnComplete( () => { } );
	}

	[TestMethod]
	public void DeadTweenLogsOnPropertyAccess()
	{
		var tween = Tween.Delay( 0.01f );
		TickFor( 0.1f );
		Assert.IsFalse( tween.IsAlive );

		ExpectError( "not alive" );
		Assert.AreEqual( 0, tween.ElapsedTime );
	}

	[TestMethod]
	public void DefaultTweenLogsOnPropertyAccess()
	{
		Tween tween = default;
		Assert.IsFalse( tween.IsAlive );
		ExpectError( "not created" );
		Assert.AreEqual( 0, tween.Duration );
	}

	[TestMethod]
	public void ExceptionInOnValueChangeStopsTween()
	{
		ExpectError( "exception in 'onValueChange'" );
		var tween = Tween.Custom( 0, 1, 1, _ => throw new InvalidOperationException( "boom" ) );
		Tick( 0.1f );
		Assert.IsFalse( tween.IsAlive );
	}

	[TestMethod]
	public void ExceptionInOnCompleteIsLogged()
	{
		ExpectError( "onComplete callback raised exception" );
		Tween.Delay( 0.01f, () => throw new InvalidOperationException( "boom" ) );
		TickFor( 0.1f );
	}

	[TestMethod]
	public void CustomWithTargetIsAllocationFriendly()
	{
		var box = new FloatBox();
		Tween.Custom( box, 0, 4, 1, static ( b, v ) => b.Value = v, Ease.Linear );
		Tick( 0.5f );
		AreEqual( 2, box.Value );
	}

	[TestMethod]
	public void CustomStartFromCurrentUsesGetter()
	{
		var box = new FloatBox { Value = 5 };
		Tween.Custom( box, new TweenSettings<float>( 15, 1, Ease.Linear ), static ( b, v ) => b.Value = v, static b => b.Value );
		Tick( 0.5f );
		AreEqual( 10, box.Value );
	}

	[TestMethod]
	public void CustomStartFromCurrentWithoutGetterWarns()
	{
		float value = -1;
		ExpectWarning( "getCurrentValue" );
		Tween.Custom( new TweenSettings<float>( 10, 1, Ease.Linear ), v => value = v );
		Tick( 0.5f );
		AreEqual( 5, value );
	}

	[TestMethod]
	public void CustomValueTypes()
	{
		Vector2 v2 = default;
		Vector3 v3 = default;
		Vector4 v4 = default;
		Color color = default;
		Rotation rotation = default;
		Angles angles = default;

		Tween.Custom( Vector2.Zero, new Vector2( 2, 4 ), 1, v => v2 = v, Ease.Linear );
		Tween.Custom( Vector3.Zero, new Vector3( 2, 4, 6 ), 1, v => v3 = v, Ease.Linear );
		Tween.Custom( Vector4.Zero, new Vector4( 2, 4, 6, 8 ), 1, v => v4 = v, Ease.Linear );
		Tween.Custom( Color.Black, Color.White, 1, v => color = v, Ease.Linear );
		Tween.Custom( Rotation.Identity, Rotation.FromYaw( 90 ), 1, v => rotation = v, Ease.Linear );
		Tween.Custom( new Angles( 0, 0, 0 ), new Angles( 0, 360, 0 ), 1, v => angles = v, Ease.Linear );

		Tick( 0.5f );
		Assert.AreEqual( 1, v2.x, Epsilon );
		Assert.AreEqual( 2, v2.y, Epsilon );
		AreEqual( new Vector3( 1, 2, 3 ), v3 );
		Assert.AreEqual( 4, v4.w, Epsilon );
		AreEqual( new Color( 0.5f, 0.5f, 0.5f, 1 ), color );
		AreEqual( Rotation.FromYaw( 45 ), rotation );
		Assert.AreEqual( 180, angles.yaw, Epsilon );
	}

	[TestMethod]
	public void SpeedBasedCustomTween()
	{
		float value = 0;
		var tween = Tween.Custom( new TweenSettings<float>( 0, 10, 5 ), v => value = v, useDurationAsSpeed: true );
		AreEqual( 2, tween.Duration );
	}

	[TestMethod]
	public void LatestTweenOverwritesPrevious()
	{
		float value = 0;
		Tween.Custom( 0, 10, 1, v => value = v, Ease.Linear );
		Tween.Custom( 0, 100, 1, v => value = v, Ease.Linear );
		Tick( 0.5f );
		AreEqual( 50, value );
	}

	[TestMethod]
	public void PooledTweenInvalidatesOldHandles()
	{
		var first = Tween.Delay( 0.01f );
		TickFor( 0.1f );
		var second = Tween.Delay( 1 );

		Assert.IsFalse( first.IsAlive );
		Assert.IsTrue( second.IsAlive );
		Assert.AreNotEqual( first.Id, second.Id );
		Assert.AreSame( first.Data, second.Data, "The pool should reuse released tweens" );
		Assert.IsFalse( first.Equals( second ) );
	}

	[TestMethod]
	public void StopAllWithoutTargetStopsEverything()
	{
		var a = Tween.Delay( 1 );
		var b = Tween.Delay( 1 );
		Tick( 0.1f );
		Assert.AreEqual( 2, Manager.StopAll() );
		Assert.IsFalse( a.IsAlive );
		Assert.IsFalse( b.IsAlive );
		Assert.AreEqual( 0, Manager.TweensCount );
	}

	[TestMethod]
	public void CompleteAllCompletesEverything()
	{
		int completed = 0;
		Tween.Delay( 1, () => completed++ );
		Tween.Delay( 2, () => completed++ );
		Assert.AreEqual( 2, Manager.CompleteAll() );
		Assert.AreEqual( 2, completed );
	}

	[TestMethod]
	public void CompleteAllFromCallbackIsDeferred()
	{
		int completed = 0;
		Tween.Delay( 5, () => completed++ );
		Tween.Delay( 0.01f, () => Manager.CompleteAll() );
		TickFor( 0.1f );
		Assert.AreEqual( 1, completed );
	}

	[TestMethod]
	public void SetPausedAllPausesEverything()
	{
		float value = 0;
		var tween = Tween.Custom( 0, 1, 1, v => value = v, Ease.Linear );
		Tick( 0.1f );
		Manager.SetPausedAll( true );
		Tick( 0.5f );
		AreEqual( 0.1f, value );
		Assert.IsTrue( tween.IsPaused );
		Manager.SetPausedAll( false );
		Tick( 0.1f );
		AreEqual( 0.2f, value );
	}

	[TestMethod]
	public void DisposedManagerDoesNotCreateTweens()
	{
		Manager.Dispose();
		var tween = Tween.Delay( 1 );
		Assert.IsFalse( tween.IsAlive );
	}

	[TestMethod]
	public void WithDirectionSwapsValues()
	{
		var settings = new TweenSettings<float>( 1, 2, 1 );
		var backward = settings.WithDirection( false );
		Assert.AreEqual( 2, backward.StartValue );
		Assert.AreEqual( 1, backward.EndValue );
		Assert.IsTrue( backward.StartFromCurrent );

		var forward = settings.WithDirection( true, 1.5f );
		Assert.AreEqual( 1.5f, forward.StartValue );
		Assert.AreEqual( 2, forward.EndValue );
		Assert.IsFalse( forward.StartFromCurrent );
	}

	[TestMethod]
	public void MaxAliveTweensIsTracked()
	{
		Tween.Delay( 1 );
		Tween.Delay( 1 );
		Tween.Delay( 1 );
		Assert.AreEqual( 3, Manager.MaxAliveTweens );
	}

	internal sealed class FloatBox
	{
		public float Value;
	}
}
