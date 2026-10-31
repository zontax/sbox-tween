namespace SboxTween.UnitTests;

/// <summary>
/// Measures managed allocations of the steady state: after a warm-up fills the pools, creating and updating
/// animations shouldn't allocate. Allocations are counted on the current thread only.
/// </summary>
[TestClass]
public class AllocationTests : TweenTestBase
{
	sealed class Box
	{
		public float Value;
		public int Completed;
	}

	/// <summary>Runs <paramref name="action"/> once to warm up, then returns the bytes allocated by a second run.</summary>
	static long Measure( Action action )
	{
		action();
		long before = GC.GetAllocatedBytesForCurrentThread();
		action();
		return GC.GetAllocatedBytesForCurrentThread() - before;
	}

	void RunFrames( int frames )
	{
		for ( int i = 0; i < frames; i++ )
			Tick( FrameTime );
	}

	[TestMethod]
	public void CustomTweensWithTargetDontAllocate()
	{
		var box = new Box();
		Manager.SetTweensCapacity( 200 );
		long bytes = Measure( () =>
		{
			for ( int i = 0; i < 100; i++ )
			{
				Tween.Custom( box, 0, 1, 0.1f, static ( b, v ) => b.Value = v, Ease.OutQuad )
					.OnComplete( box, static b => b.Completed++ );
			}

			RunFrames( 10 );
		} );

		Assert.AreEqual( 0, bytes, $"Allocated {bytes} bytes" );
		Assert.AreEqual( 200, box.Completed );
	}

	[TestMethod]
	public void TransformTweensDontAllocate()
	{
		var go = CreateObject();
		Manager.SetTweensCapacity( 50 );
		long bytes = Measure( () =>
		{
			for ( int i = 0; i < 20; i++ )
				Tween.LocalPositionX( go, 0, i + 1, 0.1f );
			Tween.LocalRotation( go, Rotation.Identity, Rotation.FromYaw( 90 ), 0.1f );
			Tween.LocalScale( go, 1, 2, 0.1f );
			RunFrames( 10 );
		} );

		Assert.AreEqual( 0, bytes, $"Allocated {bytes} bytes" );
	}

	[TestMethod]
	public void SequencesDontAllocate()
	{
		var box = new Box();
		Manager.SetTweensCapacity( 100 );
		long bytes = Measure( () =>
		{
			for ( int i = 0; i < 10; i++ )
			{
				Sequence.Create( cycles: 2, cycleMode: Sequence.SequenceCycleMode.Yoyo )
					.Chain( Tween.Custom( box, 0, 1, 0.05f, static ( b, v ) => b.Value = v ) )
					.Group( Tween.Custom( box, 0, 1, 0.05f, static ( b, v ) => b.Value = v ) )
					.ChainDelay( 0.02f )
					.ChainCallback( box, static b => b.Completed++ );
			}

			RunFrames( 15 );
		} );

		Assert.AreEqual( 0, bytes, $"Allocated {bytes} bytes" );
	}

	[TestMethod]
	public void DelaysAndShakesDontAllocate()
	{
		var go = CreateObject();
		var box = new Box();
		Manager.SetTweensCapacity( 50 );
		long bytes = Measure( () =>
		{
			Tween.Delay( box, 0.05f, static b => b.Completed++ );
			Tween.ShakeLocalPosition( go, new Vector3( 5, 5, 5 ), 0.1f );
			Tween.PunchLocalScale( go, new Vector3( 1, 1, 1 ), 0.1f );
			RunFrames( 10 );
		} );

		Assert.AreEqual( 0, bytes, $"Allocated {bytes} bytes" );
	}

	[TestMethod]
	public void AdditiveAndClockDontAllocate()
	{
		var go = CreateObject();
		var clock = new Tween.Clock();
		Manager.SetTweensCapacity( 50 );
		long bytes = Measure( () =>
		{
			Tween.LocalPositionAdditive( go, new Vector3( 1, 0, 0 ), 0.1f );
			Tween.LocalRotationAdditive( go, new Angles( 0, 90, 0 ), 0.1f );
			Tween.LocalPositionY( go, 0, 10, 0.1f ).SetClock( clock );
			RunFrames( 10 );
		} );

		Assert.AreEqual( 0, bytes, $"Allocated {bytes} bytes" );
	}

	[TestMethod]
	public void PanelStyleTweensDontAllocate()
	{
		var panel = new Panel();
		Manager.SetTweensCapacity( 50 );
		long bytes = Measure( () =>
		{
			panel.TweenOpacity( 0, 1, 0.1f );
			panel.TweenBackgroundColor( Color.Black, Color.Red, 0.1f );
			panel.TweenWidth( Length.Percent( 50 ), Length.Percent( 80 ), 0.1f );
			RunFrames( 10 );
		} );

		Assert.AreEqual( 0, bytes, $"Allocated {bytes} bytes" );
	}

	/// <summary>
	/// The engine's PanelTransform is backed by an ImmutableList, so each transform write allocates.
	/// Channels keep it to one transform per panel per frame.
	/// </summary>
	[TestMethod]
	public void PanelTransformAllocatesOncePerPanelPerFrame()
	{
		var panel = new Panel();
		Manager.SetTweensCapacity( 50 );
		Measure( () =>
		{
			panel.TweenTranslate( new Vector2( 10, 0 ), 1 );
			RunFrames( 2 );
		} );
		Manager.StopAll();
		RunFrames( 2 );

		panel.TweenTranslate( new Vector2( 10, 0 ), 1 );
		panel.TweenScale( 2, 1 );
		panel.TweenRotate( 30, 1 );
		RunFrames( 2 );

		long single = MeasureFrame();
		Assert.IsTrue( single > 0, "Writing a PanelTransform is expected to allocate" );

		// More channels on the same panel don't add more transform writes
		long perTransform = single;
		Manager.StopAll();
		RunFrames( 2 );
		panel.TweenTranslate( new Vector2( 10, 0 ), 1 );
		RunFrames( 2 );
		long oneChannel = MeasureFrame();
		Assert.IsTrue( perTransform < oneChannel * 3, $"Three channels allocate {perTransform} bytes, one channel {oneChannel}" );
	}

#if STANDALONE
	[TestMethod]
	public void AwaitInLoopDoesntAllocate()
	{
		var box = new Box();
		Manager.SetTweensCapacity( 50 );
		async Task Loop()
		{
			for ( int i = 0; i < 100; i++ )
				await Tween.Custom( box, 0, 1, 0.01f, static ( b, v ) => b.Value = v );
		}

		var task = Loop();
		// The first await boxes the async state machine
		RunFrames( 10 );
		long before = GC.GetAllocatedBytesForCurrentThread();
		RunFrames( 50 );
		long bytes = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.IsFalse( task.IsCompleted );
		Assert.AreEqual( 0, bytes, $"Allocated {bytes} bytes" );
	}
#endif

	long MeasureFrame()
	{
		long before = GC.GetAllocatedBytesForCurrentThread();
		Tick( FrameTime );
		return GC.GetAllocatedBytesForCurrentThread() - before;
	}
}
