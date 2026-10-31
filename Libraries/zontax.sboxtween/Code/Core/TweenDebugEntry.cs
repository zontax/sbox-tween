namespace SboxTween;

/// <summary>
/// A snapshot of one alive animation, used by the editor Tween Debugger.
/// Children of a Sequence follow their parent with a bigger <see cref="Depth"/>.
/// </summary>
public readonly struct TweenDebugEntry
{
	readonly Tween _tween;

	/// <summary>Nesting level: 0 for standalone tweens and main sequences, 1+ for animations inside sequences.</summary>
	public int Depth { get; }

	/// <summary>What is animated, e.g. LocalPositionX, Sequence, Delay.</summary>
	public string Name { get; }

	/// <summary>The animated object. Null for delays, callbacks and sequences.</summary>
	public object Target { get; }

	public bool IsSequence { get; }

	/// <summary>True while a sequence is suspended by a WaitWhile or WaitUntil.</summary>
	public bool IsWaiting { get; }

	/// <summary>True if the animation is driven by a <see cref="Tween.Clock"/>.</summary>
	public bool HasClock { get; }

	/// <summary>True if a CSS transition on the panel conflicts with this panel tween.</summary>
	public bool HasTransitionConflict { get; }

	public bool IsPaused { get; }
	public float TimeScale { get; }
	public UpdateType UpdateType { get; }
	public Ease Ease { get; }

	/// <summary>The duration of one cycle, including delays.</summary>
	public float Duration { get; }

	/// <summary>Normalized progress of the current cycle.</summary>
	public float Progress { get; }

	public int CyclesDone { get; }

	/// <summary>-1 means infinite cycles.</summary>
	public int CyclesTotal { get; }

	/// <summary>Only top-level animations can be stopped, completed or paused; the parent Sequence controls the nested ones.</summary>
	public bool CanControl => Depth == 0;

	/// <summary>True while the animation behind this snapshot is still alive.</summary>
	public bool IsAlive => _tween.IsAliveInternal;

	internal TweenDebugEntry( TweenData data, int depth )
	{
		_tween = new Tween( data );
		Depth = depth;
		IsSequence = data.IsSequenceRoot();
		Name = data.TweenType switch
		{
			TweenType.MainSequence or TweenType.NestedSequence => "Sequence",
			TweenType.TweenAwaiter => "Await",
			TweenType.TintableColor or TweenType.SpriteColor or TweenType.TextColor => "Color",
			TweenType.TintableAlpha or TweenType.SpriteAlpha or TweenType.TextAlpha => "Alpha",
			TweenType.LocalScaleUniform => "LocalScale",
			TweenType.PointLightRadius or TweenType.SpotLightRadius => "LightRadius",
			TweenType.SoundHandleVolume => "SoundVolume",
			TweenType.SoundHandlePitch => "SoundPitch",
			TweenType.AmbientLightColor or TweenType.VignetteColor or TweenType.FogColor or TweenType.VolumetricFogColor => "Color",
			>= TweenType.RendererAttributeFloat and <= TweenType.RendererAttributeColor => "Attribute",
			>= TweenType.AnimParameterFloat and <= TweenType.AnimParameterRotation => "AnimParameter",
			>= TweenType.PropertyFloat and <= TweenType.PropertyAngles => "Property",
			TweenType.TextTypewriter or TweenType.LabelTypewriter => "Typewriter",
			_ => data.TweenType.ToString()
		};

		if ( TweenData.IsNamed( data.TweenType ) )
			Name += $" '{data.ParameterName}'";

		if ( data.IsAdditive )
			Name += " (additive)";
		else if ( data.HasWait && data.TweenType == TweenType.Delay )
			Name = "Wait";

		IsWaiting = data.WaitPredicateShouldWait is not null && data.IsMainSequenceRoot();
		HasClock = data.Clock is not null;
		HasTransitionConflict = data.PanelTransitionConflict;
		Target = data.Target == TweenManager.DummyTarget || data.Target is TweenData ? null : data.Target;
		IsPaused = data.IsPaused;
		TimeScale = data.TimeScale;
		UpdateType = data.UpdateType;
		Ease = data.Ease;
		Duration = data.CycleDuration;
		Progress = data.DebugProgress;
		CyclesDone = data.GetCyclesDone();
		CyclesTotal = data.CyclesTotal;
	}

	/// <summary>Stops the animation without invoking OnComplete.</summary>
	public void Stop()
	{
		if ( !CanControl || !IsAlive )
			return;

		if ( IsSequence )
			new Sequence( _tween.Data ).Stop();
		else
			_tween.Stop();
	}

	/// <summary>Completes the animation, jumping to its end value.</summary>
	public void Complete()
	{
		if ( !CanControl || !IsAlive )
			return;

		if ( IsSequence )
			new Sequence( _tween.Data ).Complete();
		else
			_tween.Complete();
	}

	/// <summary>Pauses or unpauses the animation.</summary>
	public void SetPaused( bool isPaused )
	{
		if ( !CanControl || !IsAlive )
			return;

		var tween = _tween;
		tween.IsPaused = isPaused;
	}
}
