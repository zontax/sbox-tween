using System.IO;
using System.Text;

namespace SboxTween.Editor;

/// <summary>
/// Generates the repetitive overloads of <see cref="Tween"/> and <see cref="GameObjectTweenExtensions"/>.
/// To add a new animatable property, add a row to <see cref="Properties"/> and a <c>TweenType</c> with its getter/setter in TweenData.Values.cs,
/// then run Editor → Sbox Tween → Developer → Regenerate Tween API.
/// </summary>
public static class TweenCodeGenerator
{
	public const string TweenFileName = "Tween.Generated.cs";
	public const string ExtensionsFileName = "GameObjectTweenExtensions.Generated.cs";

	const string NewLine = "\r\n";
	const string Common = "TweenEasing ease = default, int cycles = 1, CycleMode cycleMode = CycleMode.Restart, float startDelay = 0, float endDelay = 0, bool useUnscaledTime = false";
	const string CommonArgs = "duration, ease, cycles, cycleMode, startDelay, endDelay, useUnscaledTime";

	/// <param name="Name">Method name, e.g. LocalPositionX.</param>
	/// <param name="Target">Target type.</param>
	/// <param name="Type">Animated value type.</param>
	/// <param name="TweenType">The internal TweenType that reads and writes the value.</param>
	/// <param name="Extension">Also generate a GameObject extension method.</param>
	/// <param name="AcceptsAngles">Rotation tweens also accept TweenSettings&lt;Angles&gt;.</param>
	/// <param name="StartRequired">Euler angles can't be read reliably, so only overloads with a start value are generated.</param>
	public record PropertyInfo( string Name, string Target, string Type, string TweenType, string Summary, bool Extension = false, bool AcceptsAngles = false, bool StartRequired = false );

	public record ValueTypeInfo( string Type, string TweenType, string Suffix );

	public static readonly PropertyInfo[] Properties =
	[
		new( "LocalPosition", "GameObject", "Vector3", "LocalPosition", "Animates the local position of a GameObject.", Extension: true ),
		new( "LocalPositionX", "GameObject", "float", "LocalPositionX", "Animates the local X position of a GameObject.", Extension: true ),
		new( "LocalPositionY", "GameObject", "float", "LocalPositionY", "Animates the local Y position of a GameObject.", Extension: true ),
		new( "LocalPositionZ", "GameObject", "float", "LocalPositionZ", "Animates the local Z position of a GameObject.", Extension: true ),
		new( "WorldPosition", "GameObject", "Vector3", "WorldPosition", "Animates the world position of a GameObject.", Extension: true ),
		new( "WorldPositionX", "GameObject", "float", "WorldPositionX", "Animates the world X position of a GameObject.", Extension: true ),
		new( "WorldPositionY", "GameObject", "float", "WorldPositionY", "Animates the world Y position of a GameObject.", Extension: true ),
		new( "WorldPositionZ", "GameObject", "float", "WorldPositionZ", "Animates the world Z position of a GameObject.", Extension: true ),
		new( "LocalRotation", "GameObject", "Rotation", "LocalRotation", "Animates the local rotation of a GameObject using spherical interpolation (the shortest path).", Extension: true, AcceptsAngles: true ),
		new( "WorldRotation", "GameObject", "Rotation", "WorldRotation", "Animates the world rotation of a GameObject using spherical interpolation (the shortest path).", Extension: true, AcceptsAngles: true ),
		new( "LocalAngles", "GameObject", "Angles", "LocalAngles", "Animates the local rotation of a GameObject by interpolating Euler angles. Unlike LocalRotation, it can rotate more than 180 degrees, e.g. a full 360 spin. Requires a start value.", Extension: true, StartRequired: true ),
		new( "WorldAngles", "GameObject", "Angles", "WorldAngles", "Animates the world rotation of a GameObject by interpolating Euler angles. Unlike WorldRotation, it can rotate more than 180 degrees, e.g. a full 360 spin. Requires a start value.", Extension: true, StartRequired: true ),
		new( "LocalScale", "GameObject", "Vector3", "LocalScale", "Animates the local scale of a GameObject.", Extension: true ),
		new( "LocalScale", "GameObject", "float", "LocalScaleUniform", "Animates the uniform local scale of a GameObject. The start value is taken from LocalScale.x.", Extension: true ),
		new( "LocalScaleX", "GameObject", "float", "LocalScaleX", "Animates the local X scale of a GameObject.", Extension: true ),
		new( "LocalScaleY", "GameObject", "float", "LocalScaleY", "Animates the local Y scale of a GameObject.", Extension: true ),
		new( "LocalScaleZ", "GameObject", "float", "LocalScaleZ", "Animates the local Z scale of a GameObject.", Extension: true ),
		new( "WorldScale", "GameObject", "Vector3", "WorldScale", "Animates the world scale of a GameObject.", Extension: true ),

		new( "Color", "Component.ITintable", "Color", "TintableColor", "Animates the tint color of a component (ModelRenderer, Light, MeshComponent, ParticleEffect, etc.)." ),
		new( "Alpha", "Component.ITintable", "float", "TintableAlpha", "Animates the alpha of the tint color of a component (ModelRenderer, Light, MeshComponent, ParticleEffect, etc.)." ),
		new( "Color", "SpriteRenderer", "Color", "SpriteColor", "Animates the color of a SpriteRenderer." ),
		new( "Alpha", "SpriteRenderer", "float", "SpriteAlpha", "Animates the alpha of a SpriteRenderer." ),
		new( "Color", "TextRenderer", "Color", "TextColor", "Animates the color of a TextRenderer." ),
		new( "Alpha", "TextRenderer", "float", "TextAlpha", "Animates the alpha of a TextRenderer." ),
		new( "LightRadius", "PointLight", "float", "PointLightRadius", "Animates the radius of a PointLight." ),
		new( "LightRadius", "SpotLight", "float", "SpotLightRadius", "Animates the radius of a SpotLight." ),
		new( "SpotLightConeOuter", "SpotLight", "float", "SpotLightConeOuter", "Animates the outer cone angle of a SpotLight." ),
		new( "SpotLightConeInner", "SpotLight", "float", "SpotLightConeInner", "Animates the inner cone angle of a SpotLight." ),
		new( "CameraFieldOfView", "CameraComponent", "float", "CameraFieldOfView", "Animates the field of view of a camera." ),
		new( "CameraOrthographicHeight", "CameraComponent", "float", "CameraOrthographicHeight", "Animates the orthographic height of a camera." ),
		new( "SoundVolume", "BaseSoundComponent", "float", "SoundVolume", "Animates the volume of a sound component." ),
		new( "SoundPitch", "BaseSoundComponent", "float", "SoundPitch", "Animates the pitch of a sound component." ),
		new( "SoundVolume", "SoundHandle", "float", "SoundHandleVolume", "Animates the volume of a playing sound." ),
		new( "SoundPitch", "SoundHandle", "float", "SoundHandlePitch", "Animates the pitch of a playing sound." ),
		new( "PanelOpacity", "Panel", "float", "PanelOpacity", "Animates the opacity style of a UI panel." ),
	];

