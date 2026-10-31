namespace SboxTween;

/// <summary>
/// Owns and updates all animations of one scene. Every scene gets its own manager through <see cref="TweenSystem"/>.
/// Can also be created manually and updated with <see cref="Update"/>, e.g. in unit tests.
/// </summary>
public sealed class TweenManager : IDisposable
{
	internal static readonly object DummyTarget = new();
	static long _lastId = 1;
	static TweenManager _scopeOverride;

	/// <summary>The scene this manager belongs to. Null for standalone managers.</summary>
	public Scene Scene { get; }

	/// <summary>True after the owning scene was destroyed. New animations are not created anymore.</summary>
	public bool IsDisposed { get; private set; }

	/// <summary>The highest number of simultaneously alive animations since the manager was created.</summary>
	public int MaxAliveTweens { get; private set; }

	internal readonly Random Random = new();
	internal readonly Dictionary<(GameObject Target, TweenType Type), (ValueContainer StartValue, int Count)> Shakes = [];

	readonly List<TweenData>[] _running = [[], [], []];
	readonly List<TweenData>[] _pending = [[], [], []];
	readonly List<TweenData> _pool = [];
	int _tweensCount;
	internal int UpdateDepth;
	bool _completeAllRequested;
	object _completeAllRequestedTarget;

	public TweenManager( Scene scene = null )
	{
		Scene = scene;
	}

	/// <summary>The number of currently alive animations, including the ones inside sequences.</summary>
	public int TweensCount => _tweensCount;

	/// <summary>
	/// Pre-allocates pooled animations so the manager doesn't allocate at runtime.
	/// </summary>
	public void SetTweensCapacity( int capacity )
	{
		if ( capacity < 0 )
		{
			TweenLog.Error( $"Invalid capacity: {capacity}." );
			return;
		}

		int total = _tweensCount + _pool.Count;
		for ( int i = total; i < capacity; i++ )
			_pool.Add( new TweenData() );

		foreach ( var list in _running )
			list.Capacity = Math.Max( list.Capacity, capacity );
	}

	#region Resolve

	/// <summary>
	/// Returns the manager of the given scene, or null if the scene has no <see cref="TweenSystem"/>.
	/// </summary>
	public static TweenManager Get( Scene scene )
	{
		if ( !scene.IsValid() )
			return null;

		return scene.GetSystem<TweenSystem>()?.Manager;
	}

	/// <summary>
	/// While the returned scope is alive, all new animations are created in <paramref name="manager"/> regardless of their target's scene.
	/// </summary>
	public static ManagerScope Push( TweenManager manager ) => new( manager );

	/// <summary>
	/// See <see cref="Push"/>.
	/// </summary>
	public readonly struct ManagerScope : IDisposable
	{
		readonly TweenManager _previous;

		internal ManagerScope( TweenManager manager )
		{
			_previous = _scopeOverride;
			_scopeOverride = manager;
		}

		public void Dispose() => _scopeOverride = _previous;
	}

	internal static TweenManager Resolve( object target, bool logError = true )
	{
		if ( _scopeOverride is not null )
			return _scopeOverride;

		if ( target is TweenData tween )
			return tween.Manager;

		// Scene is a GameObject too
		var scene = target switch
		{
			Scene s => s,
			GameObject go => go.Scene,
			Component component => component.Scene,
			_ => null
		};

		scene ??= Game.ActiveScene;
		var manager = Get( scene );
		if ( manager is null && logError )
			TweenLog.Error( Messages.NoManager );

		return manager;
	}

	#endregion

	#region Update

