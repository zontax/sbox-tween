namespace SboxTween.UnitTests;

[TestClass]
public class TransformTests : TweenTestBase
{
	[TestMethod]
	public void LocalPositionFromCurrent()
	{
		var go = CreateObject();
		go.LocalPosition = new Vector3( 10, 0, 0 );
		Tween.LocalPosition( go, new Vector3( 20, 10, 0 ), 1, Ease.Linear );
		Tick( 0.5f );
		AreEqual( new Vector3( 15, 5, 0 ), go.LocalPosition );
		Tick( 0.5f );
		AreEqual( new Vector3( 20, 10, 0 ), go.LocalPosition );
	}

	[TestMethod]
	public void StartValueIsAppliedInTheSameFrame()
	{
		var go = CreateObject();
		Tween.LocalPositionZ( go, 50, 100, 1 );
		EndFrame();
		AreEqual( 50, go.LocalPosition.z );
	}

	[TestMethod]
	public void SingleAxisKeepsOtherAxes()
	{
		var go = CreateObject();
		go.LocalPosition = new Vector3( 1, 2, 3 );
		Tween.LocalPositionY( go, 12, 1, Ease.Linear );
		Tick( 1 );
		AreEqual( new Vector3( 1, 12, 3 ), go.LocalPosition );
	}

	[TestMethod]
	public void WorldPositionRespectsParent()
	{
		var parent = CreateObject( "Parent" );
		parent.WorldPosition = new Vector3( 100, 0, 0 );
		var child = CreateObject( "Child" );
		child.SetParent( parent, false );

		Tween.WorldPosition( child, new Vector3( 0, 0, 50 ), 1, Ease.Linear );
		Tick( 1 );
		AreEqual( new Vector3( 0, 0, 50 ), child.WorldPosition );
		AreEqual( new Vector3( -100, 0, 50 ), child.LocalPosition );
	}

	[TestMethod]
	public void LocalRotationSlerps()
	{
		var go = CreateObject();
		Tween.LocalRotation( go, Rotation.FromYaw( 90 ), 1, Ease.Linear );
		Tick( 0.5f );
		AreEqual( Rotation.FromYaw( 45 ), go.LocalRotation );
		Tick( 0.5f );
		AreEqual( Rotation.FromYaw( 90 ), go.LocalRotation );
	}

	[TestMethod]
	public void LocalRotationAcceptsAngles()
	{
		var go = CreateObject();
		Tween.LocalRotation( go, new Angles( 0, 90, 0 ), 1, Ease.Linear );
		Tick( 1 );
		AreEqual( Rotation.FromYaw( 90 ), go.LocalRotation );
	}

	[TestMethod]
	public void LocalAnglesCanSpinFullCircle()
	{
		var go = CreateObject();
		Tween.LocalAngles( go, new Angles( 0, 0, 0 ), new Angles( 0, 360, 0 ), 1, Ease.Linear );
		Tick( 0.5f );
		AreEqual( Rotation.FromYaw( 180 ), go.LocalRotation );
		Tick( 0.25f );
		AreEqual( Rotation.FromYaw( 270 ), go.LocalRotation );
	}

	[TestMethod]
	public void LocalAnglesIgnoresStartFromCurrent()
	{
		var go = CreateObject();
		ExpectWarning( "Euler angles" );
		Tween.LocalAngles( go, new TweenSettings<Angles>( new Angles( 0, 90, 0 ), 1 ) );
	}

	[TestMethod]
	public void UniformScale()
	{
		var go = CreateObject();
		Tween.LocalScale( go, 3, 1, Ease.Linear );
		Tick( 0.5f );
		AreEqual( new Vector3( 2, 2, 2 ), go.LocalScale );
	}

	[TestMethod]
	public void ScaleAxis()
	{
		var go = CreateObject();
		Tween.LocalScaleZ( go, 0, 1, Ease.Linear );
		Tick( 1 );
		AreEqual( new Vector3( 1, 1, 0 ), go.LocalScale );
	}

	[TestMethod]
	public void ExtensionMethods()
	{
		var go = CreateObject();
		go.TweenLocalPositionX( 10, 1, Ease.Linear );
		go.TweenLocalScale( 2, 1, Ease.Linear );
		Tick( 1 );
		AreEqual( 10, go.LocalPosition.x );
		AreEqual( new Vector3( 2, 2, 2 ), go.LocalScale );
	}

	[TestMethod]
	public void TweenSettingsOverload()
	{
		var go = CreateObject();
		var settings = new TweenSettings<Vector3>( new Vector3( 1, 1, 1 ), new Vector3( 3, 3, 3 ), 2, Ease.Linear );
		Tween.LocalPosition( go, settings );
		Tick( 1 );
		AreEqual( new Vector3( 2, 2, 2 ), go.LocalPosition );
	}

