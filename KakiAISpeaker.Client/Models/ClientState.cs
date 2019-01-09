using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KakiAISpeaker.Client.Models
{
    /// <summary>
    ///     클라이언트 상태 모델
    /// </summary>
    public class ClientState
    {
        /// <summary>
        ///     상태 enum
        /// </summary>
        public ClientStates States { get; set; }

        /// <summary>
        ///     상태에 필요한 데이터
        /// </summary>
        public object Data { get; set; }
    }
}
