﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//       LottieGen version:
//           7.1.0+ge1fa92580f
//       
//       Command:
//           LottieGen -Language CSharp -Namespace Unigram.Assets.Icons -Public -WinUIVersion 2.7 -InputFile AskQ.json
//       
//       Input file:
//           AskQ.json (3817 bytes created 17:41+01:00 Dec 21 2021)
//       
//       LottieGen source:
//           http://aka.ms/Lottie
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
// ____________________________________
// |       Object stats       | Count |
// |__________________________|_______|
// | All CompositionObjects   |    42 |
// |--------------------------+-------|
// | Expression animators     |     3 |
// | KeyFrame animators       |     3 |
// | Reference parameters     |     3 |
// | Expression operations    |     0 |
// |--------------------------+-------|
// | Animated brushes         |     - |
// | Animated gradient stops  |     - |
// | ExpressionAnimations     |     1 |
// | PathKeyFrameAnimations   |     - |
// |--------------------------+-------|
// | ContainerVisuals         |     1 |
// | ShapeVisuals             |     1 |
// |--------------------------+-------|
// | ContainerShapes          |     - |
// | CompositionSpriteShapes  |     3 |
// |--------------------------+-------|
// | Brushes                  |     2 |
// | Gradient stops           |     2 |
// | CompositionVisualSurface |     - |
// ------------------------------------
using Microsoft.Graphics.Canvas.Geometry;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.Composition;

