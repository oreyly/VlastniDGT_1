using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.Threading;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using System.Reflection;
using System.Collections;
using System.Net.NetworkInformation;
using System.Windows.Media.Media3D;
using System.Collections.Specialized;
using System.Windows.Markup;
using System.ComponentModel;

namespace VlastniDGT_1
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private SerialPort sp = new SerialPort("COM4", 9600);

        public ObservableCollection<string> komy { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> byty { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<char> byty2 { get; set; } = new ObservableCollection<char>();
        public ObservableCollection<string> byty3 { get; set; } = new ObservableCollection<string>();
        public Dictionary<string, byte> moznosti { get; set; } = new Dictionary<string, byte>();
        private SerialPort SPort;
        public MainWindow()
        {
            DataContext = this;
            InitializeComponent();
            FieldInfo[] fieldInfos = typeof(KomunikaceDGT_2).GetFields(BindingFlags.Public |
         BindingFlags.Static | BindingFlags.FlattenHierarchy);

            moznosti = fieldInfos.Where(fi => fi.IsLiteral && !fi.IsInitOnly).ToDictionary(x => x.Name, x => (byte)x.GetRawConstantValue());
            lvMoznost.ItemsSource = moznosti;
            KomunikaceDGT_2.SeznamPortu().ToList().ForEach(komy.Add);
            KomunikaceDGT_2.Otevri("COM4");
            //SPort.Open();
        }

        private void SPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] data = new byte[SPort.BytesToRead];
            SPort.Read(data, 0, data.Length);
            data.ToList().ForEach(b => Dispatcher.Invoke(byty3.Add, b.ToString("x2")));
        }

        bool figurka = false;
        private void PrislaUpdateData(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.NewItems is not null)
            {
                foreach (byte b in e.NewItems)
                {
                    Dispatcher.Invoke(Nastav, b);
                }
            }
            //throw new NotImplementedException(); //116
        }

        private void Nastav(byte b)
        {
            byty3.Add(b.ToString("x2"));
            byty2.Add((char)b); 
            byty.Add((figurka = !figurka) ? (char)(b % 8 + 65) + "" + (8 - b / 8) : KodNaFigurku(b));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (KomunikaceDGT.Otevri((string)lvKomy.SelectedItem))
            {
                MessageBox.Show("Připojeno");
            }
            else
            {
                MessageBox.Show("Připojení se nezdařilo");
            }

            /*DateTime dt = DateTime.Now;

            bool x;

            for (int i = 0; i < 10_000_000; ++i)
            {
                x = i % 2 == 0;
            }

            Title = (DateTime.Now - dt).ToString();*/

        }

        private async void Button_Click2(object sender, RoutedEventArgs e)
        {
            int[] sachovnice = await KomunikaceDGT_2.Ping();
            foreach (int s in sachovnice)
            {
                byty3.Add(s.ToString());
            }
        }

        private async void Button_Click3(object sender, RoutedEventArgs e)
        {
            byty3.Clear();
        }

        private async void Button_Click4(object sender, RoutedEventArgs e)
        {
            /*byte[] sachovnice = await KomunikaceDGT_2.PostaveniSachovnice();
            foreach (int s in sachovnice)
            {
                byty3.Add(s.ToString());
            }*/
        }

        int i;
        private static readonly string[] Figurky = new string[]
        {
            "no piece",
            "white pawn",
            "white rook",
            "white knight",
            "white bishop",
            "white king",
            "white queen",
            "black pawn",
            "black rook",
            "black knight",
            "black bishop",
            "black king",
            "black queen",
            "Draw",
            "White wins",
            "Black wins"
        };

        private string KodNaFigurku(byte b)
        {
            return Figurky[b];
        }
        private byte Zprava;
        private string PrelozByt(byte byt)
        {
            return Zprava switch
            {
                0x86 => $"{(char)(i % 8 + 65)}{8 - i / 8} -> {KodNaFigurku(byt)}",
                _ => byt switch
                {
                    0x00 => "NOP2 / pole " + (char)(byt % 8 + 65) + (8 - byt / 8).ToString(),
                    <= 0x0f => byt + " sub sekund / pole " + (char)(byt % 8 + 65) + (8 - byt / 8).ToString(),
                    <= 0x1f => byt - 0x10 + " sekund / pole " + (char)(byt % 8 + 65) + (8 - byt / 8).ToString(),
                    <= 0x2f => byt - 0x20 + " minut / pole " + (char)(byt % 8 + 65) + (8 - byt / 8).ToString(),
                    <= 0x3f => byt - 0x30 + " hodin / pole " + (char)(byt % 8 + 65) + (8 - byt / 8).ToString(),
                    <= 0x4f => KodNaFigurku((byte)(byt - 0x40)),
                    <= 0x5f => KodNaFigurku((byte)(byt - 0x50)),
                    <= 0x69 => byt - 0x60 + " hodin (6)",
                    0x6a => "PowerUp",
                    0x6b => "EOF",
                    0x6c => "Four Rows",
                    0x6d => "Prázdná borda",
                    0x6e => "Downloaded",
                    0x6f => "Start pos",
                    <= 0x79 => byt - 0x70 + " hodin (7)",
                    0x7a => "Start pos ROT",
                    0x7b => "Start TAG",
                    0x7c => "DEBUG",
                    0x7d => "Novy hodiny",
                    0x7e => "Nova adresa busu",
                    0x7f => "Prostě 7F",
                    0xff => "Tabula raza",
                    _ => throw new Exception("Blbost")
                }
                //_ => throw new Exception("Zase přišla blbost")
            } ;
            
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SPort?.Close();
        }
    }
}
