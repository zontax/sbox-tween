namespace SboxTween.UnitTests;

[TestClass]
public class PanelTests : TweenTestBase
{
	static PanelTransform.Entry FindEntry( Panel panel, PanelTransform.EntryType type )
	{
		var list = panel.Style.Transform?.List;
		Assert.IsNotNull( list, "The panel has no transform" );
		var entry = list.FirstOrDefault( x => x.Type == type );
		Assert.AreEqual( type, entry.Type, $"No {type} entry in the transform" );
		return entry;
	}

	[TestMethod]
	public void OpacityFromDefault()
	{
		var panel = new Panel();
		panel.TweenOpacity( 0, 1, Ease.Linear );
		Tick( 0.5f );
		AreEqual( 0.5f, panel.Style.Opacity.Value );
	}

	[TestMethod]
	public void TransformChannelsCompose()
	{
		var panel = new Panel();
		panel.TweenTranslate( new Vector2( 100, -40 ), 1, Ease.Linear );
		panel.TweenScale( 2, 1, Ease.Linear );
		panel.TweenRotate( 90, 1, Ease.Linear );
		Tick( 0.5f );

		var list = panel.Style.Transform.Value.List;
		Assert.AreEqual( 3, list.Count, "One transform with three entries" );

		var translate = FindEntry( panel, PanelTransform.EntryType.Translate );
		AreEqual( 50, translate.X.Value );
		AreEqual( -20, translate.Y.Value );
		Assert.AreEqual( LengthUnit.Pixels, translate.X.Unit );
		AreEqual( 45, FindEntry( panel, PanelTransform.EntryType.Rotation ).Data.z );
		AreEqual( 1.5f, FindEntry( panel, PanelTransform.EntryType.Scale ).Data.x );
	}

	[TestMethod]
	public void TranslateInPercent()
	{
		var panel = new Panel();
		panel.TweenTranslate( Length.Percent( -100 ), Length.Pixels( 0 ), 1, Ease.Linear );
		Tick( 0.5f );
		var translate = FindEntry( panel, PanelTransform.EntryType.Translate );
		Assert.AreEqual( LengthUnit.Percentage, translate.X.Unit );
		AreEqual( -50, translate.X.Value );
		Assert.AreEqual( LengthUnit.Pixels, translate.Y.Unit );
	}

	[TestMethod]
	public void TranslateUnitMismatchIsRejected()
	{
		var panel = new Panel();
		ExpectError( "same units" );
		var tween = Tween.PanelTranslate( panel, Length.Pixels( 0 ), Length.Pixels( 0 ), Length.Percent( 10 ), Length.Pixels( 0 ), 1 );
		Assert.IsFalse( tween.IsAlive );
	}

	[TestMethod]
	public void LengthKeepsUnits()
	{
		var panel = new Panel();
		panel.Style.Width = Length.Percent( 20 );
		panel.TweenWidth( Length.Percent( 60 ), 1, Ease.Linear );
		Tick( 0.5f );
		Assert.AreEqual( LengthUnit.Percentage, panel.Style.Width.Value.Unit );
		AreEqual( 40, panel.Style.Width.Value.Value );
	}

	[TestMethod]
	public void LengthMismatchIsRejected()
	{
		var panel = new Panel();
		ExpectError( "same units" );
		var tween = panel.TweenWidth( Length.Pixels( 10 ), Length.Percent( 50 ), 1 );
		Assert.IsFalse( tween.IsAlive );
	}

	[TestMethod]
	public void UnsupportedUnitIsRejected()
	{
		var panel = new Panel();
		ExpectError( "'Auto' units" );
		var tween = panel.TweenWidth( Length.Auto, 1 );
		Assert.IsFalse( tween.IsAlive );
	}

	[TestMethod]
	public void LengthWithoutCurrentValueWarns()
	{
		var panel = new Panel();
		panel.TweenLeft( Length.Percent( 50 ), 1, Ease.Linear );
		ExpectWarning( "Can't read the current value" );
		Tick( 0.5f );
		AreEqual( 25, panel.Style.Left.Value.Value );
	}

