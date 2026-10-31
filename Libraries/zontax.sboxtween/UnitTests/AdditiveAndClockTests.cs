namespace SboxTween.UnitTests;

[TestClass]
public class AdditiveTests : TweenTestBase
{
	sealed class Box
	{
		public float Value;
		public Rotation Rotation = Rotation.Identity;
	}

	[TestMethod]
	public void AdditiveTweensBlend()
	{
		var go = CreateObject();
		go.LocalPosition = new Vector3( 100, 0, 0 );
		Tween.LocalPositionAdditive( go, new Vector3( 10, 0, 0 ), 1, Ease.Linear );
		Tween.LocalPositionAdditive( go, new Vector3( 0, 20, 0 ), 1, Ease.Linear );
		Tick( 0.5f );
		AreEqual( new Vector3( 105, 10, 0 ), go.LocalPosition );
		Tick( 0.5f );
		AreEqual( new Vector3( 110, 20, 0 ), go.LocalPosition );
	}

	[TestMethod]
	public void AdditiveKeepsExternalChanges()
	{
		var go = CreateObject();
		Tween.LocalPositionAdditive( go, new Vector3( 10, 0, 0 ), 1, Ease.Linear );
		Tick( 0.5f );
		// Something else moves the object; the additive tween only adds its own deltas
		go.LocalPosition += new Vector3( 0, 0, 50 );
		Tick( 0.5f );
		AreEqual( new Vector3( 10, 0, 50 ), go.LocalPosition );
	}

	[TestMethod]
	public void AdditiveYoyoReturnsToStart()
	{
		var go = CreateObject();
		go.LocalScale = 1;
		Tween.LocalScaleAdditive( go, new Vector3( 1, 1, 1 ), 0.5f, Ease.OutBack, cycles: 2, cycleMode: CycleMode.Yoyo );
		TickFor( 1.1f );
		AreEqual( Vector3.One, go.LocalScale );
	}

	[TestMethod]
	public void AdditiveIncrementalCycles()
	{
		var box = new Box();
		Tween.CustomAdditive( box, 1f, new TweenSettings( 1, Ease.Linear, 3, CycleMode.Incremental ), static ( b, d ) => b.Value += d, static b => b.Value );
		TickFor( 3.1f );
		AreEqual( 3, box.Value );
	}

	[TestMethod]
	public void CompleteAppliesTheRemainingDelta()
	{
		var box = new Box();
		var tween = Tween.CustomAdditive( box, 10f, new TweenSettings( 1, Ease.Linear ), static ( b, d ) => b.Value += d, static b => b.Value );
		Tick( 0.3f );
		tween.Complete();
		AreEqual( 10, box.Value );
	}

	[TestMethod]
	public void StopKeepsAppliedDelta()
	{
		var box = new Box();
		var tween = Tween.CustomAdditive( box, 10f, new TweenSettings( 1, Ease.Linear ), static ( b, d ) => b.Value += d, static b => b.Value );
		Tick( 0.3f );
		tween.Stop();
		AreEqual( 3, box.Value );
	}

	[TestMethod]
	public void GetterCompensatesRounding()
	{
		// At this magnitude a float can't represent tiny deltas, so without compensation the value drifts
		var box = new Box { Value = 100000 };
		Tween.CustomAdditive( box, 1f, new TweenSettings( 1, Ease.InOutSine ), static ( b, d ) => b.Value += d, static b => b.Value );
		TickFor( 1.1f, 0.001f );
		AreEqual( 100001, box.Value, 0.01f );
	}

	[TestMethod]
	public void MissingGetterWarns()
	{
		var box = new Box();
		ExpectWarning( "getCurrentValue" );
		Tween.CustomAdditive( box, 1f, new TweenSettings( 1 ), static ( b, d ) => b.Value += d );
	}

	[TestMethod]
	public void AdditiveRotationsCombine()
	{
		var go = CreateObject();
		Tween.LocalRotationAdditive( go, Rotation.FromYaw( 90 ), 1, Ease.Linear );
		Tween.LocalRotationAdditive( go, Rotation.FromYaw( 45 ), 1, Ease.Linear );
		TickFor( 1.1f );
		AreEqual( Rotation.FromYaw( 135 ), go.LocalRotation, 0.05f );
	}

	[TestMethod]
	public void AdditiveEulerSpinsMoreThan180()
	{
		var go = CreateObject();
		Tween.LocalRotationAdditive( go, new Angles( 0, 270, 0 ), 1, Ease.Linear );
		Tick( 0.5f );
		AreEqual( Rotation.FromYaw( 135 ), go.LocalRotation, 0.05f );
		Tick( 0.5f );
		AreEqual( Rotation.FromYaw( 270 ), go.LocalRotation, 0.05f );
	}

