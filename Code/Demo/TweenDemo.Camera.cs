namespace SboxTweenExample;

public sealed partial class TweenDemo
{
	/// <param name="ViewDistance">How far the camera stays from the stand when it's focused.</param>
	/// <param name="ViewHeight">The height of the camera when the stand is focused.</param>
	public record StandInfo( string Title, string Description, float ViewDistance = 500, float ViewHeight = 180 );

	// SkipHotload: take the edited table after a hotload instead of keeping the old values
	[SkipHotload] public static readonly StandInfo[] Stands =
	[
		new( "Ease Race", "Six boxes, six eases, one shared Tween.Clock: its TimeScale slows the whole race at once." ),
		new( "Sequence", "Chain, Group, Insert and callbacks in one Sequence. The hover is a TweenSettings<float> from the inspector." ),
		new( "Shake & Punch", "Punches return to the start value, shakes jitter around it. The camera shake uses the same tween." ),
		new( "Color & Light", "Tint and light radius tweens: a looping color Sequence and a yoyo on the light." ),
		new( "Callbacks & Async", "An async loop that awaits each tween. Stopping the scene cancels the awaits." ),
		new( "Speed & Cycles", "LocalAngles spins a full turn, Incremental cycles keep adding a quarter turn." ),
		new( "Wait & Additive", "ChainWaitUntil holds the Sequence until the gate opens. Additive spins stack when you click fast." ),
		new( "Shader Attributes", "A custom shader reads Glow, Dissolve and GlowColor from Renderer.Attributes, animated by name." ),
		new( "AnimGraph & Morphs", "Animation graph parameters and face morphs of the citizen, tweened like any other value.", 270, 125 ),
		new( "Post FX", "Vignette, chromatic aberration, color adjustments, blur and film grain on the camera." ),
		new( "Typewriter", "Types text with pauses after punctuation. The panel description uses the Label typewriter." ),
		new( "Lights", "Light temperature and intensity, a day/night switch for the sun and the sky, and a fan driven by Tween.Property." ),
		new( "Rigidbody", "Rigidbody tweens set the velocity every fixed update, so the pusher shoves the crates instead of passing through.", 520 ),
	];

	const float CameraFov = 55;
	const float CameraPitch = 7;

	/// <summary>The focused stand, or -1 for the overview of all stands.</summary>
	public int FocusIndex { get; private set; } = -1;

	public StandInfo FocusedStand => FocusIndex >= 0 ? Stands[FocusIndex] : null;

	public bool IsCameraFlying => _cameraFlight.IsAlive;

	Sequence _cameraFlight;

	static float HalfFovTan => MathF.Tan( CameraFov * 0.5f * MathF.PI / 180 );

	/// <summary>Moves the view to the left, so the subject is centered in the space right of the UI panel.</summary>
	static float PanelShift( float distance ) => distance * HalfFovTan * 0.16f;

	(Vector3 Position, Rotation Rotation) CameraView( int index )
	{
		if ( index < 0 )
		{
			// All stands fit into the 80% of the screen that the panel doesn't cover
			float halfWidth = Stands.Length * StandSpacing * 0.5f / 0.8f;
			float distance = halfWidth / HalfFovTan;
			return (WorldPosition + new Vector3( -distance, PanelShift( distance ), 480 ), Rotation.From( 10, 0, 0 ));
		}

		var info = Stands[index];
		var stand = WorldPosition + StandPosition( index );
		return (stand + new Vector3( -info.ViewDistance, PanelShift( info.ViewDistance ), info.ViewHeight ), Rotation.From( CameraPitch, 0, 0 ));
	}

	/// <summary>Flies the camera to a stand: position and rotation share one ease, the field of view breathes out and back.</summary>
	public void FocusStand( int index )
	{
		FocusIndex = Math.Clamp( index, -1, Stands.Length - 1 );
		if ( Scene.Camera is not { } camera )
			return;

		var go = camera.GameObject;
		var (position, rotation) = CameraView( FocusIndex );
		float duration = Math.Clamp( go.WorldPosition.Distance( position ) / 1500, 0.6f, 1.6f );

		_cameraFlight.Stop();
		_cameraFlight = Sequence.Create( Tween.WorldPosition( go, position, duration, Ease.InOutCubic ) )
			.Group( Sequence.Create( camera.TweenFieldOfView( CameraFov + 10, duration * 0.5f, Ease.OutSine ) )
				.Chain( camera.TweenFieldOfView( CameraFov, duration * 0.5f, Ease.InOutSine ) ) );

		// Stand views share the rotation, only the overview looks down more
		if ( go.WorldRotation.Distance( rotation ) > 0.01f )
			_cameraFlight.Group( Tween.WorldRotation( go, rotation, duration, Ease.InOutCubic ) );

		Report( FocusedStand?.Title ?? "Overview" );
	}

	public void FocusNext() => FocusStand( FocusIndex >= Stands.Length - 1 ? -1 : FocusIndex + 1 );

	public void FocusPrevious() => FocusStand( FocusIndex < 0 ? Stands.Length - 1 : FocusIndex - 1 );

	void SnapCamera()
	{
		if ( Scene.Camera is not { } camera )
			return;

		var (position, rotation) = CameraView( FocusIndex );
		camera.WorldPosition = position;
		camera.WorldRotation = rotation;
		camera.FieldOfView = CameraFov;
		camera.Transform.ClearInterpolation();
	}
}