
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UsageSample
{
    static class Program
    {
    
        static void Main()
        {
             bool success = false;
			 Bitmart bitmart = new Bitmart("ApiKey", "Secret");
		     var wallet = bitmart.GetWallet(); 
			 double usdBalance = Convert.ToDouble(wallet.Where(i => i.Id == "USDT").FirstOrDefault()?.Available.Replace(".", ",")); 
			 var ticker = bitmart.GetTicker("BTC_USDT");
             string order = bitmart.PostOrder("BTC_USDT", "0.1", "1000", "sell",out success);
 
        }
    }
}
