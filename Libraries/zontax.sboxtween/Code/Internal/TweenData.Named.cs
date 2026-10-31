namespace SboxTween;

/// <summary>
/// Values animated by name (shader attributes, animgraph parameters, morphs, properties), rigidbodies and typewriters.
/// </summary>
internal sealed partial class TweenData
{
	/// <summary>The name of the animated value, or the full text of a typewriter.</summary>
	internal string ParameterName;
	internal StringToken ParameterToken;
	/// <summary>The PropertyDescription of property tweens.</summary>
	internal object ParameterCache;

	internal int TypewriterVisible;
	internal float TypewriterWeight;
	internal bool TypewriterPauses;

	internal static bool IsNamed( TweenType tweenType ) => tweenType is >= TweenType.RendererAttributeFloat and <= TweenType.PropertyAngles;
	internal static bool IsRigidbody( TweenType tweenType ) => tweenType is TweenType.RigidbodyPosition or TweenType.RigidbodyRotation;
	static bool IsProperty( TweenType tweenType ) => tweenType is >= TweenType.PropertyFloat and <= TweenType.PropertyAngles;
	static bool IsRendererAttribute( TweenType tweenType ) => tweenType is >= TweenType.RendererAttributeFloat and <= TweenType.RendererAttributeColor;
	internal static bool IsTypewriter( TweenType tweenType ) => tweenType is TweenType.TextTypewriter or TweenType.LabelTypewriter;

	internal void SetParameter( TweenType tweenType, string name, object cache )
	{
		ParameterName = name;
		ParameterCache = cache;
		ParameterToken = IsRendererAttribute( tweenType ) ? new StringToken( name ) : default;
	}

	void ResetParameter()
	{
		ParameterName = null;
		ParameterToken = default;
		ParameterCache = null;
		TypewriterVisible = 0;
		TypewriterWeight = 0;
		TypewriterPauses = false;
	}

	/// <summary>
	/// Checks the name of a named value. Property tweens also look up the property in the TypeLibrary.
	/// </summary>
	internal static bool ValidateParameter( object target, TweenType tweenType, string name, out object cache )
	{
		cache = null;
		if ( string.IsNullOrEmpty( name ) )
		{
			TweenLog.Error( "The name of the animated value is null or empty." );
			return false;
		}

		// Null and destroyed targets are reported by the common validation
		if ( !IsProperty( tweenType ) || IsDestroyed( target ) )
			return true;

		var type = target.GetType();
		var property = TypeLibrary.GetType( type )?.GetProperty( name );
		if ( property is null )
		{
			TweenLog.Error( $"Property '{name}' was not found on {type.Name}. Only public properties of types known to the TypeLibrary can be animated by name." );
			return false;
		}

		var expected = GetPropertyValueType( tweenType );
		if ( property.PropertyType != expected )
		{
			TweenLog.Error( $"Property '{name}' of {type.Name} is {property.PropertyType.Name}, but the tween animates {expected.Name}." );
			return false;
		}

		if ( !property.CanRead || !property.CanWrite )
		{
			TweenLog.Error( $"Property '{name}' of {type.Name} must have a public getter and setter." );
			return false;
		}

		cache = property;
		return true;
	}

	static Type GetPropertyValueType( TweenType tweenType ) => tweenType switch
	{
		TweenType.PropertyVector2 => typeof( Vector2 ),
		TweenType.PropertyVector3 => typeof( Vector3 ),
		TweenType.PropertyVector4 => typeof( Vector4 ),
		TweenType.PropertyColor => typeof( Color ),
		TweenType.PropertyRotation => typeof( Rotation ),
		TweenType.PropertyAngles => typeof( Angles ),
		_ => typeof( float )
	};

	static PropType GetNamedPropType( TweenType tweenType ) => tweenType switch
	{
		TweenType.RendererAttributeVector3 or TweenType.AnimParameterVector3 or TweenType.PropertyVector3 or TweenType.PropertyAngles or TweenType.RigidbodyPosition => PropType.Vector3,
		TweenType.RendererAttributeVector4 or TweenType.PropertyVector4 => PropType.Vector4,
		TweenType.RendererAttributeColor or TweenType.PropertyColor => PropType.Color,
		TweenType.AnimParameterRotation or TweenType.PropertyRotation or TweenType.RigidbodyRotation => PropType.Rotation,
		TweenType.PropertyVector2 => PropType.Vector2,
		_ => PropType.None
	};

