﻿using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using RLottie;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Unigram.Common;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.DirectX;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;

namespace Unigram.Controls
{
    public interface IPlayerView
    {
        bool Play();
        void Pause();

        void Unload();

        bool IsLoopingEnabled { get; }

        object Tag { get; set; }
    }

    public class LottieView30Fps : LottieView
    {
        public LottieView30Fps()
            : base(false)
        {

        }
    }

    [TemplatePart(Name = "Canvas", Type = typeof(CanvasControl))]
    public class LottieView : Control, IPlayerView
    {
        private CanvasControl _canvas;
        private CanvasBitmap _bitmap;

        private Grid _layoutRoot;

        private bool _hideThumbnail = true;

        private string _source;
        private LottieAnimation _animation;

        private double _animationFrameRate;
        private int _animationTotalFrame;

        private bool _shouldPlay;

        // Detect from hardware?
        private readonly bool _limitFps = true;

        private bool _skipFrame;

        private int _index;
        private bool _backward;
        private bool _flipped;

        private bool _isLoopingEnabled = true;
        private bool _isCachingEnabled = true;

        private SizeInt32 _frameSize = new SizeInt32 { Width = 256, Height = 256 };
        private DecodePixelType _decodeFrameType = DecodePixelType.Physical;

        private readonly LoopThread _thread;
        private readonly LoopThread _threadUI;
        private readonly object _subscribeLock = new object();
        private bool _subscribed;
        private bool _unsubscribe;

        private bool _unloaded;

        public LottieView()
            : this(CompositionCapabilities.GetForCurrentView().AreEffectsFast())
        {
        }

        public LottieView(bool fullFps)
        {
            _limitFps = !fullFps;
            _thread = fullFps ? LoopThread.Chats : LoopThreadPool.Stickers.Get();
            _threadUI = fullFps ? LoopThread.Chats : LoopThread.Stickers;

            DefaultStyleKey = typeof(LottieView);
        }

        protected override void OnApplyTemplate()
        {
            var canvas = GetTemplateChild("Canvas") as CanvasControl;
            if (canvas == null)
            {
                return;
            }

            _canvas = canvas;
            _canvas.CreateResources += OnCreateResources;
            _canvas.Draw += OnDraw;

            _layoutRoot = GetTemplateChild("LayoutRoot") as Grid;
            _layoutRoot.Loading += OnLoading;
            _layoutRoot.Loaded += OnLoaded;
            _layoutRoot.Unloaded += OnUnloaded;

            OnSourceChanged(UriToPath(Source), _source);

            base.OnApplyTemplate();
        }

        private bool Load()
        {
            if (_unloaded && _layoutRoot != null && _layoutRoot.IsLoaded)
            {
                while (_layoutRoot.Children.Count > 0)
                {
                    _layoutRoot.Children.Remove(_layoutRoot.Children[0]);
                }

                _canvas = new CanvasControl();
                _canvas.CreateResources += OnCreateResources;
                _canvas.Draw += OnDraw;

                _layoutRoot.Children.Add(_canvas);

                _unloaded = false;
                OnSourceChanged(UriToPath(Source), _source);

                return true;
            }

            return false;
        }

        private void OnLoading(FrameworkElement sender, object args)
        {
            Load();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Load();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unload();
        }
        
        public void Unload()
        {
            _shouldPlay = false;
            _unloaded = true;
            Subscribe(false);

            if (_canvas != null)
            {
                _canvas.CreateResources -= OnCreateResources;
                _canvas.Draw -= OnDraw;
                _canvas.RemoveFromVisualTree();
                _canvas = null;
            }

            _source = null;

            //_bitmap?.Dispose();
            _bitmap = null;

            //_animation?.Dispose();
            _animation = null;
        }

        private void OnTick(object sender, EventArgs args)
        {
            try
            {
                Invalidate();
            }
            catch
            {
                lock (_subscribeLock)
                {
                    _unsubscribe = true;
                    _thread.Tick -= OnTick;
                }
            }
        }

        private void OnInvalidate(object sender, EventArgs e)
        {
            _canvas?.Invalidate();
        }

