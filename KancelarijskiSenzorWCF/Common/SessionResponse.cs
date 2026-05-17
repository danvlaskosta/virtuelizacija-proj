using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;


namespace Common
{
    [DataContract]
    public class SessionResponse
    {
        [DataMember]
        public ResponseResult Result { get; set; }

        [DataMember]
        public SessionStatus Status { get; set; }

        [DataMember]
        public string Message { get; set; }

        public SessionResponse() { }

        public SessionResponse(ResponseResult result, SessionStatus status, string message = "")
        {
            Result = result;
            Status = status;
            Message = message;
        }
        
    }
}