	bool TryGetNamedValue( out ValueContainer value )
	{
		var target = Target;
		var property = ParameterCache as PropertyDescription;
		switch ( TweenType )
		{
			case TweenType.RendererAttributeFloat: value = ValueContainer.From( (target as Renderer).Attributes.GetFloat( ParameterToken ) ); return true;
			case TweenType.RendererAttributeVector3: value = ValueContainer.From( (target as Renderer).Attributes.GetVector( ParameterToken ) ); return true;
			case TweenType.RendererAttributeVector4: value = ValueContainer.From( (target as Renderer).Attributes.GetVector4( ParameterToken ) ); return true;
			case TweenType.RendererAttributeColor:
			{
				var v = (target as Renderer).Attributes.GetVector4( ParameterToken );
				value = ValueContainer.From( new Color( v.x, v.y, v.z, v.w ) );
				return true;
			}

			case TweenType.AnimParameterFloat: value = ValueContainer.From( (target as SkinnedModelRenderer).GetFloat( ParameterName ) ); return true;
			case TweenType.AnimParameterVector3: value = ValueContainer.From( (target as SkinnedModelRenderer).GetVector( ParameterName ) ); return true;
			case TweenType.AnimParameterRotation: value = ValueContainer.From( (target as SkinnedModelRenderer).GetRotation( ParameterName ) ); return true;
			case TweenType.Morph: value = ValueContainer.From( (target as SkinnedModelRenderer).Morphs.Get( ParameterName ) ); return true;

			case TweenType.PropertyFloat: value = ValueContainer.From( (float)property.GetValue( target ) ); return true;
			case TweenType.PropertyVector2: value = ValueContainer.From( (Vector2)property.GetValue( target ) ); return true;
			case TweenType.PropertyVector3: value = ValueContainer.From( (Vector3)property.GetValue( target ) ); return true;
			case TweenType.PropertyVector4: value = ValueContainer.From( (Vector4)property.GetValue( target ) ); return true;
			case TweenType.PropertyColor: value = ValueContainer.From( (Color)property.GetValue( target ) ); return true;
			case TweenType.PropertyRotation: value = ValueContainer.From( (Rotation)property.GetValue( target ) ); return true;
			case TweenType.PropertyAngles: value = ValueContainer.From( (Angles)property.GetValue( target ) ); return true;

			case TweenType.RigidbodyPosition: value = ValueContainer.From( BodyPosition( target as Rigidbody ) ); return true;
			case TweenType.RigidbodyRotation: value = ValueContainer.From( BodyRotation( target as Rigidbody ) ); return true;
		}

		value = default;
		return false;
	}

	bool TrySetNamedValue()
	{
		var target = Target;
		var property = ParameterCache as PropertyDescription;
		switch ( TweenType )
		{
			case TweenType.RendererAttributeFloat: (target as Renderer).Attributes.Set( ParameterToken, CurrentFloat ); return true;
			case TweenType.RendererAttributeVector3: (target as Renderer).Attributes.Set( ParameterToken, CurrentVector3 ); return true;
			case TweenType.RendererAttributeVector4: (target as Renderer).Attributes.Set( ParameterToken, CurrentVector4 ); return true;
			case TweenType.RendererAttributeColor:
			{
				var c = CurrentColor;
				(target as Renderer).Attributes.Set( ParameterToken, new Vector4( c.r, c.g, c.b, c.a ) );
				return true;
			}

			case TweenType.AnimParameterFloat: (target as SkinnedModelRenderer).Set( ParameterName, CurrentFloat ); return true;
			case TweenType.AnimParameterVector3: (target as SkinnedModelRenderer).Set( ParameterName, CurrentVector3 ); return true;
			case TweenType.AnimParameterRotation: (target as SkinnedModelRenderer).Set( ParameterName, CurrentRotation ); return true;
			// No fade: the tween already animates the weight
			case TweenType.Morph: (target as SkinnedModelRenderer).Morphs.Set( ParameterName, CurrentFloat, 0 ); return true;

			case TweenType.PropertyFloat: property.SetValue( target, CurrentFloat ); return true;
			case TweenType.PropertyVector2: property.SetValue( target, CurrentVector2 ); return true;
			case TweenType.PropertyVector3: property.SetValue( target, CurrentVector3 ); return true;
			case TweenType.PropertyVector4: property.SetValue( target, CurrentVector4 ); return true;
			case TweenType.PropertyColor: property.SetValue( target, CurrentColor ); return true;
			case TweenType.PropertyRotation: property.SetValue( target, CurrentRotation ); return true;
			case TweenType.PropertyAngles: property.SetValue( target, CurrentAngles ); return true;

			case TweenType.RigidbodyPosition: SetRigidbodyPosition( target as Rigidbody, CurrentVector3 ); return true;
			case TweenType.RigidbodyRotation: SetRigidbodyRotation( target as Rigidbody, CurrentRotation ); return true;

			case TweenType.TextTypewriter:
			case TweenType.LabelTypewriter:
				SetTypewriterText();
				return true;
		}

		return false;
	}

