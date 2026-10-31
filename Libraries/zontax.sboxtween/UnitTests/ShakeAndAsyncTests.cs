using System.Threading;

namespace SboxTween.UnitTests;

[TestClass]
public class ShakeTests : TweenTestBase
{
	[TestMethod]
	public void ShakeReturnsToStartValue()
	{
		var go = CreateObject();
		go.LocalPosition = new Vector3( 10, 20, 30 );
		var tween = Tween.ShakeLocalPosition( go, new Vector3( 5, 5, 5 ), 1 );

		bool moved = false;
		for ( int i = 0; i < 30; i++ )
		{
			Tick( FrameTime );
			if ( go.LocalPosition.Distance( new Vector3( 10, 20, 30 ) ) > 0.5f )
				moved = true;
		}

		Assert.IsTrue( moved, "Shake should move the object" );
		TickFor( 1 );
		Assert.IsFalse( tween.IsAlive );
		AreEqual( new Vector3( 10, 20, 30 ), go.LocalPosition );
	}

	[TestMethod]
	public void ShakeStaysWithinStrength()
	{
		var go = CreateObject();
		Tween.ShakeLocalPosition( go, new Vector3( 0, 0, 10 ), 1, enableFalloff: false );
		for ( int i = 0; i < 60; i++ )
		{
			Tick( FrameTime );
			var p = go.LocalPosition;
			Assert.AreEqual( 0, p.x, Epsilon );
			Assert.AreEqual( 0, p.y, Epsilon );
			Assert.IsTrue( MathF.Abs( p.z ) <= 10.001f, $"z = {p.z}" );
		}
	}

	[TestMethod]
	public void PunchMovesInStrengthDirection()
	{
		var go = CreateObject();
		Tween.PunchLocalPosition( go, new Vector3( 0, 0, 10 ), 1, asymmetryFactor: 1 );
		float max = 0;
		float min = 0;
		for ( int i = 0; i < 70; i++ )
		{
			Tick( FrameTime );
			max = MathF.Max( max, go.LocalPosition.z );
			min = MathF.Min( min, go.LocalPosition.z );
		}

		Assert.IsTrue( max > 5, $"max = {max}" );
		// Full asymmetry means no recoil below the start value
		Assert.IsTrue( min > -0.001f, $"min = {min}" );
		AreEqual( 0, go.LocalPosition.z );
	}

	[TestMethod]
	public void ShakeRotationAndScaleReturn()
	{
		var go = CreateObject();
		go.LocalRotation = Rotation.FromYaw( 30 );
		go.LocalScale = 2;
		Tween.ShakeLocalRotation( go, new Vector3( 10, 10, 10 ), 0.5f );
		Tween.PunchLocalScale( go, new Vector3( 1, 1, 1 ), 0.5f );
		TickFor( 0.6f );
		AreEqual( Rotation.FromYaw( 30 ), go.LocalRotation );
		AreEqual( new Vector3( 2, 2, 2 ), go.LocalScale );
	}

	[TestMethod]
	public void OverlappingShakesShareStartValue()
	{
		var go = CreateObject();
		go.LocalPosition = new Vector3( 1, 2, 3 );
		Tween.ShakeLocalPosition( go, new Vector3( 5, 5, 5 ), 1 );
		TickFor( 0.3f );
		// The second shake starts while the object is displaced, but it must return to the original position
		Tween.ShakeLocalPosition( go, new Vector3( 5, 5, 5 ), 1 );
		TickFor( 1.5f );
		AreEqual( new Vector3( 1, 2, 3 ), go.LocalPosition );
		Assert.AreEqual( 0, Manager.Shakes.Count );
	}

	[TestMethod]
	public void ShakeCustomReportsValues()
	{
		var values = new List<Vector3>();
		Tween.ShakeCustom( values, new Vector3( 100, 0, 0 ), new ShakeSettings( new Vector3( 1, 1, 1 ), 0.5f ), static ( list, v ) => list.Add( v ) );
		TickFor( 0.6f );
		Assert.IsTrue( values.Count > 10 );
		AreEqual( new Vector3( 100, 0, 0 ), values[^1] );
	}

