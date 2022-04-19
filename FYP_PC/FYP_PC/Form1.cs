using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Excel = Microsoft.Office.Interop.Excel;
using System.Collections;
using System.Threading;

namespace ard_wid
{
    public partial class Form1 : Form
    {

        public SerialPort sp = new SerialPort();
        public Stopwatch sw = new Stopwatch(); //measures time , x axis of graph
        //array lists to store values for saving in Excel
        public ArrayList time_col = new ArrayList();
        public ArrayList temperature_col = new ArrayList();
        public ArrayList humidity_col = new ArrayList();
        //Excel file location and name
        public string filename = "D:\\test data";

        public Form1()
        {
            InitializeComponent();
            sp.PortName = "COM3";
            sp.BaudRate = 9600;
            sp.Parity = Parity.None;
            sp.Handshake = Handshake.None;
            sp.StopBits = StopBits.One;
            sp.DataBits = 8;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Stop_btn.Enabled = false;
            Save_btn.Enabled = false;
            Open_btn.Enabled = false;
        }

        private void Start_btn_Click(object sender, EventArgs e)
        {
            sp.Open();
            sp.DataReceived += Sp_DataReceived;
            sw.Start();
            Stop_btn.Enabled = true;
            Start_btn.Enabled = false;
            Save_btn.Enabled = false;
            Open_btn.Enabled = false;
        }

        private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data, temperature, humidity;
            double time;
            string[] data_array = new string[2];
            data = sp.ReadLine();
            data_array = Regex.Split(data,", ");
            try
            {
                temperature = data_array[0];
                Temp_txt.Invoke(new Action(() => Temp_txt.Text = temperature));
                humidity = data_array[1];
                Hum_txt.Invoke(new Action(() => Hum_txt.Text = humidity));
                temperature_col.Add(temperature);
                humidity_col.Add(humidity);
                time = sw.ElapsedMilliseconds / 1000.000;
                time_col.Add(time);
                chart1.Invoke(new Action(() => chart1.Series["Temperature"].Points.AddXY(time, temperature)));
                chart1.Invoke(new Action(() => chart1.Series["Humidity"].Points.AddXY(time, humidity)));
            }
            catch(System.IndexOutOfRangeException)
            {
                Thread.Sleep(500);
            }
            //throw new NotImplementedException();
        }

        private void Stop_btn_Click(object sender, EventArgs e)
        {
            sp.DataReceived -= Sp_DataReceived;
            sp.Close();
            sw.Stop();
            temperature_col.TrimToSize();
            humidity_col.TrimToSize();
            time_col.TrimToSize();
            Start_btn.Enabled = true;
            Save_btn.Enabled = true;
            Stop_btn.Enabled = false;
        }

        private void Save_btn_Click(object sender, EventArgs e)
        {
            string date = DateTime.Now.ToString(@"M\.dd\.yyyy h\.mm\.ss tt");

            //Initialize Excel file
            Excel.Application xlApp = new Excel.Application();
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            Excel.Range xlRange;
            object misValue = System.Reflection.Missing.Value;
            xlWorkBook = xlApp.Workbooks.Add(misValue);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

            //Fill Excel sheet
            xlWorkSheet.Cells[1, 1] = "Time";
            xlWorkSheet.Cells[1, 2] = "Temperature";
            xlWorkSheet.Cells[1, 3] = "Humidity";
            for (int i = 0; i < time_col.Count; i++)
            {
                xlWorkSheet.Cells[i + 2, 1] = time_col[i];
                xlWorkSheet.Cells[i + 2, 2] = temperature_col[i];
                xlWorkSheet.Cells[i + 2, 3] = humidity_col[i];
            }

            //Make columns autofit
            xlRange = xlWorkSheet.get_Range("A1", "C1");
            xlRange.EntireColumn.AutoFit();

            //Save Excel file
            filename = filename + " " + date + ".xlsx";
            xlWorkBook.SaveAs(filename, Excel.XlFileFormat.xlOpenXMLWorkbook, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);
            xlWorkBook.Close(true, misValue, misValue);
            xlApp.Quit();

            Marshal.ReleaseComObject(xlWorkSheet);
            Marshal.ReleaseComObject(xlWorkBook);
            Marshal.ReleaseComObject(xlApp);

            Console.Beep();
            MessageBox.Show("Data saved to Excel File.");

            //Clear arrays for next iteration
            temperature_col.Clear();
            humidity_col.Clear();
            time_col.Clear();

            Open_btn.Enabled = true;
        }

        private void Open_btn_Click(object sender, EventArgs e)
        {
            Process.Start(filename);
        }
    }
}
