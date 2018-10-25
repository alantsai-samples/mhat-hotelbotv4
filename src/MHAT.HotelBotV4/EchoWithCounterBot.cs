// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace MHAT.HotelBotV4
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class EchoWithCounterBot : IBot
    {
        private readonly EchoBotAccessors _accessors;
        private readonly ILogger _logger;

        private readonly DialogSet _dialogs;

        private readonly DialogSet _dialogsWaterfall;

        /// <summary>
        /// Initializes a new instance of the <see cref="EchoWithCounterBot"/> class.
        /// </summary>
        /// <param name="accessors">A class containing <see cref="IStatePropertyAccessor{T}"/> used to manage state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public EchoWithCounterBot(EchoBotAccessors accessors, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<EchoWithCounterBot>();
            _logger.LogTrace("EchoBot turn start.");
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));

            _dialogs = new DialogSet(_accessors.DialogState);
            _dialogs.Add(new TextPrompt("askName"));

            _dialogsWaterfall = new DialogSet(_accessors.DialogState);

            var waterfallSteps = new WaterfallStep[]
            {
                GetStartStayDateAsync,
                GetStayDayAsync,
                GetNumberOfOccupantAsync,
                GetBedSizeAsync,
                GetConfirmAsync,
                GetSummaryAsync,
            };

            _dialogsWaterfall.Add(new WaterfallDialog("formFlow", waterfallSteps));
            _dialogsWaterfall.Add(new DateTimePrompt("dateTime"));
            _dialogsWaterfall.Add(new NumberPrompt<int>("number"));
            _dialogsWaterfall.Add(new ChoicePrompt("choice"));
            _dialogsWaterfall.Add(new ConfirmPrompt("confirm"));
        }

        private async Task<DialogTurnResult> GetSummaryAsync
            (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                await stepContext.Context.SendActivityAsync
                    ($"訂單下定完成，訂單號：{DateTime.Now.Ticks}");
            }
            else
            {
                await stepContext.Context.SendActivityAsync("已經取消訂單");
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> GetConfirmAsync
            (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            (await GetCounterState(stepContext.Context))
                .RoomReservation.BedSize = (string)stepContext.Result;

            return await stepContext.PromptAsync("confirm", new PromptOptions()
            {
                Prompt = MessageFactory.Text("親確認您的訂房條件：")
            });
        }

        private async Task<DialogTurnResult> GetBedSizeAsync
            (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            (await GetCounterState(stepContext.Context))
                .RoomReservation.NumberOfPepole = (int)stepContext.Result;

            var choices = new List<Choice>()
            {
                new Choice("單人床"),
                new Choice("雙人床"),
            };

            return await stepContext.PromptAsync("choice",
                new PromptOptions()
                {
                    Prompt = MessageFactory.Text("請選擇床型"),
                    Choices = choices,
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> GetNumberOfOccupantAsync
            (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            (await GetCounterState(stepContext.Context))
                .RoomReservation.NumberOfNightToStay = (int)stepContext.Result - 1;

            return await stepContext.PromptAsync("number",
                new PromptOptions()
                {
                    Prompt = MessageFactory.Text("幾人入住"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> GetStayDayAsync
            (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            (await GetCounterState(stepContext.Context))
                .RoomReservation.StartDate = (DateTime) stepContext.Result;

            return await stepContext.PromptAsync("number", new PromptOptions()
            {
                 Prompt = MessageFactory.Text("請輸入要住幾天"),
            }, 
            cancellationToken);
        }

        private async Task<DialogTurnResult> GetStartStayDateAsync
            (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync("datetime",
                new PromptOptions()
                {
                    Prompt = MessageFactory.Text("請輸入入住日期"),
                },
                cancellationToken);
        }

        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                CounterState state = await GetCounterState(turnContext);

                var userInfo = await _accessors.UserInfo.GetAsync(turnContext, () => new Model.UserInfo());

                var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
                var dialogResult = await dialogContext.ContinueDialogAsync(cancellationToken);

                if (string.IsNullOrEmpty(userInfo.Name)
                        && dialogResult.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.PromptAsync(
                            "askName",
                            new PromptOptions
                            { Prompt = MessageFactory.Text("請問尊姓大名？") },
                            cancellationToken);

                }
                else if (dialogResult.Status == DialogTurnStatus.Complete)
                {
                    if (dialogResult.Result != null)
                    {
                        userInfo.Name = dialogResult.Result.ToString();

                        await _accessors.UserInfo.SetAsync(turnContext, userInfo);
                        await _accessors.UserState.SaveChangesAsync(turnContext);

                        await turnContext.SendActivityAsync($"{userInfo.Name} 您好");
                    }
                }
                else
                {
                    // Bump the turn count for this conversation.
                    state.TurnCount++;

                    // Set the property using the accessor.
                    await _accessors.CounterState.SetAsync(turnContext, state);

                    // Save the new turn count into the conversation state.

                    // Echo back to the user whatever they typed.
                    var responseMessage = $"Name: {userInfo.Name} Turn {state.TurnCount}: You sent '{turnContext.Activity.Text}'\n";
                    await turnContext.SendActivityAsync(responseMessage);
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }

            await _accessors.ConversationState.SaveChangesAsync(turnContext);
        }

        private async Task<CounterState> GetCounterState(ITurnContext turnContext)
        {
            // Get the conversation state from the turn context.
            return await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());
        }
    }
}
