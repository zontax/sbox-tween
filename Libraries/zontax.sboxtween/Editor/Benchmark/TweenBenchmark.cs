using System.Diagnostics;
using System.Text;
using System.Threading;

namespace SboxTween.Editor;

/// <summary>
/// Measures how fast animations are created and updated, and how much they allocate.
/// Runs in a temporary scene with its own <see cref="TweenManager"/>, so it doesn't touch the open scenes.
/// </summary>
public static class TweenBenchmark
{
	/// <param name="Milliseconds">Total time for creation, time per frame for updates.</param>
	/// <param name="NanosecondsPerItem">Time per animation.</param>
	/// <param name="AllocatedBytes">Managed allocations of the measured run, on this thread.</param>
	public record Result( string Name, int Count, double Milliseconds, double NanosecondsPerItem, long AllocatedBytes );

	sealed class Box
	{
		public float Value;
	}

	const float FrameTime = 0.016666668f;

	public static List<Result> Run( int count = 10000, int frames = 60 )
	{
		var results = new List<Result>();
		var scene = new Scene();
		using ( scene.Push() )
		{
			var manager = new TweenManager( scene );
			using ( TweenManager.Push( manager ) )
			{
				manager.SetTweensCapacity( count * 2 );
				var box = new Box();

				results.Add( MeasureCreate( manager, count, "Create Tween.Custom", () =>
				{
					for ( int i = 0; i < count; i++ )
						Tween.Custom( box, 0, 1, 100, static ( b, v ) => b.Value = v );
				} ) );

				results.Add( MeasureUpdate( manager, count, frames, "Update Tween.Custom", () =>
				{
					for ( int i = 0; i < count; i++ )
						Tween.Custom( box, 0, 1, 100, static ( b, v ) => b.Value = v, Ease.InOutSine );
				} ) );

				var objects = new GameObject[count];
				for ( int i = 0; i < count; i++ )
					objects[i] = new GameObject( true, "Benchmark" );

				results.Add( MeasureUpdate( manager, count, frames, "Update LocalPosition", () =>
				{
					for ( int i = 0; i < count; i++ )
						Tween.LocalPosition( objects[i], Vector3.Zero, new Vector3( 100, 0, 0 ), 100 );
				} ) );

				results.Add( MeasureUpdate( manager, count, frames, "Update LocalRotation", () =>
				{
					for ( int i = 0; i < count; i++ )
						Tween.LocalRotation( objects[i], Rotation.Identity, Rotation.FromYaw( 170 ), 100 );
				} ) );

				int sequences = count / 4;
				results.Add( MeasureUpdate( manager, sequences * 4, frames, "Update Sequence (4 children)", () =>
				{
					for ( int i = 0; i < sequences; i++ )
					{
						Sequence.Create( cycles: -1, cycleMode: Sequence.SequenceCycleMode.Yoyo )
							.Chain( Tween.Custom( box, 0, 1, 0.5f, static ( b, v ) => b.Value = v ) )
							.Group( Tween.Custom( box, 0, 1, 0.5f, static ( b, v ) => b.Value = v ) )
							.Chain( Tween.Custom( box, 0, 1, 0.5f, static ( b, v ) => b.Value = v ) );
					}
				} ) );

				int panelCount = count / 10;
				var panels = new Panel[panelCount];
				for ( int i = 0; i < panelCount; i++ )
					panels[i] = new Panel();

				results.Add( MeasureUpdate( manager, panelCount, frames, "Update panel opacity", () =>
				{
					foreach ( var panel in panels )
						panel.TweenOpacity( 0, 1, 100 );
				} ) );

				results.Add( MeasureUpdate( manager, panelCount, frames, "Update panel translate", () =>
				{
					foreach ( var panel in panels )
						panel.TweenTranslate( Vector2.Zero, new Vector2( 100, 0 ), 100 );
				} ) );

				results.Add( MeasureAwait( manager, box ) );

				foreach ( var go in objects )
					go.Destroy();
			}

			manager.Dispose();
		}

		scene.Destroy();
		return results;
	}