	[TestMethod]
	public void CustomRotationAdditive()
	{
		var box = new Box();
		Tween.CustomAdditive( box, Rotation.FromPitch( 60 ), new TweenSettings( 1, Ease.OutQuad ), static ( b, d ) => b.Rotation *= d, static b => b.Rotation );
		TickFor( 1.1f );
		AreEqual( Rotation.FromPitch( 60 ), box.Rotation, 0.05f );
	}

	[TestMethod]
	public void AdditiveSpeedBased()
	{
		var box = new Box();
		var tween = Tween.CustomAdditive( box, 10f, new TweenSettings( 5 ), static ( b, d ) => b.Value += d, static b => b.Value, useDurationAsSpeed: true );
		AreEqual( 2, tween.Duration );
	}

	[TestMethod]
	public void AdditiveInsideSequence()
	{
		var go = CreateObject();
		Sequence.Create()
			.Chain( Tween.LocalPositionAdditive( go, new Vector3( 10, 0, 0 ), 1, Ease.Linear ) )
			.Chain( Tween.LocalPositionAdditive( go, new Vector3( 10, 0, 0 ), 1, Ease.Linear ) );
		TickFor( 2.1f );
		AreEqual( new Vector3( 20, 0, 0 ), go.LocalPosition );
	}
}

[TestClass]
public class ClockTests : TweenTestBase
{
	[TestMethod]
	public void ClockTimeScaleAffectsAllAnimations()
	{
		float a = 0;
		float b = 0;
		var clock = new Tween.Clock();
		Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ).SetClock( clock );
		Sequence.Create().Chain( Tween.Custom( 0, 1, 1, v => b = v, Ease.Linear ) ).SetClock( clock );
		clock.TimeScale = 0.5f;
		Tick( 0.5f );
		AreEqual( 0.25f, a );
		AreEqual( 0.25f, b );
	}

	[TestMethod]
	public void ClockMultipliesTweenTimeScale()
	{
		float a = 0;
		var clock = new Tween.Clock { TimeScale = 2 };
		var tween = Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ).SetClock( clock );
		tween.TimeScale = 0.25f;
		Tick( 1 );
		AreEqual( 0.5f, a );
	}

	[TestMethod]
	public void ManualClockAdvancesOnlyByTicks()
	{
		float a = 0;
		var clock = new Tween.Clock { IsManual = true };
		Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ).SetClock( clock );
		EndFrame();

		Tick( 0.5f );
		AreEqual( 0, a );

		clock.Tick( 0.1f );
		clock.Tick( 0.1f );
		Tick( 0.5f );
		AreEqual( 0.2f, a );

		// Ticks are consumed once
		Tick( 0.5f );
		AreEqual( 0.2f, a );
	}

	[TestMethod]
	public void ManualClockSharesTicksBetweenAnimations()
	{
		float a = 0;
		float b = 0;
		var clock = new Tween.Clock { IsManual = true };
		Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ).SetClock( clock );
		Tween.Custom( 0, 1, 1, v => b = v, Ease.Linear ).SetClock( clock );
		clock.Tick( 0.4f );
		Tick( 0.1f );
		AreEqual( 0.4f, a );
		AreEqual( 0.4f, b );
	}

	[TestMethod]
	public void ManualClockCanRewind()
	{
		float a = 0;
		var clock = new Tween.Clock { IsManual = true };
		Tween.Custom( 0, 1, 1, v => a = v, Ease.Linear ).SetClock( clock );
		clock.Tick( 0.6f );
		Tick( 0 );
		clock.Tick( -0.2f );
		Tick( 0 );
		AreEqual( 0.4f, a );
	}

	[TestMethod]
	public void ClockIsBoundToOneUpdateType()
	{
		var clock = new Tween.Clock();
		Tween.Delay( 1 ).SetClock( clock );
		var lateTween = Tween.Custom( new TweenSettings<float>( 0, 1, new TweenSettings( 1, updateType: UpdateType.LateUpdate ) ), _ => { } );
		ExpectError( "bound to the 'Update' update type" );
		lateTween.SetClock( clock );
	}

	[TestMethod]
	public void TweenWithClockCantBeAddedToSequence()
	{
		var tween = Tween.Delay( 1 ).SetClock( new Tween.Clock() );
		ExpectError( "Clock to a Sequence" );
		Sequence.Create().Chain( tween );
	}

	[TestMethod]
	public void ClockCantBeSetAfterStart()
	{
		var tween = Tween.Delay( 1 );
		Tick( 0.1f );
		ExpectError( "already been started" );
		tween.SetClock( new Tween.Clock() );
	}

	[TestMethod]
	public void InvalidClockTickIsClamped()
	{
		var clock = new Tween.Clock { IsManual = true };
		ExpectError( "deltaTime" );
		clock.Tick( 100 );
		ExpectError( "Invalid deltaTime" );
		clock.Tick( float.NaN );
	}
}
