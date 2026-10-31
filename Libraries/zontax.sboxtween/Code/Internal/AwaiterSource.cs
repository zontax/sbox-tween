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
		var cancellationToken = CancellationToken.None;
		if ( awaited.Data is not null && awaited.Data.Id == awaited.Id && awaited.Data.HasCancellationToken )
			cancellationToken = awaited.Data.CancellationToken;

		if ( awaited.IsAliveInternal && awaited.TryManipulate() )
		{
			if ( cancellationToken.IsCancellationRequested )
			{
				Tween.CancelTweenOrSequence( awaited );
			}
			else
			{
				var wait = Tween.CreateAwaiterTween( awaited.Data );
				if ( wait.IsAliveInternal )
				{
					var source = new AwaiterSource( cancellationToken );
					wait.Data.OnComplete( source, static s => s.Complete(), false );
					return source._source.Task;
				}
			}
		}

		return cancellationToken.IsCancellationRequested ? Task.FromCanceled( cancellationToken ) : Task.CompletedTask;
	}

	void Complete()
	{
		if ( _cancellationToken.IsCancellationRequested )
			_source.TrySetCanceled( _cancellationToken );
		else
			_source.TrySetResult();
	}

	internal void Cancel() => _source.TrySetCanceled();
}
