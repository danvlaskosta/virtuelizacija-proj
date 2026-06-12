using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Common;
using System.Configuration;
using System.Globalization;
using System.IO;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[KLIJENT] Pokretanje aplikacije...");

            // ucitavanje putanja iz konfiguracije
            string csvFilePath = ConfigurationManager.AppSettings["CsvPath"];
            string logFilePath = ConfigurationManager.AppSettings["LogPath"];

            // kreiranje WCF kanala (Proxy)
            ChannelFactory<ISensorService> factory = new ChannelFactory<ISensorService>("SensorServiceEndpoint");
            ISensorService proxy = factory.CreateChannel();

            Console.WriteLine("[WCF KLIJENT] Komunikacioni kanal uspjesno otvoren.");

            try
            {
                //startovanje sesije prema protokolu zadatak 1.c)
                // saljem prazan objekat kao meta-zaglavlje da najavimo strukturu zadatak 1.a)
                SensorSample metaHeader = new SensorSample();
                SessionResponse startResponse = proxy.StartSession(metaHeader);
                Console.WriteLine($"[WCF KLIJENT] Odgovor servera na StartSession: {startResponse.Message}");

                //koriscenje naseg namjenskog IDisposable omotaca zadatak 4
                using (CsvResourceWrapper csvResource = new CsvResourceWrapper(csvFilePath))
                {
                    StreamReader reader = csvResource.Reader;

                    //preskacemo prvi red CSV-a jer sadrzi tekstualne nazive kolona
                    if (!reader.EndOfStream)
                    {
                        reader.ReadLine();
                    }

                    int uspesnoPoslato = 0;
                    int ukupnoProcitano = 0;

                    //otvaramo fajl za logovanje nevalidnih redova zadatak 5
                    using (StreamWriter logWriter = new StreamWriter(logFilePath, false))
                    {
                        // petlja ide sekvencijalno dok ne napunimo tacno 110 redova zadatak 5
                        while (!reader.EndOfStream && uspesnoPoslato < 110)
                        {
                            string line = reader.ReadLine();
                            ukupnoProcitano++;

                            if (string.IsNullOrWhiteSpace(line)) continue;

                            // Razbijamo CSV red pomocu zareza
                            string[] parts = line.Split(',');

                            try
                            {
                                // InvariantCulture garantuje da ce se tacka prepoznati kao decimalni separator
                                // raspored kolona iz Kaggle dataset-aa
                                double volume = double.Parse(parts[1], CultureInfo.InvariantCulture);
                                double co = double.Parse(parts[8], CultureInfo.InvariantCulture);
                                double no2 = double.Parse(parts[9], CultureInfo.InvariantCulture);
                                double pressure = double.Parse(parts[4], CultureInfo.InvariantCulture);

                                DateTime dateTime;
                                //prva kolona parts[0] sadrzi datum i vrijeme
                                if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                                {
                                    dateTime = DateTime.Now;
                                }

                                //pakovanje podataka u SensorSample objekat
                                SensorSample sample = new SensorSample(volume, co, no2, pressure, dateTime);

                                //sekvencijalno slanje preko WCF-a zadatak 1.b)
                                SessionResponse response = proxy.PushSample(sample);

                                if (response.Result == ResponseResult.ACK)
                                {
                                    uspesnoPoslato++;
                                    Console.WriteLine($"[SLANJE] Uzorak #{ukupnoProcitano} poslat. (Uspjesno: {uspesnoPoslato}/110)");
                                }
                            }
                            catch (Exception ex)
                            {
                                //ako parsiranje padne, belezimo los red u log i nastavljamo petlju dalje zadatak 5
                                logWriter.WriteLine($"[NEVALIDAN RED #{ukupnoProcitano}] Sadrzaj: '{line}' | GRESKA: {ex.Message}");
                                Console.WriteLine($"[UPOZORENJE] Red #{ukupnoProcitano} je nevalidan! Detalji upisani u log.");
                            }
                        }
                    }

                    Console.WriteLine($"\n[KLIJENT] Gotovo. Procitano redova: {ukupnoProcitano}, uspjesno poslato: {uspesnoPoslato}.");
                }

                //zatvaranje sesije prema protokolu zadatak 1.c)
                SessionResponse endResponse = proxy.EndSession();
                Console.WriteLine($"[WCF KLIJENT] Odgovor servera na EndSession: {endResponse.Message} (Status: {endResponse.Status})");
            }
            catch (FaultException<ValidationFault> valEx)
            {
                Console.WriteLine($"[WCF GRESKA - VALIDACIJA]: {valEx.Detail.ErrorMessage}");
            }
            catch (FaultException<DataFormatFault> dfEx)
            {
                Console.WriteLine($"[WCF GRESKA - FORMAT]: {dfEx.Detail.ErrorMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GRESKA U KOMUNIKACIJI]: {ex.Message}");
            }
            finally
            {
                //zatvaranje fabrike kanala
                if (factory.State == CommunicationState.Opened)
                {
                    factory.Close();
                }
                Console.WriteLine("\nPritisnite bilo koji taster za izlaz...");
                Console.ReadKey();
            }
        }
    }
}