	[TestMethod]
	public void ShorthandLengths()
	{
		var panel = new Panel();
		panel.TweenMargin( 0, 10, 1, Ease.Linear );
		panel.TweenGap( 0, 8, 1, Ease.Linear );
		panel.TweenBorderRadius( 0, 6, 1, Ease.Linear );
		Tick( 1 );
		AreEqual( 10, panel.Style.MarginLeft.Value.Value );
		AreEqual( 10, panel.Style.MarginBottom.Value.Value );
		AreEqual( 8, panel.Style.ColumnGap.Value.Value );
		AreEqual( 6, panel.Style.BorderBottomRightRadius.Value.Value );
	}

	[TestMethod]
	public void ColorsAndFilters()
	{
		var panel = new Panel();
		panel.TweenBackgroundColor( Color.Black, Color.White, 1, Ease.Linear );
		panel.TweenBorderColor( Color.Red, 1, Ease.Linear );
		panel.TweenFilterBlur( 0, 8, 1, Ease.Linear );
		panel.TweenFilterSaturate( 0, 1, Ease.Linear );
		Tick( 0.5f );
		AreEqual( new Color( 0.5f, 0.5f, 0.5f, 1 ), panel.Style.BackgroundColor.Value );
		AreEqual( 4, panel.Style.FilterBlur.Value.GetPixels( 1 ) );
		AreEqual( 0.5f, panel.Style.FilterSaturate.Value.GetPixels( 1 ) );
		Assert.IsTrue( panel.Style.BorderTopColor.HasValue );
	}

	[TestMethod]
	public void ValueStaysAfterComplete()
	{
		var panel = new Panel();
		panel.TweenOpacity( 0.2f, 0.1f );
		TickFor( 0.3f );
		AreEqual( 0.2f, panel.Style.Opacity.Value );
	}

	[TestMethod]
	public void ReleaseStyleOnComplete()
	{
		var panel = new Panel();
		panel.TweenOpacity( 0.2f, 0.1f ).ReleaseStyleOnComplete();
		panel.TweenScale( 1.2f, 0.1f ).ReleaseStyleOnComplete();
		TickFor( 0.3f );
		Assert.IsNull( panel.Style.Opacity );
		Assert.IsNull( panel.Style.Transform );
	}

	[TestMethod]
	public void ReleaseOnStop()
	{
		var panel = new Panel();
		var tween = panel.TweenBackgroundColor( Color.Red, 1 ).ReleaseStyleOnComplete();
		Tick( 0.2f );
		Assert.IsTrue( panel.Style.BackgroundColor.HasValue );
		tween.Stop();
		Tick( 0 );
		Assert.IsNull( panel.Style.BackgroundColor );
	}

	[TestMethod]
	public void ReleaseKeepsPropertyOfAnotherAliveTween()
	{
		var panel = new Panel();
		panel.TweenScale( 1.5f, 0.1f ).ReleaseStyleOnComplete();
		panel.TweenScale( new Vector2( 2, 2 ), 1 );
		TickFor( 0.3f );
		Assert.IsNotNull( panel.Style.Transform, "The second tween still animates the scale" );
	}

	[TestMethod]
	public void ReleaseOnlyKeepsOtherChannels()
	{
		var panel = new Panel();
		panel.TweenTranslate( new Vector2( 10, 0 ), 0.1f );
		panel.TweenScale( 1.5f, 0.1f ).ReleaseStyleOnComplete();
		TickFor( 0.3f );
		var list = panel.Style.Transform.Value.List;
		Assert.AreEqual( 1, list.Count );
		Assert.AreEqual( PanelTransform.EntryType.Translate, list[0].Type );
	}

	[TestMethod]
	public void ReleaseOnNonPanelTweenIsRejected()
	{
		ExpectError( "only works with panel tweens" );
		Tween.Delay( 1 ).ReleaseStyleOnComplete();
	}

	[TestMethod]
	public void ReleaseTweenedStyles()
	{
		var panel = new Panel();
		panel.TweenOpacity( 0.5f, 0.1f );
		panel.TweenRotate( 10, 0.1f );
		panel.TweenWidth( 0, 100, 0.1f );
		TickFor( 0.3f );
		panel.ReleaseTweenedStyles();
		Assert.IsNull( panel.Style.Opacity );
		Assert.IsNull( panel.Style.Transform );
		Assert.IsNull( panel.Style.Width );
	}

