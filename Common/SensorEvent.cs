using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Common
{
    // --- EventArgs klase (nose podatke uz događaj) ---

    public class TransferEventArgs : EventArgs
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }

        public TransferEventArgs(string message)
        {
            Timestamp = DateTime.Now;
            Message = message;
        }
    }

    public class SampleEventArgs : EventArgs
    {
        public SensorSample Sample { get; set; }
        public int SampleNumber { get; set; }

        public SampleEventArgs(SensorSample sample, int sampleNumber)
        {
            Sample = sample;
            SampleNumber = sampleNumber;
        }
    }

    public class WarningEventArgs : EventArgs
    {
        public string WarningType { get; set; }   // npr. "PressureSpike", "COSpike"
        public string Direction { get; set; }      // "iznad" ili "ispod"
        public double Delta { get; set; }
        public double Threshold { get; set; }

        public WarningEventArgs(string warningType, string direction, double delta, double threshold)
        {
            WarningType = warningType;
            Direction = direction;
            Delta = delta;
            Threshold = threshold;
        }
    }
}