using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public enum SessionStatus  //status obrade na serveru
    {
        IN_PROGRESS,
        COMPLETED
    }

    public enum ResponseResult  //odgovor servera klijentu
    {
        ACK,  //uspjesno primljeno
        NACK  //odbijeno zbog greske
    }


}
