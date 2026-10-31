namespace SboxTween.UnitTests;

/// <summary>
/// Every test gets its own scene and <see cref="TweenManager"/>, ticked manually with <see cref="Tick"/>.
/// Any SboxTween warning or error that wasn't expected with <see cref="ExpectError"/> fails the test.
/// </summary>
public abstract class TweenTestBase
{
	protected const float Epsilon = 0.0001f;
	protected const float FrameTime = 0.016666668f;

	protected Scene Scene { get; private set; }
	protected TweenManager Manager { get; private set; }

	IDisposable _sceneScope;
	TweenManager.ManagerScope _managerScope;
	readonly List<(TweenLogLevel Level, string Message)> _logs = [];
	readonly List<(TweenLogLevel Level, string Substring)> _expected = [];

	[TestInitialize]
	public void InitTween()
	{
		Scene = new Scene();
		_sceneScope = Scene.Push();
		Manager = new TweenManager( Scene );
		_managerScope = TweenManager.Push( Manager );
		_logs.Clear();
		_expected.Clear();
		TweenLog.Interceptor = ( level, message ) => _logs.Add( (level, message) );
	}

	[TestCleanup]
	public void CleanupTween()
	{
		TweenLog.Interceptor = null;
		_managerScope.Dispose();
		Manager.Dispose();
		_sceneScope.Dispose();
		Scene.Destroy();

		AssertLogs();
	}

	/// <summary>
	/// Expects an error containing <paramref name="substring"/> at some point during the test.
	/// </summary>
	protected void ExpectError( string substring ) => _expected.Add( (TweenLogLevel.Error, substring) );

	/// <summary>
	/// Expects a warning containing <paramref name="substring"/> at some point during the test.
	/// </summary>
	protected void ExpectWarning( string substring ) => _expected.Add( (TweenLogLevel.Warning, substring) );

	/// <summary>
	/// Checks that every log so far was expected, then clears both lists.
	/// </summary>
	protected void AssertLogs()
	{
		var unexpected = new List<string>();
		var pending = new List<(TweenLogLevel Level, string Substring)>( _expected );
		foreach ( var (level, message) in _logs )
		{
			int index = pending.FindIndex( e => e.Level == level && message.Contains( e.Substring ) );
			if ( index < 0 )
			{
				unexpected.Add( $"[{level}] {message}" );
				continue;
			}

			pending.RemoveAt( index );
		}

		_logs.Clear();
		_expected.Clear();

		if ( unexpected.Count > 0 )
			Assert.Fail( "Unexpected SboxTween logs:\n" + string.Join( "\n", unexpected ) );

		if ( pending.Count > 0 )
			Assert.Fail( "Expected SboxTween logs were not produced:\n" + string.Join( "\n", pending.Select( p => $"[{p.Level}] {p.Substring}" ) ) );
	}

	/// <summary>
	/// Simulates one frame: Update stage, then LateUpdate stage with start values applied, like <see cref="TweenSystem"/>.
	/// </summary>
	protected void Tick( float deltaTime, float? unscaledDeltaTime = null )
	{
		float unscaled = unscaledDeltaTime ?? deltaTime;
		Manager.Update( UpdateType.Update, deltaTime, unscaled );
		Manager.Update( UpdateType.LateUpdate, deltaTime, unscaled );
		Manager.ApplyStartValues( UpdateType.Update );
		Manager.ApplyStartValues( UpdateType.LateUpdate );
	}

	/// <summary>
	/// Applies start values of tweens created this frame, like the end of the current frame.
	/// </summary>
	protected void EndFrame()
	{
		Manager.ApplyStartValues( UpdateType.Update );
		Manager.ApplyStartValues( UpdateType.LateUpdate );
	}

	protected void TickFor( float seconds, float step = FrameTime )
	{
		int steps = (int)MathF.Round( seconds / step );
		for ( int i = 0; i < steps; i++ )
			Tick( step );
	}

	protected GameObject CreateObject( string name = "Test" )
	{
		var go = new GameObject( true, name );
		return go;
	}

	protected static void AreEqual( float expected, float actual, float delta = Epsilon, string message = null )
		=> Assert.AreEqual( expected, actual, delta, message );

	protected static void AreEqual( Vector3 expected, Vector3 actual, float delta = Epsilon )
	{
		Assert.AreEqual( expected.x, actual.x, delta, $"x: expected {expected}, actual {actual}" );
		Assert.AreEqual( expected.y, actual.y, delta, $"y: expected {expected}, actual {actual}" );
		Assert.AreEqual( expected.z, actual.z, delta, $"z: expected {expected}, actual {actual}" );
	}

	protected static void AreEqual( Color expected, Color actual, float delta = Epsilon )
	{
		Assert.AreEqual( expected.r, actual.r, delta, $"r: expected {expected}, actual {actual}" );
		Assert.AreEqual( expected.g, actual.g, delta, $"g: expected {expected}, actual {actual}" );
		Assert.AreEqual( expected.b, actual.b, delta, $"b: expected {expected}, actual {actual}" );
		Assert.AreEqual( expected.a, actual.a, delta, $"a: expected {expected}, actual {actual}" );
	}

	protected static void AreEqual( Rotation expected, Rotation actual, float degrees = 0.01f )
	{
		float angle = expected.Distance( actual );
		Assert.IsTrue( angle <= degrees, $"Rotations differ by {angle} degrees. Expected {expected.Angles()}, actual {actual.Angles()}" );
	}
}
