namespace SboxTween.UnitTests;

[TestClass]
public class TestInit
{
	public static TestAppSystem AppSystem;

	[AssemblyInitialize]
	public static void AssemblyInitialize( TestContext context )
	{
		AppSystem = new TestAppSystem();
		AppSystem.Init();
	}

	[AssemblyCleanup]
	public static void AssemblyCleanup()
	{
		AppSystem.Shutdown();
	}
}
