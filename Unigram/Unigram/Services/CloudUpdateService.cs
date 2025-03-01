﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Td.Api;
using Unigram.Common;
using Unigram.Services.Updates;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Unigram.Services
{
    public interface ICloudUpdateService
    {
        CloudUpdate NextUpdate { get; }

        Task UpdateAsync(bool force);
        //Task<CloudUpdate> GetNextUpdateAsync();
        //Task<IList<CloudUpdate>> GetHistoryAsync();
    }

    public class CloudUpdateService : ICloudUpdateService
    {
        private readonly FileContext<CloudUpdate> _mapping = new FileContext<CloudUpdate>();

        private readonly IProtoService _protoService;
        private readonly INetworkService _networkService;
        private readonly IEventAggregator _aggregator;

        private long? _chatId;
        private CloudUpdate _nextUpdate;

        private long _lastCheck;
        private bool _checking;

        public CloudUpdateService(IProtoService protoService, INetworkService networkService, IEventAggregator aggregator)
        {
            _protoService = protoService;
            _networkService = networkService;
            _aggregator = aggregator;
        }

        public CloudUpdate NextUpdate => _nextUpdate;

        public async void Update()
        {
            await UpdateAsync(false);
        }

        public async Task UpdateAsync(bool force)
        {
            if (ApiInfo.IsStoreRelease)
            {
                return;
            }

            var diff = Environment.TickCount - _lastCheck;
            var skip = diff < 5 * 60 * 1000 || _checking;

            if (skip && !force)
            {
                return;
            }

            _checking = true;
            _lastCheck = diff;

            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("updates", CreationCollisionOption.OpenIfExists);
            var files = await folder.GetFilesAsync();

            var current = Package.Current.Id.Version.ToVersion();
            var sets = new List<Version>();

            foreach (var file in files)
            {
                var split = System.IO.Path.GetFileNameWithoutExtension(file.Name);
                if (Version.TryParse(split, out Version version))
                {
                    sets.Add(version);
                }
            }

            foreach (var version in sets)
            {
                // If this isn't the most recent version and it isn't in use, just delete it
                if (version <= current)
                {
                    var file = await folder.TryGetItemAsync($"{version}.msixbundle") as StorageFile;
                    if (file != null)
                    {
                        await file.DeleteAsync();
                    }
                }

                // We can safely ignore any other version as next steps will take care of them
            }

            var cloud = await GetNextUpdateAsync();
            if (cloud != null && cloud.Version > current)
            {
                _nextUpdate = cloud;

                // There's a new version for the current font
                if (cloud.Document.Local.IsDownloadingCompleted || cloud.File != null)
                {
                    _aggregator.Publish(new UpdateAppVersion(cloud));
                }
                else if (cloud.Document.Local.CanBeDownloaded && !cloud.Document.Local.IsDownloadingActive)
                {
                    var date = Utils.ToDateTime(cloud.Date);
                    var epoch = DateTime.Now - date;

                    if (epoch.TotalDays >= 3 || !_networkService.IsMetered)
                    {
                        _protoService.DownloadFile(cloud.Document.Id, 16);
                        UpdateManager.Subscribe(cloud, _protoService, cloud.Document, UpdateFile, true);
                    }
                }
            }

            // This call is needed to delete old updates from disk
            await GetHistoryAsync();

            _checking = false;
        }

        private async void UpdateFile(object target, File file)
        {
            var cloud = target as CloudUpdate;
            if (cloud == null)
            {
                return;
            }

            if (file.Local.IsDownloadingCompleted)
            {
                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("updates", CreationCollisionOption.OpenIfExists);
                var result = await TryCopyPartLocally(folder, file.Local.Path, cloud.Version);

                var current = Package.Current.Id.Version.ToVersion();
                if (current < cloud.Version)
                {
                    _nextUpdate = cloud;
                    _nextUpdate.File = result;

                    _aggregator.Publish(new UpdateAppVersion(cloud));
                }
            }
        }

        public async Task<CloudUpdate> GetNextUpdateAsync()
        {
            if (ApiInfo.IsStoreRelease)
            {
                return null;
            }

            if (_chatId == null)
            {
                var chat = await _protoService.SendAsync(new SearchPublicChat(Constants.AppChannel)) as Chat;
                if (chat != null)
                {
                    _chatId = chat.Id;
                }
            }

            if (_chatId == null)
            {
                return null;
            }

            var chatId = _chatId.Value;
            await _protoService.SendAsync(new OpenChat(chatId));

            var messages = await _protoService.SendAsync(new SearchChatMessages(chatId, string.Empty, null, 0, 0, 10, new SearchMessagesFilterDocument(), 0)) as Messages;
            if (messages == null)
            {
                _protoService.Send(new CloseChat(chatId));
                return null;
            }

            _protoService.Send(new CloseChat(chatId));

            foreach (var message in messages.MessagesValue)
            {
                var document = message.Content as MessageDocument;
                if (document == null)
                {
                    continue;
                }

                var hashtags = new List<string>();
                var changelog = string.Empty;

                foreach (var entity in document.Caption.Entities)
                {
                    if (entity.Type is TextEntityTypeHashtag)
                    {
                        hashtags.Add(document.Caption.Text.Substring(entity.Offset, entity.Length));
                    }
                    else if (entity.Type is TextEntityTypeCode)
                    {
                        changelog = document.Caption.Text.Substring(entity.Offset, entity.Length);
                    }
                }

                if (!hashtags.Contains("#update") || !document.Document.FileName.Contains("x64") || !document.Document.FileName.EndsWith(".msixbundle"))
                {
                    continue;
                }

                var split = document.Document.FileName.Split('_');
                if (split.Length >= 3 && Version.TryParse(split[1], out Version version))
                {
                    var set = new CloudUpdate
                    {
                        MessageId = message.Id,
                        Changelog = changelog,
                        Version = version,
                        Document = document.Document.DocumentValue
                    };

                    var current = Package.Current.Id.Version.ToVersion();
                    var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("updates", CreationCollisionOption.OpenIfExists);

                    if (set.Version > current)
                    {
                        if (set.Document.Local.IsDownloadingCompleted)
                        {
                            set.File = await TryCopyPartLocally(folder, set.Document.Local.Path, set.Version);
                        }
                        else
                        {
                            set.File = await folder.TryGetItemAsync($"{set.Version}.msixbundle") as StorageFile;
                        }

                        return set;
                    }
                    else
                    {
                        // Delete the file from chat cache as it isn't needed anymore
                        _protoService.Send(new DeleteFileW(set.Document.Id));
                    }
                }
            }

            return null;
        }

        public async Task<IList<CloudUpdate>> GetHistoryAsync()
        {
            if (ApiInfo.IsStoreRelease)
            {
                return null;
            }

            var chat = await _protoService.SendAsync(new SearchPublicChat("cGFnbGlhY2Npb19kaV9naGlhY2Npbw")) as Chat;
            if (chat == null)
            {
                return null;
            }

            await _protoService.SendAsync(new OpenChat(chat.Id));

            var response = await _protoService.SendAsync(new SearchChatMessages(chat.Id, "#update", null, 0, 0, 100, new SearchMessagesFilterDocument(), 0)) as Messages;
            if (response == null)
            {
                _protoService.Send(new CloseChat(chat.Id));
                return null;
            }

            _protoService.Send(new CloseChat(chat.Id));

            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("updates", CreationCollisionOption.OpenIfExists);

            var dict = new List<CloudUpdate>();
            var thumbnails = new Dictionary<string, File>();

            var results = new List<CloudUpdate>();

            foreach (var message in response.MessagesValue)
            {
                var document = message.Content as MessageDocument;
                if (document == null)
                {
                    continue;
                }

                var hashtags = new List<string>();
                var changelog = string.Empty;

                foreach (var entity in document.Caption.Entities)
                {
                    if (entity.Type is TextEntityTypeHashtag)
                    {
                        hashtags.Add(document.Caption.Text.Substring(entity.Offset, entity.Length));
                    }
                    else if (entity.Type is TextEntityTypeCode)
                    {
                        changelog = document.Caption.Text.Substring(entity.Offset, entity.Length);
                    }
                }

                if (!hashtags.Contains("#update") || !document.Document.FileName.Contains("x64") || !document.Document.FileName.EndsWith(".msixbundle"))
                {
                    continue;
                }

                var split = document.Document.FileName.Split('_');
                if (split.Length >= 3 && Version.TryParse(split[1], out Version version))
                {
                    var set = new CloudUpdate
                    {
                        MessageId = message.Id,
                        Changelog = changelog,
                        Version = version,
                        Document = document.Document.DocumentValue,
                        Date = message.Date
                    };

                    dict.Add(set);
                }
            }

            var current = Package.Current.Id.Version.ToVersion();
            var latest = dict.Count > 0 ? dict.Max(x => x.Version) : null;

            foreach (var set in dict)
            {
                if (set.Version < current)
                {
                    // Delete the file from chat cache as it isn't needed anymore
                    _protoService.Send(new DeleteFileW(set.Document.Id));
                }

                results.Add(set);
            }

            return results.OrderByDescending(x => x.Version).ToList();
        }

        private static async Task<StorageFile> TryCopyPartLocally(StorageFolder folder, string path, Version version)
        {
            var fileName = $"{version}.msixbundle";

            var cache = await folder.TryGetItemAsync(fileName) as StorageFile;
            if (cache == null)
            {
                try
                {
                    var result = await StorageFile.GetFileFromPathAsync(path);
                    return await result.CopyAsync(folder, fileName, NameCollisionOption.FailIfExists);
                }
                catch { }
            }

            return cache;
        }
    }

    public class CloudUpdate
    {
        public long MessageId { get; set; }

        public Version Version { get; set; }
        public string Changelog { get; set; }

        public File Document { get; set; }
        public StorageFile File { get; set; }

        public int Date { get; set; }

        public bool UpdateFile(File file)
        {
            if (Document.Id == file.Id)
            {
                Document = file;
                return true;
            }

            return false;
        }
    }
}
