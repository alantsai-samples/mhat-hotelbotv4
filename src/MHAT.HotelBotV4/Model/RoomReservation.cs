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

        public override string ToString()
        {
            return $"入住日期：{StartDate}{Environment.NewLine}" +
            $"入住幾晚：{NumberOfNightToStay}{Environment.NewLine}" +
            $"幾人：{NumberOfPepole}{Environment.NewLine}" +
            $"床型：{BedSize}{Environment.NewLine}";
        }
    }
}