	/// <summary>
	/// Advances all animations of the given update type. The engine calls it automatically through <see cref="TweenSystem"/>.
	/// </summary>
	public void Update( UpdateType updateType, float deltaTime, float unscaledDeltaTime )
	{
		if ( IsDisposed )
			return;

		updateType = SanitizeUpdateType( updateType );
		var running = _running[Index( updateType )];
		switch ( updateType )
		{
			case UpdateType.Update:
			case UpdateType.FixedUpdate:
				AddNewTweens( updateType );
				UpdateTweenList( running, deltaTime, unscaledDeltaTime );
				break;
			case UpdateType.LateUpdate:
				// LateUpdate runs in the same frame after tweens were created in OnUpdate, so only tweens from previous frames are updated
				UpdateTweenList( running, deltaTime, unscaledDeltaTime );
				AddNewTweens( updateType );
				break;
		}
	}

	/// <summary>
	/// Applies start values of the animations created this frame, so they are rendered at their start value in the same frame they were created.
	/// </summary>
	public void ApplyStartValues( UpdateType updateType )
	{
		if ( IsDisposed )
			return;

		updateType = SanitizeUpdateType( updateType );
		var running = _running[Index( updateType )];
		int oldCount = running.Count;
		AddNewTweens( updateType );
		UpdateDepth++;
		try
		{
			for ( int i = oldCount; i < running.Count; i++ )
			{
				var tween = running[i];
				if ( tween is not null
					&& tween.IsAlive
					&& !tween.StartFromCurrent
					&& tween.StartDelay == 0
					&& tween.CanManipulate()
					&& tween.ElapsedTimeTotal == 0 )
				{
					tween.SetElapsedTimeTotal( 0, true );
				}
			}
		}
		finally
		{
			UpdateDepth--;
		}

		AddNewTweens( updateType );
		ProcessRequestedCompleteAll();
	}

	void UpdateTweenList( List<TweenData> list, float deltaTime, float unscaledDeltaTime )
	{
		UpdateDepth++;
		int count = list.Count;
		int numRemoved = 0;
		try
		{
			// Process tweens in the order of creation, so the latest tween on the same value overwrites the previous ones
			for ( int i = 0; i < count; i++ )
			{
				var tween = list[i];
				int newIndex = i - numRemoved;
				if ( tween is null )
				{
					numRemoved++;
					continue;
				}

				if ( tween.UpdateAndCheckIfRunning( tween.UseUnscaledTime ? unscaledDeltaTime : deltaTime ) )
				{
					if ( i != newIndex )
					{
						list[newIndex] = tween;
						list[i] = null;
					}
				}
				else
				{
					tween.ReportOnFinally();
					Release( tween );
					list[i] = null;
					numRemoved++;
				}
			}
		}
		catch ( Exception e )
		{
			TweenLog.Exception( e, "SboxTween recovered from an exception while updating animations." );
			list.RemoveAll( x => x is null );
			numRemoved = 0;
		}
		finally
		{
			UpdateDepth--;
		}

		if ( numRemoved > 0 )
			list.RemoveRange( list.Count - numRemoved, numRemoved );

		ProcessRequestedCompleteAll();
	}

	internal void AddNewTweens( UpdateType updateType )
	{
		var pending = _pending[Index( updateType )];
		if ( pending.Count == 0 )
			return;

		var running = _running[Index( updateType )];
		foreach ( var tween in pending )
		{
			tween.ModificationIsNotAllowed = true;
			running.Add( tween );
		}

		pending.Clear();
	}

	internal void AddAllNewTweens()
	{
		AddNewTweens( UpdateType.Update );
		AddNewTweens( UpdateType.LateUpdate );
		AddNewTweens( UpdateType.FixedUpdate );
	}

	internal void RequestCompleteAll( object target )
	{
		_completeAllRequested = true;
		_completeAllRequestedTarget = target;
	}

	internal void ProcessRequestedCompleteAll()
	{
		if ( !_completeAllRequested )
			return;

		_completeAllRequested = false;
		var target = _completeAllRequestedTarget;
		_completeAllRequestedTarget = null;
		CompleteAll( target );
	}

