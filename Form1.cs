using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace fiscales2da
{
    public partial class Form1 : Form
    {
        int nroMes, anio;
        int tiempo = 0;
        string _punto_venta = "", _fecha_archivo = "", _ip = "", _fecha = "";
        string s1_inicio, s1_fin, s2_inicio, s2_fin, s3_inicio, s3_fin, s4_inicio, s4_fin;

        string[] meses = {"SELECCIONAR","ENERO", "FEBRERO", "MARZO", "ABRIL", "MAYO", "JUNIO",
                              "JULIO", "AGOSTO", "SEPTIEMBRE", "OCTUBRE", "NOVIEMBRE", "DICIEMBRE"
                             };
        StreamReader lector;

        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DateTime fechaActual = DateTime.Today;
            anio_numerico.Value = fechaActual.Year;
            anio = Convert.ToInt32(anio_numerico.Value);

            s1_inicio = "01"; s1_fin = "07";
            s2_inicio = "08"; s2_fin = "14";
            s3_inicio = "15"; s3_fin = "21";
            s4_inicio = "22";

            cb.Items.Clear();
            cb.SelectedItem = meses[0];

            for (int i = 0; i < meses.Length; i++)
            {
                cb.Items.Add(meses[i]);
            }

            //cargo el combo de las IP y asigno solamente la IP con la posicion 1 despues de pasar por el Split
            llenoComboTxt("ip.txt", ':', comboIp, 1);

        }

        private void comboPeriodo_SelectedIndexChanged(object sender, EventArgs e)
        {
            //En base al periodo seleccionado, armo parte del nombre del archivo .

            string anioMes = anio.ToString() + "-" + nroMes.ToString("D2") + "-";
            string[] lines = comboPeriodo.Text.Split(new string[] { "al" }, StringSplitOptions.None);
            _fecha = anioMes + lines[0].Trim() + "-al-"+anioMes +lines[1].Trim();
            _fecha_archivo = anio.ToString().Remove(0, 2) + nroMes.ToString("D2") + lines[0].Trim() + " " + anio.ToString().Remove(0, 2) + nroMes.ToString("D2") + lines[1].Trim();
        }

        private void timer_tiempo_Tick(object sender, EventArgs e)
        {
            tiempo++;
            
            TimeSpan time = TimeSpan.FromSeconds(tiempo);
            lbl_tiempo.Text = time.ToString(@"mm\:ss");
        }

        private void anio_numerico_ValueChanged(object sender, EventArgs e)
        {
            anio = Convert.ToInt32(anio_numerico.Value);
        }

        private void cb_SelectedIndexChanged(object sender, EventArgs e)
        {
            nroMes = cb.SelectedIndex;
            
            if (nroMes != 0)
            {

                int diaMes = System.DateTime.DaysInMonth(anio, nroMes);
                switch (diaMes)
                {
                    case 28:
                        s4_fin = "28";
                        break;
                    case 29:
                        s4_fin = "29";
                        break;
                    case 30:
                        s4_fin = "30";
                        break;
                    case 31:
                        s4_fin = "31";
                        break;
                }
               
                comboPeriodo.Items.Clear();
                comboPeriodo.Items.Add(s1_inicio + " al " + s1_fin);
                comboPeriodo.Items.Add(s2_inicio + " al " + s2_fin);
                comboPeriodo.Items.Add(s3_inicio + " al " + s3_fin);
                comboPeriodo.Items.Add(s4_inicio + " al " + s4_fin);
            }
            else
            {
                MessageBox.Show("Debe Seleccionar un Mes Válido.");
                
            }
        }
        private void comboIp_SelectedIndexChanged(object sender, EventArgs e)
        {
            string[] lines = comboIp.Text.Split(new string[] { " --> " }, StringSplitOptions.None);
            _punto_venta = lines[1];
            _ip = lines[0];
        }
       
        private void llenoComboTxt(string archivoTxt, char CaracterSplit, ComboBox nombreCombo, int posicion )
        {
            lector = new StreamReader(archivoTxt);
            
            while (lector.Peek() >= 0)
            {
                var datos = lector.ReadLine().Split(CaracterSplit);
                string ip = datos[posicion];
                string nombre = datos[0];

                nombreCombo.Items.Add(ip + " --> " + nombre);
              
            }
        }
        private int ejecutable(string _ip, string _pv, string _fecha, string _fecha_archivo)
        {
            CheckForIllegalCrossThreadCalls = false;
            //ejem: getaudar -p 192.168.0.199 -i xml -o PV-0069-Playa-afip-del-01-09-2020-al-07-09-2020 -a 201001 201002
            string sentencia = "getaudar -p " + _ip + " -i xml -o " + _pv + "-afip-del-" + _fecha + ".zip -a " + _fecha_archivo;

            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;

            cmd.Start();

            cmd.StandardInput.WriteLine(sentencia);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();

            while (!cmd.StandardOutput.EndOfStream)
            {
                string line = cmd.StandardOutput.ReadLine();
                textBox1.Text += line + Environment.NewLine;

            }
            cmd.WaitForExit();

            if (textBox1.Text.Contains("Fin de la descarga"))
            {
                return 1;
            }
            else 
            {
                return 0;
            }
          
        }
        private async void btn_iniciar_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            lbl_status.Visible = false;
            this.BackColor = Color.FromKnownColor(KnownColor.Control);
            if ((_ip != "") && (_punto_venta != "") && (_fecha != "") && (_fecha_archivo != ""))
            {
                tiempo = 0;
                timer_tiempo.Enabled = true;
                btn_iniciar.Enabled  = false;
                progressBar1.Visible = true;

                progressBar1.Maximum = 100;
                progressBar1.Step = 1;
                var someTask = Task<int>.Factory.StartNew(() => ejecutable(_ip, _punto_venta, _fecha, _fecha_archivo));
                await someTask;
                
                if (someTask.Result == 1)
                {
                    progressBar1.Visible = false;
                    btn_iniciar.Enabled = true;
                    timer_tiempo.Enabled = false;
                    Process.Start(Application.StartupPath);
                    this.BackColor = Color.Green;
                    lbl_status.Visible = true;
                    lbl_status.Text = "Proceso Exitoso";
                    
                }
                else
                {
                    progressBar1.Visible = false;
                    btn_iniciar.Enabled = true;
                    timer_tiempo.Enabled = false;
                    this.BackColor = Color.Tomato;
                    lbl_status.Visible = true;
                    lbl_status.Text = "Proceso Fallido";

                }
                
            }
            else
            {
                MessageBox.Show("Uno o mas campos estan sin Completar.");
            }
            
        }
       
    }
}