        private void OnCreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            if (_bitmap != null)
            {
                _bitmap.Dispose();
            }

            var buffer = ArrayPool<byte>.Shared.Rent(_frameSize.Width * _frameSize.Height * 4);
            _bitmap = CanvasBitmap.CreateFromBytes(sender, buffer, _frameSize.Width, _frameSize.Height, DirectXPixelFormat.B8G8R8A8UIntNormalized);
            ArrayPool<byte>.Shared.Return(buffer);

            if (args.Reason == CanvasCreateResourcesReason.FirstTime)
            {
                OnSourceChanged(UriToPath(Source), _source);
                Invalidate();
            }
        }

        private void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_bitmap == null || _animation == null || _unloaded)
            {
                return;
            }

            if (_flipped)
            {
                args.DrawingSession.Transform = Matrix3x2.CreateScale(-1, 1, sender.Size.ToVector2() / 2);
            }

            args.DrawingSession.DrawImage(_bitmap, new Rect(0, 0, sender.Size.Width, sender.Size.Height));

            if (_hideThumbnail)
            {
                _hideThumbnail = false;

                FirstFrameRendered?.Invoke(this, EventArgs.Empty);
                ElementCompositionPreview.SetElementChildVisual(this, null);
            }

            Monitor.Enter(_subscribeLock);
            if (_unsubscribe)
            {
                _unsubscribe = false;
                Monitor.Exit(_subscribeLock);
                Subscribe(false);
            }
            else
            {
                Monitor.Exit(_subscribeLock);
            }

            if (!_limitFps)
            {
                sender.Invalidate();
            }
        }

        public void Invalidate()
        {
            var animation = _animation;
            if (animation == null || _canvas == null || _bitmap == null || _unloaded)
            {
                return;
            }

            var index = _index;
            var framesPerUpdate = _limitFps ? _animationFrameRate < 60 ? 1 : 2 : 1;

            if (_animationFrameRate < 60 && !_limitFps)
            {
                if (_skipFrame)
                {
                    _skipFrame = false;
                    return;
                }

                _skipFrame = true;
            }

            animation.RenderSync(_bitmap, index);

            IndexChanged?.Invoke(this, index);

            if (_backward)
            {
                if (index - framesPerUpdate > 0)
                {
                    _index -= framesPerUpdate;
                }
                else
                {
                    _index = 0;
                    _backward = false;

                    if (!_isLoopingEnabled)
                    {
                        lock (_subscribeLock)
                        {
                            _unsubscribe = true;
                            _thread.Tick -= OnTick;
                        }
                    }
                }
            }
            else
            {
                if (index + framesPerUpdate < _animationTotalFrame)
                {
                    PositionChanged?.Invoke(this, Math.Min(1, Math.Max(0, (double)(index + 1) / _animationTotalFrame)));

                    _index += framesPerUpdate;
                }
                else
                {
                    _index = 0;

                    if (!_isLoopingEnabled)
                    {
                        lock (_subscribeLock)
                        {
                            _unsubscribe = true;
                            _thread.Tick -= OnTick;
                        }
                    }

                    PositionChanged?.Invoke(this, 1);
                }
            }
        }

        public void Seek(double position)
        {
            if (position is < 0 or > 1)
            {
                return;
            }

            var animation = _animation;
            if (animation == null)
            {
                return;
            }

            _index = (int)Math.Min(_animation.TotalFrame - 1, Math.Ceiling(_animation.TotalFrame * position));
        }

        public int Ciccio => _animation.TotalFrame;
        public int Index => _index == int.MaxValue ? 0 : _index;

        private void OnSourceChanged(Uri newValue, Uri oldValue)
        {
            OnSourceChanged(UriToPath(newValue), UriToPath(oldValue));
        }

        private async void OnSourceChanged(string newValue, string oldValue)
        {
            var canvas = _canvas;
            if (canvas == null && !Load())
            {
                return;
            }

            if (newValue == null)
            {
                Unload();
                return;
            }

            if (string.Equals(newValue, oldValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(newValue, _source, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _source = newValue;

            var shouldPlay = _shouldPlay;

            var animation = await Task.Run(() => LottieAnimation.LoadFromFile(newValue, _frameSize, _isCachingEnabled, ColorReplacements));
            if (animation == null || !string.Equals(newValue, _source, StringComparison.OrdinalIgnoreCase))
            {
                // The app can't access the file specified
                return;
            }

            if (_shouldPlay)
            {
                shouldPlay = true;
            }

            _animation = animation;
            _hideThumbnail = true;

            _animationFrameRate = animation.FrameRate;
            _animationTotalFrame = animation.TotalFrame;

            if (_backward)
            {
                _index = _animationTotalFrame - 1;
            }
            else
            {
                _index = 0; //_isCachingEnabled ? 0 : _animationTotalFrame - 1;
            }

            //canvas.Paused = true;
            //canvas.ResetElapsedTime();
            //canvas.TargetElapsedTime = update > TimeSpan.Zero ? update : TimeSpan.MaxValue;

            if (AutoPlay || _shouldPlay)
            {
                _shouldPlay = false;
                Subscribe(true);
                //canvas.Paused = false;
            }
            else if (!_unloaded)
            {
                Subscribe(false);

                // Invalidate to render the first frame
                Invalidate();
                _canvas.Invalidate();
            }
        }

        public bool Play()
        {
            return Play(false);
        }

        public bool Play(bool backward = false)
        {
            Load();

            var canvas = _canvas;
            if (canvas == null)
            {
                _shouldPlay = true;
                return false;
            }

            var animation = _animation;
            if (animation == null)
            {
                _shouldPlay = true;
                return false;
            }

            _shouldPlay = false;
            _backward = backward;

            //canvas.Paused = false;
            if (_subscribed)
            {
                return false;
            }

            Subscribe(true);
            return true;
            //OnInvalidate();
        }

        public void Pause()
        {
            Subscribe(false);
        }

        private void Subscribe(bool subscribe)
        {
            lock (_subscribeLock)
            {
                if (subscribe)
                {
                    _unsubscribe = false;
                }

                _subscribed = subscribe;
                _thread.Tick -= OnTick;
                _threadUI.Invalidate -= OnInvalidate;

                if (subscribe)
                {
                    _thread.Tick += OnTick;
                    _threadUI.Invalidate += OnInvalidate;
                }
            }
        }

        private string UriToPath(Uri uri)
        {
            if (uri == null)
            {
                return null;
            }

            switch (uri.Scheme)
            {
                case "ms-appx":
                    return Path.Combine(new[] { Package.Current.InstalledLocation.Path }.Union(uri.Segments.Select(x => x.Trim('/'))).ToArray());
                case "ms-appdata":
                    switch (uri.Host)
                    {
                        case "local":
                            return Path.Combine(new[] { ApplicationData.Current.LocalFolder.Path }.Union(uri.Segments.Select(x => x.Trim('/'))).ToArray());
                        case "temp":
                            return Path.Combine(new[] { ApplicationData.Current.TemporaryFolder.Path }.Union(uri.Segments.Select(x => x.Trim('/'))).ToArray());
                    }
                    break;
                case "file":
                    return uri.LocalPath;
            }

            return null;
        }

        #region IsLoopingEnabled

        public bool IsLoopingEnabled
        {
            get => (bool)GetValue(IsLoopingEnabledProperty);
            set => SetValue(IsLoopingEnabledProperty, value);
        }

        public static readonly DependencyProperty IsLoopingEnabledProperty =
            DependencyProperty.Register("IsLoopingEnabled", typeof(bool), typeof(LottieView), new PropertyMetadata(true, OnLoopingEnabledChanged));

        private static void OnLoopingEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LottieView)d)._isLoopingEnabled = (bool)e.NewValue;
        }

        #endregion

        #region IsCachingEnabled

        public bool IsCachingEnabled
        {
            get => (bool)GetValue(IsCachingEnabledProperty);
            set => SetValue(IsCachingEnabledProperty, value);
        }

        public static readonly DependencyProperty IsCachingEnabledProperty =
            DependencyProperty.Register("IsCachingEnabled", typeof(bool), typeof(LottieView), new PropertyMetadata(true, OnCachingEnabledChanged));

        private static void OnCachingEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LottieView)d)._isCachingEnabled = (bool)e.NewValue;
        }

        #endregion

        #region IsBackward

        public bool IsBackward
        {
            get => (bool)GetValue(IsBackwardProperty);
            set => SetValue(IsBackwardProperty, value);
        }

        public static readonly DependencyProperty IsBackwardProperty =
            DependencyProperty.Register("IsBackward", typeof(bool), typeof(LottieView), new PropertyMetadata(false, OnBackwardChanged));

        private static void OnBackwardChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LottieView)d)._backward = (bool)e.NewValue;
        }

        #endregion

        #region IsFlipped

        public bool IsFlipped
        {
            get => (bool)GetValue(IsFlippedProperty);
            set => SetValue(IsFlippedProperty, value);
        }

        public static readonly DependencyProperty IsFlippedProperty =
            DependencyProperty.Register("IsFlipped", typeof(bool), typeof(LottieView), new PropertyMetadata(false, OnFlippedChanged));

        private static void OnFlippedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LottieView)d)._flipped = (bool)e.NewValue;
        }

        #endregion

        #region FrameSize

        public SizeInt32 FrameSize
        {
            get => (SizeInt32)GetValue(FrameSizeProperty);
            set => SetValue(FrameSizeProperty, value);
        }

        public static readonly DependencyProperty FrameSizeProperty =
            DependencyProperty.Register("FrameSize", typeof(SizeInt32), typeof(LottieView), new PropertyMetadata(new SizeInt32 { Width = 256, Height = 256 }, OnFrameSizeChanged));

        private static void OnFrameSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LottieView)d).OnFrameSizeChanged((SizeInt32)e.NewValue, ((LottieView)d)._decodeFrameType);
        }

        #endregion

        #region DecodeFrameType

        public DecodePixelType DecodeFrameType
        {
            get { return (DecodePixelType)GetValue(DecodeFrameTypeProperty); }
            set { SetValue(DecodeFrameTypeProperty, value); }
        }

        public static readonly DependencyProperty DecodeFrameTypeProperty =
            DependencyProperty.Register("DecodeFrameType", typeof(DecodePixelType), typeof(LottieView), new PropertyMetadata(DecodePixelType.Physical, OnDecodeFrameTypeChanged));

        private static void OnDecodeFrameTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LottieView)d).OnFrameSizeChanged(((LottieView)d)._frameSize, (DecodePixelType)e.NewValue);
        }

        #endregion

        private void OnFrameSizeChanged(SizeInt32 frameSize, DecodePixelType decodeFrameType)
        {
            if (decodeFrameType == DecodePixelType.Logical)
            {
                // TODO: subscribe for DPI changed event
                var dpi = DisplayInformation.GetForCurrentView().LogicalDpi / 96.0f;

                _decodeFrameType = decodeFrameType;
                _frameSize = new SizeInt32
                {
                    Width = (int)(frameSize.Width * dpi),
                    Height = (int)(frameSize.Height * dpi)
                };
            }
            else
            {
                _decodeFrameType = decodeFrameType;
                _frameSize = frameSize;
            }
        }

        #region AutoPlay

        public bool AutoPlay
        {
            get => (bool)GetValue(AutoPlayProperty);
            set => SetValue(AutoPlayProperty, value);
        }

        public static readonly DependencyProperty AutoPlayProperty =
            DependencyProperty.Register("AutoPlay", typeof(bool), typeof(LottieView), new PropertyMetadata(true));

        #endregion

        #region Source

        public Uri Source
        {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Uri), typeof(LottieView), new PropertyMetadata(null, OnSourceChanged));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LottieView)d).OnSourceChanged((Uri)e.NewValue, (Uri)e.OldValue);
        }

        #endregion

        public event EventHandler<double> PositionChanged;
        public event EventHandler<int> IndexChanged;

        public event EventHandler FirstFrameRendered;

        public IReadOnlyDictionary<int, int> ColorReplacements { get; set; }
    }
}