	internal readonly struct UpdateDepthScope : IDisposable
	{
		readonly TweenManager _manager;

		internal UpdateDepthScope( TweenManager manager )
		{
			_manager = manager;
			manager.UpdateDepth++;
		}

		public void Dispose()
		{
			_manager.UpdateDepth--;
			_manager.ProcessRequestedCompleteAll();
		}
	}

	#endregion

	#region Lifetime

	internal TweenData Fetch( UpdateType updateType )
	{
		TweenData tween;
		if ( _pool.Count == 0 )
		{
			tween = new TweenData();
		}
		else
		{
			int last = _pool.Count - 1;
			tween = _pool[last];
			_pool.RemoveAt( last );
		}

		tween.Manager = this;
		tween.Id = _lastId;
		updateType = SanitizeUpdateType( updateType );
		tween.UpdateType = updateType;
		_pending[Index( updateType )].Add( tween );
		_tweensCount++;
		return tween;
	}

	internal Tween Animate( TweenData tween )
	{
		if ( TweenConfig.WarnZeroDuration && tween.AnimationDuration <= 0 )
			TweenLog.Warning( $"Tween duration ({tween.AnimationDuration}) <= 0. {Messages.CanBeDisabled( nameof( TweenConfig.WarnZeroDuration ) )}" );

		return Add( tween );
	}

	internal Tween Add( TweenData tween )
	{
		if ( tween.Target is null || tween.IsTargetDestroyed() )
		{
			TweenLog.Error( $"Tween's target is null or has been destroyed: {tween.GetDescription()}." );
			tween.Kill();
			_pending[Index( tween.UpdateType )].Remove( tween );
			Release( tween );
			return default;
		}

		if ( TweenConfig.WarnTweenOnDisabledTarget )
		{
			var go = tween.Target switch
			{
				GameObject g => g,
				Component c => c.GameObject,
				_ => null
			};

			if ( go is not null && !go.Active )
				TweenLog.Warning( $"Tween is started on a GameObject that is not active in the hierarchy: {go.Name}. {Messages.CanBeDisabled( nameof( TweenConfig.WarnTweenOnDisabledTarget ) )}" );
		}

		_lastId++;
		MaxAliveTweens = Math.Max( MaxAliveTweens, _tweensCount );
		return new Tween( tween );
	}

	void Release( TweenData tween )
	{
		tween.Reset();
		tween.Manager = null;
		_pool.Add( tween );
		_tweensCount--;
	}

	/// <summary>
	/// Stops all animations without invoking callbacks and prevents new ones from being created.
	/// </summary>
	public void Dispose()
	{
		if ( IsDisposed )
			return;

		IsDisposed = true;
		AddAllNewTweens();
		foreach ( var list in _running )
		{
			foreach ( var tween in list )
			{
				if ( tween is null )
					continue;

				if ( tween.TweenType == TweenType.TweenAwaiter && tween.IsAlive )
					(tween.OnCompleteTarget as AwaiterSource)?.Cancel();

				tween.Kill();
			}

			list.Clear();
		}

		Shakes.Clear();
		_tweensCount = 0;
	}

	#endregion

	#region Queries

	/// <summary>
	/// Invokes <paramref name="predicate"/> on every alive animation (on <paramref name="onTarget"/> only, if provided).
	/// Returns the number of processed animations.
	/// </summary>
	internal int ProcessAll( object onTarget, Predicate<TweenData> predicate, bool allowToProcessTweensInsideSequence, bool onlyRunning = false )
	{
		int result = 0;
		for ( int i = 0; i < _running.Length; i++ )
		{
			UpdateDepth++;
			try
			{
				result += ProcessList( _running[i], onTarget, predicate, allowToProcessTweensInsideSequence );
				if ( !onlyRunning )
					result += ProcessList( _pending[i], onTarget, predicate, allowToProcessTweensInsideSequence );
			}
			finally
			{
				UpdateDepth--;
			}
		}

		return result;
	}

