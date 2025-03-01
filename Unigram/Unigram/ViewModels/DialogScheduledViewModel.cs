﻿using Unigram.Services;
using Unigram.Services.Factories;

namespace Unigram.ViewModels
{
    public class DialogScheduledViewModel : DialogViewModel
    {
        public DialogScheduledViewModel(IProtoService protoService, ICacheService cacheService, ISettingsService settingsService, IEventAggregator aggregator, ILocationService locationService, INotificationsService pushService, IPlaybackService playbackService, IVoipService voipService, IGroupCallService groupCallService, INetworkService networkService, IStorageService storageService, IMessageFactory messageFactory)
            : base(protoService, cacheService, settingsService, aggregator, locationService, pushService, playbackService, voipService, groupCallService, networkService, storageService, messageFactory)
        {
        }

        public override DialogType Type => DialogType.ScheduledMessages;
    }
}