	[TestMethod]
	public void ZeroStrengthIsReported()
	{
		var go = CreateObject();
		ExpectError( "strength is (0, 0, 0)" );
		Tween.ShakeLocalPosition( go, Vector3.Zero, 1 );
	}

	[TestMethod]
	public void ShakeCameraCreatesSequence()
	{
		var go = CreateObject();
		var camera = go.Components.Create<CameraComponent>();
		var sequence = Tween.ShakeCamera( camera, 1 );
		Assert.IsTrue( sequence.IsAlive );
		TickFor( 0.6f );
		Assert.IsFalse( sequence.IsAlive );
		AreEqual( Rotation.Identity, go.LocalRotation );
	}
}

[TestClass]
public class AsyncTests : TweenTestBase
{
	[TestMethod]
	public void TaskCompletesWhenTweenCompletes()
	{
		var tween = Tween.Delay( 0.5f );
		var task = tween.ToTask();
		Assert.IsFalse( task.IsCompleted );
		TickFor( 0.4f );
		Assert.IsFalse( task.IsCompleted );
		TickFor( 0.2f );
		Assert.IsTrue( task.IsCompletedSuccessfully );
	}

	[TestMethod]
	public void TaskCompletesWhenTweenIsStopped()
	{
		var tween = Tween.Delay( 1 );
		var task = tween.ToTask();
		Tick( 0.1f );
		tween.Stop();
		Tick( 0.1f );
		Assert.IsTrue( task.IsCompletedSuccessfully );
	}

	[TestMethod]
	public void SequenceTaskCompletes()
	{
		var sequence = Sequence.Create().ChainDelay( 0.2f ).ChainDelay( 0.2f );
		var task = sequence.ToTask();
		TickFor( 0.3f );
		Assert.IsFalse( task.IsCompleted );
		TickFor( 0.2f );
		Assert.IsTrue( task.IsCompletedSuccessfully );
	}

	[TestMethod]
	public void DeadTweenTaskIsCompleted()
	{
		var tween = Tween.Delay( 0.01f );
		TickFor( 0.1f );
		Assert.IsTrue( tween.ToTask().IsCompletedSuccessfully );
	}

	[TestMethod]
	public void AwaitWorks()
	{
		bool finished = false;
		async Task Run()
		{
			await Tween.Delay( 0.1f );
			finished = true;
		}

		var task = Run();
		TickFor( 0.2f );
		task.Wait( 1000 );
		Assert.IsTrue( finished );
	}

	[TestMethod]
	public void CancellationStopsTween()
	{
		using var cts = new CancellationTokenSource();
		float value = 0;
		var tween = Tween.Custom( 0, 1, 1, v => value = v, Ease.Linear ).SetCancellationToken( cts.Token );
		var task = tween.ToTask();
		Tick( 0.5f );
		cts.Cancel();
		Tick( 0.1f );
		Assert.IsFalse( tween.IsAlive );
		AreEqual( 0.5f, value );
		Tick( 0.1f );
		Assert.IsTrue( task.IsCanceled );
	}

	[TestMethod]
	public void CancellationCanComplete()
	{
		using var cts = new CancellationTokenSource();
		float value = 0;
		var tween = Tween.Custom( 0, 1, 1, v => value = v, Ease.Linear ).SetCancellationToken( cts.Token, TweenSettings.CancellationMode.Complete );
		Tick( 0.5f );
		cts.Cancel();
		Tick( 0.1f );
		Assert.IsFalse( tween.IsAlive );
		AreEqual( 1, value );
	}

