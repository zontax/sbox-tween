namespace SboxTween;

/// <summary>
/// Type-agnostic storage for animated values. Every supported value type fits into four floats.
/// </summary>
internal struct ValueContainer
{
	public float X;
	public float Y;
	public float Z;
	public float W;

	public ValueContainer( float x, float y = 0, float z = 0, float w = 0 )
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public static ValueContainer From( float value ) => new( value );
	public static ValueContainer From( Vector2 value ) => new( value.x, value.y );
	public static ValueContainer From( Vector3 value ) => new( value.x, value.y, value.z );
	public static ValueContainer From( Vector4 value ) => new( value.x, value.y, value.z, value.w );
	public static ValueContainer From( Color value ) => new( value.r, value.g, value.b, value.a );
	public static ValueContainer From( Rotation value ) => new( value.x, value.y, value.z, value.w );
	public static ValueContainer From( Angles value ) => new( value.pitch, value.yaw, value.roll );

	public readonly float Float => X;
	public readonly Vector2 Vector2 => new( X, Y );
	public readonly Vector3 Vector3 => new( X, Y, Z );
	public readonly Vector4 Vector4 => new( X, Y, Z, W );
	public readonly Color Color => new( X, Y, Z, W );
	public readonly Rotation Rotation => new( X, Y, Z, W );
	public readonly Angles Angles => new( X, Y, Z );

	public readonly float Magnitude => MathF.Sqrt( X * X + Y * Y + Z * Z + W * W );

	public readonly bool IsDefault => X == 0 && Y == 0 && Z == 0 && W == 0;

	public void NormalizeRotation()
	{
		var length = Magnitude;
		if ( length < 1e-6f )
		{
			this = From( Rotation.Identity );
			return;
		}

		X /= length;
		Y /= length;
		Z /= length;
		W /= length;
	}

	public static ValueContainer operator +( ValueContainer a, ValueContainer b ) => new( a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W );
	public static ValueContainer operator -( ValueContainer a, ValueContainer b ) => new( a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W );
	public static ValueContainer operator *( ValueContainer a, float t ) => new( a.X * t, a.Y * t, a.Z * t, a.W * t );

	public static bool operator ==( ValueContainer a, ValueContainer b ) => a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;
	public static bool operator !=( ValueContainer a, ValueContainer b ) => !(a == b);

	public override readonly bool Equals( object obj ) => obj is ValueContainer other && this == other;
	public override readonly int GetHashCode() => HashCode.Combine( X, Y, Z, W );
	public override readonly string ToString() => $"({X}, {Y}, {Z}, {W})";

	/// <summary>
	/// The angle in degrees between two rotations stored in containers.
	/// </summary>
	public static float RotationAngle( ValueContainer a, ValueContainer b ) => a.Rotation.Distance( b.Rotation );
}
