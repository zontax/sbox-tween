#if STANDALONE
using System.Runtime.CompilerServices;
using System.Threading;

namespace SboxTween;

public readonly partial struct Tween
{
	/// <summary>
	/// The awaiter of STANDALONE builds, where the whitelist allows custom awaiters. Awaiting doesn't create a Task,
	/// so repeated awaits inside one async method don't allocate. The continuation runs right from the animation update.
	/// <br/><br/>
	/// Like the Task-based awaiter, it throws <see cref="OperationCanceledException"/> if the animation's CancellationToken was canceled,
	/// or if the <see cref="TweenManager"/> was disposed while waiting.
	/// </summary>
	public readonly struct Awaiter : ICriticalNotifyCompletion
	{
		readonly Tween _tween;
		readonly CancellationToken _cancellationToken;
		readonly TweenManager _manager;

		internal Awaiter( Tween tween )
		{
			_tween = tween;
			_cancellationToken = AwaiterSource.GetCancellationToken( tween );
			_manager = tween.Data?.Manager;
		}

		public bool IsCompleted => !_tween.IsAliveInternal;

		public void OnCompleted( Action continuation ) => UnsafeOnCompleted( continuation );

		public void UnsafeOnCompleted( Action continuation )
		{
			var wait = AwaiterSource.CreateWait( _tween, _cancellationToken );
			if ( wait.IsAliveInternal )
				wait.Data.OnComplete( continuation, false );
			else
				continuation();
		}

		public void GetResult()
		{
			if ( _manager is { IsDisposed: true } )
				throw new OperationCanceledException( "The TweenManager was disposed while awaiting the animation." );

			_cancellationToken.ThrowIfCancellationRequested();
		}
	}
}
#endif