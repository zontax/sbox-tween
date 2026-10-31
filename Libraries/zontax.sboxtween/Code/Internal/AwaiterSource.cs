using System.Threading;

namespace SboxTween;

/// <summary>
/// Completes a Task when the awaited animation is no longer alive.
/// Implemented with an infinite helper animation that watches the awaited one, so all ways an animation can end are covered.
/// </summary>
internal sealed class AwaiterSource
{
	readonly TaskCompletionSource _source = new();
	readonly CancellationToken _cancellationToken;

	AwaiterSource( CancellationToken cancellationToken )
	{
		_cancellationToken = cancellationToken;
	}

	internal static Task Create( Tween awaited )
	{
		var cancellationToken = GetCancellationToken( awaited );
		var wait = CreateWait( awaited, cancellationToken );
		if ( wait.IsAliveInternal )
		{
			var source = new AwaiterSource( cancellationToken );
			wait.Data.OnComplete( source, CompleteSource, false );
			return source._source.Task;
		}

		return cancellationToken.IsCancellationRequested ? Task.FromCanceled( cancellationToken ) : Task.CompletedTask;
	}

	internal static CancellationToken GetCancellationToken( Tween awaited )
	{
		var data = awaited.Data;
		return data is not null && data.Id == awaited.Id && data.HasCancellationToken ? data.CancellationToken : CancellationToken.None;
	}

	/// <summary>
	/// The helper animation that completes when <paramref name="awaited"/> ends, or a dead tween if there is nothing to wait for.
	/// </summary>
	internal static Tween CreateWait( Tween awaited, CancellationToken cancellationToken )
	{
		if ( !awaited.IsAliveInternal || !awaited.TryManipulate() )
			return default;

		if ( cancellationToken.IsCancellationRequested )
		{
			Tween.CancelTweenOrSequence( awaited );
			return default;
		}

		return Tween.CreateAwaiterTween( awaited.Data );
	}

	static void CompleteSource( AwaiterSource source ) => source.Complete();

	void Complete()
	{
		if ( _cancellationToken.IsCancellationRequested )
			_source.TrySetCanceled( _cancellationToken );
		else
			_source.TrySetResult();
	}

	internal void Cancel() => _source.TrySetCanceled();
}
