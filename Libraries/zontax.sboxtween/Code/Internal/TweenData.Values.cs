namespace SboxTween;

internal sealed partial class TweenData
{
	internal static PropType GetPropType( TweenType tweenType )
	{
		switch ( tweenType )
		{
			case TweenType.CustomColor:
			case TweenType.TintableColor:
			case TweenType.SpriteColor:
			case TweenType.TextColor:
				return PropType.Color;

			case TweenType.CustomVector2:
				return PropType.Vector2;

			case TweenType.CustomVector3:
			case TweenType.CustomAngles:
			case TweenType.ShakeLocalPosition:
			case TweenType.ShakeLocalScale:
			case TweenType.ShakeCustom:
			case TweenType.LocalPosition:
			case TweenType.WorldPosition:
			case TweenType.LocalAngles:
			case TweenType.WorldAngles:
			case TweenType.LocalScale:
			case TweenType.WorldScale:
				return PropType.Vector3;

			case TweenType.CustomVector4:
				return PropType.Vector4;

			case TweenType.CustomRotation:
			case TweenType.ShakeLocalRotation:
			case TweenType.LocalRotation:
			case TweenType.WorldRotation:
				return PropType.Rotation;

			case TweenType.None:
				return PropType.None;

			case >= TweenType.PanelOpacity:
				return PanelTweens.GetPropType( tweenType );

			default:
			{
				var propType = GetGeneratedPropType( tweenType );
				if ( propType == PropType.None )
					propType = GetNamedPropType( tweenType );

				return propType == PropType.None ? PropType.Float : propType;
			}
		}
	}

