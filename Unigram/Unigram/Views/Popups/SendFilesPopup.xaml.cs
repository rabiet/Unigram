﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Td.Api;
using Unigram.Collections;
using Unigram.Common;
using Unigram.Controls;
using Unigram.Controls.Chats;
using Unigram.Converters;
using Unigram.Entities;
using Unigram.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;

namespace Unigram.Views.Popups
{
    public sealed partial class SendFilesPopup : ContentPopup, IViewWithAutocomplete, INotifyPropertyChanged
    {
        public DialogViewModel ViewModel { get; set; }
        public MvxObservableCollection<StorageMedia> Items { get; private set; }

        private ICollection _autocomplete;
        public ICollection Autocomplete
        {
            get => _autocomplete;
            set
            {
                if (_autocomplete != value)
                {
                    _autocomplete = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Autocomplete)));
                }
            }
        }

        public bool IsMediaOnly => Items.All(x => x is StoragePhoto or StorageVideo);
        public bool IsAlbumAvailable
        {
            get
            {
                if (IsMediaSelected)
                {
                    return Items.Count > 1 && Items.Count <= 10 && Items.All(x => (x is StoragePhoto || x is StorageVideo) && x.Ttl == 0);
                }

                return Items.Count is > 1 and <= 10;
            }
        }
        public bool IsTtlAvailable { get; }

        private bool _isMediaSelected;
        public bool IsMediaSelected
        {
            get => _isMediaSelected;
            set
            {
                if (_isMediaSelected != value)
                {
                    _isMediaSelected = value;
                    _isFilesSelected = !value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMediaSelected)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFilesSelected)));
                }
            }
        }

        private bool _isFilesSelected;
        public bool IsFilesSelected
        {
            get => _isFilesSelected;
            set
            {
                if (_isFilesSelected != value)
                {
                    _isFilesSelected = value;
                    _isMediaSelected = !value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFilesSelected)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMediaSelected)));
                }
            }
        }

        public string SendWithoutCompression
        {
            get
            {
                if (IsMediaSelected)
                {
                    return Items.Count == 1 ? Strings.Resources.SendAsFile : Strings.Resources.SendAsFiles;
                }

                return string.Format(Strings.Resources.SendItems, Locale.Declension("Files", Items.Count));
            }
        }

        private bool _wasAlbum = true;

        private bool _isAlbum = true;
        public bool IsAlbum
        {
            get => _isAlbum;
            set
            {
                if (_isAlbum != value)
                {
                    _isAlbum = IsAlbumAvailable && value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAlbum)));
                }
            }
        }

        public FormattedText Caption
        {
            get => CaptionInput.GetFormattedText(false);
            set => CaptionInput.SetText(value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool CanSchedule { get; set; }
        public bool IsSavedMessages { get; set; }

        public bool? Schedule { get; private set; }
        public bool? Silent { get; private set; }

        public SendFilesPopup(IEnumerable<StorageMedia> items, bool media, bool ttl, bool schedule, bool savedMessages)
        {
            InitializeComponent();

            PrimaryButtonText = Strings.Resources.Send;
            SecondaryButtonText = Strings.Resources.Cancel;

            IsTtlAvailable = ttl;
            IsSavedMessages = savedMessages;
            CanSchedule = schedule;

            Items = new MvxObservableCollection<StorageMedia>(items);
            Items.CollectionChanged += OnCollectionChanged;
            IsMediaSelected = media && IsMediaOnly;
            IsFilesSelected = !IsMediaSelected;

            UpdateView();
            UpdatePanel();
        }

        private void OnCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SendWithoutCompression)));

            if (Items.Count > 0)
            {
                UpdateView();
                UpdatePanel();
            }
            else
            {
                Hide();
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SetResult(ContentDialogResult.Primary);
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            SetResult(ContentDialogResult.Secondary);
        }

        private void Autocomplete_ItemClick(object sender, ItemClickEventArgs e)
        {
            var chat = ViewModel.Chat;
            if (chat == null)
            {
                return;
            }

            CaptionInput.Document.GetText(TextGetOptions.NoHidden, out string text);

            if (e.ClickedItem is User user && ChatTextBox.SearchByUsername(text.Substring(0, Math.Min(CaptionInput.Document.Selection.EndPosition, text.Length)), out string username, out _))
            {
                var adjust = 0;

                string insert;
                if (string.IsNullOrEmpty(user.Username))
                {
                    insert = string.IsNullOrEmpty(user.FirstName) ? user.LastName : user.FirstName;
                    adjust = 1;
                }
                else
                {
                    insert = user.Username;
                }

                var range = CaptionInput.Document.GetRange(CaptionInput.Document.Selection.StartPosition - username.Length - adjust, CaptionInput.Document.Selection.StartPosition);
                range.SetText(TextSetOptions.None, insert);

                if (string.IsNullOrEmpty(user.Username))
                {
                    range.Link = $"\"tg-user://{user.Id}\"";
                }

                CaptionInput.Document.GetRange(range.EndPosition, range.EndPosition).SetText(TextSetOptions.None, " ");
                CaptionInput.Document.Selection.StartPosition = range.EndPosition + 1;
            }
            else if (e.ClickedItem is EmojiData emoji && ChatTextBox.SearchByEmoji(text.Substring(0, Math.Min(CaptionInput.Document.Selection.EndPosition, text.Length)), out string replacement, out _))
            {
                var insert = $"{emoji.Value} ";
                var start = CaptionInput.Document.Selection.StartPosition - 1 - replacement.Length + insert.Length;
                var range = CaptionInput.Document.GetRange(CaptionInput.Document.Selection.StartPosition - 1 - replacement.Length, CaptionInput.Document.Selection.StartPosition);
                range.SetText(TextSetOptions.None, insert);

                //TextField.Document.GetRange(start, start).SetText(TextSetOptions.None, " ");
                //TextField.Document.Selection.StartPosition = start + 1;
                CaptionInput.Document.Selection.StartPosition = start;
            }

            Autocomplete = null;
        }

        private void Autocomplete_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue)
            {
                return;
            }

            var content = args.ItemContainer.ContentTemplateRoot as Grid;

            if (args.Item is User user)
            {
                var photo = content.Children[0] as ProfilePicture;
                var title = content.Children[1] as TextBlock;

                var name = title.Inlines[0] as Run;
                var username = title.Inlines[1] as Run;

                name.Text = user.GetFullName();
                username.Text = string.IsNullOrEmpty(user.Username) ? string.Empty : $" @{user.Username}";

                photo.SetUser(ViewModel.ProtoService, user, 36);
            }
        }

        private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue)
            {
                return;
            }

            var storage = args.Item as StorageMedia;
            if (storage == null)
            {
                return;
            }

            var root = args.ItemContainer.ContentTemplateRoot as Grid;
            if (root == null)
            {
                return;
            }

            if (root is AspectView aspect)
            {
                aspect.Constraint = new Size(storage.Width, storage.Height);
            }

            var glyph = root.FindName("Glyph") as TextBlock;
            glyph.Text = storage is StoragePhoto
                ? Icons.Image
                : storage is StorageVideo
                ? Icons.Play
                : Icons.Document;

            var title = root.FindName("Title") as TextBlock;
            var subtitle = root.FindName("Subtitle") as TextBlock;

            if (title == null || subtitle == null)
            {
                return;
            }

            title.Text = storage.File.Name;
            subtitle.Text = FileSizeConverter.Convert((long)storage.Size);
        }

        private void Grid_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            var storage = args.NewValue as StorageMedia;
            if (storage == null)
            {
                return;
            }

            var root = sender.Parent as Grid;
            if (root == null)
            {
                return;
            }

            var overlay = root.FindName("Overlay") as Border;

            var mute = root.FindName("Mute") as ToggleButton;
            var crop = root.FindName("Crop") as ToggleButton;
            var ttl = root.FindName("Ttl") as ToggleButton;

            overlay.Visibility = storage is StorageVideo ? Visibility.Visible : Visibility.Collapsed;

            if (storage is StorageVideo video)
            {
                mute.IsChecked = video.IsMuted;
                mute.Visibility = Visibility.Visible;
            }
            else
            {
                mute.Visibility = Visibility.Collapsed;
            }

            crop.Visibility = storage is StoragePhoto ? Visibility.Visible : Visibility.Collapsed;
            ttl.Visibility = IsTtlAvailable ? Visibility.Visible : Visibility.Collapsed;
        }

        public void Accept()
        {
            if (CaptionInput.HandwritingView.IsOpen)
            {
                void handler(object s, RoutedEventArgs args)
                {
                    CaptionInput.HandwritingView.Unloaded -= handler;

                    Caption = CaptionInput.GetFormattedText();
                    Hide(ContentDialogResult.Primary);
                }

                CaptionInput.HandwritingView.Unloaded += handler;
                CaptionInput.HandwritingView.TryClose();
            }
            else
            {
                Caption = CaptionInput.GetFormattedText();
                Hide(ContentDialogResult.Primary);
            }
        }

        private async void OnPaste(object sender, TextControlPasteEventArgs e)
        {
            var content = Clipboard.GetContent();
            if (content.AvailableFormats.Contains(StandardDataFormats.Text))
            {
                e.Handled = true;
            }
            else
            {
                e.Handled = true;
                await HandlePackageAsync(content);
            }
        }

        private void ListView_DragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void ListView_Drop(object sender, DragEventArgs e)
        {
            await HandlePackageAsync(e.DataView);
        }

        public async Task HandlePackageAsync(DataPackageView package)
        {
            try
            {
                if (package.AvailableFormats.Contains(StandardDataFormats.Bitmap))
                {
                    var bitmap = await package.GetBitmapAsync();

                    var fileName = string.Format("image_{0:yyyy}-{0:MM}-{0:dd}_{0:HH}-{0:mm}-{0:ss}.png", DateTime.Now);
                    var cache = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

                    using (var stream = await bitmap.OpenReadAsync())
                    {
                        var result = await ImageHelper.TranscodeAsync(stream, cache, BitmapEncoder.PngEncoderId);
                        var photo = await StoragePhoto.CreateAsync(result);
                        if (photo == null)
                        {
                            return;
                        }

                        Items.Add(photo);

                        UpdateView();
                        UpdatePanel();
                    }
                }
                else if (package.AvailableFormats.Contains(StandardDataFormats.StorageItems))
                {
                    var items = await package.GetStorageItemsAsync();
                    var results = await StorageMedia.CreateAsync(items);

                    foreach (var item in results)
                    {
                        Items.Add(item);
                    }

                    UpdateView();
                    UpdatePanel();
                }
            }
            catch { }
        }

        private int _itemsState = -1;
        private int _panelState = -1;

        private void UpdateView()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMediaOnly)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAlbumAvailable)));

            if (IsMediaSelected && !IsMediaOnly)
            {
                IsMediaSelected = false;
                IsFilesSelected = true;
            }
        }

        private void UpdatePanel()
        {
            if (IsAlbum && !IsAlbumAvailable)
            {
                IsAlbum = false;
            }
            else if (_wasAlbum && IsAlbumAvailable)
            {
                IsAlbum = true;
            }

            var state = IsAlbum && IsAlbumAvailable && IsMediaSelected ? 1 : 0;
            if (state != _panelState)
            {
                _panelState = state;
                if (state == 1)
                {
                    FindName(nameof(AlbumPanel));
                    AlbumPanel.Visibility = Visibility.Visible;

                    if (ListPanel != null)
                    {
                        ListPanel.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    if (Album != null)
                    {
                        AlbumPanel.Visibility = Visibility.Collapsed;
                    }

                    FindName(nameof(ListPanel));
                    ListPanel.Visibility = Visibility.Visible;
                }
            }

            if (Album?.ItemsPanelRoot is SendFilesAlbumPanel panel && IsAlbum)
            {
                var layout = new List<Size>();

                foreach (var item in Items)
                {
                    layout.Add(new Size(item.Width, item.Height));
                }

                panel.Sizes = layout;
                panel.Invalidate();
            }

            var mediaState = IsMediaSelected ? 1 : 0;
            if (mediaState != _itemsState && List != null)
            {
                _itemsState = mediaState;
                List.ItemTemplate = Resources[mediaState == 1 ? "MediaItemTemplate" : "FileItemTemplate"] as DataTemplate;
            }
        }

        private void PivotRadioButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateView();
            UpdatePanel();
        }

        private void Album_Click(object sender, RoutedEventArgs e)
        {
            _wasAlbum = AlbumButton.IsChecked == true;
            IsAlbum = _wasAlbum;

            UpdateView();
            UpdatePanel();
        }

        private void SendFilesAlbumPanel_Loading(FrameworkElement sender, object args)
        {
            UpdatePanel();
        }

        private void Ttl_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            var media = button.Tag as StorageMedia;

            var slider = new Slider();
            slider.IsThumbToolTipEnabled = false;
            slider.Header = MessageTtlConverter.Convert(MessageTtlConverter.ConvertSeconds(media.Ttl));
            slider.Minimum = 0;
            slider.Maximum = 28;
            slider.StepFrequency = 1;
            slider.SmallChange = 1;
            slider.LargeChange = 1;
            slider.Value = MessageTtlConverter.ConvertSeconds(media.Ttl);
            slider.ValueChanged += (s, args) =>
            {
                var index = (int)args.NewValue;
                var label = MessageTtlConverter.Convert(index);

                slider.Header = label;
                media.Ttl = MessageTtlConverter.ConvertBack(index);
            };

            var text = new TextBlock();
            text.Style = Navigation.BootStrapper.Current.Resources["InfoCaptionTextBlockStyle"] as Style;
            text.TextWrapping = TextWrapping.Wrap;
            text.Text = media is StoragePhoto
                ? Strings.Resources.MessageLifetimePhoto
                : Strings.Resources.MessageLifetimeVideo;

            var stack = new StackPanel();
            stack.Width = 260;
            stack.Children.Add(slider);
            stack.Children.Add(text);

            var flyout = new Flyout();
            flyout.Content = stack;

            flyout.ShowAt(button.Parent as UIElement, new FlyoutShowOptions { Placement = FlyoutPlacementMode.TopEdgeAlignedRight });
        }

        private void Mute_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            if (button.Tag is StorageVideo video)
            {
                button.IsChecked = !button.IsChecked == true;
                video.IsMuted = button.IsChecked == true;
            }
        }

        private async void Crop_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            if (button.Tag is StorageMedia media)
            {
                var dialog = new EditMediaPopup(media);

                var confirm = await dialog.ShowAsync();
                if (confirm == ContentDialogResult.Primary)
                {
                    media.Refresh();
                }
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button.Tag is StorageMedia media)
            {
                Items.Remove(media);
            }
        }

        protected override void OnApplyTemplate()
        {
            IsPrimaryButtonSplit = CanSchedule;

            var button = GetTemplateChild("PrimarySplitButton") as Button;
            if (button != null && CanSchedule)
            {
                button.Click += PrimaryButton_ContextRequested;
            }

            base.OnApplyTemplate();
        }

        private void PrimaryButton_ContextRequested(object sender, RoutedEventArgs args)
        {
            var self = IsSavedMessages;

            var flyout = new MenuFlyout();
            flyout.CreateFlyoutItem(new RelayCommand(() => { Silent = true; Hide(ContentDialogResult.Primary); }), Strings.Resources.SendWithoutSound, new FontIcon { Glyph = Icons.AlertOff });
            flyout.CreateFlyoutItem(new RelayCommand(() => { Schedule = true; Hide(ContentDialogResult.Primary); }), self ? Strings.Resources.SetReminder : Strings.Resources.ScheduleMessage, new FontIcon { Glyph = Icons.CalendarClock });

            flyout.ShowAt(sender as FrameworkElement, new FlyoutShowOptions { Placement = FlyoutPlacementMode.BottomEdgeAlignedRight });
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            CaptionInput.Focus(FocusState.Keyboard);
            Window.Current.CoreWindow.CharacterReceived += OnCharacterReceived;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.CharacterReceived -= OnCharacterReceived;
        }

        private void OnCharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            var character = Encoding.UTF32.GetString(BitConverter.GetBytes(args.KeyCode));
            if (character.Length == 0)
            {
                return;
            }
            else if (character != "\u0016" && character != "\r" && char.IsControl(character[0]))
            {
                return;
            }
            else if (character != "\u0016" && character != "\r" && char.IsWhiteSpace(character[0]))
            {
                return;
            }

            var focused = FocusManager.GetFocusedElement();
            if (focused is null or (not TextBox and not RichEditBox))
            {
                if (character == "\u0016" && CaptionInput.CanPasteClipboardContent)
                {
                    CaptionInput.Focus(FocusState.Keyboard);
                    CaptionInput.PasteFromClipboard();
                }
                else if (character == "\r")
                {
                    Accept();
                }
                else
                {
                    CaptionInput.Focus(FocusState.Keyboard);
                    CaptionInput.InsertText(character);
                }

                args.Handled = true;
            }
        }

        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            // We don't want to unfocus the text are when the context menu gets opened
            EmojiFlyout.ShowAt(CaptionInput, new FlyoutShowOptions { ShowMode = FlyoutShowMode.Transient });
        }

        private void Emoji_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is EmojiData emoji)
            {
                EmojiFlyout.Hide();

                CaptionInput.InsertText(emoji.Value);
                CaptionInput.Focus(FocusState.Programmatic);
            }
        }
    }

    public class SendFilesAlbumPanel : Grid
    {
        public const double ITEM_MARGIN = 2;
        public const double MAX_WIDTH = 320 + ITEM_MARGIN;
        public const double MAX_HEIGHT = 420 + ITEM_MARGIN;

        private (Rect[], Size) _positions;
        private ((Rect, MosaicItemPosition)[], Size)? _positionsBase;

        public List<Size> Sizes;

        protected override Size MeasureOverride(Size availableSize)
        {
            var sizes = Sizes;
            if (sizes == null || sizes.Count == 1)
            {
                return base.MeasureOverride(availableSize);
            }

            var positions = GetPositionsForWidth(availableSize.Width);

            for (int i = 0; i < Math.Min(positions.Item1.Length, Children.Count); i++)
            {
                Children[i].Measure(new Size(positions.Item1[i].Width, positions.Item1[i].Height));
            }

            _positions = positions;
            return positions.Item2;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var positions = _positions;
            if (positions.Item1 == null || positions.Item1.Length == 1)
            {
                return base.ArrangeOverride(finalSize);
            }

            for (int i = 0; i < Math.Min(positions.Item1.Length, Children.Count); i++)
            {
                Children[i].Arrange(positions.Item1[i]);
            }

            return positions.Item2;
        }

        public void Invalidate()
        {
            _positionsBase = null;

            InvalidateMeasure();
            InvalidateArrange();
        }

        private (Rect[], Size) GetPositionsForWidth(double w)
        {
            var positions = _positionsBase ??= MosaicAlbumLayout.chatMessageBubbleMosaicLayout(new Size(MAX_WIDTH, MAX_HEIGHT), Sizes);

            var ratio = w / MAX_WIDTH;
            var rects = new Rect[positions.Item1.Length];

            for (int i = 0; i < rects.Length; i++)
            {
                var rect = positions.Item1[i].Item1;
                rects[i] = new Rect(rect.X * ratio, rect.Y * ratio, rect.Width * ratio, rect.Height * ratio);
            }

            return (rects, new Size(positions.Item2.Width * ratio, positions.Item2.Height * ratio));
        }
    }
}