	#region Rigidbody

	static Vector3 BodyPosition( Rigidbody body ) => body.PhysicsBody.IsValid() ? body.PhysicsBody.Position : body.WorldPosition;
	static Rotation BodyRotation( Rigidbody body ) => body.PhysicsBody.IsValid() ? body.PhysicsBody.Rotation : body.WorldRotation;

	/// <summary>
	/// The last update of a rigidbody tween places the body exactly at the end value and stops it, so it doesn't keep flying.
	/// </summary>
	bool IsRigidbodyArriving => IsCompleting || (CyclesTotal != -1 && CyclesDone == CyclesTotal);

	void SetRigidbodyPosition( Rigidbody body, Vector3 position )
	{
		if ( IsRigidbodyArriving )
		{
			body.WorldPosition = position;
			body.Velocity = Vector3.Zero;
			return;
		}

		// The velocity that reaches the position in one physics step, so the body collides on the way
		float dt = Time.Delta;
		if ( dt > 0 )
			body.Velocity = (position - BodyPosition( body )) / dt;
	}

	void SetRigidbodyRotation( Rigidbody body, Rotation rotation )
	{
		if ( IsRigidbodyArriving )
		{
			body.WorldRotation = rotation;
			body.AngularVelocity = Vector3.Zero;
			return;
		}

		float dt = Time.Delta;
		if ( dt > 0 )
			body.AngularVelocity = AngularVelocity( BodyRotation( body ), rotation, dt );
	}

	/// <summary>
	/// World space angular velocity, in radians per second, that turns <paramref name="from"/> into <paramref name="to"/> in <paramref name="dt"/> seconds.
	/// </summary>
	internal static Vector3 AngularVelocity( Rotation from, Rotation to, float dt )
	{
		var delta = to * from.Inverse;
		if ( delta.w < 0 )
			delta = new Rotation( -delta.x, -delta.y, -delta.z, -delta.w );

		var axis = new Vector3( delta.x, delta.y, delta.z );
		float sinHalfAngle = axis.Length;
		if ( sinHalfAngle < 1e-6f )
			return Vector3.Zero;

		float angle = 2 * MathF.Atan2( sinHalfAngle, delta.w );
		return axis * (angle / (sinHalfAngle * dt));
	}

	#endregion

	#region Typewriter

	const float SentencePause = 8;
	const float ClausePause = 3;

	/// <summary>
	/// The pause after a character, in characters. Punctuation pauses only before whitespace, so "3.14" and "..." inside a word don't pause.
	/// </summary>
	static float PauseAfter( string text, int index )
	{
		if ( index >= text.Length - 1 || !char.IsWhiteSpace( text[index + 1] ) )
			return 0;

		return text[index] switch
		{
			'.' or '!' or '?' or '…' => SentencePause,
			',' or ';' or ':' or '—' => ClausePause,
			_ => 0
		};
	}

	/// <summary>
	/// The length of the typing in characters, including the punctuation pauses.
	/// </summary>
	internal static float CalcTypewriterWeight( string text, bool pauseOnPunctuation )
	{
		float weight = text.Length;
		if ( pauseOnPunctuation )
		{
			for ( int i = 0; i < text.Length; i++ )
				weight += PauseAfter( text, i );
		}

		return weight;
	}

	int CalcVisibleCharacters( float progress )
	{
		var text = ParameterName;
		float position = Math.Clamp( progress, 0, 1 ) * TypewriterWeight;
		if ( !TypewriterPauses )
			return Math.Min( (int)position, text.Length );

		// A character appears when the position reaches the end of its slot, then its pause follows
		float slotEnd = 0;
		for ( int i = 0; i < text.Length; i++ )
		{
			slotEnd += 1;
			if ( position < slotEnd )
				return i;

			slotEnd += PauseAfter( text, i );
		}

		return text.Length;
	}

	void SetTypewriterText()
	{
		var text = ParameterName;
		int visible = CalcVisibleCharacters( CurrentFloat );
		if ( visible == TypewriterVisible )
			return;

		TypewriterVisible = visible;
		var shown = visible >= text.Length ? text : text[..visible];
		if ( Target is TextRenderer renderer )
			renderer.Text = shown;
		else if ( Target is Label label )
			label.Text = shown;
	}

	#endregion
}