using SboxTween.Editor;

namespace SboxTween.UnitTests;

/// <summary>
/// Prints the benchmark report. Run on its own with: dotnet test --filter TestCategory=Benchmark --logger "console;verbosity=detailed"
/// </summary>
[TestClass]
public class BenchmarkTests
{
	public TestContext TestContext { get; set; }

	[TestMethod, TestCategory( "Benchmark" )]
	public void Benchmark()
	{
		var results = TweenBenchmark.Run( 10000, 60 );
		TestContext.WriteLine( TweenBenchmark.Format( results ) );

		// Steady-state updates of tweens without panel transforms must not allocate
		foreach ( var result in results.Where( x => x.Name.StartsWith( "Update" ) && !x.Name.Contains( "translate" ) ) )
			Assert.AreEqual( 0, result.AllocatedBytes, $"{result.Name} allocated {result.AllocatedBytes} bytes" );
	}
}
