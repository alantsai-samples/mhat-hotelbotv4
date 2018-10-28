using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MHAT.HotelBotV4.Dialog
{
    public class HotelDialogSet : DialogSet
    {
        private EchoBotAccessors _accessors;

        public string askNameWaterfall { get; } = "askNameWaterfall";

        public HotelDialogSet
            (IStatePropertyAccessor<DialogState> dialogState, EchoBotAccessors accessors) 
            : base(dialogState)
        {
            _accessors = accessors;

            var askNameDialogSet = new WaterfallStep[]
            {
                StartPromptName,
                ProcessPromptName,
            };

            Add(new WaterfallDialog(askNameWaterfall, askNameDialogSet));

            Add(new TextPrompt("textPrompt"));
        }

        private async Task<DialogTurnResult> ProcessPromptName
            (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userInfo = await _accessors.UserInfo.GetAsync
                (stepContext.Context, () => new Model.UserInfo());

            userInfo.Name = stepContext.Result.ToString();

            await _accessors.UserInfo.SetAsync(stepContext.Context, userInfo);
            await _accessors.UserState.SaveChangesAsync(stepContext.Context);

            await stepContext.Context.SendActivityAsync($"{userInfo.Name} 您好");

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> StartPromptName
            (WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync("textPrompt", new PromptOptions()
            {
                Prompt = MessageFactory.Text("請問尊姓大名？"),
            }, 
            cancellationToken);
        }
    }
}