	[TestMethod]
	public void SequenceReleaseStyleOnComplete()
	{
		var panel = new Panel();
		panel.TweenPopIn( 0.1f ).ReleaseStyleOnComplete();
		TickFor( 0.3f );
		Assert.IsNull( panel.Style.Opacity );
		Assert.IsNull( panel.Style.Transform );
	}

	[TestMethod]
	public void PunchScaleReturns()
	{
		var panel = new Panel();
		panel.PunchScale( new Vector2( 0.3f, 0.3f ), 0.3f );
		float max = 1;
		for ( int i = 0; i < 20; i++ )
		{
			Tick( FrameTime );
			max = MathF.Max( max, FindEntry( panel, PanelTransform.EntryType.Scale ).Data.x );
		}

		Assert.IsTrue( max > 1.1f, $"max = {max}" );
		TickFor( 0.2f );
		AreEqual( 1, FindEntry( panel, PanelTransform.EntryType.Scale ).Data.x );
	}

	[TestMethod]
	public void ShakeTranslateReturnsToCurrentOffset()
	{
		var panel = new Panel();
		panel.TweenTranslate( new Vector2( 30, 0 ), 0.01f );
		TickFor( 0.1f );
		panel.ShakeTranslate( new Vector2( 10, 10 ), 0.3f );
		TickFor( 0.5f );
		var translate = FindEntry( panel, PanelTransform.EntryType.Translate );
		AreEqual( 30, translate.X.Value );
		AreEqual( 0, translate.Y.Value );
	}

	[TestMethod]
	public void ShakeRotateReturns()
	{
		var panel = new Panel();
		panel.ShakeRotate( 15, 0.3f );
		TickFor( 0.5f );
		AreEqual( 0, FindEntry( panel, PanelTransform.EntryType.Rotation ).Data.z );
	}

	[TestMethod]
	public void PopInAndFadeOut()
	{
		var panel = new Panel();
		panel.TweenPopIn( 0.2f );
		EndFrame();
		AreEqual( 0, panel.Style.Opacity.Value );
		AreEqual( 0.8f, FindEntry( panel, PanelTransform.EntryType.Scale ).Data.x );
		TickFor( 0.3f );
		AreEqual( 1, panel.Style.Opacity.Value );
		AreEqual( 1, FindEntry( panel, PanelTransform.EntryType.Scale ).Data.x );

		panel.TweenFadeOut( 0.2f );
		TickFor( 0.3f );
		AreEqual( 0, panel.Style.Opacity.Value );
	}

	[TestMethod]
	public void SlideIn()
	{
		var panel = new Panel();
		panel.TweenSlideIn( new Vector2( 0, 40 ), 0.2f );
		EndFrame();
		AreEqual( 40, FindEntry( panel, PanelTransform.EntryType.Translate ).Y.Value );
		TickFor( 0.3f );
		AreEqual( 0, FindEntry( panel, PanelTransform.EntryType.Translate ).Y.Value );
	}

	[TestMethod]
	public void DeletedPanelStopsTween()
	{
		var panel = new Panel();
		var tween = panel.TweenOpacity( 0, 1 );
		Tick( 0.1f );
		panel.Delete( true );
		Tick( 0.1f );
		Assert.IsFalse( tween.IsAlive );
	}

	[TestMethod]
	public void DeleteAfter()
	{
		var panel = new Panel();
		panel.DeleteAfter( panel.TweenFadeOut( 0.1f ) );
		TickFor( 0.3f );
		// The engine finishes the deletion on the next UI tick, after a possible :outro transition
		Assert.IsTrue( panel.IsDeleting || !panel.IsValid() );
	}

	[TestMethod]
	public void StopTweensOnPanel()
	{
		var panel = new Panel();
		panel.TweenOpacity( 0, 1 );
		panel.TweenScale( 2, 1 );
		Assert.AreEqual( 2, panel.StopTweens() );
	}

	[TestMethod]
	public void PanelDebugNames()
	{
		var panel = new Panel();
		panel.TweenScale( 2, 1 );
		var entries = new List<TweenDebugEntry>();
		Manager.CollectDebugEntries( entries );
		Assert.AreEqual( "PanelScaleUniform", entries[0].Name );
		Assert.AreSame( panel, entries[0].Target );
	}
}
