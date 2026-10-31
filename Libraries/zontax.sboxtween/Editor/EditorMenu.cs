namespace SboxTween.Editor;

public static class EditorMenu
{
	[Menu( "Editor", "Sbox Tween/Menu Option" )]
	public static void OpenMyMenu()
	{
		EditorUtility.DisplayDialog( "It worked!", "This is being called from Sbox Tween library's editor code!" );
	}
}
