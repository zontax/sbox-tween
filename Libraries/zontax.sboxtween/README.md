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

// Components, with a static method or an extension method
Tween.Color( modelRenderer, Color.Red, 0.5f );
pointLight.TweenRadius( 300, 1 );
camera.TweenFieldOfView( 90, 0.3f );

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

Use `SetCancellationToken( token )` to stop (or complete) an animation when a token is canceled. Awaiting a canceled animation throws `OperationCanceledException`, as does awaiting while the scene's `TweenManager` is disposed.

`await` goes through a `Task` (about 130 bytes per await) because the s&box whitelist doesn't allow custom awaiters. Projects that disable the whitelist and compile with the `STANDALONE` define get `Tween.Awaiter`, a struct awaiter: repeated awaits inside one async method don't allocate, and the continuation runs right from the animation update instead of being posted to the synchronization context. `ToTask()` works in both builds.

## UI panels

Panel tweens animate the inline style of a `Panel` (`panel.Style`). Every property has a static `Tween.PanelX( panel, … )` method and a `panel.TweenX( … )` extension.

```csharp
panel.TweenTranslate( new Vector2( 0, -40 ), 0.3f, Ease.OutBack );         // pixels
panel.TweenTranslate( Length.Percent( -100 ), Length.Pixels( 0 ), 0.3f );  // any units
panel.TweenScale( 1.2f, 0.2f );
panel.TweenRotate( 15, 0.2f );
panel.TweenOpacity( 0, 0.2f );
panel.TweenBackgroundColor( Color.Red, 0.2f );
panel.TweenFilterBlur( 6, 0.3f );
panel.TweenWidth( Length.Percent( 100 ), 0.5f );

panel.PunchScale( new Vector2( 0.1f, 0.1f ), 0.3f );
panel.ShakeTranslate( new Vector2( 10, 4 ), 0.4f );

// Intro and outro templates
panel.TweenSlideIn( new Vector2( 0, 40 ) );
panel.TweenPopIn();
panel.DeleteAfter( panel.TweenPopOut() );
```

How it works with the s&box UI:

- **Transform and opacity are cheap.** Translate, rotate and scale write into per-panel channels that are composed into one `transform` per frame, so several transform tweens on one panel don't overwrite each other, and the layout isn't recalculated. Width, margins, paddings, gaps and positions go through the layout engine and recalculate it every frame.
- **Inline style wins over CSS.** The animated value stays after the tween ends, so `:hover` and classes can't change that property anymore. Call `.ReleaseStyleOnComplete()` on a tween or sequence to give the property back to CSS when it ends in any way (completed, stopped, panel deleted), or `panel.ReleaseTweenedStyles()` to clear everything at once.
- **Razor.** Start tweens from code (`OnTreeFirstBuilt`, `OnTreeBuilt`, event handlers, `@ref` panels). Don't put an animated property into a razor `style="…"` attribute: razor writes it again whenever the attribute string changes. Classes are fine. If razor removes a panel, its tweens stop.
- **CSS transitions.** A `transition` on the same property restarts on every tween update and makes the tween lag. SboxTween warns about it (`TweenConfig.WarnPanelTransitions`) and marks such tweens in the Tween Debugger.
- **Lengths** keep their units; start and end values must use the same unit. Starting from the current value reads the inline style first, then the computed style.

## Components

Every component tween has a static `Tween.X( component, … )` method and a `component.TweenX( … )` extension method.

```csharp
light.TweenIntensity( 4, 0.5f );
light.TweenTemperature( 1800, 12000, 3, cycles: -1, cycleMode: CycleMode.Yoyo ); // Kelvin
sun.TweenSkyColor( Color.Black, 2 );
camera.TweenBackgroundColor( Color.Black, 1 );
text.TweenFontSize( 96, 0.4f, Ease.OutBack );

// Post-processing components on the camera
vignette.TweenIntensity( 0.8f, 0.1f );
aberration.TweenScale( 1, 0.1f );
adjustments.TweenSaturation( 0, 1 );
blur.TweenSize( 0.5f, 0.3f );
fog.TweenColor( Color.Gray, 2 );
volume.TweenBlendWeight( 1, 0.5f );
```

Supported: tint and alpha of any `ITintable` (ModelRenderer, Light, …), SpriteRenderer, TextRenderer (color, alpha, font size, scale), lights (intensity, temperature, radius, cone, ambient color, sun sky color), camera (field of view, orthographic height, near/far planes, background color), sounds, Bloom, Vignette, ColorAdjustments, ChromaticAberration, Blur, Pixelate, FilmGrain, DepthOfField, MotionBlur, Sharpen, Fog, VolumetricFogVolume and PostProcessVolume.

## Shader attributes, animgraph and morphs

Values without a property of their own are animated by name.

```csharp
// Renderer.Attributes: float, Vector3, Vector4 and Color (stored as float4)
renderer.TweenAttribute( "Dissolve", 0, 1, 1.2f );
renderer.TweenAttribute( "GlowColor", Color.Black, Color.Cyan, 0.5f );

// Animation graph parameters: float, Vector3 and Rotation
citizen.TweenParameter( "duck", 1, 0.4f );
citizen.TweenParameter( "aim_head", new Vector3( 1, -0.7f, 0 ), new Vector3( 1, 0.7f, 0 ), 2, cycles: -1, cycleMode: CycleMode.Yoyo );

// Morphs (blend shapes)
citizen.TweenMorph( "lipcornerpullerL", 1, 0.3f );
```

