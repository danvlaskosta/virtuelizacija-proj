using System;
using System.ServiceModel;
using Common;

namespace Service
{
    class Program
    {
        static void Main(string[] args)
        {
            SensorService serviceInstance = new SensorService();

            // --- Zadatak 8: Pretplata na događaje ---

            serviceInstance.OnTransferStarted += (sender, e) =>
            {
                Console.WriteLine($"[EVENT - OnTransferStarted] {e.Timestamp:HH:mm:ss} | {e.Message}");
            };

            serviceInstance.OnSampleReceived += (sender, e) =>
            {
                Console.WriteLine($"[EVENT - OnSampleReceived] Uzorak #{e.SampleNumber} | " +
                                  $"P={e.Sample.Pressure:F2}, CO={e.Sample.CO:F2}, " +
                                  $"NO2={e.Sample.NO2:F2}, Vol={e.Sample.Volume:F2}");
            };

            serviceInstance.OnTransferCompleted += (sender, e) =>
            {
                Console.WriteLine($"[EVENT - OnTransferCompleted] {e.Timestamp:HH:mm:ss} | {e.Message}");
            };

            serviceInstance.OnWarningRaised += (sender, e) =>
            {
                Console.WriteLine($"[EVENT - UPOZORENJE] Tip: {e.WarningType} | " +
                                  $"Smjer: {e.Direction} ocekivanog | " +
                                  $"Delta: {e.Delta:F2} | Prag: {e.Threshold:F2}");
            };

            // --- Pokretanje WCF hosta ---
            using (ServiceHost host = new ServiceHost(serviceInstance))
            {
                host.Open();

                Console.WriteLine("=========================================================");
                Console.WriteLine("[WCF SERVER] Servis za nadzor kancelarijskih senzora je pokrenut.");
                Console.WriteLine("Server slusa na: net.tcp://localhost:4005/SensorService");
                Console.WriteLine("Pritisnite [Enter] za gasenje servera...");
                Console.WriteLine("=========================================================\n");

                Console.ReadLine();

                host.Close();
            }
        }
    }
}