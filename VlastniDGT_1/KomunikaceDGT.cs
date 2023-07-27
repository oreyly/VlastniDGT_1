using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

/*enum StavKomunikace
{
    Nic,
    OdeslalPozadavek,
    PrijimaData,
    DataPrijmuta,
    Posloucha
}*/

namespace VlastniDGT_1
{
    public static class KomunikaceDGT
    {
        /*public const byte DGT_REQ_RESET = 0x40;
        public const byte DGT_REQ_SBI_CLOCK = 0x41;
        public const byte DGT_REQ_BOARD = 0x42;
        public const byte DGT_REQ_UPDATE_SBI = 0x43;
        public const byte DGT_REQ_UPDATE_BOARD = 0x44;
        public const byte DGT_REQ_SERIALNR = 0x45;
        public const byte DGT_REQ_BUSADDRESS = 0x46;
        public const byte DGT_REQ_TRADEMARK = 0x47;
        public const byte DGT_REQ_HARDWARE_VERSION = 0x48;
        public const byte DGT_REQ_LOG_MOVES = 0x49;
        public const byte DGT_REQ_BUSMODE = 0x4a;

        public const byte DGT_REQ_UPDATE_SBI_NICE = 0x4b;
        public const byte DGT_REQ_BATTERY_STATUS = 0x4c;
        public const byte DGT_REQ_VERSION = 0x4d;
        public const byte DGT_REQ_LONG_SERIALNR = 0x55;

        public const byte DGT_REQ_I2C_CLOCK = 0x56;
        public const byte DGT_REQ_UPDATE_I2C = 0x57;
        public const byte DGT_REQ_UPDATE_I2C_NICE = 0x58;
        */
        public const byte DGT_BUS_PING = 0x87;
        public const byte DGT_BUS_RESET = 0x89;
        public const byte DGT_BUS_REQ_BOARD = 0x82;
        public const byte DGT_BUS_REQ_CHANGES = 0x83;

        public const byte DGT_MSG_BATTERY_STATUS = 0xa0;
        public const byte DGT_BUS_REQ_SERIALNR = 0x91;

        public static SerialPort SPort;
        public static bool Otevreno;

        public static byte[] Sachovnice;

        private static StavKomunikace Stav;

        static KomunikaceDGT()
        {
            Stav = StavKomunikace.Nic;
            Sachovnice = new byte[64];
            AppDomain.CurrentDomain.ProcessExit += Destruktor;
        }

        private static void Destruktor(object sender, EventArgs e)
        {
            SPort?.Close();
            Otevreno = false; //Já vím, že je to zbytečný ty troubo z budoucnosti
        }

        public static bool Otevri(string port)
        {
            try
            {
                SPort?.Close();

                SPort = new SerialPort(port, 9600);
                SPort.DataReceived += SPort_DataReceived;

                SPort.Open();
                Otevreno = true;
            }
            catch
            {
                Otevreno = false;
                return false;
            }

            return true;
        }

        public static string[] SeznamPortu()
        {
            return SerialPort.GetPortNames();
        }

        /*public static async Task<(byte ,byte[])> PosliPozadavek(byte pozadavek)
        {
            if (!Otevreno || Ziskava)
            {
                return (default, null);
            }

            if (Stav == StavKomunikace.Posloucha)
            {
                SPort.Write(new byte[] { DGT_REQ_RESET }, 0, 1);
                await Task.Delay(50);

                Zprava = default;
                Data = null;
                Index = 0;
                Stav = StavKomunikace.Nic;
            }

            if (pozadavek is DGT_REQ_UPDATE_BOARD or DGT_REQ_UPDATE_I2C or DGT_REQ_UPDATE_I2C_NICE or DGT_REQ_UPDATE_SBI or DGT_REQ_UPDATE_SBI_NICE)
            {
                Stav = StavKomunikace.Posloucha;
                UpdateData.Clear();
                SPort.Write(new byte[] { pozadavek }, 0, 1);
                return (default, null);
            }

            Stav = StavKomunikace.OdeslalPozadavek;

            SPort.Write(new byte[] { pozadavek }, 0, 1);

            if (pozadavek == DGT_REQ_RESET)
            {
                return (default, null);
            }

            while(Stav != StavKomunikace.DataPrijmuta)
            {
                await Task.Delay(50);
            }

            (byte, byte[]) vysledek = (Zprava, Data);

            Zprava = default;
            Data = null;
            Index = 0;
            Stav = StavKomunikace.Nic;
            

            return vysledek;
        }*/