	static void Frame( TweenManager manager )
	{
		manager.Update( UpdateType.Update, FrameTime, FrameTime );
		manager.Update( UpdateType.LateUpdate, FrameTime, FrameTime );
		manager.ApplyStartValues( UpdateType.Update );
		manager.ApplyStartValues( UpdateType.LateUpdate );
	}

	static void Clear( TweenManager manager )
	{
		manager.StopAll();
		Frame( manager );
	}

	static Result MeasureCreate( TweenManager manager, int count, string name, Action create )
	{
		// Warm up the pool and the JIT
		create();
		Clear( manager );
		GC.Collect();

		// The stopwatch is created before counting, it allocates itself
		var watch = new Stopwatch();
		long bytes = GC.GetAllocatedBytesForCurrentThread();
		watch.Start();
		create();
		watch.Stop();
		bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;

		Clear( manager );
		return new Result( name, count, watch.Elapsed.TotalMilliseconds, watch.Elapsed.TotalMilliseconds * 1e6 / count, bytes );
	}

	static Result MeasureUpdate( TweenManager manager, int count, int frames, string name, Action create )
	{
		create();
		// Warm up for as long as the measurement, so one-time costs like the first cycle crossing are excluded
		for ( int i = 0; i < frames; i++ )
			Frame( manager );
		GC.Collect();

		// The stopwatch is created before counting, it allocates itself
		var watch = new Stopwatch();
		long bytes = GC.GetAllocatedBytesForCurrentThread();
		watch.Start();
		for ( int i = 0; i < frames; i++ )
			Frame( manager );
		watch.Stop();
		bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;

		Clear( manager );
		double perFrame = watch.Elapsed.TotalMilliseconds / frames;
		return new Result( name, count, perFrame, perFrame * 1e6 / count, bytes );
	}

	static Result MeasureAwait( TweenManager manager, Box box )
	{
		const int loops = 100;
		const int awaitsPerLoop = 10;
		var tasks = new Task[loops];

		async Task Loop()
		{
			for ( int i = 0; i < awaitsPerLoop; i++ )
				await Tween.Custom( box, 0, 1, 0.01f, static ( b, v ) => b.Value = v );
		}

		void Run()
		{
			for ( int i = 0; i < loops; i++ )
				tasks[i] = Loop();

			for ( int frame = 0; frame < awaitsPerLoop * 4 && !tasks.All( static t => t.IsCompleted ); frame++ )
				Frame( manager );
		}

		// Without a synchronization context Task continuations run inline, like the continuations of Tween.Awaiter
		var context = SynchronizationContext.Current;
		SynchronizationContext.SetSynchronizationContext( null );
		try
		{
			Run();
			// The stopwatch is created before counting, it allocates itself
			var watch = new Stopwatch();
			long bytes = GC.GetAllocatedBytesForCurrentThread();
			watch.Start();
			Run();
			watch.Stop();
			bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;

			Clear( manager );
			const int count = loops * awaitsPerLoop;
#if STANDALONE
			const string name = "Await in loop (struct)";
#else
			const string name = "Await in loop (Task)";
#endif
			return new Result( name, count, watch.Elapsed.TotalMilliseconds, watch.Elapsed.TotalMilliseconds * 1e6 / count, bytes );
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext( context );
		}
	}

	public static string Format( IEnumerable<Result> results )
	{
		var sb = new StringBuilder();
		sb.AppendLine( $"{"Scenario",-30}{"Count",8}{"ms",12}{"ns/item",12}{"Allocated",14}" );
		foreach ( var r in results )
			sb.AppendLine( $"{r.Name,-30}{r.Count,8}{r.Milliseconds,12:0.000}{r.NanosecondsPerItem,12:0}{FormatBytes( r.AllocatedBytes ),14}" );
		return sb.ToString();
	}

	static string FormatBytes( long bytes ) => bytes switch
	{
		< 1024 => $"{bytes} B",
		< 1024 * 1024 => $"{bytes / 1024.0:0.0} KB",
		_ => $"{bytes / (1024.0 * 1024):0.0} MB"
	};
}
