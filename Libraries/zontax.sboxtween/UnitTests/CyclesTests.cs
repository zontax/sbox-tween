namespace SboxTween.UnitTests;

[TestClass]
public class CyclesTests : TweenTestBase
{
	[TestMethod]
	public void RestartRepeatsFromStart()
	{
		float value = 0;
		int completed = 0;
		var tween = Tween.Custom( 0, 10, 1, v => value = v, Ease.Linear, cycles: 2 ).OnComplete( () => completed++ );
		Assert.AreEqual( 2, tween.CyclesTotal );
		AreEqual( 2, tween.DurationTotal );

		Tick( 0.5f );
		AreEqual( 5, value );
		Tick( 0.75f );
		AreEqual( 2.5f, value );
		Assert.AreEqual( 1, tween.CyclesDone );
		Assert.AreEqual( 0, completed );

		Tick( 0.75f );
		AreEqual( 10, value );
		Assert.AreEqual( 1, completed );
		Assert.IsFalse( tween.IsAlive );
	}

	[TestMethod]
	public void YoyoGoesBack()
	{
		float value = 0;
		Tween.Custom( 0, 10, 1, v => value = v, Ease.Linear, cycles: 2, cycleMode: CycleMode.Yoyo );
		Tick( 1.25f );
		AreEqual( 7.5f, value );
		Tick( 0.75f );
		AreEqual( 0, value );
	}

	[TestMethod]
	public void YoyoKeepsEaseOnBackwardCycle()
	{
		float value = 0;
		Tween.Custom( 0, 1, 1, v => value = v, Ease.InQuad, cycles: 2, cycleMode: CycleMode.Yoyo );
		Tick( 1.5f );
		// Yoyo mirrors the eased value: 1 - InQuad( 0.5 )
		AreEqual( 0.75f, value );
	}

	[TestMethod]
	public void RewindReversesEase()
	{
		float value = 0;
		Tween.Custom( 0, 1, 1, v => value = v, Ease.InQuad, cycles: 2, cycleMode: CycleMode.Rewind );
		Tick( 1.5f );
		// Rewind plays the ease backwards: InQuad( 1 - 0.5 )
		AreEqual( 0.25f, value );
	}

	[TestMethod]
	public void IncrementalAccumulates()
	{
		float value = 0;
		Tween.Custom( 0, 1, 1, v => value = v, Ease.Linear, cycles: 3, cycleMode: CycleMode.Incremental );
		Tick( 1.5f );
		AreEqual( 1.5f, value );
		TickFor( 2 );
		AreEqual( 3, value );
	}

	[TestMethod]
	public void InfiniteCyclesNeverComplete()
	{
		float value = 0;
		var tween = Tween.Custom( 0, 1, 1, v => value = v, Ease.Linear, cycles: -1 );
		Assert.IsTrue( float.IsPositiveInfinity( tween.DurationTotal ) );
		TickFor( 10.5f, 0.5f );
		Assert.IsTrue( tween.IsAlive );
		Assert.AreEqual( 10, tween.CyclesDone );
		AreEqual( 0.5f, value );
	}

	[TestMethod]
	public void CompleteOnInfiniteCompletesCurrentCycle()
	{
		float value = 0;
		var tween = Tween.Custom( 0, 1, 1, v => value = v, Ease.Linear, cycles: -1, cycleMode: CycleMode.Yoyo );
		Tick( 1.5f );
		tween.Complete();
		Assert.IsFalse( tween.IsAlive );
		// The second (backward) cycle ends at the start value
		AreEqual( 0, value );
	}

	[TestMethod]
	public void SetRemainingCyclesStopsAtEndValue()
	{
		float value = 0;
		var tween = Tween.Custom( 0, 1, 1, v => value = v, Ease.Linear, cycles: -1, cycleMode: CycleMode.Yoyo );
		Tick( 1.5f );
		tween.SetRemainingCycles( stopAtEndValue: true );
		TickFor( 3 );
		Assert.IsFalse( tween.IsAlive );
		AreEqual( 1, value );
	}

	[TestMethod]
	public void SetRemainingCyclesStopsAtStartValue()
	{
		float value = 0;
		var tween = Tween.Custom( 0, 1, 1, v => value = v, Ease.Linear, cycles: -1, cycleMode: CycleMode.Yoyo );
		Tick( 0.5f );
		tween.SetRemainingCycles( stopAtEndValue: false );
		TickFor( 3 );
		Assert.IsFalse( tween.IsAlive );
		AreEqual( 0, value );
	}

	[TestMethod]
	public void SetRemainingCyclesInt()
	{
		var tween = Tween.Delay( 1 );
		tween.SetRemainingCycles( 3 );
		Assert.AreEqual( 3, tween.CyclesTotal );
		AreEqual( 3, tween.DurationTotal );
	}

	[TestMethod]
	public void CyclesWithDelays()
	{
		float value = -1;
		var tween = Tween.Custom( 0, 1, 1, v => value = v, Ease.Linear, cycles: 2, startDelay: 1 );
		AreEqual( 2, tween.Duration );
		AreEqual( 4, tween.DurationTotal );
		Tick( 1.5f );
		AreEqual( 0.5f, value );
		Tick( 1 );
		// The start delay is applied on every cycle; a restarted cycle waits at the start value
		Assert.AreEqual( 1, tween.CyclesDone );
		AreEqual( 0, value );
		Tick( 1 );
		AreEqual( 0.5f, value );
	}

	[TestMethod]
	public void NegativeTimeScalePlaysBackward()
	{
		float value = 0;
		bool completed = false;
		var tween = Tween.Custom( 0, 10, 1, v => value = v, Ease.Linear ).OnComplete( () => completed = true );
		Tick( 0.6f );
		tween.TimeScale = -1;
		Tick( 0.2f );
		AreEqual( 4, value );
		Tick( 1 );
		AreEqual( 0, value );
		Assert.IsFalse( tween.IsAlive );
		Assert.IsFalse( completed, "A tween that rewinds to the start doesn't invoke OnComplete" );
	}
}