	[TestMethod]
	public void DestroyedTargetStopsTween()
	{
		var go = CreateObject();
		bool completed = false;
		var tween = Tween.LocalPositionX( go, 10, 1 ).OnComplete( () => completed = true );
		Tick( 0.1f );
		go.DestroyImmediate();

		ExpectError( "OnComplete callback was ignored" );
		Tick( 0.1f );
		Assert.IsFalse( tween.IsAlive );
		Assert.IsFalse( completed );
		Assert.AreEqual( 0, Manager.TweensCount );
	}

	[TestMethod]
	public void InvalidTargetReturnsDeadTween()
	{
		var go = CreateObject();
		go.DestroyImmediate();
		ExpectError( "target is null or has been destroyed" );
		var tween = Tween.LocalPositionX( go, 10, 1 );
		Assert.IsFalse( tween.IsAlive );
		Assert.AreEqual( 0, Manager.TweensCount );
	}

	[TestMethod]
	public void StopAllOnTarget()
	{
		var a = CreateObject( "A" );
		var b = CreateObject( "B" );
		var tweenA = Tween.LocalPositionX( a, 10, 1 );
		var tweenA2 = Tween.LocalScale( a, 2, 1 );
		var tweenB = Tween.LocalPositionX( b, 10, 1 );

		Assert.AreEqual( 2, Tween.StopAll( a ) );
		Assert.IsFalse( tweenA.IsAlive );
		Assert.IsFalse( tweenA2.IsAlive );
		Assert.IsTrue( tweenB.IsAlive );
		Assert.AreEqual( 1, b.StopTweens() );
	}

	[TestMethod]
	public void CompleteAllOnTarget()
	{
		var go = CreateObject();
		Tween.LocalPositionX( go, 10, 1 );
		Assert.AreEqual( 1, go.CompleteTweens() );
		AreEqual( 10, go.LocalPosition.x );
	}

	[TestMethod]
	public void SetPausedAllOnTarget()
	{
		var go = CreateObject();
		var tween = Tween.LocalPositionX( go, 10, 1 );
		go.SetTweensPaused( true );
		Assert.IsTrue( tween.IsPaused );
	}

	[TestMethod]
	public void GetTweensCountOnTarget()
	{
		var go = CreateObject();
		Tween.LocalPositionX( go, 10, 1 );
		Tween.Delay( 1 );
		Assert.AreEqual( 1, Manager.GetTweensCount( go ) );
		Assert.AreEqual( 2, Manager.GetTweensCount() );
	}

	[TestMethod]
	public void PositionAtSpeed()
	{
		var go = CreateObject();
		var tween = Tween.LocalPositionAtSpeed( go, new Vector3( 100, 0, 0 ), 50 );
		AreEqual( 2, tween.Duration );
	}

	[TestMethod]
	public void RotationAtSpeed()
	{
		var go = CreateObject();
		var tween = Tween.LocalRotationAtSpeed( go, Rotation.FromYaw( 90 ), 45 );
		AreEqual( 2, tween.Duration, 0.01f );
	}

	[TestMethod]
	public void EndValueEqualsCurrentWarns()
	{
		var go = CreateObject();
		Tween.LocalPositionX( go, 0, 1 );
		ExpectWarning( "equals the current animated value" );
		Tick( 0.1f );
	}

	[TestMethod]
	public void RotationEqualsCurrentWarningShowsAngles()
	{
		var go = CreateObject();
		go.LocalRotation = Rotation.FromYaw( 180 );
		Tween.LocalRotation( go, Rotation.FromYaw( 180 ), 1 );
		ExpectWarning( "Yaw" );
		Tick( 0.1f );
	}

	[TestMethod]
	public void TweenOnDisabledObjectWarns()
	{
		var go = CreateObject();
		go.Enabled = false;
		ExpectWarning( "not active in the hierarchy" );
		Tween.LocalPositionX( go, 10, 1 );
	}

	[TestMethod]
	public void BounceExactAmplitude()
	{
		var go = CreateObject();
		Tween.LocalPositionZ( go, 0, 100, 1, TweenEasing.BounceExact( 10 ) );
		// The first bounce happens between t = 1 / 2.75 and t = 2 / 2.75
		float min = float.MaxValue;
		for ( int i = 1; i <= 100; i++ )
		{
			Tick( 0.01f );
			if ( i > 40 && i < 70 )
				min = MathF.Min( min, go.LocalPosition.z );
		}

		Tick( 0.01f );
		AreEqual( 90, min, 0.5f );
		AreEqual( 100, go.LocalPosition.z );
	}
}
