using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.ServiceModel;
using Common;

namespace Service
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class SensorService : ISensorService
    {

        private readonly double pThreshold; //1. e) pragovi 
        private readonly double coThreshold;
        private readonly double no2Threshold;

        // Memorija u okviru sesije za racunanje tekucih statistika
        private SensorSample lastSample = null;
        private double totalVolume = 0;
        private double totalCO = 0;
        private double totalNO2 = 0;
        private double totalPressure = 0;
        private int sampleCount = 0;
        private bool isSessionActive = false;

        public SensorService()
        {
            // Citanje pragova iz App.config-a preko ConfigurationManager-a
            pThreshold = double.Parse(ConfigurationManager.AppSettings["P_threshold"]);
            coThreshold = double.Parse(ConfigurationManager.AppSettings["CO_threshold"]);
            no2Threshold = double.Parse(ConfigurationManager.AppSettings["NO2_threshold"]);
        }

        public SessionResponse StartSession(SensorSample metaHeader)
        {
            // Zadatak 3: Validacija postojanja meta-zaglavlja
            if (metaHeader == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault("DataFormatFault: Meta-zaglavlje ne sme biti null."));

            // Resetovanje stanja za novu sesiju (Zadatak 1.c)
            lastSample = null;
            totalVolume = 0; totalCO = 0; totalNO2 = 0; totalPressure = 0;
            sampleCount = 0;
            isSessionActive = true;

            Console.WriteLine($"\n[SERVER] Primljena poruka: StartSession. Sesija uspesno otvorena.");
            return new SessionResponse(ResponseResult.ACK, SessionStatus.IN_PROGRESS, "Sesija je pokrenuta.");
        }

        public SessionResponse PushSample(SensorSample sample)
        {
            // Zadatak 3: Validacija aktivne sesije i dozvoljenih opsega podataka
            if (!isSessionActive)
                throw new FaultException<ValidationFault>(new ValidationFault("ValidationFault: Nema aktivne sesije."));

            if (sample == null)
                throw new FaultException<DataFormatFault>(new DataFormatFault("DataFormatFault: SensorSample ne sme biti null."));

            if (sample.Pressure <= 0)
                throw new FaultException<ValidationFault>(new ValidationFault($"ValidationFault: Dozvoljeni opseg prekoracen. Pritisak mora biti veci od 0 (Prosledjeno: {sample.Pressure})."));

            // --- RACUNANJE ANOMALIJA (Zadaci sa razlikama u tacki t i t-Delta_t) ---

            // 1. Provera naglog skoka u odnosu na prethodni uzorak
            if (lastSample != null)
            {
                double deltaP = Math.Abs(sample.Pressure - lastSample.Pressure);
                double deltaCO = Math.Abs(sample.CO - lastSample.CO);
                double deltaNO2 = Math.Abs(sample.NO2 - lastSample.NO2);

                // Ukoliko razlika premasi prag, dizemo dogadjaj (ispisujemo na konzolu servera)
                if (deltaP > pThreshold)
                    Console.WriteLine($"[DOGADJAJ - ANOMALIJA] Nagla promena pritiska! |Delta P| = {deltaP:F2} (Prag: {pThreshold})");

                if (deltaCO > coThreshold)
                    Console.WriteLine($"[DOGADJAJ - ANOMALIJA] Kvalitet vazduha ugrozen! Nagli skok CO! |Delta CO| = {deltaCO:F2} (Prag: {coThreshold})");

                if (deltaNO2 > no2Threshold)
                    Console.WriteLine($"[DOGADJAJ - ANOMALIJA] Kvalitet vazduha ugrozen! Nagli skok NO2! |Delta NO2| = {deltaNO2:F2} (Prag: {no2Threshold})");
            }

            // 2. Provera odstupanja od +-25% u odnosu na tekuci prosek (Zadatak 1.e)
            if (sampleCount > 0)
            {
                double avgPressure = totalPressure / sampleCount;
                double odstupanjeP = Math.Abs(sample.Pressure - avgPressure) / avgPressure;

                if (odstupanjeP > 0.25)
                    Console.WriteLine($"[UPOZORENJE] Trenutni pritisak {sample.Pressure} odstupa vise od 25% od tekuceg proseka sesije ({avgPressure:F2})");
            }

            // Azuriramo statistike za prosek
            sampleCount++;
            totalVolume += sample.Volume;
            totalCO += sample.CO;
            totalNO2 += sample.NO2;
            totalPressure += sample.Pressure;

            // Pamtimo tekuci uzorak kao prethodni za sledece slanje
            lastSample = sample;

            return new SessionResponse(ResponseResult.ACK, SessionStatus.IN_PROGRESS);
        }

        public SessionResponse EndSession()
            {
                isSessionActive = false;
                Console.WriteLine($"[SERVER] Primljena poruka: EndSession. Sesija uspesno zavrsena. Ukupno primljeno {sampleCount} uzoraka.");
                return new SessionResponse(ResponseResult.ACK, SessionStatus.COMPLETED, "Uspesno zavrseno slanje.");
            }

        }

}
