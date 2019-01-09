using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KakiAISpeaker.Bot.Models
{
    /// <summary>
    /// simple result
    /// </summary>
    public class SpeechResult
    {
        public string RecognitionStatus { get; set; }
        public string DisplayText { get; set; }
        public string Offset { get; set; }
        public string Duration { get; set; }
    }
}
