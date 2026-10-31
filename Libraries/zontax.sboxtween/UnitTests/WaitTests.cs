namespace SboxTween.UnitTests;

[TestClass]
public class WaitTests : TweenTestBase
{
	sealed class Flag
	{
		public bool Value;
	}

	[TestMethod]
	public void WaitWhileSuspendsSequence()
	{
		var flag = new Flag { Value = true };
		float a = 0;
		float b = 0;
		var sequence = Sequence.Create()
			.Chain( Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ) )
			.ChainWaitWhile( flag, static f => f.Value )
			.Chain( Tween.Custom( 0, 1, 1, v => b = v, Ease.Linear ) );

		// Waits don't change the duration
		AreEqual( 2, sequence.Duration );

		TickFor( 1.5f, 0.1f );
		AreEqual( 1, a );
		AreEqual( 0, b );
		Assert.IsTrue( sequence.IsWaiting );

		TickFor( 2, 0.1f );
		AreEqual( 0, b );
		Assert.IsTrue( sequence.IsAlive );

		flag.Value = false;
		Tick( 0.1f );
		Assert.IsFalse( sequence.IsWaiting );
		TickFor( 0.5f, 0.1f );
		Assert.IsTrue( b > 0.3f && b < 0.7f, $"b = {b}" );

		TickFor( 1, 0.1f );
		AreEqual( 1, b );
		Assert.IsFalse( sequence.IsAlive );
	}

	[TestMethod]
	public void WaitUntilWaitsForTrue()
	{
		var flag = new Flag();
		bool done = false;
		Sequence.Create()
			.ChainWaitUntil( flag, static f => f.Value )
			.ChainCallback( () => done = true );

		TickFor( 1, 0.1f );
		Assert.IsFalse( done );
		flag.Value = true;
		TickFor( 0.3f, 0.1f );
		Assert.IsTrue( done );
	}

	[TestMethod]
	public void WaitTimesOut()
	{
		bool done = false;
		Sequence.Create()
			.ChainWaitWhile( () => true, timeout: 1 )
			.ChainCallback( () => done = true );

		TickFor( 0.8f, 0.1f );
		Assert.IsFalse( done );
		TickFor( 0.5f, 0.1f );
		Assert.IsTrue( done );
	}

	[TestMethod]
	public void DestroyedTargetEndsWaitAndStopsSequence()
	{
		var go = CreateObject();
		var sequence = Sequence.Create()
			.ChainDelay( 0.1f )
			.ChainWaitWhile( go, static _ => true )
			.ChainDelay( 1 );

		TickFor( 0.3f, 0.1f );
		Assert.IsTrue( sequence.IsWaiting );
		go.DestroyImmediate();
		TickFor( 0.3f, 0.1f );
		Assert.IsFalse( sequence.IsWaiting );
	}

	[TestMethod]
	public void InsertWaitAtTime()
	{
		var flag = new Flag { Value = true };
		float a = 0;
		Sequence.Create()
			.Chain( Tween.Custom( 0, 2, 2, v => a = v, Ease.Linear ) )
			.InsertWaitWhile( 1, flag, static f => f.Value );

		TickFor( 1.5f, 0.1f );
		// The animation stops at the wait inserted at 1 second
		Assert.IsTrue( a >= 0.9f && a <= 1.2f, $"a = {a}" );
		flag.Value = false;
		TickFor( 1.5f, 0.1f );
		AreEqual( 2, a );
	}

	[TestMethod]
	public void NestedSequenceWait()
	{
		var flag = new Flag { Value = true };
		bool done = false;
		var inner = Sequence.Create().ChainWaitWhile( flag, static f => f.Value );
		Sequence.Create()
			.ChainDelay( 0.2f )
			.Chain( inner )
			.ChainCallback( () => done = true );

		TickFor( 1, 0.1f );
		Assert.IsFalse( done );
		flag.Value = false;
		TickFor( 0.3f, 0.1f );
		Assert.IsTrue( done );
	}

	[TestMethod]
	public void CallbackAtExactEndIsInvoked()
	{
		// 0.1 + 0.1 is exactly 0.2 in float, so the update lands exactly on the end of the sequence
		var flag = new Flag();
		int calls = 0;
		Sequence.Create()
			.ChainWaitWhile( flag, static f => f.Value )
			.ChainDelay( 0.2f )
			.ChainCallback( () => calls++ );

		TickFor( 0.5f, 0.1f );
		Assert.AreEqual( 1, calls );
	}

	[TestMethod]
	public void WaitAtExactEndSuspends()
	{
		var flag = new Flag { Value = true };
		var sequence = Sequence.Create()
			.ChainDelay( 0.2f )
			.ChainWaitWhile( flag, static f => f.Value );

		TickFor( 0.5f, 0.1f );
		Assert.IsTrue( sequence.IsWaiting );
		flag.Value = false;
		TickFor( 0.2f, 0.1f );
		Assert.IsFalse( sequence.IsAlive );
	}

	[TestMethod]
	public void CompleteIgnoresWait()
	{
		bool done = false;
		var sequence = Sequence.Create()
			.ChainWaitWhile( () => true )
			.ChainCallback( () => done = true );

		TickFor( 0.3f, 0.1f );
		sequence.Complete();
		Assert.IsTrue( done );
		Assert.IsFalse( sequence.IsAlive );
	}

	[TestMethod]
	public void WaitRequiresSingleCycleLinearSequence()
	{
		ExpectError( "only supported on sequences with 'cycles: 1'" );
		Sequence.Create( cycles: 2 ).ChainWaitWhile( () => true );
		ExpectError( "only supported on sequences with 'cycles: 1'" );
		Sequence.Create( sequenceEase: Ease.InQuad ).ChainWaitWhile( () => true );
	}

	[TestMethod]
	public void WaitSequenceRejectsCyclesAndNegativeTimeScale()
	{
		var sequence = Sequence.Create().ChainDelay( 1 ).ChainWaitWhile( () => true );
		ExpectError( "cycles other than 1" );
		sequence.SetRemainingCycles( 3 );
		Assert.AreEqual( 1, sequence.CyclesTotal );

		ExpectError( "Negative 'TimeScale'" );
		sequence.TimeScale = -1;
		AreEqual( 1, sequence.TimeScale );
	}

	[TestMethod]
	public void PredicateExceptionAbortsWait()
	{
		bool done = false;
		ExpectError( "Wait predicate raised exception" );
		Sequence.Create()
			.ChainWaitWhile( () => throw new InvalidOperationException( "boom" ) )
			.ChainCallback( () => done = true );
		TickFor( 0.2f, 0.1f );
		Assert.IsTrue( done );
	}
}