	[TestMethod]
	public void AlreadyCanceledTokenStopsImmediately()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();
		var tween = Tween.Delay( 1 ).SetCancellationToken( cts.Token );
		Assert.IsFalse( tween.IsAlive );
	}

	[TestMethod]
	public void SequenceCancellation()
	{
		using var cts = new CancellationTokenSource();
		var sequence = Sequence.Create().ChainDelay( 1 ).SetCancellationToken( cts.Token );
		Tick( 0.1f );
		cts.Cancel();
		Tick( 0.1f );
		Assert.IsFalse( sequence.IsAlive );
	}

	[TestMethod]
	public void TweenWithTokenCantBeAddedToSequence()
	{
		using var cts = new CancellationTokenSource();
		var tween = Tween.Delay( 1 ).SetCancellationToken( cts.Token );
		ExpectError( "CancellationToken" );
		Sequence.Create().Chain( tween );
	}

	[TestMethod]
	public void DisposingManagerCancelsAwaiters()
	{
		var task = Tween.Delay( 1 ).ToTask();
		Tick( 0.1f );
		Manager.Dispose();
		Assert.IsTrue( task.IsCanceled );
	}

	[TestMethod]
	public void AwaitThrowsWhenCanceled()
	{
		using var cts = new CancellationTokenSource();
		bool finished = false;
		async Task Run()
		{
			await Tween.Delay( 1 ).SetCancellationToken( cts.Token );
			finished = true;
		}

		var task = Run();
		Tick( 0.1f );
		cts.Cancel();
		TickFor( 0.2f );
		Assert.IsTrue( task.IsCanceled );
		Assert.IsFalse( finished );
	}

	[TestMethod]
	public void AwaitThrowsWhenManagerIsDisposed()
	{
		async Task Run() => await Tween.Delay( 1 );

		var task = Run();
		Tick( 0.1f );
		Manager.Dispose();
		Assert.IsTrue( task.IsCanceled );
	}

	[TestMethod]
	public void AwaitOfDeadTweenContinuesSynchronously()
	{
		var tween = Tween.Delay( 0.01f );
		TickFor( 0.1f );
		bool finished = false;
		async Task Run()
		{
			await tween;
			finished = true;
		}

		Assert.IsTrue( Run().IsCompletedSuccessfully );
		Assert.IsTrue( finished );
	}

	[TestMethod]
	public void AwaitSequenceWorks()
	{
		int step = 0;
		async Task Run()
		{
			await Sequence.Create().ChainDelay( 0.1f ).ChainDelay( 0.1f );
			step = 1;
			await Tween.Delay( 0.1f );
			step = 2;
		}

		var task = Run();
		TickFor( 0.15f );
		Assert.AreEqual( 0, step );
		TickFor( 0.1f );
		Assert.AreEqual( 1, step );
		TickFor( 0.15f );
		Assert.AreEqual( 2, step );
		Assert.IsTrue( task.IsCompletedSuccessfully );
	}
}

[TestClass]
public class TimeScaleTests : TweenTestBase
{
	[TestMethod]
	public void TweenTimeScaleAnimatesTimeScale()
	{
		var target = Tween.Delay( 10 );
		Tween.TweenTimeScale( target, 0, 1, Ease.Linear );
		Tick( 0.5f );
		AreEqual( 0.5f, target.TimeScale );
		Tick( 0.5f );
		AreEqual( 0, target.TimeScale );
	}

	[TestMethod]
	public void TweenTimeScaleStopsWhenTargetDies()
	{
		var target = Tween.Delay( 0.2f );
		var timeScaleTween = Tween.TweenTimeScale( target, 2, 1 );
		TickFor( 0.3f );
		Assert.IsFalse( target.IsAlive );
		Assert.IsFalse( timeScaleTween.IsAlive );
	}

	[TestMethod]
	public void GlobalTimeScaleAnimatesSceneTimeScale()
	{
		Scene.TimeScale = 1;
		Tween.GlobalTimeScale( Scene, new TweenSettings<float>( 0.5f, new TweenSettings( 1, Ease.Linear, useUnscaledTime: true ) ) );
		Tick( 0.5f, 0.5f );
		AreEqual( 0.75f, Scene.TimeScale );
		Tick( 0.5f, 0.5f );
		AreEqual( 0.5f, Scene.TimeScale );
	}