	internal ValueContainer GetAnimatedValue()
	{
		var target = Target;
		switch ( TweenType )
		{
			case TweenType.GlobalTimeScale: return ValueContainer.From( (target as Scene).TimeScale );
			case TweenType.TweenTimeScale:
			case TweenType.TweenTimeScaleSequence:
			{
				var tweenTarget = target as TweenData;
				if ( TargetTweenId != tweenTarget.Id || !tweenTarget.IsAlive )
					return ValueContainer.From( 1 );

				return ValueContainer.From( tweenTarget.TimeScale );
			}

			case TweenType.ShakeLocalPosition: return ValueContainer.From( (target as GameObject).LocalPosition );
			case TweenType.ShakeLocalRotation: return ValueContainer.From( (target as GameObject).LocalRotation );
			case TweenType.ShakeLocalScale: return ValueContainer.From( (target as GameObject).LocalScale );

			case TweenType.LocalPosition: return ValueContainer.From( (target as GameObject).LocalPosition );
			case TweenType.LocalPositionX: return ValueContainer.From( (target as GameObject).LocalPosition.x );
			case TweenType.LocalPositionY: return ValueContainer.From( (target as GameObject).LocalPosition.y );
			case TweenType.LocalPositionZ: return ValueContainer.From( (target as GameObject).LocalPosition.z );
			case TweenType.WorldPosition: return ValueContainer.From( (target as GameObject).WorldPosition );
			case TweenType.WorldPositionX: return ValueContainer.From( (target as GameObject).WorldPosition.x );
			case TweenType.WorldPositionY: return ValueContainer.From( (target as GameObject).WorldPosition.y );
			case TweenType.WorldPositionZ: return ValueContainer.From( (target as GameObject).WorldPosition.z );
			case TweenType.LocalRotation: return ValueContainer.From( (target as GameObject).LocalRotation );
			case TweenType.WorldRotation: return ValueContainer.From( (target as GameObject).WorldRotation );
			case TweenType.LocalAngles: return ValueContainer.From( (target as GameObject).LocalRotation.Angles() );
			case TweenType.WorldAngles: return ValueContainer.From( (target as GameObject).WorldRotation.Angles() );
			case TweenType.LocalScale: return ValueContainer.From( (target as GameObject).LocalScale );
			case TweenType.LocalScaleX: return ValueContainer.From( (target as GameObject).LocalScale.x );
			case TweenType.LocalScaleY: return ValueContainer.From( (target as GameObject).LocalScale.y );
			case TweenType.LocalScaleZ: return ValueContainer.From( (target as GameObject).LocalScale.z );
			case TweenType.LocalScaleUniform: return ValueContainer.From( (target as GameObject).LocalScale.x );
			case TweenType.WorldScale: return ValueContainer.From( (target as GameObject).WorldScale );

			case TweenType.TintableColor: return ValueContainer.From( (target as Component.ITintable).Color );
			case TweenType.TintableAlpha: return ValueContainer.From( (target as Component.ITintable).Color.a );
			case TweenType.SpriteColor: return ValueContainer.From( (target as SpriteRenderer).Color );
			case TweenType.SpriteAlpha: return ValueContainer.From( (target as SpriteRenderer).Color.a );
			case TweenType.TextColor: return ValueContainer.From( (target as TextRenderer).Color );
			case TweenType.TextAlpha: return ValueContainer.From( (target as TextRenderer).Color.a );
			case TweenType.PointLightRadius: return ValueContainer.From( (target as PointLight).Radius );
			case TweenType.SpotLightRadius: return ValueContainer.From( (target as SpotLight).Radius );
			case TweenType.SpotLightConeOuter: return ValueContainer.From( (target as SpotLight).ConeOuter );
			case TweenType.SpotLightConeInner: return ValueContainer.From( (target as SpotLight).ConeInner );
			case TweenType.CameraFieldOfView: return ValueContainer.From( (target as CameraComponent).FieldOfView );
			case TweenType.CameraOrthographicHeight: return ValueContainer.From( (target as CameraComponent).OrthographicHeight );
			case TweenType.SoundVolume: return ValueContainer.From( (target as BaseSoundComponent).Volume );
			case TweenType.SoundPitch: return ValueContainer.From( (target as BaseSoundComponent).Pitch );
			case TweenType.SoundHandleVolume: return ValueContainer.From( (target as SoundHandle).Volume );
			case TweenType.SoundHandlePitch: return ValueContainer.From( (target as SoundHandle).Pitch );

			default:
				if ( PanelTweens.IsPanelType( TweenType ) )
					return PanelTweens.Get( this );

				if ( TryGetGeneratedValue( out var value ) || TryGetNamedValue( out value ) )
					return value;

				throw new InvalidOperationException( $"Tween type {TweenType} doesn't support reading the current value." );
		}
	}

	internal float CurrentFloat => StartValue.X + EndValueOrDiff.X * EasedInterpolationFactor;
	internal Vector2 CurrentVector2 => (StartValue + EndValueOrDiff * EasedInterpolationFactor).Vector2;
	internal Vector3 CurrentVector3 => (StartValue + EndValueOrDiff * EasedInterpolationFactor).Vector3;
	internal Vector4 CurrentVector4 => (StartValue + EndValueOrDiff * EasedInterpolationFactor).Vector4;
	internal Color CurrentColor => (StartValue + EndValueOrDiff * EasedInterpolationFactor).Color;
	internal Rotation CurrentRotation => Rotation.Slerp( StartValue.Rotation, EndValueOrDiff.Rotation, EasedInterpolationFactor, false );
	internal Angles CurrentAngles => (StartValue + EndValueOrDiff * EasedInterpolationFactor).Angles;

	/// <summary>
	/// Progress of the current cycle without validation, for the debugger. Works for nested animations too.
	/// </summary>
	internal float DebugProgress
	{
		get
		{
			if ( ElapsedTimeTotal == float.MaxValue || (CyclesTotal != -1 && GetCyclesDone() >= CyclesTotal) )
				return 1;

			if ( CycleDuration <= 0 )
				return State == State.After ? 1 : 0;

			float elapsed = ElapsedTimeTotal - WaitDelay - CycleDuration * GetCyclesDone();
			return Math.Clamp( elapsed / CycleDuration, 0, 1 );
		}
	}

