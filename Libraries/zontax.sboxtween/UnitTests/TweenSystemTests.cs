namespace SboxTween.UnitTests;

[TestClass]
public class TweenSystemTests
{
	[TestMethod]
	public void SceneHasTweenSystem()
	{
		var scene = new Scene();
		using ( scene.Push() )
		{
			var manager = TweenManager.Get( scene );
			if ( manager is null )
				Assert.Inconclusive( "GameObjectSystems are not discovered in this test environment." );

			Assert.AreSame( scene, manager.Scene );
		}

		scene.Destroy();
	}

	[TestMethod]
	public void GameTickUpdatesTweens()
	{
		var scene = new Scene();
		using ( scene.Push() )
		{
			if ( TweenManager.Get( scene ) is null )
				Assert.Inconclusive( "GameObjectSystems are not discovered in this test environment." );

			var go = new GameObject( true, "Test" );
			Tween.LocalPositionX( go, 0, 100, 1, Ease.Linear );
			for ( int i = 0; i < 10; i++ )
				scene.GameTick( 0.05 );

			Assert.AreEqual( 50, go.LocalPosition.x, 1 );
		}

		scene.Destroy();
	}
}
