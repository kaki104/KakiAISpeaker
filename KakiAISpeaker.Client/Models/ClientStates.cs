using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KakiAISpeaker.Client.Models
{
    /// <summary>
    /// 클라이언트 상태
    /// </summary>
    public enum ClientStates
    {
        Idle,

        /// <summary>
        ///     대화시작
        /// </summary>
        StartConversation,

        /// <summary>
        ///     시스템 메시지 시작
        /// </summary>
        PlaySystemVoice,

        /// <summary>
        ///     시스템 메시지 종료
        /// </summary>
        StopSystemVoice,

        /// <summary>
        ///     녹음 시작
        /// </summary>
        StartRecoding,

        /// <summary>
        ///     녹음 끝
        /// </summary>
        StopRecoding,

        /// <summary>
        ///     녹음된 음성 전달
        /// </summary>
        SendVoiceCommand,

        /// <summary>
        ///     음성 명령 결과 반환
        /// </summary>
        ReceiveVoiceCommandResult,

        /// <summary>
        ///     음성 명령 결과 재생 시작
        /// </summary>
        PlayVoiceCommandResult,

        /// <summary>
        ///     음성 명령 결과 재생 종료
        /// </summary>
        StopVoiceCommandResult,

        /// <summary>
        ///     대화 종료
        /// </summary>
        EndConversation
    }
}
