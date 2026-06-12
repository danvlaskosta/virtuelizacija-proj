using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace Common
{

    [DataContract]
    public class SensorSample

    {
        [DataMember]
        public double Volume { get; set; }

        [DataMember]
        public double CO { get; set; }

        [DataMember]
        public double NO2 { get; set; }

        [DataMember]
        public double Pressure { get; set; }

        [DataMember]
       public DateTime SampleDateTime { get; set; }

       public SensorSample() { }   //prazan konstruktor

        public SensorSample(double volume, double co, double no2, double pressure, DateTime dateTime)
        {
            Volume = volume;
            CO = co;
            NO2 = no2;
            Pressure = pressure;
            SampleDateTime = dateTime;
        }

    }
}