	public static readonly ValueTypeInfo[] ValueTypes =
	[
		new( "float", "CustomFloat", "Float" ),
		new( "Vector2", "CustomVector2", "Vector2" ),
		new( "Vector3", "CustomVector3", "Vector3" ),
		new( "Vector4", "CustomVector4", "Vector4" ),
		new( "Color", "CustomColor", "Color" ),
		new( "Rotation", "CustomRotation", "Rotation" ),
		new( "Angles", "CustomAngles", "Angles" ),
	];

	static readonly string[] Header =
	[
		"// <auto-generated>",
		"// Generated by SboxTween.Editor.TweenCodeGenerator (Editor → Sbox Tween → Developer → Regenerate Tween API). Don't edit manually.",
		"// </auto-generated>",
		"",
		"namespace SboxTween;",
		"",
	];

	/// <summary>
	/// Generates the source of Tween.Generated.cs.
	/// </summary>
	public static string GenerateTween()
	{
		var lines = new List<string>( Header ) { "public readonly partial struct Tween", "{" };
		foreach ( var property in Properties )
			lines.AddRange( PropertyOverloads( property, false ) );

		foreach ( var valueType in ValueTypes )
			lines.AddRange( CustomOverloads( valueType ) );

		TrimLastEmpty( lines );
		lines.AddRange( ["}", "", "internal static class CustomInvokers", "{"] );
		lines.AddRange( Invokers( false ) );
		lines.AddRange( ["}", "", "internal static class CustomInvokers<T> where T : class", "{"] );
		lines.AddRange( Invokers( true ) );
		lines.AddRange( ["}", ""] );
		return string.Join( NewLine, lines );
	}

	/// <summary>
	/// Generates the source of GameObjectTweenExtensions.Generated.cs.
	/// </summary>
	public static string GenerateExtensions()
	{
		var lines = new List<string>( Header )
		{
			"/// <summary>",
			"/// Shortcuts for <see cref=\"Tween\"/> methods that animate a GameObject.",
			"/// <code>",
			"/// GameObject.TweenLocalPositionZ( 100, 1, Ease.OutBack );",
			"/// </code>",
			"/// </summary>",
			"public static partial class GameObjectTweenExtensions",
			"{"
		};

		foreach ( var property in Properties.Where( x => x.Extension ) )
			lines.AddRange( PropertyOverloads( property, true ) );

		TrimLastEmpty( lines );
		lines.AddRange( ["}", ""] );
		return string.Join( NewLine, lines );
	}

