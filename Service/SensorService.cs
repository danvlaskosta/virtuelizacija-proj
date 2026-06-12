using System;
using System.Configuration;
using System.IO;
using System.ServiceModel;
using Common;

namespace Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SensorService : ISensorService
    {
        // --- Pragovi iz konfiguracije ---
        private readonly double pThreshold;
        private readonly double coThreshold;
        private readonly double no2Threshold;

        // --- Stanje sesije ---
        private SensorSample lastSample = null;
        private double totalPressure = 0;
        private int sampleCount = 0;
        private bool isSessionActive = false;
        private string outputFolder;
        private StreamWriter measurementsWriter = null;
        private StreamWriter rejectsWriter = null;

        // --- Događaji (Zadatak 8) ---
        public event EventHandler<TransferEventArgs> OnTransferStarted;
        public event EventHandler<SampleEventArgs> OnSampleReceived;
        public event EventHandler<TransferEventArgs> OnTransferCompleted;
        public event EventHandler<WarningEventArgs> OnWarningRaised;

        public SensorService()
        {
            pThreshold = double.Parse(ConfigurationManager.AppSettings["P_threshold"]);
            coThreshold = double.Parse(ConfigurationManager.AppSettings["CO_threshold"]);
            no2Threshold = double.Parse(ConfigurationManager.AppSettings["NO2_threshold"]);
            outputFolder = ConfigurationManager.AppSettings["OutputFolder"];
        }

        // --- Pomoćne metode za okidanje događaja ---
        protected virtual void RaiseTransferStarted(string message)
        {
            OnTransferStarted?.Invoke(this, new TransferEventArgs(message));
        }

        protected virtual void RaiseSampleReceived(SensorSample sample, int number)
        {
            OnSampleReceived?.Invoke(this, new SampleEventArgs(sample, number));
        }

        protected virtual void RaiseTransferCompleted(string message)
        {
            OnTransferCompleted?.Invoke(this, new TransferEventArgs(message));
        }

        protected virtual void RaiseWarning(string type, string direction, double delta, double threshold)
        {
            OnWarningRaised?.Invoke(this, new WarningEventArgs(type, direction, delta, threshold));
        }

        // --- WCF operacije ---

        public SessionResponse StartSession(SensorSample metaHeader)
        {
            if (metaHeader == null)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("DataFormatFault: Meta-zaglavlje ne sme biti null."));

            // Reset stanja
            lastSample = null;
            totalPressure = 0;
            sampleCount = 0;
            isSessionActive = true;

            // Zadatak 7 + 8
            Console.WriteLine("\n[SERVER] Primljena poruka: StartSession.");
            RaiseTransferStarted("Sesija otvorena — prenos u toku...");

            // Zadatak 6: Kreiranje output fajlova
            Directory.CreateDirectory(outputFolder);
            string sessionFile = Path.Combine(outputFolder, "measurements_session.csv");
            string rejectsFile = Path.Combine(outputFolder, "rejects.csv");

            measurementsWriter = new StreamWriter(sessionFile, append: false);
            measurementsWriter.WriteLine("DateTime,Volume,CO,NO2,Pressure");

            rejectsWriter = new StreamWriter(rejectsFile, append: false);
            rejectsWriter.WriteLine("DateTime,Volume,CO,NO2,Pressure,Razlog");

            return new SessionResponse(ResponseResult.ACK, SessionStatus.IN_PROGRESS, "Sesija je pokrenuta.");
        }

        public SessionResponse PushSample(SensorSample sample)
        {
            if (!isSessionActive)
                throw new FaultException<ValidationFault>(
                    new ValidationFault("ValidationFault: Nema aktivne sesije."));

            if (sample == null)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("DataFormatFault: SensorSample ne sme biti null."));

            if (sample.Pressure <= 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault($"ValidationFault: Pritisak mora biti veci od 0 (Prosledjeno: {sample.Pressure})."));

            if (sample.CO < 0)
            {
                rejectsWriter?.WriteLine(
                    $"{sample.SampleDateTime:yyyy-MM-dd HH:mm:ss},{sample.Volume},{sample.CO},{sample.NO2},{sample.Pressure},Negativna vrednost CO");
                rejectsWriter?.Flush();
                throw new FaultException<ValidationFault>(
                    new ValidationFault($"ValidationFault: CO ne sme biti negativan (Prosledjeno: {sample.CO})."));
            }

            if (sample.NO2 < 0)
            {
                rejectsWriter?.WriteLine(
                    $"{sample.SampleDateTime:yyyy-MM-dd HH:mm:ss},{sample.Volume},{sample.CO},{sample.NO2},{sample.Pressure},Negativna vrednost NO2");
                rejectsWriter?.Flush();
                throw new FaultException<ValidationFault>(
                    new ValidationFault($"ValidationFault: NO2 ne sme biti negativan (Prosledjeno: {sample.NO2})."));
            }

            // --- Zadatak 9: Detekcija nagle promjene pritiska ---
            if (lastSample != null)
            {
                double deltaP = sample.Pressure - lastSample.Pressure;
                if (Math.Abs(deltaP) > pThreshold)
                {
                    string direction = deltaP > 0 ? "iznad" : "ispod";
                    RaiseWarning("PressureSpike", direction, Math.Abs(deltaP), pThreshold);
                }

                // --- Zadatak 10: Detekcija naglih promjena gasova ---
                double deltaCO = sample.CO - lastSample.CO;
                if (Math.Abs(deltaCO) > coThreshold)
                {
                    string direction = deltaCO > 0 ? "iznad" : "ispod";
                    RaiseWarning("COSpike", direction, Math.Abs(deltaCO), coThreshold);
                }

                double deltaNO2 = sample.NO2 - lastSample.NO2;
                if (Math.Abs(deltaNO2) > no2Threshold)
                {
                    string direction = deltaNO2 > 0 ? "iznad" : "ispod";
                    RaiseWarning("NO2Spike", direction, Math.Abs(deltaNO2), no2Threshold);
                }
            }

            // --- Zadatak 9: OutOfBandWarning (±25% od tekućeg proseka) ---
            if (sampleCount > 0)
            {
                double avgPressure = totalPressure / sampleCount;
                if (sample.Pressure < 0.75 * avgPressure)
                    RaiseWarning("OutOfBandWarning", "ispod", Math.Abs(sample.Pressure - avgPressure), avgPressure * 0.25);
                else if (sample.Pressure > 1.25 * avgPressure)
                    RaiseWarning("OutOfBandWarning", "iznad", Math.Abs(sample.Pressure - avgPressure), avgPressure * 0.25);
            }

            // Ažuriranje statistika
            sampleCount++;
            totalPressure += sample.Pressure;
            lastSample = sample;

            // Zadatak 6: Upisivanje uzorka u measurements_session.csv
            measurementsWriter?.WriteLine(
                $"{sample.SampleDateTime:yyyy-MM-dd HH:mm:ss}," +
                $"{sample.Volume.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                $"{sample.CO.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                $"{sample.NO2.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                $"{sample.Pressure.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            measurementsWriter?.Flush();

            // Zadatak 8: OnSampleReceived
            RaiseSampleReceived(sample, sampleCount);

            return new SessionResponse(ResponseResult.ACK, SessionStatus.IN_PROGRESS);
        }

        public SessionResponse EndSession()
        {
            isSessionActive = false;

            // Zadatak 7 + 8
            RaiseTransferCompleted($"Zavrsен prenos. Ukupno primljeno {sampleCount} uzoraka.");

            measurementsWriter?.Close();
            measurementsWriter = null;
            rejectsWriter?.Close();
            rejectsWriter = null;
            Console.WriteLine($"[SERVER] Fajlovi snimljeni u: {outputFolder}");

            return new SessionResponse(ResponseResult.ACK, SessionStatus.COMPLETED, "Uspesno zavrseno slanje.");
        }
    }
}