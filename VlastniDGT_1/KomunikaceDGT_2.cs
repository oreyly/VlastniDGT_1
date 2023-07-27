using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VlastniDGT_1
{
    enum StavKomunikace
    {
        Nic,
        OdeslalPozadavek,
        PrijimaData,
        DataPrijmuta
    }

    public static class KomunikaceDGT_2
    {
        #region PC -> Board
        public const byte DGT_BUS_REQ_SBI_CLOCK = 0x81; //DGT_MSG_BUS_SBI_CLOCK
        public const byte DGT_BUS_REQ_BOARD = 0x82; //DGT_MSG_BUS_BOARD_DUMP
        public const byte DGT_BUS_REQ_CHANGES = 0x83; //DGT_MSG_BUS_UPDATE_ODD
        public const byte DGT_BUS_REPEAT_CHANGES = 0x84; //DGT_MSG_BUS_UPDATE_ODD
        public const byte DGT_BUS_SET_START_GAME = 0x85; //DGT_MSG_BUS_START_GAME_WRITTEN
        public const byte DGT_BUS_REQ_FROM_START = 0x86; //DGT_MSG_BUS_FROM_START
        public const byte DGT_BUS_PING = 0x87; //DGT_MSG_BUS_PING
        public const byte DGT_BUS_END_BUSMODE = 0x88; //None
        public const byte DGT_BUS_RESET = 0x89; //None
        public const byte DGT_BUS_IGNORE_NEXT_BUS_PING = 0x8a; //DGT_MSG_BUS_PING
        public const byte DGT_BUS_REQ_VERSION = 0x8b; //DGT_MSG_BUS_VERSION
        public const byte DGT_BUS_REQ_ALL_D = 0x8d; //DGT_MSG_BUS_UPDATE_ODD | DGT_MSG_BUS_SBI_CLOCK | DGT_MSG_BUS_BOARD_DUMP
        public const byte DGT_BUS_REQ_SERIALNR = 0x91; //DGT_MSG_BUS_SERIALNR
        public const byte DGT_BUS_RPING = 0x92; //DGT_MSG_BUS_PING
        public const byte DGT_BUS_REQ_I2C_CLOCK = 0x93; //DGT_MSG_BUS_12C(clock)
        public const byte DGT_BUS_REQ_I2C_BUTTON = 0x94; //DGT_MSG_BUS_12C(button)
        public const byte DGT_BUS_REQ_I2C_ACK = 0x95; //DGT_MSG_BUS_12C(ack)
        public const byte DGT_BUS_REQ_STATS = 0x96; //DGT_MSG_BUS_STATS
        public const byte DGT_BUS_REQ_TRADEMARK = 0x97; //DGT_MSG_BUS_TRADEMARK
        public const byte DGT_BUS_REQ_BATTERY = 0x98; //DGT_MSG_BUS_BATTERY_STATUS
        #endregion

        #region Board -> PC
        public const byte DGT_MSG_BUS_BOARD_DUMP = 0x83;
        public const byte DGT_MSG_BUS_SBI_CLOCK = 0x84;
        public const byte DGT_MSG_BUS_UPDATE_ODD = 0x85;
        public const byte DGT_MSG_BUS_FROM_START = 0x86;
        public const byte DGT_MSG_BUS_PING = 0x87;
        public const byte DGT_MSG_BUS_START_GAME_WRITTEN = 0x88;
        public const byte DGT_MSG_BUS_VERSION = 0x89;
        public const byte DGT_MSG_BUS_UPDATE_EVEN = 0x8b;
        public const byte DGT_MSG_BUS_HARDWARE_VERSION = 0x8c;
        public const byte DGT_MSG_BUS_SERIALNR = 0xb0;
        public const byte DGT_MSG_BUS_I2C = 0xb1;
        public const byte DGT_MSG_BUS_STATS = 0xb2;
        public const byte DGT_MSG_BUS_CONFIG = 0xb3;
        public const byte DGT_MSG_BUS_TRADEMARK = 0xb4;
        public const byte DGT_MSG_BUS_BATTERY_STATUS = 0xb5;
        #endregion

        public static SerialPort SPort;

        private static StavKomunikace Stav;

        static KomunikaceDGT_2()
        {
            AppDomain.CurrentDomain.ProcessExit += Destruktor;
        }

        private static void Destruktor(object sender, EventArgs e)
        {
            SPort?.Close();
        }

        public static bool Otevri(string port)
        {
            try
            {
                SPort?.Close();

                SPort = new SerialPort(port, 9600);

                SPort.Open();
                Stav = StavKomunikace.Nic;
                HloubkaRekurze = 0;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static string[] SeznamPortu()
        {
            return SerialPort.GetPortNames();
        }

        private static List<byte> Data;
        private static byte Zprava;
        private static int PocetBytu;
        private static int HloubkaRekurze;

        private static void PosliPozadavek(byte pozadavek, int adresa, SerialDataReceivedEventHandler prijimac)
        {
            if (Stav != StavKomunikace.Nic)
            {
                throw new Exception("Port je zaneprázdněn");
            }

            byte[] adresaB = IntToBytes(adresa);

            Data = new List<byte>();

            Stav = StavKomunikace.PrijimaData;

            SPort.DataReceived += prijimac;
            SPort.Write(new byte[] { pozadavek, adresaB[0], adresaB[1], (byte)((pozadavek + adresaB[0] + adresaB[1]) & 0x7f) }, 0, 4);
        }

        #region Ping
        private static void PrijimacPing(object sender, SerialDataReceivedEventArgs e)
        {
            if (Stav == StavKomunikace.DataPrijmuta)
            {
                return;
            }

            if (Stav != StavKomunikace.PrijimaData)
            {
                throw new Exception("Přišla nevyžádaná data");
            }

            byte[] novaData = new byte[SPort.BytesToRead];
            SPort.Read(novaData, 0, novaData.Length);
            Data.AddRange(novaData);
        }
        public static async Task<int[]> Ping(int adresa = 0)
        {
            if (++HloubkaRekurze >= 10)
            {
                throw new Exception("Ping nelze provést");
            }

            PosliPozadavek(DGT_BUS_PING, adresa, PrijimacPing);

            await Task.Delay(2000);
            Stav = StavKomunikace.DataPrijmuta;

            int[] adresyVys = new int[Data.Count / 6];

            for (int i = 0; i < Data.Count; i += 6)
            {
                if (Data[i] != DGT_MSG_BUS_PING)
                {
                    return await Ping(adresa);
                }

                if (BytesToInt(Data[i + 1], Data[i + 2]) != 6)
                {
                    return await Ping(adresa);
                }

                if ((Data.Skip(i).Take(5).Sum(b => b) & 0x7f) != Data[i + 5])
                {
                    return await Ping(adresa);
                }

                adresyVys[i / 6] = BytesToInt(Data[i + 3], Data[i + 4]);
            }

            HloubkaRekurze = 0;
            Stav = StavKomunikace.Nic;
            SPort.DataReceived -= PrijimacPing;
            return adresyVys;
        }
        #endregion

        private static void UniverzalniPrijimac(object sender, SerialDataReceivedEventArgs e)
        {
            if (Stav == StavKomunikace.Nic)
            {
                throw new Exception("Přišla nevyžádaná data");
            }

            if (Stav == StavKomunikace.DataPrijmuta)
            {
                throw new Exception("Přišlo více dat, než bylo nahlášeno");
            }

            byte[] novaData = new byte[SPort.BytesToRead];
            SPort.Read(novaData, 0, novaData.Length);
            Data.AddRange(novaData);

            if (Stav == StavKomunikace.OdeslalPozadavek)
            {
                if (Data.Count >= 3)
                {
                    Zprava = Data[0];
                    PocetBytu = BytesToInt(Data[1], Data[2]);
                }

                Stav = StavKomunikace.PrijimaData;
            }

            if (Data.Count == PocetBytu)
            {
                Stav = StavKomunikace.DataPrijmuta;
            }

            if (Data.Count > PocetBytu)
            {
                throw new Exception("Přišlo více dat, než bylo nahlášeno");
            }

        }

        public static async Task<byte[]> PostaveniSachovnice(int adresa)
        {
            if (++HloubkaRekurze >= 10)
            {
                throw new Exception("Postavení šachovnice nelze získat");
            }

            PosliPozadavek(DGT_BUS_REQ_BOARD, adresa, UniverzalniPrijimac);

            while (Stav != StavKomunikace.DataPrijmuta)
            {
                await Task.Delay(50);
            }

            if (Data[0] != DGT_MSG_BUS_BOARD_DUMP)
            {
                return await PostaveniSachovnice(adresa);
            }

            if (PocetBytu != 70)
            {
                return await PostaveniSachovnice(adresa);
            }

            if ((Data.Take(Data.Count-1).Sum(b => b) & 0x7f) != Data.Last())
            {
                return await PostaveniSachovnice(adresa);
            }

            byte[] postaveniVys = Data.Skip(5).Take(64).ToArray();


            HloubkaRekurze = 0;
            Stav = StavKomunikace.Nic;
            SPort.DataReceived -= UniverzalniPrijimac;
            return postaveniVys;
        }

        private static byte[] IntToBytes(int hodnota)
        {
            return new byte[] { (byte)(hodnota >> 7), (byte)(hodnota & 0x7f) };
        }

        private static int BytesToInt(byte byt1, byte byt2)
        {
            return (byt1 << 7) + (byt2 & 0x7f);
        }
    }
}