	/// <summary>
	/// Writes both generated files into <paramref name="apiDirectory"/>. Returns the paths of the files that changed.
	/// </summary>
	public static List<string> WriteFiles( string apiDirectory )
	{
		var changed = new List<string>();
		WriteIfChanged( Path.Combine( apiDirectory, TweenFileName ), GenerateTween(), changed );
		WriteIfChanged( Path.Combine( apiDirectory, ExtensionsFileName ), GenerateExtensions(), changed );
		return changed;
	}

	static void WriteIfChanged( string path, string contents, List<string> changed )
	{
		if ( File.Exists( path ) && File.ReadAllText( path ) == contents )
			return;

		File.WriteAllText( path, contents, new UTF8Encoding( false ) );
		changed.Add( path );
	}

	static IEnumerable<string> PropertyOverloads( PropertyInfo p, bool extension )
	{
		var self = extension ? $"this {p.Target} target" : $"{p.Target} target";
		var method = extension ? $"Tween{p.Name}" : p.Name;
		var call = extension ? $"Tween.{p.Name}" : p.Name;
		var inherit = $"\t/// <inheritdoc cref=\"Tween.{p.Name}({p.Target}, TweenSettings{{{p.Type}}})\"/>";

		if ( !p.StartRequired )
		{
			yield return inherit;
			yield return $"\tpublic static Tween {method}( {self}, {p.Type} endValue, float duration, {Common} )";
			yield return $"\t\t=> {call}( target, new TweenSettings<{p.Type}>( endValue, new TweenSettings( {CommonArgs} ) ) );";
			yield return "";
		}

		yield return inherit;
		yield return $"\tpublic static Tween {method}( {self}, {p.Type} startValue, {p.Type} endValue, float duration, {Common} )";
		yield return $"\t\t=> {call}( target, new TweenSettings<{p.Type}>( startValue, endValue, new TweenSettings( {CommonArgs} ) ) );";
		yield return "";

		if ( !p.StartRequired )
		{
			yield return inherit;
			yield return $"\tpublic static Tween {method}( {self}, {p.Type} endValue, TweenSettings settings ) => {call}( target, new TweenSettings<{p.Type}>( endValue, settings ) );";
			yield return "";
		}

		if ( extension )
		{
			yield return inherit;
			yield return $"\tpublic static Tween {method}( {self}, TweenSettings<{p.Type}> settings ) => {call}( target, settings );";
			yield return "";
			if ( p.AcceptsAngles )
			{
				yield return inherit;
				yield return $"\tpublic static Tween {method}( {self}, TweenSettings<Angles> settings ) => {call}( target, settings );";
				yield return "";
			}

			yield break;
		}

		yield return $"\t/// <summary>{p.Summary}</summary>";
		var settings = p.StartRequired ? "ValidateAnglesData( settings )" : "settings";
		yield return $"\tpublic static Tween {p.Name}( {p.Target} target, TweenSettings<{p.Type}> settings ) => Animate( target, TweenType.{p.TweenType}, {settings} );";
		yield return "";

		if ( p.AcceptsAngles )
		{
			yield return inherit;
			yield return $"\tpublic static Tween {p.Name}( {p.Target} target, TweenSettings<Angles> settings ) => {p.Name}( target, ToRotation( settings ) );";
			yield return "";
		}
	}

