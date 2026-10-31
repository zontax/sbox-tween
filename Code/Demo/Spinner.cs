namespace SboxTweenExample;

/// <summary>
/// Spins the object around its up axis. The demo animates <see cref="Speed"/> by name with Tween.Property.
/// </summary>
[Title( "Spinner" ), Category( "SboxTween" ), Icon( "autorenew" )]
public sealed class Spinner : Component
{
	/// <summary>Degrees per second.</summary>
	[Property] public float Speed { get; set; } = 90;

	protected override void OnUpdate()
	{
		LocalRotation *= Rotation.FromYaw( Speed * Time.Delta );
	}
}