	/// <summary>
	/// Guards user callbacks of custom tweens from recursive calls.
	/// </summary>
	internal bool BeginCustomCallback()
	{
		if ( IsUpdating )
		{
			LogError( Messages.RecursiveCall );
			return false;
		}

		IsUpdating = true;
		return true;
	}

	/// <summary>
	/// Applies the current interpolated value to the target. Returns false if the animation was stopped.
	/// </summary>
	bool SetAnimatedValue()
	{
		var target = Target;
		if ( IsDestroyed( target ) )
		{
			EmergencyStop( true );
			return false;
		}

		var start = StartValue;
		var delta = EndValueOrDiff;
		float t = EasedInterpolationFactor;

		float FloatVal() => start.X + delta.X * t;
		Vector3 Vector3Val() => new( start.X + delta.X * t, start.Y + delta.Y * t, start.Z + delta.Z * t );
		Color ColorVal() => new( start.X + delta.X * t, start.Y + delta.Y * t, start.Z + delta.Z * t, start.W + delta.W * t );
		Rotation RotationVal() => Rotation.Slerp( start.Rotation, delta.Rotation, t, false );

		switch ( TweenType )
		{
			case TweenType.MainSequence:
			case TweenType.NestedSequence:
			case TweenType.Delay:
			case TweenType.Callback:
				break;

			case TweenType.TweenAwaiter:
				UpdateTweenAwaiter();
				break;

			case TweenType.TweenTimeScale:
			case TweenType.TweenTimeScaleSequence:
			{
				var tweenTarget = target as TweenData;
				if ( TargetTweenId != tweenTarget.Id || !tweenTarget.IsAlive )
				{
					EmergencyStop( false );
					return false;
				}

				// The parent Sequence controls the timeScale of a nested animation
				if ( !tweenTarget.IsCompleting && tweenTarget.CanManipulate() )
					tweenTarget.TimeScale = FloatVal();
				break;
			}

			case TweenType.GlobalTimeScale: (target as Scene).TimeScale = MathF.Max( 0, FloatVal() ); break;

			case TweenType.CustomFloat:
			case TweenType.CustomColor:
			case TweenType.CustomVector2:
			case TweenType.CustomVector3:
			case TweenType.CustomVector4:
			case TweenType.CustomRotation:
			case TweenType.CustomAngles:
			case TweenType.ShakeCustom:
				OnValueChangeInvoker( this );
				break;

			case TweenType.ShakeLocalPosition: (target as GameObject).LocalPosition = start.Vector3 + GetShakeValue(); break;
			case TweenType.ShakeLocalRotation: (target as GameObject).LocalRotation = start.Rotation * Rotation.From( new Angles( GetShakeValue() ) ); break;
			case TweenType.ShakeLocalScale: (target as GameObject).LocalScale = start.Vector3 + GetShakeValue(); break;

			case TweenType.LocalPosition: (target as GameObject).LocalPosition = Vector3Val(); break;
			case TweenType.LocalPositionX: { var go = target as GameObject; go.LocalPosition = go.LocalPosition.WithX( FloatVal() ); break; }
			case TweenType.LocalPositionY: { var go = target as GameObject; go.LocalPosition = go.LocalPosition.WithY( FloatVal() ); break; }
			case TweenType.LocalPositionZ: { var go = target as GameObject; go.LocalPosition = go.LocalPosition.WithZ( FloatVal() ); break; }
			case TweenType.WorldPosition: (target as GameObject).WorldPosition = Vector3Val(); break;
			case TweenType.WorldPositionX: { var go = target as GameObject; go.WorldPosition = go.WorldPosition.WithX( FloatVal() ); break; }
			case TweenType.WorldPositionY: { var go = target as GameObject; go.WorldPosition = go.WorldPosition.WithY( FloatVal() ); break; }
			case TweenType.WorldPositionZ: { var go = target as GameObject; go.WorldPosition = go.WorldPosition.WithZ( FloatVal() ); break; }
			case TweenType.LocalRotation: (target as GameObject).LocalRotation = RotationVal(); break;
			case TweenType.WorldRotation: (target as GameObject).WorldRotation = RotationVal(); break;
			case TweenType.LocalAngles: (target as GameObject).LocalRotation = Rotation.From( new Angles( Vector3Val() ) ); break;
			case TweenType.WorldAngles: (target as GameObject).WorldRotation = Rotation.From( new Angles( Vector3Val() ) ); break;
			case TweenType.LocalScale: (target as GameObject).LocalScale = Vector3Val(); break;
			case TweenType.LocalScaleX: { var go = target as GameObject; go.LocalScale = go.LocalScale.WithX( FloatVal() ); break; }
			case TweenType.LocalScaleY: { var go = target as GameObject; go.LocalScale = go.LocalScale.WithY( FloatVal() ); break; }
			case TweenType.LocalScaleZ: { var go = target as GameObject; go.LocalScale = go.LocalScale.WithZ( FloatVal() ); break; }
			case TweenType.LocalScaleUniform: (target as GameObject).LocalScale = FloatVal(); break;
			case TweenType.WorldScale: (target as GameObject).WorldScale = Vector3Val(); break;

			case TweenType.TintableColor: (target as Component.ITintable).Color = ColorVal(); break;
			case TweenType.TintableAlpha: { var tintable = target as Component.ITintable; tintable.Color = tintable.Color.WithAlpha( FloatVal() ); break; }
			case TweenType.SpriteColor: (target as SpriteRenderer).Color = ColorVal(); break;
			case TweenType.SpriteAlpha: { var sprite = target as SpriteRenderer; sprite.Color = sprite.Color.WithAlpha( FloatVal() ); break; }
			case TweenType.TextColor: (target as TextRenderer).Color = ColorVal(); break;
			case TweenType.TextAlpha: { var text = target as TextRenderer; text.Color = text.Color.WithAlpha( FloatVal() ); break; }
			case TweenType.PointLightRadius: (target as PointLight).Radius = FloatVal(); break;
			case TweenType.SpotLightRadius: (target as SpotLight).Radius = FloatVal(); break;
			case TweenType.SpotLightConeOuter: (target as SpotLight).ConeOuter = FloatVal(); break;
			case TweenType.SpotLightConeInner: (target as SpotLight).ConeInner = FloatVal(); break;
			case TweenType.CameraFieldOfView: (target as CameraComponent).FieldOfView = FloatVal(); break;
			case TweenType.CameraOrthographicHeight: (target as CameraComponent).OrthographicHeight = FloatVal(); break;
			case TweenType.SoundVolume: (target as BaseSoundComponent).Volume = FloatVal(); break;
			case TweenType.SoundPitch: (target as BaseSoundComponent).Pitch = FloatVal(); break;
			case TweenType.SoundHandleVolume: (target as SoundHandle).Volume = FloatVal(); break;
			case TweenType.SoundHandlePitch: (target as SoundHandle).Pitch = FloatVal(); break;

			default:
				if ( PanelTweens.IsPanelType( TweenType ) )
				{
					PanelTweens.Set( this );
					break;
				}

				if ( !TrySetGeneratedValue() && !TrySetNamedValue() )
					throw new InvalidOperationException( $"Tween type {TweenType} doesn't support setting the value." );

				break;
		}

		return true;
	}

	void UpdateTweenAwaiter()
	{
		if ( !IsAlive )
			return;

		var target = Target as TweenData;
		if ( TargetTweenId == target.Id && target.IsAlive )
		{
			if ( target.HasCancellationToken && target.CancellationToken.IsCancellationRequested )
			{
				SboxTween.Tween.CancelTweenOrSequence( new Tween( target ) );
				if ( !IsAlive )
					return;

				ForceComplete();
			}

			return;
		}

		ForceComplete();
	}
}
