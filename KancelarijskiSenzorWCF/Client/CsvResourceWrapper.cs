using System;
using System.IO;

namespace Client
{
    // Klasa implementira IDisposable interfejs za bezbedno upravljanje resursima fajla
    public class CsvResourceWrapper : IDisposable
    {
        // Unmanaged/Managed resursi koji moraju biti ocisceni
        private StreamReader streamReader = null;
        private bool disposed = false; // Kontrolna zastavica

        public StreamReader Reader
        {
            get { return streamReader; }
        }

        // Konstruktor otvara resurs (fajl)
        public CsvResourceWrapper(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Fajl na putanji {path} nije pronadjen.");
            }

            // Alociramo resurs
            streamReader = new StreamReader(path);
            Console.WriteLine("[DISPOSE PATTERN] Resurs uspesno alociran (Otvaranje CSV fajla).");
        }

        // 1. Javna Dispose metoda koju klijent ili using blok rucno pozivaju
        public void Dispose()
        {
            Dispose(true);
            // Govorimo Garbage Collectoru da nema potrebe da trosi vreme na destruktor
            GC.SuppressFinalize(this);
            Console.WriteLine("[DISPOSE PATTERN] Rucno pozvan Dispose(). Destruktor je potisnut.");
        }

        // 2. Zasticena pomocna virtualna metoda koja obavlja stvarno ciscenje
        protected virtual void Dispose(bool disposing)
        {
            // Ako je resurs vec jednom ociscen, preskaci
            if (!disposed)
            {
                if (disposing)
                {
                    // Cistimo managed resurse (zatvaramo StreamReader koji drzi zakljucan fajl)
                    if (streamReader != null)
                    {
                        streamReader.Close();
                        streamReader.Dispose();
                        Console.WriteLine("  -> StreamReader i mrezni tokovi su bezbedno zatvoreni.");
                    }
                }

                // Oznacavamo da je objekat uspesno unisten sa Hip-a
                disposed = true;
            }
        }

        // 3. Destruktor (Finalizer) - Sigurnosna mreza ako programer zaboravi rucni Dispose
        ~CsvResourceWrapper()
        {
            Dispose(false);
            Console.WriteLine("[DISPOSE PATTERN] Destruktor je odradio ciscenje u poslednji cas.");
        }
    }
}