	[TestMethod]
	public void GlobalTimeScaleForcesUnscaledTime()
	{
		ExpectWarning( "UseUnscaledTime" );
		Tween.GlobalTimeScale( Scene, new TweenSettings<float>( 0.5f, 1 ) );
	}
}

[TestClass]
public class EasingTests
{
	static IEnumerable<Ease> StandardEases => Enum.GetValues<Ease>().Where( e => e != Ease.Custom && e != Ease.Default );

	[TestMethod]
	public void AllEasesStartAtZeroAndEndAtOne()
	{
		foreach ( var ease in StandardEases )
		{
			Assert.AreEqual( 0, TweenEasing.Evaluate( 0, ease ), 0.001f, $"{ease} at 0" );
			Assert.AreEqual( 1, TweenEasing.Evaluate( 1, ease ), 0.001f, $"{ease} at 1" );
		}
	}

	[TestMethod]
	public void KnownMidpoints()
	{
		Assert.AreEqual( 0.5f, TweenEasing.Evaluate( 0.5f, Ease.Linear ), 0.0001f );
		Assert.AreEqual( 0.25f, TweenEasing.Evaluate( 0.5f, Ease.InQuad ), 0.0001f );
		Assert.AreEqual( 0.75f, TweenEasing.Evaluate( 0.5f, Ease.OutQuad ), 0.0001f );
		Assert.AreEqual( 0.5f, TweenEasing.Evaluate( 0.5f, Ease.InOutSine ), 0.0001f );
		Assert.AreEqual( 0.125f, TweenEasing.Evaluate( 0.5f, Ease.InCubic ), 0.0001f );
		Assert.IsTrue( TweenEasing.Evaluate( 0.5f, Ease.OutBack ) > 1, "OutBack overshoots" );
		Assert.IsTrue( TweenEasing.Evaluate( 0.2f, Ease.InBack ) < 0, "InBack undershoots" );
	}

	[TestMethod]
	public void InAndOutAreSymmetric()
	{
		foreach ( var (inEase, outEase) in new[] { (Ease.InQuad, Ease.OutQuad), (Ease.InCubic, Ease.OutCubic), (Ease.InSine, Ease.OutSine), (Ease.InExpo, Ease.OutExpo), (Ease.InBounce, Ease.OutBounce) } )
		{
			for ( float t = 0; t <= 1; t += 0.1f )
				Assert.AreEqual( 1 - TweenEasing.Evaluate( 1 - t, inEase ), TweenEasing.Evaluate( t, outEase ), 0.001f, $"{inEase}/{outEase} at {t}" );
		}
	}
}

[TestClass]
public class ParametricEasingTests : TweenTestBase
{
	[TestMethod]
	public void OvershootIsStronger()
	{
		float normal = 0;
		float strong = 0;
		Tween.Custom( 0, 1, 1, v => normal = MathF.Max( normal, v ), Ease.OutBack );
		Tween.Custom( 0, 1, 1, v => strong = MathF.Max( strong, v ), TweenEasing.Overshoot( 3 ) );
		TickFor( 1 );
		Assert.IsTrue( strong > normal, $"strong {strong}, normal {normal}" );
	}

	[TestMethod]
	public void ElasticEndsAtOne()
	{
		float value = 0;
		Tween.Custom( 0, 1, 1, v => value = v, TweenEasing.Elastic( 1, 0.5f ) );
		TickFor( 1.1f );
		AreEqual( 1, value );
	}

	[TestMethod]
	public void CurveEase()
	{
		float value = 0;
		var curve = new Curve( new Curve.Frame( 0, 0 ), new Curve.Frame( 1, 1 ) );
		Tween.Custom( 0, 10, 1, v => value = v, curve );
		Tick( 1 );
		AreEqual( 10, value, 0.01f );
	}

	[TestMethod]
	public void InvalidCurveFallsBackToDefault()
	{
		ExpectError( "at least 2 frames" );
		ExpectError( "Curve is not configured correctly" );
		float value = 0;
		Tween.Custom( 0, 1, 1, v => value = v, TweenEasing.FromCurve( default ) );
		Tick( 1 );
		AreEqual( 1, value );
	}
}
