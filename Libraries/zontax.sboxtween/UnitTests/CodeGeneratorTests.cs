using SboxTween.Editor;

namespace SboxTween.UnitTests;

[TestClass]
public class CodeGeneratorTests
{
	static string ApiDirectory( [CallerFilePath] string testFile = "" ) => Path.GetFullPath( Path.Combine( Path.GetDirectoryName( testFile ), "..", "Code", "Api" ) );

	[TestMethod]
	public void TweenFileIsUpToDate() => AssertSame( TweenCodeGenerator.TweenFileName, TweenCodeGenerator.GenerateTween() );

	[TestMethod]
	public void ExtensionsFileIsUpToDate() => AssertSame( TweenCodeGenerator.ExtensionsFileName, TweenCodeGenerator.GenerateExtensions() );

	static void AssertSame( string fileName, string generated )
	{
		var path = Path.Combine( ApiDirectory(), fileName );
		var actual = File.ReadAllText( path );
		if ( actual == generated )
			return;

		var actualLines = actual.Split( "\r\n" );
		var generatedLines = generated.Split( "\r\n" );
		int line = 0;
		while ( line < actualLines.Length && line < generatedLines.Length && actualLines[line] == generatedLines[line] )
			line++;

		Assert.Fail( $"{fileName} is out of date, run Editor → Sbox Tween → Developer → Regenerate Tween API. First difference at line {line + 1}:\n" +
			$"file:      {(line < actualLines.Length ? actualLines[line] : "<end>")}\n" +
			$"generated: {(line < generatedLines.Length ? generatedLines[line] : "<end>")}" );
	}
}
