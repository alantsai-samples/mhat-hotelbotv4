using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MHAT.HotelBotV4.Dialog
{
    public class HotelDialogSet : DialogSet
    {
        public HotelDialogSet(IStatePropertyAccessor<DialogState> dialogState) 
            : base(dialogState)
        {
        }
    }
}