namespace Unigram.Assets.Icons
{
    // Name:        u_ask_q
    // Frame rate:  60 fps
    // Frame count: 30
    // Duration:    500.0 mS
    sealed class AskQ
        : Microsoft.UI.Xaml.Controls.IAnimatedVisualSource
        , Microsoft.UI.Xaml.Controls.IAnimatedVisualSource2
    {
        // Animation duration: 0.500 seconds.
        internal const long c_durationTicks = 5000000;

        public Microsoft.UI.Xaml.Controls.IAnimatedVisual TryCreateAnimatedVisual(Compositor compositor)
        {
            object ignored = null;
            return TryCreateAnimatedVisual(compositor, out ignored);
        }

        public Microsoft.UI.Xaml.Controls.IAnimatedVisual TryCreateAnimatedVisual(Compositor compositor, out object diagnostics)
        {
            diagnostics = null;

            if (AskQ_AnimatedVisual.IsRuntimeCompatible())
            {
                return
                    new AskQ_AnimatedVisual(
                        compositor
                        );
            }

            return null;
        }

        /// <summary>
        /// Gets the number of frames in the animation.
        /// </summary>
        public double FrameCount => 30d;

        /// <summary>
        /// Gets the frame rate of the animation.
        /// </summary>
        public double Framerate => 60d;

        /// <summary>
        /// Gets the duration of the animation.
        /// </summary>
        public TimeSpan Duration => TimeSpan.FromTicks(c_durationTicks);

        /// <summary>
        /// Converts a zero-based frame number to the corresponding progress value denoting the
        /// start of the frame.
        /// </summary>
        public double FrameToProgress(double frameNumber)
        {
            return frameNumber / 30d;
        }

        /// <summary>
        /// Returns a map from marker names to corresponding progress values.
        /// </summary>
        public IReadOnlyDictionary<string, double> Markers =>
            new Dictionary<string, double>
            {
                { "NormalToPointerOver_Start", 0.0 },
                { "NormalToPointerOver_End", 1 },
            };

        /// <summary>
        /// Sets the color property with the given name, or does nothing if no such property
        /// exists.
        /// </summary>
        public void SetColorProperty(string propertyName, Color value)
        {
        }

        /// <summary>
        /// Sets the scalar property with the given name, or does nothing if no such property
        /// exists.
        /// </summary>
        public void SetScalarProperty(string propertyName, double value)
        {
        }

        sealed class AskQ_AnimatedVisual : Microsoft.UI.Xaml.Controls.IAnimatedVisual
        {
            const long c_durationTicks = 5000000;
            readonly Compositor _c;
            readonly ExpressionAnimation _reusableExpressionAnimation;
            CompositionColorBrush _colorBrush_White;
            ContainerVisual _root;
            CubicBezierEasingFunction _cubicBezierEasingFunction_0;
            ExpressionAnimation _rootProgress;
            StepEasingFunction _holdThenStepEasingFunction;
            Vector2KeyFrameAnimation _scaleVector2Animation_1;

            static void StartProgressBoundAnimation(
                CompositionObject target,
                string animatedPropertyName,
                CompositionAnimation animation,
                ExpressionAnimation controllerProgressExpression)
            {
                target.StartAnimation(animatedPropertyName, animation);
                var controller = target.TryGetAnimationController(animatedPropertyName);
                controller.Pause();
                controller.StartAnimation("Progress", controllerProgressExpression);
            }

            Vector2KeyFrameAnimation CreateVector2KeyFrameAnimation(float initialProgress, Vector2 initialValue, CompositionEasingFunction initialEasingFunction)
            {
                var result = _c.CreateVector2KeyFrameAnimation();
                result.Duration = TimeSpan.FromTicks(c_durationTicks);
                result.InsertKeyFrame(initialProgress, initialValue, initialEasingFunction);
                return result;
            }

            // - - - Shape tree root for layer: icon
            // - - ShapeGroup: Group 3
            CanvasGeometry Geometry_0()
            {
                CanvasGeometry result;
                using (var builder = new CanvasPathBuilder(null))
                {
                    builder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
                    builder.BeginFigure(new Vector2(-80F, 0F));
                    builder.AddCubicBezier(new Vector2(-80F, -44.1829987F), new Vector2(-44.1829987F, -80F), new Vector2(0F, -80F));
                    builder.AddCubicBezier(new Vector2(44.1829987F, -80F), new Vector2(80F, -44.1829987F), new Vector2(80F, 0F));
                    builder.AddCubicBezier(new Vector2(80F, 44.1829987F), new Vector2(44.1829987F, 80F), new Vector2(0F, 80F));
                    builder.AddCubicBezier(new Vector2(-13.4919996F, 80F), new Vector2(-26.2180004F, 76.6559982F), new Vector2(-37.3779984F, 70.7480011F));
                    builder.AddLine(new Vector2(-73.7870026F, 79.8509979F));
                    builder.AddCubicBezier(new Vector2(-75.4909973F, 80.2770004F), new Vector2(-77.2929993F, 79.7770004F), new Vector2(-78.5350037F, 78.5360031F));
                    builder.AddCubicBezier(new Vector2(-79.7770004F, 77.2939987F), new Vector2(-80.2770004F, 75.4909973F), new Vector2(-79.8509979F, 73.7870026F));
                    builder.AddLine(new Vector2(-70.7480011F, 37.3779984F));
                    builder.AddCubicBezier(new Vector2(-76.6559982F, 26.2169991F), new Vector2(-80F, 13.4919996F), new Vector2(-80F, 0F));
                    builder.EndFigure(CanvasFigureLoop.Closed);
                    result = CanvasGeometry.CreatePath(builder);
                }
                return result;
            }

            // - - - Shape tree root for layer: icon
            // - - ShapeGroup: Group 2
            CanvasGeometry Geometry_1()
            {
                CanvasGeometry result;
                using (var builder = new CanvasPathBuilder(null))
                {
                    builder.BeginFigure(new Vector2(-15.0039997F, -8.64900017F));
                    builder.AddCubicBezier(new Vector2(-15.0039997F, -18.4650002F), new Vector2(-7.6960001F, -23.757F), new Vector2(-0.0120000001F, -23.757F));
                    builder.AddCubicBezier(new Vector2(7.67299986F, -23.757F), new Vector2(15.0039997F, -18.4820004F), new Vector2(15.0039997F, -8.64900017F));
                    builder.AddCubicBezier(new Vector2(15.0039997F, 8.01799965F), new Vector2(0F, 5.67799997F), new Vector2(0F, 23.757F));
                    builder.EndFigure(CanvasFigureLoop.Open);
                    result = CanvasGeometry.CreatePath(builder);
                }
                return result;
            }

            // - - - Shape tree root for layer: icon
            // - - ShapeGroup: Group 1
            CanvasGeometry Geometry_2()
            {
                CanvasGeometry result;
                using (var builder = new CanvasPathBuilder(null))
                {
                    builder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
                    builder.BeginFigure(new Vector2(6.99499989F, 35.1259995F));
                    builder.AddCubicBezier(new Vector2(6.99499989F, 31.2619991F), new Vector2(3.86299992F, 28.1299992F), new Vector2(-0.00100000005F, 28.1299992F));
                    builder.AddCubicBezier(new Vector2(-3.86500001F, 28.1299992F), new Vector2(-6.99700022F, 31.2619991F), new Vector2(-6.99700022F, 35.1259995F));
                    builder.AddCubicBezier(new Vector2(-6.99700022F, 38.9900017F), new Vector2(-3.86500001F, 42.1220016F), new Vector2(-0.00100000005F, 42.1220016F));
                    builder.AddCubicBezier(new Vector2(3.86299992F, 42.1220016F), new Vector2(6.99499989F, 38.9900017F), new Vector2(6.99499989F, 35.1259995F));
                    builder.EndFigure(CanvasFigureLoop.Closed);
                    result = CanvasGeometry.CreatePath(builder);
                }
                return result;
            }

            CompositionColorBrush ColorBrush_White()
            {
                return _colorBrush_White = _c.CreateColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
            }

            // - - Shape tree root for layer: icon
            // - ShapeGroup: Group 3
            // Stop 0
            CompositionColorGradientStop GradientStop_0_AlmostMediumTurquoise_FF66C7DD()
            {
                return _c.CreateColorGradientStop(0F, Color.FromArgb(0xFF, 0x66, 0xC7, 0xDD));
            }

            // - - Shape tree root for layer: icon
            // - ShapeGroup: Group 3
            // Stop 1
            CompositionColorGradientStop GradientStop_1_AlmostSteelBlue_FF3B81C1()
            {
                return _c.CreateColorGradientStop(1F, Color.FromArgb(0xFF, 0x3B, 0x81, 0xC1));
            }

            // - Shape tree root for layer: icon
            // ShapeGroup: Group 3
            CompositionLinearGradientBrush LinearGradientBrush()
            {
                var result = _c.CreateLinearGradientBrush();
                var colorStops = result.ColorStops;
                colorStops.Add(GradientStop_0_AlmostMediumTurquoise_FF66C7DD());
                colorStops.Add(GradientStop_1_AlmostSteelBlue_FF3B81C1());
                result.MappingMode = CompositionMappingMode.Absolute;
                result.StartPoint = new Vector2(0.569999993F, -79.1719971F);
                result.EndPoint = new Vector2(-0.59799999F, 79.6660004F);
                return result;
            }

            // - Shape tree root for layer: icon
            // ShapeGroup: Group 3
            CompositionPathGeometry PathGeometry_0()
            {
                return _c.CreatePathGeometry(new CompositionPath(Geometry_0()));
            }

            // - Shape tree root for layer: icon
            // ShapeGroup: Group 2
            CompositionPathGeometry PathGeometry_1()
            {
                return _c.CreatePathGeometry(new CompositionPath(Geometry_1()));
            }

            // - Shape tree root for layer: icon
            // ShapeGroup: Group 1
            CompositionPathGeometry PathGeometry_2()
            {
                return _c.CreatePathGeometry(new CompositionPath(Geometry_2()));
            }

            // Shape tree root for layer: icon
            // Path 1
            CompositionSpriteShape SpriteShape_0()
            {
                var result = _c.CreateSpriteShape(PathGeometry_0());
                result.Offset = new Vector2(100F, 100F);
                result.FillBrush = LinearGradientBrush();
                StartProgressBoundAnimation(result, "Scale", ScaleVector2Animation_0(), RootProgress());
                return result;
            }

            // Shape tree root for layer: icon
            // Path 1
            CompositionSpriteShape SpriteShape_1()
            {
                var result = _c.CreateSpriteShape(PathGeometry_1());
                result.Offset = new Vector2(100.011002F, 88.7570038F);
                result.StrokeBrush = ColorBrush_White();
                result.StrokeDashCap = CompositionStrokeCap.Round;
                result.StrokeStartCap = CompositionStrokeCap.Round;
                result.StrokeEndCap = CompositionStrokeCap.Round;
                result.StrokeMiterLimit = 5F;
                result.StrokeThickness = 10F;
                StartProgressBoundAnimation(result, "Scale", ScaleVector2Animation_1(), _rootProgress);
                return result;
            }

            // Shape tree root for layer: icon
            // Path 1
            CompositionSpriteShape SpriteShape_2()
            {
                var result = _c.CreateSpriteShape(PathGeometry_2());
                result.Offset = new Vector2(100F, 100F);
                result.FillBrush = _colorBrush_White;
                StartProgressBoundAnimation(result, "Scale", _scaleVector2Animation_1, _rootProgress);
                return result;
            }

            // The root of the composition.
            ContainerVisual Root()
            {
                var result = _root = _c.CreateContainerVisual();
                var propertySet = result.Properties;
                propertySet.InsertScalar("Progress", 0F);
                // Shape tree root for layer: icon
                result.Children.InsertAtTop(ShapeVisual_0());
                return result;
            }

            CubicBezierEasingFunction CubicBezierEasingFunction_0()
            {
                return _cubicBezierEasingFunction_0 = _c.CreateCubicBezierEasingFunction(new Vector2(0.600000024F, 0F), new Vector2(0.400000006F, 1F));
            }

            ExpressionAnimation RootProgress()
            {
                var result = _rootProgress = _c.CreateExpressionAnimation("_.Progress");
                result.SetReferenceParameter("_", _root);
                return result;
            }

            // Shape tree root for layer: icon
            ShapeVisual ShapeVisual_0()
            {
                var result = _c.CreateShapeVisual();
                result.Size = new Vector2(200F, 200F);
                var shapes = result.Shapes;
                // ShapeGroup: Group 3
                shapes.Add(SpriteShape_0());
                // ShapeGroup: Group 2
                shapes.Add(SpriteShape_1());
                // ShapeGroup: Group 1
                shapes.Add(SpriteShape_2());
                return result;
            }

            StepEasingFunction HoldThenStepEasingFunction()
            {
                var result = _holdThenStepEasingFunction = _c.CreateStepEasingFunction();
                result.IsFinalStepSingleFrame = true;
                return result;
            }

            // Scale
            StepEasingFunction StepThenHoldEasingFunction()
            {
                var result = _c.CreateStepEasingFunction();
                result.IsInitialStepSingleFrame = true;
                return result;
            }

            // - Shape tree root for layer: icon
            // ShapeGroup: Group 3
            // Scale
            Vector2KeyFrameAnimation ScaleVector2Animation_0()
            {
                // Frame 0.
                var result = CreateVector2KeyFrameAnimation(0F, new Vector2(1F, 1F), HoldThenStepEasingFunction());
                // Frame 8.
                result.InsertKeyFrame(0.266666681F, new Vector2(1.12F, 1.12F), CubicBezierEasingFunction_0());
                // Frame 16.
                result.InsertKeyFrame(0.533333361F, new Vector2(0.949999988F, 0.949999988F), _cubicBezierEasingFunction_0);
                // Frame 24.
                result.InsertKeyFrame(0.800000012F, new Vector2(1F, 1F), _cubicBezierEasingFunction_0);
                return result;
            }

            // Scale
            Vector2KeyFrameAnimation ScaleVector2Animation_1()
            {
                // Frame 0.
                var result = _scaleVector2Animation_1 = CreateVector2KeyFrameAnimation(0F, new Vector2(1F, 1F), StepThenHoldEasingFunction());
                // Frame 3.
                result.InsertKeyFrame(0.100000001F, new Vector2(1F, 1F), _holdThenStepEasingFunction);
                // Frame 11.
                result.InsertKeyFrame(0.366666675F, new Vector2(1.12F, 1.12F), _cubicBezierEasingFunction_0);
                // Frame 19.
                result.InsertKeyFrame(0.633333325F, new Vector2(0.949999988F, 0.949999988F), _cubicBezierEasingFunction_0);
                // Frame 27.
                result.InsertKeyFrame(0.899999976F, new Vector2(1F, 1F), _cubicBezierEasingFunction_0);
                return result;
            }

            internal AskQ_AnimatedVisual(
                Compositor compositor
                )
            {
                _c = compositor;
                _reusableExpressionAnimation = compositor.CreateExpressionAnimation();
                Root();
            }

            public Visual RootVisual => _root;
            public TimeSpan Duration => TimeSpan.FromTicks(c_durationTicks);
            public Vector2 Size => new Vector2(200F, 200F);
            void IDisposable.Dispose() => _root?.Dispose();

            internal static bool IsRuntimeCompatible()
            {
                return Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7);
            }
        }
    }
}
