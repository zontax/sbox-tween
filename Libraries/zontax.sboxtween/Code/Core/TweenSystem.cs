namespace SboxTween;

/// <summary>
/// Hooks a <see cref="TweenManager"/> into every scene's update loop.
/// <list type="bullet">
/// <item><see cref="UpdateType.Update"/> runs at <see cref="GameObjectSystem.Stage.StartUpdate"/>, before components' OnUpdate.</item>
/// <item><see cref="UpdateType.LateUpdate"/> runs before <see cref="GameObjectSystem.Stage.UpdateBones"/>, after components' OnUpdate. Start values of new animations are applied here too.</item>
/// <item><see cref="UpdateType.FixedUpdate"/> runs at <see cref="GameObjectSystem.Stage.StartFixedUpdate"/>.</item>
/// </list>
/// </summary>
public sealed class TweenSystem : GameObjectSystem<TweenSystem>
{
	public TweenManager Manager { get; }

	public TweenSystem( Scene scene ) : base( scene )
	{
		Manager = new TweenManager( scene );

		Listen( Stage.StartUpdate, -100, OnUpdate, "SboxTween.Update" );
		Listen( Stage.UpdateBones, -100, OnLateUpdate, "SboxTween.LateUpdate" );
		Listen( Stage.StartFixedUpdate, -100, OnFixedUpdate, "SboxTween.FixedUpdate" );
	}

	void OnUpdate()
	{
		Manager.Update( UpdateType.Update, Time.Delta, RealTime.Delta );
	}

	void OnLateUpdate()
	{
		Manager.Update( UpdateType.LateUpdate, Time.Delta, RealTime.Delta );
		Manager.ApplyStartValues( UpdateType.Update );
		Manager.ApplyStartValues( UpdateType.LateUpdate );
	}

	void OnFixedUpdate()
	{
		float timeScale = Scene.TimeScale;
		float unscaledDelta = timeScale > 0 ? Time.Delta / timeScale : 0;
		Manager.Update( UpdateType.FixedUpdate, Time.Delta, unscaledDelta );
	}

	public override void Dispose()
	{
		Manager.Dispose();
		base.Dispose();
	}
}
