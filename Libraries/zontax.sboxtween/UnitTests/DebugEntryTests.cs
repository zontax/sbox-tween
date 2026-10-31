namespace SboxTween.UnitTests;

[TestClass]
public class DebugEntryTests : TweenTestBase
{
	[TestMethod]
	public void SequenceChildrenFollowTheirParent()
	{
		var go = CreateObject();
		Tween.Delay( 1 );
		var inner = Sequence.Create().Chain( Tween.LocalScale( go, 2, 1 ) );
		Sequence.Create()
			.Chain( Tween.LocalPositionX( go, 10, 1 ) )
			.Chain( inner );

		var entries = new List<TweenDebugEntry>();
		Manager.CollectDebugEntries( entries );

		CollectionAssert.AreEqual( new[] { "Delay", "Sequence", "LocalPositionX", "Sequence", "LocalScale" }, entries.Select( x => x.Name ).ToArray() );
		CollectionAssert.AreEqual( new[] { 0, 0, 1, 1, 2 }, entries.Select( x => x.Depth ).ToArray() );
		Assert.IsNull( entries[0].Target );
		Assert.AreSame( go, entries[2].Target );
		Assert.IsFalse( entries[2].CanControl );
	}

	[TestMethod]
	public void ProgressOfNestedTween()
	{
		var go = CreateObject();
		Sequence.Create()
			.Chain( Tween.LocalPositionX( go, 10, 1 ) )
			.Chain( Tween.LocalPositionY( go, 10, 1 ) );
		Tick( 1.5f );

		var entries = new List<TweenDebugEntry>();
		Manager.CollectDebugEntries( entries );
		AreEqual( 0.75f, entries[0].Progress );
		AreEqual( 1, entries[1].Progress );
		AreEqual( 0.5f, entries[2].Progress );
	}

	[TestMethod]
	public void ControlsWorkOnTopLevelEntries()
	{
		bool completed = false;
		Sequence.Create().ChainDelay( 1 ).OnComplete( () => completed = true );
		var delay = Tween.Delay( 1 );

		var entries = new List<TweenDebugEntry>();
		Manager.CollectDebugEntries( entries );
		var sequenceEntry = entries.First( x => x.IsSequence );
		var delayEntry = entries.First( x => x.Name == "Delay" && x.CanControl );

		delayEntry.SetPaused( true );
		Assert.IsTrue( delay.IsPaused );

		// Nested entries ignore controls instead of logging errors
		entries.First( x => !x.CanControl ).Stop();

		sequenceEntry.Complete();
		Assert.IsTrue( completed );
		Assert.IsFalse( sequenceEntry.IsAlive );

		delayEntry.Stop();
		Assert.IsFalse( delay.IsAlive );
	}
}
