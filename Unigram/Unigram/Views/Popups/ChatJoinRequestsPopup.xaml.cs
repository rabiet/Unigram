﻿using System.Runtime.InteropServices.WindowsRuntime;
using Telegram.Td.Api;
using Unigram.Collections;
using Unigram.Common;
using Unigram.Controls;
using Unigram.Converters;
using Unigram.Services;
using Unigram.ViewModels;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Unigram.Views.Popups
{
    public sealed partial class ChatJoinRequestsPopup : ContentPopup
    {
        public ChatJoinRequestsViewModel ViewModel => DataContext as ChatJoinRequestsViewModel;

        public ChatJoinRequestsPopup(IProtoService protoService, ICacheService cacheService, ISettingsService settingsService, IEventAggregator aggregator, Chat chat, string inviteLink)
        {
            InitializeComponent();
            DataContext = new ChatJoinRequestsViewModel(chat, inviteLink, protoService, cacheService, settingsService, aggregator);

            Title = Strings.Resources.MemberRequests;
            PrimaryButtonText = Strings.Resources.Close;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void OnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue)
            {
                return;
            }

            var content = args.ItemContainer.ContentTemplateRoot as Grid;
            var request = args.Item as ChatJoinRequest;

            var user = ViewModel.CacheService.GetUser(request.UserId);
            if (user == null)
            {
                return;
            }

            if (args.Phase == 0)
            {
                var title = content.Children[1] as TextBlock;
                title.Text = user.GetFullName();

                var stack = content.Children[4] as StackPanel;
                var primary = stack.Children[0] as Button;
                var secondary = stack.Children[1] as HyperlinkButton;

                primary.CommandParameter = request;
                primary.Command = ViewModel.AcceptCommand;

                secondary.CommandParameter = request;
                secondary.Command = ViewModel.DismissCommand;

                primary.Content = ViewModel.IsChannel
                    ? Strings.Resources.AddToChannel
                    : Strings.Resources.AddToGroup;
            }
            else if (args.Phase == 1)
            {
                var time = content.Children[2] as TextBlock;
                time.Text = Converter.DateExtended(request.Date);

                if (string.IsNullOrEmpty(request.Bio))
                {
                    var subtitle = content.Children[3] as TextBlock;
                    subtitle.Visibility = Visibility.Collapsed;

                    Grid.SetRow(content.Children[4] as StackPanel, 1);
                }
                else
                {
                    var subtitle = content.Children[3] as TextBlock;
                    subtitle.Text = request.Bio;
                    subtitle.Visibility = Visibility.Visible;

                    Grid.SetRow(content.Children[4] as StackPanel, 2);
                }
            }
            else if (args.Phase == 2)
            {
                var photo = content.Children[0] as ProfilePicture;
                photo.SetUser(ViewModel.ProtoService, user, 36);
            }

            if (args.Phase < 2)
            {
                args.RegisterUpdateCallback(OnContainerContentChanging);
            }

            args.Handled = true;
        }
    }

    public class ChatJoinRequestsViewModel : TLViewModelBase
    {
        private readonly Chat _chat;
        private readonly string _inviteLink;

        public ChatJoinRequestsViewModel(Chat chat, string inviteLink, IProtoService protoService, ICacheService cacheService, ISettingsService settingsService, IEventAggregator aggregator)
            : base(protoService, cacheService, settingsService, aggregator)
        {
            _chat = chat;
            _inviteLink = inviteLink;

            Items = new ItemCollection(protoService, chat, inviteLink);

            AcceptCommand = new RelayCommand<ChatJoinRequest>(Accept);
            DismissCommand = new RelayCommand<ChatJoinRequest>(Dismiss);
        }

        public bool IsChannel => _chat?.Type is ChatTypeSupergroup supergroup && supergroup.IsChannel;

        public ItemCollection Items { get; private set; }

        public RelayCommand<ChatJoinRequest> AcceptCommand { get; }
        private void Accept(ChatJoinRequest request)
        {
            Process(request, true);
        }

        public RelayCommand<ChatJoinRequest> DismissCommand { get; }
        private void Dismiss(ChatJoinRequest request)
        {
            Process(request, false);
        }

        private void Process(ChatJoinRequest request, bool approve)
        {
            Items.Remove(request);
            ProtoService.Send(new ProcessChatJoinRequest(_chat.Id, request.UserId, approve));
        }

        public class ItemCollection : MvxObservableCollection<ChatJoinRequest>, ISupportIncrementalLoading
        {
            private readonly IProtoService _protoService;
            private readonly Chat _chat;
            private readonly string _inviteLink;

            private ChatJoinRequest _offset;
            private bool _hasMoreItems = true;

            public ItemCollection(IProtoService protoService, Chat chat, string inviteLink)
            {
                _protoService = protoService;
                _chat = chat;
                _inviteLink = inviteLink;
            }

            public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
            {
                return AsyncInfo.Run(async token =>
                {
                    var response = await _protoService.SendAsync(new GetChatJoinRequests(_chat.Id, _inviteLink, string.Empty, _offset, 10));
                    if (response is ChatJoinRequests requests)
                    {
                        foreach (var item in requests.Requests)
                        {
                            _offset = item;
                            Add(item);
                        }

                        _hasMoreItems = requests.Requests.Count > 0;
                    }

                    return new LoadMoreItemsResult { Count = 0 };
                });
            }

            public bool HasMoreItems => _hasMoreItems;
        }
    }
}
