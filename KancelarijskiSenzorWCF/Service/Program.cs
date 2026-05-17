using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace Service
{
     class Program
    {
        static void Main(string[] args)
        {
            //kreiramo instancu naseg servisa
            SensorService serviceInstance = new SensorService();


            //prosledjujemo instancu u ServiceHost koji cita 
            //konfiguracije iz App.config

            using (ServiceHost host = new ServiceHost(serviceInstance))
            {
                host.Open();

                Console.WriteLine("=========================================================");
                Console.WriteLine("[WCF SERVER] Servis za nadzor kancelarijskih senzora je pokrenut.");
                Console.WriteLine("Server slusa na: net.tcp://localhost:4005/SensorService");
                Console.WriteLine("Pritisnite [Enter] u bilo kom trenutku za gasenje servera...");
                Console.WriteLine("=========================================================");

                Console.ReadLine();

                host.Close();
            }
        }
    }
}