        private static bool Ziskava;
        public static bool ZastavZiskavaniPohybu()
        {
            try
            {
                Ziskava = false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static ObservableCollection<KeyValuePair<byte, byte>> ZmenaNaSachovnici;
        /*public static async Task ZiskavejPohyb()
        {
            if (!Otevreno || Ziskava)
            {
                return;
            }

            if (Stav == StavKomunikace.Posloucha)
            {
                SPort.Write(new byte[] { DGT_REQ_RESET }, 0, 1);
                await Task.Delay(50);

                Zprava = default;
                Data = null;
                Index = 0;
            }

            UpdateData.Clear();

            Ziskava = true;
            Stav = StavKomunikace.Posloucha;

            while(Ziskava)
            {
                ++g;
                Stav = StavKomunikace.OdeslalPozadavek;
                SPort.Write(new byte[] { DGT_REQ_BOARD }, 0, 1);

                while (Stav != StavKomunikace.DataPrijmuta)
                {
                    await Task.Delay(50);
                }

                if (Data.Length != 64)
                {
                    continue;
                }

                for (byte i = 0; i < Data.Length; ++i)
                {
                    if (Sachovnice[i] != Data[i])
                    {
                        ZmenaNaSachovnici.Add(new KeyValuePair<byte, byte>(i, Data[i]));
                    }
                }

                Sachovnice = Data;
                Index = 0;
            }

            Zprava = default;
            Data = null;
            Index = 0;
            Stav = StavKomunikace.Nic;
        }*/

        private static int g = 0;

        private static byte Zprava;
        private static int PocetBytu;
        private static byte[] Data;
        private static int Index;

        public static ObservableCollection<byte> UpdateData = new ObservableCollection<byte>();
        private static List<byte> bafr = new List<byte>();
        private static void SPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] data;
            switch (Stav)
            {
                case StavKomunikace.OdeslalPozadavek:
                    data = new byte[SPort.BytesToRead];
                    SPort.Read(data, 0, data.Length);

                    if (bafr.Count > 0)
                    {
                        bafr.AddRange(data);
                        data = bafr.ToArray();
                    }

                    if (data.Length < 3)
                    {
                        bafr.Clear();
                        bafr.AddRange(data);
                        break;
                    }

                    Zprava = data[0];
                    PocetBytu = (data[1] << 7) + (data[2] & 0x7f) - 3;
                    Data = new byte[PocetBytu];
                    ZapisByty(data.Skip(3).ToArray());

                    if (Index < PocetBytu)
                    {
                        Stav = StavKomunikace.PrijimaData;
                    }
                    else
                    {
                        Stav = StavKomunikace.DataPrijmuta;
                        Index = 0;
                    }
                    break;

                case StavKomunikace.PrijimaData:
                    data = new byte[SPort.BytesToRead];
                    SPort.Read(data, 0, data.Length);
                    ZapisByty(data);

                    if (Index >= PocetBytu)
                    {
                        Stav = StavKomunikace.DataPrijmuta;
                    }
                    break;

                case StavKomunikace.DataPrijmuta:
                    data = new byte[SPort.BytesToRead];
                    SPort.Read(data, 0, data.Length);
                    throw new Exception("Přišlo více dat");

                default:
                    data = new byte[5];
                    SPort.Read(data, 0, 5);
                    Zprava = data[0];
                    PocetBytu = (data[1] << 7) + (data[2] & 0x7f) - 3;

                    foreach (byte b in data.Skip(3))
                    {
                        UpdateData.Add(b);
                    }

                    break;

                /*case StavKomunikace.Nic:
                    throw new Exception("Přišla nevyžádaná data!");*/
            }
        }

        private static void ZapisByty(byte[] byty)
        {
            byty.CopyTo(Data, Index);
            Index += byty.Length; 
        }
    }
}
