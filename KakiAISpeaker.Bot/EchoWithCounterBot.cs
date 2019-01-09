// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KakiAISpeaker.Bot.Helpers;
using KakiAISpeaker.Bot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace KakiAISpeaker.Bot
{
    /// <summary>
    ///     Represents a bot that processes incoming activities.
    ///     For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    ///     This is a Transient lifetime service.  Transient lifetime services are created
    ///     each time they're requested. For each Activity received, a new instance of this
    ///     class is created. Objects that are expensive to construct, or have a lifetime
    ///     beyond the single turn, should be carefully managed.
    ///     For example, the <see cref="MemoryStorage" /> object and associated
    ///     <see cref="IStatePropertyAccessor{T}" /> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1" />
    public class EchoWithCounterBot : IBot
    {
        private readonly EchoBotAccessors _accessors;
        private readonly BlobHelper _blobHelper;
        private readonly ILogger _logger;
        private readonly PollyHelper _pollyHelper;
        private readonly SpeechHelper _speechHelper;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EchoWithCounterBot" /> class.
        /// </summary>
        /// <seealso
        ///     cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider" />
        public EchoWithCounterBot(EchoBotAccessors accessors, ILoggerFactory loggerFactory,
            SpeechHelper speechHelper, BlobHelper blobHelper, PollyHelper pollyHelper)
        {
            _speechHelper = speechHelper;
            _blobHelper = blobHelper;
            _pollyHelper = pollyHelper;

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger<EchoWithCounterBot>();
            _logger.LogTrace("EchoBot turn start.");
            _accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));
        }

        /// <summary>
        ///     Every conversation turn for our Echo Bot will call this method.
        ///     There are no dialogs used, since it's "single turn" processing, meaning a single
        ///     request and response.
        /// </summary>
        /// <param name="turnContext">
        ///     A <see cref="ITurnContext" /> containing all the data needed
        ///     for processing this conversation turn.
        /// </param>
        /// <param name="cancellationToken">
        ///     (Optional) A <see cref="CancellationToken" /> that can be used by other objects
        ///     or threads to receive notice of cancellation.
        /// </param>
        /// <returns>A <see cref="Task" /> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet" />
        /// <seealso cref="ConversationState" />
        /// <seealso cref="IMiddleware" />
        public async Task OnTurnAsync(ITurnContext turnContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Get the conversation state from the turn context.
                var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

                // Bump the turn count for this conversation.
                state.TurnCount++;

                // Set the property using the accessor.
                await _accessors.CounterState.SetAsync(turnContext, state);

                // Save the new turn count into the conversation state.
                await _accessors.ConversationState.SaveChangesAsync(turnContext);

                IMessageActivity responseMessageActivity;
                switch (turnContext.Activity.Text.ToLower())
                {
                    case "start":
                        responseMessageActivity = turnContext.Activity.CreateReply();
                        responseMessageActivity.Text = $"Start conversation {state.TurnCount}";
                        break;
                    case "local test":
                        //로컬에서 테스트할 때
                        var fileFullPath = Environment.GetEnvironmentVariable("HOME") + "test.wav";
                        var testResult = await _speechHelper.GetSpeechResultFromAudioAsync(fileFullPath);
                        if (testResult == null) return;

                        var mp3FileName = $"{testResult.DisplayText}.mp3";
                        var list = await _blobHelper.ListBlobAsync();
                        var exist = list.Results.Cast<CloudBlockBlob>()
                            .FirstOrDefault(i => i.Name == mp3FileName);
                        if (exist == null)
                        {
                            var pollyResult =
                                await _pollyHelper.GetSynthesizeSpeechFromTextAsync(testResult.DisplayText);
                            if (pollyResult == null) return;
                            await _blobHelper.UploadBlockBlobAsync(mp3FileName, pollyResult.AudioStream);
                        }

                        var testAttachment = await MakeAttachmentAsync(mp3FileName);
                        responseMessageActivity = turnContext.Activity.CreateReply();
                        responseMessageActivity.Text = testResult.DisplayText;
                        responseMessageActivity.Attachments = new List<Attachment> {testAttachment};
                        break;
                    case "voice command":
                        var attachment = turnContext.Activity.Attachments.FirstOrDefault();
                        if (attachment == null)
                        {
                            responseMessageActivity = new Activity(text: $"No voice data found {state.TurnCount}");
                        }
                        else
                        {
                            var result = await _speechHelper.GetSpeechResultFromAttachmentAsync(attachment);
                            responseMessageActivity = await GetResponseFromSpeechResultAsync(turnContext, result);
                        }

                        break;
                    default:
                        responseMessageActivity = new Activity(text: $"Unknown message {state.TurnCount}");
                        break;
                }

                await turnContext.SendActivityAsync(responseMessageActivity);
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }


        private async Task<IMessageActivity> GetResponseFromSpeechResultAsync(ITurnContext turnContext,
            SpeechResult speechResult)
        {
            if (speechResult == null) return null;

            var reply = turnContext.Activity.CreateReply();

            if (speechResult.DisplayText.Contains("도움"))
            {
                var helpAttachment = await MakeAttachmentAsync("도움말");
                reply.Text = "help message";
                reply.Attachments.Add(helpAttachment);
            }
            else if (speechResult.DisplayText.Contains("운전시작") || speechResult.DisplayText.Contains("운전 시작"))
            {
                var runAttachment = await MakeAttachmentAsync("운전시작");
                reply.Text = "run message";
                reply.Attachments.Add(runAttachment);
            }
            else if (speechResult.DisplayText.Contains("운전종료") || speechResult.DisplayText.Contains("운전 종료"))
            {
                var stopAttachment = await MakeAttachmentAsync("운전종료");
                reply.Text = "stop message";
                reply.Attachments.Add(stopAttachment);
            }
            else if (speechResult.DisplayText.Contains("오늘 날씨") || speechResult.DisplayText.Contains("오늘날씨") ||
                     speechResult.DisplayText.Contains("오늘의날씨"))
            {
                //날씨 api를 조회해서 결과 반환 - 하루에 한번 날짜로 파일명 생성
                //해당 내용을 AWS를 이용해서 음성으로 변환
                //변환된 mp3 파일을 Blob에 올리고 경로 반환
                var weatherAttachment = await MakeAttachmentAsync("오늘날씨");
                reply.Text = "today weather message";
                reply.Attachments.Add(weatherAttachment);
            }
            else if (speechResult.DisplayText.Contains("미세 먼지 정보") || speechResult.DisplayText.Contains("미세먼지 정보") ||
                     speechResult.DisplayText.Contains("미세먼지정보") || speechResult.DisplayText.Contains("미세먼지") ||
                     speechResult.DisplayText.Contains("미세 먼지"))
            {
                var dustAttachment = await MakeAttachmentAsync("미세먼지정보");
                reply.Text = "today dust message";
                reply.Attachments.Add(dustAttachment);
            }
            else
            {
                reply.Text = "Unknown command";
                reply.Value = speechResult.DisplayText;
            }

            return reply;
        }

        /// <summary>
        ///     Create an attachment
        /// </summary>
        /// <param name="findText"></param>
        /// <returns></returns>
        private async Task<Attachment> MakeAttachmentAsync(string findText)
        {
            var list = await _blobHelper.ListBlobAsync();
            var result = list.Results.Cast<CloudBlockBlob>().FirstOrDefault(i => i.Name.Contains(findText));
            if (result == null) return null;
            var audio = new Attachment
            {
                ContentUrl = result.SnapshotQualifiedUri.ToString(),
                ContentType = result.BlobType.ToString(),
                Name = result.Name
            };
            return audio;
        }
    }
}