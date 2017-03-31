using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.NetworkInformation;
using System.Threading;
using System.Diagnostics;
using System.Collections;


namespace OIMS
{
    public class PingX: Ping
    {
        private string ipaddress = "";
        public string IPAddressString
        {
            get { return ipaddress; }
            set { ipaddress = value; }
        }

        public void SendAsync1(System.Net.IPAddress ip, Int32 ttl, Byte[] buff, PingOptions opts, object userToken)
        {
            IPAddressString = ip.ToString();
            Console.WriteLine("=============", IPAddressString);
            SendAsync(ip, ttl, buff, opts, this);
        }
    }

    static class IOTConnectionListModule
    {
        private static List<Ping> pingers = new List<Ping>();
        private static int instances = 0;

        private static object @lock = new object();

        private static int result = 0;
        private static int timeOut = 250;

        private static int ttl = 5;
        private static Hashtable hashtableLocal = new Hashtable();


        public static Hashtable refreshIOTNodeList(string baseIP)
        {
            //clear list of IOT nodel list ip addressess conected to OIMS middleware 
            hashtableLocal.Clear();

            Console.WriteLine("Pinging 255 destinations of D-class in {0}*", baseIP);

            CreatePingers(255);

            PingOptions po = new PingOptions(ttl, true);
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            byte[] data = enc.GetBytes("ab");

            SpinWait wait = new SpinWait();
            int cnt = 1;

            Stopwatch watch = Stopwatch.StartNew();
            System.Net.IPAddress ipaddr;

            foreach (PingX p in pingers)
            {
                lock (@lock)
                {
                    instances += 1;
                }

                ipaddr = System.Net.IPAddress.Parse(string.Concat(baseIP, cnt.ToString()));
                p.SendAsync1(ipaddr, timeOut, data, po, p);
                cnt += 1;
            }

            while (instances > 0)
            {
                wait.SpinOnce();
            }

            watch.Stop();
            DestroyPingers();

            Console.WriteLine("Finished in {0}. Found {1} active IP-addresses.", watch.Elapsed.ToString(), result);

            //return list of IOT nodel list ip addressess conected to OIMS middleware 
            return hashtableLocal;
        }

        public static void Ping_completed(object s, PingCompletedEventArgs e)
        {
            PingX p = (PingX)s;
            lock (@lock)
            {
                instances -= 1;
            }

            if (e.Reply.Status == IPStatus.Success)
            {
                Console.WriteLine(string.Concat("Active IP: ", e.Reply.Address.ToString()));
                result += 1;
                hashtableLocal.Add(e.Reply.Address.ToString(), true);
            }
            else
            {
                Console.WriteLine(String.Concat("Non-active IP: ", p.IPAddressString));// e.Reply.Address.ToString()))
            }
        }


        private static void CreatePingers(int cnt)
        {
            for (int i = 1; i <= cnt; i++)
            {
                PingX p = new PingX();
                p.PingCompleted += Ping_completed;
                pingers.Add(p);
            }
        }

        private static void DestroyPingers()
        {
            foreach (PingX p in pingers)
            {
                p.PingCompleted -= Ping_completed;
                p.Dispose();
            }
            pingers.Clear();
        }
    }

    public class ListOfIOTNodes
    {
        public Hashtable NodeIPlistHashtable;

        public ListOfIOTNodes()
        {
            NodeIPlistHashtable = new Hashtable();
        }

        public void refreshConnectedIOTNodeList(string baseIP1)
        {
            NodeIPlistHashtable.Clear();
            string baseIP = baseIP1; //"10.1.173.";
            NodeIPlistHashtable = IOTConnectionListModule.refreshIOTNodeList(baseIP);
        }

    }


}