	static int ProcessList( List<TweenData> list, object onTarget, Predicate<TweenData> predicate, bool allowToProcessTweensInsideSequence )
	{
		int numProcessed = 0;
		int totalCount = 0;
		// Index loop because predicates can create new animations in pending lists
		for ( int i = 0; i < list.Count; i++ )
		{
			var tween = list[i];
			if ( tween is null )
				continue;

			totalCount++;
			if ( onTarget is not null )
			{
				if ( !ReferenceEquals( tween.Target, onTarget ) )
					continue;

				if ( !allowToProcessTweensInsideSequence && tween.IsInSequence )
				{
					tween.LogError( Messages.CantManipulateNested );
					continue;
				}
			}

			if ( tween.IsAlive && predicate( tween ) )
				numProcessed++;
		}

		return onTarget is null ? totalCount : numProcessed;
	}

	/// <summary>
	/// Returns the number of alive animations (on <paramref name="onTarget"/> only, if provided).
	/// </summary>
	public int GetTweensCount( object onTarget = null )
	{
		if ( onTarget is null )
			return _tweensCount;

		return ProcessAll( onTarget, static _ => true, true );
	}

	/// <inheritdoc cref="Tween.StopAll"/>
	public int StopAll( object onTarget = null )
	{
		// OnFinally() is not invoked here, the manager invokes it when it releases the stopped animations
		int result = ProcessAll( onTarget, static tween =>
		{
			if ( tween.IsInSequence )
			{
				// Nested animations are processed by the main sequence root
				if ( tween.IsMainSequenceRoot() && new Tween( tween ).TryManipulate( false ) )
					new Sequence( tween ).ReleaseTweens();
			}
			else
			{
				tween.Kill();
			}

			return true;
		}, false );

		ForceUpdateIfTargetIsNull( onTarget );
		return result;
	}

	/// <inheritdoc cref="Tween.CompleteAll"/>
	public int CompleteAll( object onTarget = null )
	{
		if ( UpdateDepth != 0 )
		{
			RequestCompleteAll( onTarget );
			return GetTweensCount( onTarget );
		}

		AddAllNewTweens();
		int result = ProcessAll( onTarget, static tween =>
		{
			if ( tween.IsInSequence )
			{
				if ( tween.IsMainSequenceRoot() )
					new Sequence( tween ).Complete();
			}
			else
			{
				new Tween( tween ).Complete();
			}

			return true;
		}, false, onlyRunning: true );

		ForceUpdateIfTargetIsNull( onTarget );
		return result;
	}

	/// <inheritdoc cref="Tween.SetPausedAll"/>
	public int SetPausedAll( bool isPaused, object onTarget = null )
	{
		if ( isPaused )
		{
			return ProcessAll( onTarget, static tween =>
			{
				// The parent Sequence controls the pause of nested animations
				if ( !tween.CanManipulate() )
					return false;

				if ( tween.IsInSequence && tween.FindMainSequence().IsCompleting )
					return false;

				return tween.TrySetPause( true );
			}, false );
		}

		return ProcessAll( onTarget, static tween => tween.TrySetPause( false ), false );
	}

	void ForceUpdateIfTargetIsNull( object onTarget )
	{
		if ( onTarget is not null || UpdateDepth != 0 )
			return;

		Update( UpdateType.Update, 0, 0 );
		Update( UpdateType.LateUpdate, 0, 0 );
		Update( UpdateType.FixedUpdate, 0, 0 );
	}

	#endregion

	static int Index( UpdateType updateType ) => (int)updateType - 1;

	internal static UpdateType SanitizeUpdateType( UpdateType updateType )
	{
		switch ( updateType )
		{
			case UpdateType.Update:
			case UpdateType.LateUpdate:
			case UpdateType.FixedUpdate:
				return updateType;
			case UpdateType.Default:
				return TweenConfig.DefaultUpdateType;
			default:
				TweenLog.Error( $"Invalid update type: {updateType}" );
				return UpdateType.Update;
		}
	}
}
