namespace SboxTween.UnitTests;

[TestClass]
public class SequenceTests : TweenTestBase
{
	[TestMethod]
	public void ChainRunsSequentially()
	{
		float a = 0;
		float b = 0;
		var sequence = Sequence.Create()
			.Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ) )
			.Chain( Tween.Custom( 0, 1, 1, v => b = v, Ease.Linear ) );

		AreEqual( 2, sequence.Duration );
		Tick( 0.5f );
		AreEqual( 0.5f, a );
		AreEqual( 0, b );

		Tick( 1 );
		AreEqual( 1, a );
		AreEqual( 0.5f, b );

		Tick( 0.5f );
		AreEqual( 1, b );
		Assert.IsFalse( sequence.IsAlive );
		Assert.AreEqual( 0, Manager.TweensCount );
	}

	[TestMethod]
	public void GroupRunsInParallel()
	{
		float a = 0;
		float b = 0;
		var sequence = Sequence.Create()
			.Group( Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ) )
			.Group( Tween.Custom( 0, 1, 2, v => b = v, Ease.Linear ) );

		AreEqual( 2, sequence.Duration );
		Tick( 0.5f );
		AreEqual( 0.5f, a );
		AreEqual( 0.25f, b );
	}

	[TestMethod]
	public void GroupAfterChainStartsWithPreviousAnimation()
	{
		float a = 0;
		float b = 0;
		float c = 0;
		Sequence.Create()
			.Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ) )
			.Chain( Tween.Custom( 0, 1, 1, v => b = v, Ease.Linear ) )
			.Group( Tween.Custom( 0, 1, 1, v => c = v, Ease.Linear ) );

		Tick( 1.5f );
		AreEqual( 1, a );
		AreEqual( 0.5f, b );
		AreEqual( 0.5f, c );
	}

	[TestMethod]
	public void InsertAtTime()
	{
		float a = 0;
		float b = 0;
		var sequence = Sequence.Create()
			.Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ) )
			.Insert( 2, Tween.Custom( 0, 1, 1, v => b = v, Ease.Linear ) );

		AreEqual( 3, sequence.Duration );
		Tick( 2.5f );
		AreEqual( 1, a );
		AreEqual( 0.5f, b );
	}

	[TestMethod]
	public void CallbacksAreInvokedInOrder()
	{
		var order = new List<int>();
		Sequence.Create()
			.ChainCallback( () => order.Add( 1 ) )
			.ChainDelay( 0.5f )
			.ChainCallback( order, static o => o.Add( 2 ) )
			.InsertCallback( 0.25f, () => order.Add( 3 ) )
			.OnComplete( () => order.Add( 4 ) );

		TickFor( 1 );
		CollectionAssert.AreEqual( new[] { 1, 3, 2, 4 }, order );
	}

	[TestMethod]
	public void TweenChainCreatesSequence()
	{
		float a = 0;
		float b = 0;
		var sequence = Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear )
			.Chain( Tween.Custom( 0, 1, 1, v => b = v, Ease.Linear ) );
		Assert.IsTrue( sequence.IsAlive );
		AreEqual( 2, sequence.Duration );
	}

	[TestMethod]
	public void SequenceRestartCycles()
	{
		float a = 0;
		int completed = 0;
		var sequence = Sequence.Create( cycles: 2 )
			.Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ) )
			.ChainCallback( () => completed++ );

		Tick( 1.5f );
		AreEqual( 0.5f, a );
		Assert.AreEqual( 1, completed );
		Tick( 0.5f );
		Assert.AreEqual( 2, completed );
		Assert.IsFalse( sequence.IsAlive );
	}

	[TestMethod]
	public void SequenceYoyoCycles()
	{
		float a = 0;
		Sequence.Create( cycles: 2, cycleMode: Sequence.SequenceCycleMode.Yoyo )
			.Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.InQuad ) );

		Tick( 1.5f );
		// Yoyo plays the timeline backward, so the child's ease is reversed as well
		AreEqual( 0.25f, a );
		Tick( 0.5f );
		AreEqual( 0, a );
	}

	[TestMethod]
	public void SequenceYoyoChildrenKeepsEase()
	{
		float a = 0;
		Sequence.Create( cycles: 2, cycleMode: Sequence.SequenceCycleMode.YoyoChildren )
			.Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.InQuad ) );

		// Like in PrimeTween, the cycle direction is read before the update, so tick in small steps
		TickFor( 1.5f, 0.125f );
		AreEqual( 0.75f, a );
	}

	[TestMethod]
	public void SequenceEaseAppliesToTimeline()
	{
		float a = 0;
		Sequence.Create( sequenceEase: Ease.InQuad )
			.Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ) );

		Tick( 0.5f );
		AreEqual( 0.25f, a );
	}

	[TestMethod]
	public void NestedSequences()
	{
		float a = 0;
		float b = 0;
		var inner = Sequence.Create()
			.Chain( Tween.Custom( 0, 1, 1, v => b = v, Ease.Linear ) );

		var outer = Sequence.Create()
			.Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ) )
			.Chain( inner );

		AreEqual( 2, outer.Duration );
		Tick( 1.5f );
		AreEqual( 1, a );
		AreEqual( 0.5f, b );
		Tick( 1 );
		Assert.IsFalse( outer.IsAlive );
		Assert.IsFalse( inner.IsAlive );
	}

	[TestMethod]
	public void StopSkipsCallbacks()
	{
		int calls = 0;
		var sequence = Sequence.Create()
			.ChainDelay( 1 )
			.ChainCallback( () => calls++ )
			.OnComplete( () => calls++ );

		Tick( 0.5f );
		sequence.Stop();
		Assert.IsFalse( sequence.IsAlive );
		TickFor( 2 );
		Assert.AreEqual( 0, calls );
		Assert.AreEqual( 0, Manager.TweensCount );
	}

	[TestMethod]
	public void CompleteInvokesAllCallbacks()
	{
		float a = 0;
		int calls = 0;
		var sequence = Sequence.Create()
			.Chain( Tween.Custom( 0, 1, 1, v => a = v ) )
			.ChainCallback( () => calls++ )
			.OnComplete( () => calls++ );

		sequence.Complete();
		Assert.IsFalse( sequence.IsAlive );
		AreEqual( 1, a );
		Assert.AreEqual( 2, calls );
	}

	[TestMethod]
	public void CompleteInfiniteSequence()
	{
		float a = 0;
		var sequence = Sequence.Create( cycles: -1, cycleMode: Sequence.SequenceCycleMode.Yoyo )
			.Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ) );

		Tick( 0.5f );
		sequence.Complete();
		Assert.IsFalse( sequence.IsAlive );
		AreEqual( 1, a );
	}

	[TestMethod]
	public void ManipulatingNestedTweenIsNotAllowed()
	{
		var tween = Tween.Delay( 1 );
		Sequence.Create().Chain( tween );
		ExpectError( "nested" );
		tween.Stop();
		Assert.IsTrue( tween.IsAlive );
	}

	[TestMethod]
	public void StopAllOnTargetInsideSequenceIsNotAllowed()
	{
		var go = CreateObject();
		Sequence.Create().Chain( Tween.LocalPositionX( go, 10, 1 ) );
		ExpectError( "nested" );
		Assert.AreEqual( 0, Tween.StopAll( go ) );
	}

	[TestMethod]
	public void InfiniteTweenCantBeAdded()
	{
		var tween = Tween.Delay( 1 );
		tween.SetRemainingCycles( -1 );
		ExpectError( "infinite tweens" );
		var sequence = Sequence.Create().Chain( tween );
		AreEqual( 0, sequence.Duration );
	}

	[TestMethod]
	public void TweenCantBeAddedTwice()
	{
		var tween = Tween.Delay( 1 );
		Sequence.Create().Chain( tween );
		ExpectError( "only once" );
		Sequence.Create().Chain( tween );
	}

	[TestMethod]
	public void StartedSequenceCantBeModified()
	{
		var sequence = Sequence.Create().ChainDelay( 1 );
		Tick( 0.1f );
		ExpectError( "already been started" );
		sequence.ChainDelay( 1 );
	}

	[TestMethod]
	public void PausedSequence()
	{
		float a = 0;
		var sequence = Sequence.Create().Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ) );
		Tick( 0.2f );
		sequence.IsPaused = true;
		Tick( 0.5f );
		AreEqual( 0.2f, a );
		sequence.IsPaused = false;
		Tick( 0.2f );
		AreEqual( 0.4f, a );
	}

	[TestMethod]
	public void SequenceTimeScale()
	{
		float a = 0;
		var sequence = Sequence.Create().Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ) );
		sequence.TimeScale = 0.5f;
		Tick( 1 );
		AreEqual( 0.5f, a );
	}

	[TestMethod]
	public void SequenceElapsedTimeSeeks()
	{
		float a = 0;
		float b = 0;
		var sequence = Sequence.Create()
			.Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ) )
			.Chain( Tween.Custom( 0, 1, 1, v => b = v, Ease.Linear ) );
		Tick( 0 );
		sequence.ElapsedTime = 1.5f;
		AreEqual( 1, a );
		AreEqual( 0.5f, b );
		sequence.ElapsedTime = 0.5f;
		AreEqual( 0.5f, a );
		AreEqual( 0, b );
	}

	[TestMethod]
	public void SequenceOnUpdate()
	{
		int updates = 0;
		Sequence.Create()
			.ChainDelay( 1 )
			.OnUpdate( this, ( _, _ ) => updates++ );
		Tick( 0.5f );
		Tick( 0.5f );
		Assert.IsTrue( updates >= 2 );
	}

	[TestMethod]
	public void ExceptionInChildCallbackDoesNotBreakSequence()
	{
		bool completed = false;
		ExpectError( "onComplete callback raised exception" );
		Sequence.Create()
			.ChainCallback( () => throw new InvalidOperationException( "boom" ) )
			.ChainDelay( 0.1f )
			.OnComplete( () => completed = true );
		TickFor( 0.2f );
		Assert.IsTrue( completed );
	}

	[TestMethod]
	public void DestroyedTargetStopsWholeSequence()
	{
		var go = CreateObject();
		bool completed = false;
		var sequence = Sequence.Create()
			.Chain( Tween.LocalPositionX( go, 10, 1 ) )
			.ChainDelay( 1 )
			.OnComplete( () => completed = true );
		Tick( 0.1f );
		go.DestroyImmediate();

		ExpectError( "OnComplete callback was ignored" );
		Tick( 0.1f );
		Assert.IsFalse( sequence.IsAlive );
		Assert.IsFalse( completed );
	}
}
