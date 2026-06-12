using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.ServiceModel;
namespace Common
{
    [ServiceContract]
    public  interface ISensorService
    {
        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        SessionResponse StartSession(SensorSample metaHeader);


        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        SessionResponse PushSample(SensorSample sample);

        [OperationContract]
        SessionResponse EndSession();
    }

    //klasa za greske koje server baca ako validacija padne

    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string ErrorMessage { get; set; }
        public ValidationFault(string msg)
        {
            ErrorMessage = msg;
        }
    }

    [DataContract]
    public class DataFormatFault
    {
        [DataMember]
        public string ErrorMessage { get; set; }

        public DataFormatFault(string msg)
        {
            ErrorMessage = msg;
        }

    }
}
