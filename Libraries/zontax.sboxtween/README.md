# Sbox Tween Library

Fast, allocation-friendly tweens for s&box. A port of [PrimeTween](https://github.com/KyryloKuzyk/PrimeTween) to the s&box scene system.

Open `scenes/tween_demo.scene` of the example project and press Play to see the showcase.

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

## Waits

Suspend a Sequence until a condition is met. Waits don't change the Sequence duration and work on sequences with 1 cycle and linear ease.

```csharp
Sequence.Create()
	.Chain( Tween.LocalPositionZ( door, 100, 1 ) )
	.ChainWaitWhile( player, static p => p.IsInDoorway, timeout: 5 )
	.Chain( Tween.LocalPositionZ( door, 0, 1 ) );
```

## Additive tweens

Additive tweens animate deltas, so several of them blend on the same value:

```csharp
// Each call adds a quarter turn; fast calls stack smoothly
Tween.LocalRotationAdditive( GameObject, new Angles( 0, 90, 0 ), 0.5f, Ease.OutBack );
Tween.LocalPositionAdditive( GameObject, Vector3.Up * 20, 0.3f, cycles: 2, cycleMode: CycleMode.Yoyo );

Tween.CustomAdditive( this, 10f, new TweenSettings( 1 ), static ( self, delta ) => self.Value += delta, static self => self.Value );
```

Pass `getCurrentValue` to custom additive tweens: it lets the tween compensate float rounding so the value doesn't drift.

## Clock

A `Tween.Clock` controls the time of many animations at once:

```csharp
var clock = new Tween.Clock();
Tween.LocalPositionX( go, 100, 1 ).SetClock( clock );
Sequence.Create().Chain( Tween.LocalScale( go, 2, 1 ) ).SetClock( clock );
clock.TimeScale = 0.5f; // slows down both

// A manual clock advances only by Tick()
var manual = new Tween.Clock { IsManual = true };
manual.Tick( Time.Delta );
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

## Editor tools

The **Editor → Sbox Tween** menu:

- **Tween Debugger** opens the *Tweens* dock: every alive animation of the playing scene, with sequence children nested under their parent, progress, cycles and pause state. Click a target to select it, use the row buttons to pause, complete or stop an animation. *Max alive* helps to pick a value for `TweenManager.SetTweensCapacity()`.
- **Pause / Resume / Complete / Stop All Tweens** act on the playing scene.
- **Documentation** opens this file.
- **Developer → Regenerate Tween API** regenerates `Code/Api/*.Generated.cs`.

## Architecture notes

- Every scene gets its own `TweenManager` through `TweenSystem`. Tweens die with their scene.
- `UpdateType.Update` runs before components' `OnUpdate`, `LateUpdate` runs after it, `FixedUpdate` runs on fixed steps.
- Destroying a GameObject with running tweens is safe: they stop on the next update.
- Repetitive overloads are generated by `Editor/CodeGen/TweenCodeGenerator.cs`. To add an animatable property, add a row to its `Properties` table and a `TweenType` with a getter/setter in `Code/Internal/TweenData.Values.cs`, then run **Developer → Regenerate Tween API**. A unit test fails if the generated files are out of date.
- Unit tests: `dotnet test` in `UnitTests`.
