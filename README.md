# Sbox Tween Library

Fast, allocation-friendly tweens for s&box. A port of [PrimeTween](https://github.com/KyryloKuzyk/PrimeTween) to the s&box scene system.

The library lives in `Libraries/zontax.sboxtween`. Open `scenes/tween_demo.scene` and press Play to see the showcase.

## Animations

```csharp
using SboxTween;

// Animate local Z position to 100 in 1 second
Tween.LocalPositionZ( GameObject, endValue: 100, duration: 1, ease: Ease.OutBack );

// The same with an extension method
GameObject.TweenLocalPositionZ( 100, 1, Ease.OutBack );

// Rotate along the shortest path, or spin a full circle with Euler angles
Tween.LocalRotation( GameObject, new Angles( 0, 90, 0 ), 1 );
Tween.LocalAngles( GameObject, Angles.Zero, new Angles( 0, 360, 0 ), 1, cycles: -1 );

// Components
Tween.Color( modelRenderer, Color.Red, 0.5f );
Tween.LightRadius( pointLight, 300, 1 );
Tween.CameraFieldOfView( camera, 90, 0.3f );

// Anything else
Tween.Custom( this, 0, 10, 1, static ( self, value ) => self.Value = value );
```

## Shakes, delays, callbacks

```csharp
Tween.ShakeCamera( camera, strengthFactor: 0.5f );
Tween.PunchLocalScale( GameObject, new Vector3( 0.3f, 0.3f, -0.2f ), 0.4f );

Tween.Delay( this, 1, static self => self.DoSomething() );
Tween.LocalScale( GameObject, 0, 0.5f ).OnComplete( GameObject, static go => go.Destroy() );
```

## Cycles

```csharp
// -1 repeats forever. CycleMode: Restart, Yoyo, Incremental, Rewind
Tween.LocalPositionZ( GameObject, 50, 0.5f, cycles: -1, cycleMode: CycleMode.Yoyo );
```

## Sequences

```csharp
Sequence.Create( cycles: 2, Sequence.SequenceCycleMode.Yoyo )
	.Group( Tween.LocalPositionX( GameObject, 100, 1.5f ) )
	.Group( Tween.LocalScale( GameObject, 2, 0.5f ) )      // runs in parallel
	.Chain( Tween.LocalRotation( GameObject, new Angles( 0, 45, 0 ), 1 ) ) // runs after both
	.ChainDelay( 1 )
	.ChainCallback( () => Log.Info( "Cycle completed" ) )
	.Insert( 0.5f, Tween.Color( renderer, Color.Red, 0.5f ) );
```

## Async/await

```csharp
async Task Animate()
{
	await Tween.LocalPositionZ( GameObject, 100, 0.5f );
	await Tween.Delay( 1 );
	await Sequence.Create().Chain( ... );
}
```

Use `SetCancellationToken( token )` to stop (or complete) an animation when a token is canceled.

## Controlling tweens

```csharp
var tween = Tween.LocalPositionX( GameObject, 100, 1 );
if ( tween.IsAlive )
	Log.Info( tween.Progress );

tween.IsPaused = true;
tween.TimeScale = 2;
tween.Stop();      // keeps the current value
tween.Complete();  // jumps to the end value

Tween.StopAll( onTarget: GameObject );
Tween.GlobalTimeScale( 0.2f, 0.5f ); // animates Scene.TimeScale
```

## Inspector integration

`TweenSettings`, `TweenSettings<T>` and `ShakeSettings` can be exposed as `[Property]` and tweaked in the inspector:

```csharp
[Property] public TweenSettings<float> Hover { get; set; } = new( 0, 40, 1, Ease.InOutSine );

Tween.LocalPositionZ( GameObject, Hover );
```

## Architecture notes

- Every scene gets its own `TweenManager` through `TweenSystem`. Tweens die with their scene.
- `UpdateType.Update` runs before components' `OnUpdate`, `LateUpdate` runs after it, `FixedUpdate` runs on fixed steps.
- Destroying a GameObject with running tweens is safe: they stop on the next update.
- Unit tests: `dotnet test` in `Libraries/zontax.sboxtween/UnitTests`.