	static IEnumerable<string> CustomOverloads( ValueTypeInfo v )
	{
		var inherit = $"\t/// <inheritdoc cref=\"Tween.Custom{{T}}(T, TweenSettings{{{v.Type}}}, Action{{T, {v.Type}}}, Func{{T, {v.Type}}}, bool)\"/>";
		var core = $"TweenType.{v.TweenType}, ValueContainer.From( settings.StartValue ), ValueContainer.From( settings.EndValue ), settings.Settings, settings.StartFromCurrent, onValueChange";

		yield return inherit;
		yield return $"\tpublic static Tween Custom( {v.Type} startValue, {v.Type} endValue, float duration, Action<{v.Type}> onValueChange, {Common} )";
		yield return $"\t\t=> Custom( new TweenSettings<{v.Type}>( startValue, endValue, new TweenSettings( {CommonArgs} ) ), onValueChange );";
		yield return "";
		yield return inherit;
		yield return $"\tpublic static Tween Custom( {v.Type} startValue, {v.Type} endValue, TweenSettings settings, Action<{v.Type}> onValueChange )";
		yield return $"\t\t=> Custom( new TweenSettings<{v.Type}>( startValue, endValue, settings ), onValueChange );";
		yield return "";
		yield return inherit;
		yield return $"\tpublic static Tween Custom( TweenSettings<{v.Type}> settings, Action<{v.Type}> onValueChange, Func<{v.Type}> getCurrentValue = null, bool useDurationAsSpeed = false )";
		yield return $"\t\t=> CustomCore( TweenManager.DummyTarget, {core}, CustomInvokers.{v.Suffix}, getCurrentValue, CustomInvokers.{v.Suffix}Getter, useDurationAsSpeed );";
		yield return "";
		yield return inherit;
		yield return $"\tpublic static Tween Custom<T>( T target, {v.Type} startValue, {v.Type} endValue, float duration, Action<T, {v.Type}> onValueChange, {Common} ) where T : class";
		yield return $"\t\t=> Custom( target, new TweenSettings<{v.Type}>( startValue, endValue, new TweenSettings( {CommonArgs} ) ), onValueChange );";
		yield return "";
		yield return inherit;
		yield return $"\tpublic static Tween Custom<T>( T target, {v.Type} startValue, {v.Type} endValue, TweenSettings settings, Action<T, {v.Type}> onValueChange ) where T : class";
		yield return $"\t\t=> Custom( target, new TweenSettings<{v.Type}>( startValue, endValue, settings ), onValueChange );";
		yield return "";
		yield return "\t/// <summary>";
		yield return "\t/// Animates a custom value. Accessing outside variables from the callbacks creates a closure that allocates; use <paramref name=\"target\"/> to animate without allocations.";
		yield return "\t/// <code>";
		yield return "\t/// Tween.Custom( this, new TweenSettings&lt;float&gt;( 1, 1 ), static ( self, value ) => self.Value = value, static self => self.Value );";
		yield return "\t/// </code>";
		yield return "\t/// </summary>";
		yield return "\t/// <param name=\"getCurrentValue\">Reads the current value when <see cref=\"TweenSettings{T}.StartFromCurrent\"/> is true.</param>";
		yield return "\t/// <param name=\"useDurationAsSpeed\">Treat the duration as speed (units per second) and calculate the real duration from the distance.</param>";
		yield return $"\tpublic static Tween Custom<T>( T target, TweenSettings<{v.Type}> settings, Action<T, {v.Type}> onValueChange, Func<T, {v.Type}> getCurrentValue = null, bool useDurationAsSpeed = false ) where T : class";
		yield return $"\t\t=> CustomCore( target, {core}, CustomInvokers<T>.{v.Suffix}, getCurrentValue, CustomInvokers<T>.{v.Suffix}Getter, useDurationAsSpeed );";
		yield return "";
	}

	static List<string> Invokers( bool generic )
	{
		var lines = new List<string>();
		foreach ( var v in ValueTypes )
		{
			var action = generic ? $"Action<T, {v.Type}>" : $"Action<{v.Type}>";
			var func = generic ? $"Func<T, {v.Type}>" : $"Func<{v.Type}>";
			var args = generic ? $"d.Target as T, d.Current{v.Suffix}" : $"d.Current{v.Suffix}";
			var getArgs = generic ? "d.Target as T" : "";
			lines.AddRange( Invoker( v.Suffix, action, args ) );
			lines.Add( $"\tinternal static readonly Action<TweenData> {v.Suffix}Getter = static d => d.StartValue = ValueContainer.From( (d.CustomGetter as {func})( {getArgs} ) );" );
			lines.Add( "" );
		}

		if ( generic )
		{
			// Shakes report the start value plus the shake offset
			lines.AddRange( Invoker( "Shake", "Action<T, Vector3>", "d.Target as T, d.StartValue.Vector3 + d.GetShakeValue()" ) );
		}

		TrimLastEmpty( lines );
		return lines;
	}

	static IEnumerable<string> Invoker( string name, string delegateType, string arguments )
	{
		yield return $"\tinternal static readonly Action<TweenData> {name} = static d =>";
		yield return "\t{";
		yield return "\t\tif ( !d.BeginCustomCallback() )";
		yield return "\t\t\treturn;";
		yield return "";
		yield return "\t\ttry";
		yield return "\t\t{";
		yield return $"\t\t\t(d.CustomOnValueChange as {delegateType})( {arguments} );";
		yield return "\t\t}";
		yield return "\t\tfinally";
		yield return "\t\t{";
		yield return "\t\t\td.IsUpdating = false;";
		yield return "\t\t}";
		yield return "\t};";
		yield return "";
	}

	static void TrimLastEmpty( List<string> lines )
	{
		if ( lines.Count > 0 && lines[^1] == "" )
			lines.RemoveAt( lines.Count - 1 );
	}
}