- A shader attribute that was never set reads as zero, and the renderer can't read the defaults from the shader or the material. Pass the start value when it matters. Set constant attributes in the shader (`Default()`) or the material rather than right after creating the renderer: the engine can recreate the scene object, and attributes set before that are lost. Tweens write every frame, so they aren't affected.
- A tweened morph stays overridden after the tween ends; call `Morphs.Clear( name )` to give it back to the animation.

## Properties by name

`Tween.Property` animates any public property known to the TypeLibrary, including `[Property]` fields of your own components. It's the fallback for things that have no dedicated method.

```csharp
skybox.TweenProperty( "Tint", Color.Black, 1.5f );
spinner.TweenProperty( "Speed", 900, 1.2f, Ease.InOutCubic );
Tween.Property( myObject, "Offset", Vector3.Zero, new Vector3( 0, 0, 50 ), 1 );
```

The property is looked up once, when the tween starts; a missing property or a different type is reported as an error. The value is boxed on every update, so this allocates; prefer a typed method or `Tween.Custom` in hot paths.

## Rigidbody

```csharp
body.TweenPosition( target, 1.2f, Ease.InOutCubic );
body.TweenRotation( Rotation.FromYaw( 90 ), 0.8f );
```

Rigidbody tweens set the velocity (or the angular velocity) every fixed update, so the body reaches each point of the curve in one physics step and collides on the way instead of teleporting through other bodies. They always use `UpdateType.FixedUpdate`; create a sequence of them with `Sequence.Create( updateType: UpdateType.FixedUpdate )`. When the tween completes, the body is placed exactly at the end value and stopped; a stopped tween leaves the body with its current velocity. Use `Rigidbody.Locking` to keep the other axes still.

## Typewriter

```csharp
sign.TweenTypewriter( "Hello, world!", 1.5f );           // TextRenderer
label.TweenTypewriterAtSpeed( "Loading...", 30 );        // UI Label, characters per second
Tween.Typewriter( sign, null, 1 );                       // types the current text
```

The typing pauses after sentences (`. ! ?`) and, shorter, after clauses (`, ; :`) when they're followed by a space; pass `pauseOnPunctuation: false` to type evenly. The default ease is linear. Every new visible character sets the text once, which allocates a string.

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
- **Developer → Regenerate Tween API** regenerates `Code/Api/*.Generated.cs` and `Code/Internal/TweenData.Generated.cs`.
- **Developer → Run Benchmark** measures creation, updates and allocations in a temporary scene.

## Performance

Starting and updating animations doesn't allocate once the pool is warm (`TweenManager.SetTweensCapacity()` warms it up front). `AllocationTests` check that for custom, transform, sequence, delay, shake, additive, clock and panel style tweens.

Benchmark in the editor, 10 000 animations, 60 frames:

| Scenario | ns per animation | Allocated |
| --- | ---: | ---: |
| Create `Tween.Custom` | 82 | 0 B |
| Update `Tween.Custom` | 68 | 0 B |
| Update `LocalPosition` | 116 | 0 B |
| Update `LocalRotation` | 156 | 0 B |
| Update sequence child | 47 | 0 B |
| Update panel opacity | 92 | 0 B |
| Update panel translate | 415 | ~190 KB per frame for 1000 panels |

Panel transforms allocate inside the engine: `PanelTransform` is an immutable list, so every write creates a new one. SboxTween writes at most one transform per panel per frame.

## Hotload

Running animations survive a hotload. Callbacks stored by the library are named methods rather than lambdas, because hotload matches methods by name and can fail to upgrade a lambda of a running animation. Callbacks from your code are upgraded together with your assembly; if a callback's type no longer matches after a reload, `OnUpdate` logs an error and isn't called again instead of throwing every frame.

The same applies to your lambdas: when you edit the code around them, a running tween can lose its lambda callback ("Unable to find matching substitution for a lambda method"). Endless loops that restart themselves from `OnComplete` should use a named method:

```csharp
Tween.Delay( this, 2 ).OnComplete( this, OnTick ); // instead of static demo => demo.Tick()
static void OnTick( MyComponent self ) => self.Tick();
```

## Architecture notes

- Every scene gets its own `TweenManager` through `TweenSystem`. Tweens die with their scene.
- `UpdateType.Update` runs before components' `OnUpdate`, `LateUpdate` runs after it, `FixedUpdate` runs on fixed steps.
- Destroying a GameObject with running tweens is safe: they stop on the next update.
- Repetitive overloads, extension methods and simple getters/setters are generated by `Editor/CodeGen/TweenCodeGenerator.cs`. To add an animatable property, add a `TweenType` and a row to the `Properties` table: a row with `Member` gets a generated getter and setter (`Code/Internal/TweenData.Generated.cs`), other rows need one in `Code/Internal/TweenData.Values.cs`. Then run **Developer → Regenerate Tween API**, or `CodeGeneratorTests.RegenerateWhenRequested` with the environment variable `SBOXTWEEN_REGENERATE=1`. A unit test fails if the generated files are out of date.
- Unit tests: `dotnet test` in `UnitTests`. Run them for the STANDALONE build too: `dotnet test -p:DefineConstants="SANDBOX%3BSTANDALONE"`. Benchmark: `dotnet test -c Release --filter TestCategory=Benchmark --logger "console;verbosity=detailed"`.
