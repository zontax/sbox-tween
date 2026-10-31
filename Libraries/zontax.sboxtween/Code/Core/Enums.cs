namespace SboxTween;

/// <summary>
/// Standard Robert Penner's easing functions. See https://easings.net for visual examples.
/// </summary>
public enum Ease : sbyte
{
	/// <summary>Uses a <see cref="Curve"/> or a parametric ease supplied via <see cref="TweenEasing"/>.</summary>
	Custom = -1,
	/// <summary>Uses <see cref="TweenConfig.DefaultEase"/>.</summary>
	Default = 0,
	Linear = 1,
	InSine, OutSine, InOutSine,
	InQuad, OutQuad, InOutQuad,
	InCubic, OutCubic, InOutCubic,
	InQuart, OutQuart, InOutQuart,
	InQuint, OutQuint, InOutQuint,
	InExpo, OutExpo, InOutExpo,
	InCirc, OutCirc, InOutCirc,
	InElastic, OutElastic, InOutElastic,
	InBack, OutBack, InOutBack,
	InBounce, OutBounce, InOutBounce
}

/// <summary>
/// Controls how an animation behaves with multiple cycles.
/// </summary>
public enum CycleMode : byte
{
	/// <summary>Restarts the animation from the beginning.</summary>
	Restart = 0,
	/// <summary>Animates forth and back, like a yoyo. Easing is the same on the backward cycle.</summary>
	Yoyo = 1,
	/// <summary>At the end of a cycle increments the end value by the difference between start and end values.</summary>
	Incremental = 2,
	/// <summary>Rewinds the animation as if time was reversed. Easing is reversed on the backward cycle.</summary>
	Rewind = 3
}

/// <summary>
/// Controls which scene stage updates the animation.
/// </summary>
public enum UpdateType : byte
{
	/// <summary>Uses <see cref="TweenConfig.DefaultUpdateType"/>.</summary>
	Default,
	/// <summary>Updated at the start of the scene update, before components' OnUpdate.</summary>
	Update,
	/// <summary>Updated after components' OnUpdate, before bones and cameras are processed.</summary>
	LateUpdate,
	/// <summary>Updated at the start of every fixed update step.</summary>
	FixedUpdate
}

internal enum State : byte
{
	None,
	Before,
	Running,
	After
}

internal enum PropType : byte
{
	None,
	Float,
	Color,
	Vector2,
	Vector3,
	Vector4,
	Rotation
}

internal enum ParametricEase : byte
{
	None,
	Overshoot,
	Bounce,
	BounceExact,
	Elastic
}

internal enum TweenType : byte
{
	None,
	MainSequence,
	NestedSequence,
	Delay,
	Callback,
	TweenAwaiter,
	TweenTimeScale,
	TweenTimeScaleSequence,
	GlobalTimeScale,

	CustomFloat,
	CustomColor,
	CustomVector2,
	CustomVector3,
	CustomVector4,
	CustomRotation,
	CustomAngles,

	ShakeLocalPosition,
	ShakeLocalRotation,
	ShakeLocalScale,
	ShakeCustom,

	LocalPosition,
	LocalPositionX,
	LocalPositionY,
	LocalPositionZ,
	WorldPosition,
	WorldPositionX,
	WorldPositionY,
	WorldPositionZ,
	LocalRotation,
	WorldRotation,
	LocalAngles,
	WorldAngles,
	LocalScale,
	LocalScaleX,
	LocalScaleY,
	LocalScaleZ,
	LocalScaleUniform,
	WorldScale,

	TintableColor,
	TintableAlpha,
	SpriteColor,
	SpriteAlpha,
	TextColor,
	TextAlpha,
	PointLightRadius,
	SpotLightRadius,
	SpotLightConeOuter,
	SpotLightConeInner,
	CameraFieldOfView,
	CameraOrthographicHeight,
	SoundVolume,
	SoundPitch,
	SoundHandleVolume,
	SoundHandlePitch,

	// Generated accessors, see TweenCodeGenerator.Properties
	LightIntensity,
	LightTemperature,
	AmbientLightColor,
	DirectionalLightSkyColor,
	CameraZNear,
	CameraZFar,
	CameraBackgroundColor,
	TextFontSize,
	TextScale,
	BloomStrength,
	BloomThreshold,
	BloomTint,
	VignetteIntensity,
	VignetteSmoothness,
	VignetteColor,
	ColorAdjustmentsBlend,
	ColorAdjustmentsSaturation,
	ColorAdjustmentsHueRotate,
	ColorAdjustmentsBrightness,
	ColorAdjustmentsContrast,
	ChromaticAberrationScale,
	BlurSize,
	PixelateScale,
	FilmGrainIntensity,
	DepthOfFieldFocalDistance,
	DepthOfFieldBlurSize,
	MotionBlurScale,
	SharpenScale,
	PostProcessVolumeBlendWeight,
	FogColor,
	FogStartDistance,
	FogDistanceRange,
	FogAttenuationDistance,
	FogMaximumOpacity,
	VolumetricFogStrength,
	VolumetricFogColor,

	RigidbodyPosition,
	RigidbodyRotation,

	// Animate a value by name, stored in TweenData.ParameterName
	RendererAttributeFloat,
	RendererAttributeVector3,
	RendererAttributeVector4,
	RendererAttributeColor,
	AnimParameterFloat,
	AnimParameterVector3,
	AnimParameterRotation,
	Morph,
	PropertyFloat,
	PropertyVector2,
	PropertyVector3,
	PropertyVector4,
	PropertyColor,
	PropertyRotation,
	PropertyAngles,

	// The full text is stored in TweenData.ParameterName
	TextTypewriter,
	LabelTypewriter,

	// Panel types must stay together, PanelTweens relies on the range
	PanelOpacity,
	PanelTranslate,
	PanelRotate,
	PanelScale,
	PanelScaleUniform,
	PanelShakeTranslate,
	PanelShakeRotate,
	PanelShakeScale,
	PanelBackgroundColor,
	PanelFontColor,
	PanelBorderColor,
	PanelBackgroundTint,
	PanelFilterBlur,
	PanelFilterBrightness,
	PanelFilterSaturate,
	PanelFilterContrast,
	PanelBackdropBlur,
	PanelWidth,
	PanelHeight,
	PanelLeft,
	PanelTop,
	PanelRight,
	PanelBottom,
	PanelMargin,
	PanelMarginLeft,
	PanelMarginTop,
	PanelMarginRight,
	PanelMarginBottom,
	PanelPadding,
	PanelPaddingLeft,
	PanelPaddingTop,
	PanelPaddingRight,
	PanelPaddingBottom,
	PanelGap,
	PanelFontSize,
	PanelBorderRadius
}
