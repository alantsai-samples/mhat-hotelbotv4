using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MHAT.HotelBotV4.Model
{
    public class RoomReservation
    {
        public DateTime StartDate { get; set; }
        public int NumberOfNightToStay { get; set; }
        public int NumberOfPepole { get; set; }
        public string BedSize { get; set; }
    }
}
