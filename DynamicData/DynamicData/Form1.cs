using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using ZedGraph;



namespace DynamicData
{
    public partial class Form1 : Form
    {
        //mes rajouts///
        UdpClient serveur = new UdpClient(9000);
        UdpClient receivemessage = new UdpClient(13000);
        byte[] datatosend = new byte[1024];
        double val; // valeur ou est stocke la temperature recu de l'arduino
        double time;
        Boolean suspend = false; // boolean permettant de gerer le thread
        Thread th1;
        
        ////////////////
        // Starting time in milliseconds
        int tickStart = 0;
        
        
        public Form1()
        {
            InitializeComponent();
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //////////////////initialisons le thread////////////////////////////
             th1 = new Thread(asktemperature);
            th1.Start();

            //////////////////////////////rajouts////////////////////////////////
            
            serveur.Connect(IPAddress.Parse("169.254.245.198"), int.Parse("8888"));
            labeltemp.Text = "0";
           

            receivemessage.Client.ReceiveTimeout = 100;
            receivemessage.Client.Blocking = false;

            /////////////////////////gestion du graphique///////////////////////////////////////////

            GraphPane myPane = zedGraphControl1.GraphPane; // objet de type graphpane
            myPane.Title.Text = "Arduino temperature Dynamic Data Update monitoring with ZedGraph\n" +
                  "(After 25 seconds the graph scrolls)";
            myPane.XAxis.Title.Text = "Time, Seconds";
            myPane.YAxis.Title.Text = "temperature value";

             
            //Enregistre 100 points. À un taux d'échantillonnage de 50ms
            //Le RollingPointPairList est une classe de stockage efficace qui 
            //maintient un ensemble de roulement de données de points sans avoir besoin de passer des valeurs de données
            RollingPointPairList list = new RollingPointPairList(1000);

            // on ajoute initialement une courbe avec la liste de donnee vide
            // Color is gold, and there will be no symbols
            LineItem curve = myPane.AddCurve("temperature", list, Color.Gold, SymbolType.None);

            // Sample at 50ms intervals
            timer1.Interval = 50;
            timer1.Enabled = true;
            timer1.Start();

            
            //contrôle de la plage de l'axe X de sorte qu'il défile en continu 
            myPane.XAxis.Scale.Min = 0;
            myPane.XAxis.Scale.Max = 30;
            myPane.XAxis.Scale.MinorStep = 1;
            myPane.XAxis.Scale.MajorStep = 5;

            // graduation des axes
            zedGraphControl1.AxisChange();

            // On enregistre l'heure de debut qu'on prend comme reference
            tickStart = Environment.TickCount;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
                 ///ecriture sur le label////
                labeltemp.Text = Convert.ToString(val);
                // on s'assure davoir au moins une courbe dans la curvelist
                if (zedGraphControl1.GraphPane.CurveList.Count <= 0)
                    return;

                // on se reserve la premiere courbe dans le graphique
                LineItem curve = zedGraphControl1.GraphPane.CurveList[0] as LineItem;

                if (curve == null)
                    return;

                // on recup la liste de points
                IPointListEdit list = curve.Points as IPointListEdit;
                // If this is null, it means the reference at curve.Points does not
                // support IPointListEdit, so we won't be able to modify it
                if (list == null)
                    return;

                // le temps est mesuree en seconde
                time = (Environment.TickCount - tickStart) / 1000.0;


                /////////on ajoute les nouveaux points a la liste/////////////////////////////
                list.Add(time, val);
                ////////////////////////////////////////////



                // on Garde l'échelle de X à une plage de 30 secondes, avec un
                // pas important entre la valeur de X max et l'extrémité de l'axe
                Scale xScale = zedGraphControl1.GraphPane.XAxis.Scale;
                if (time > xScale.Max - xScale.MajorStep)
                {
                    xScale.Max = time + xScale.MajorStep;
                    xScale.Min = xScale.Max - 30.0;

                }

                // on s'assure que la modification des axes est aussi effectives pour les y
                zedGraphControl1.AxisChange();
                // on force le dessin
                zedGraphControl1.Invalidate();
           
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            SetSize();
        }

        // on Définis la taille et l'emplacement du ZedGraphControl
        private void SetSize()
        {
            // Control is always 100 pixels inset from the client rectangle of the form
            Rectangle formRect = this.ClientRectangle;
            formRect.Inflate(-100, -100);

            if (zedGraphControl1.Size != formRect.Size)
            {
                zedGraphControl1.Location = formRect.Location;
                zedGraphControl1.Size = formRect.Size;
            }
        
        }

        private void btnstop_Click(object sender, EventArgs e)

        {
          timer1.Stop();
          suspend = true;
        }

        private void btnstart_Click(object sender, EventArgs e)

        {           
            timer1.Start();
            suspend = false;         
        }

        private void asktemperature()
        {
            while (true)
            {
                while (!suspend)
                {
                    ////////// demande de la temperature au arduino/////
                    datatosend = ASCIIEncoding.ASCII.GetBytes("temperature");
                    serveur.Send(datatosend, datatosend.Length);
                    /////////// on ecoute le reseau pour obtenir la reponse////

                    try
                    {
                        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 13000);
                        Byte[] rcvbytes = receivemessage.Receive(ref RemoteIpEndPoint);

        
                        
                            val = double.Parse(ASCIIEncoding.ASCII.GetString(rcvbytes)); // on recupere la valeur recu pour lutiliser dans le tracer du graphe
                            //labeltemp.Text = ASCIIEncoding.ASCII.GetString(rcvbytes);
                            //MessageBox.Show(Convert.ToString(val));
                        
                        
                        
                    }
                    catch (Exception ex)
                    {
                        string code = ex.HelpLink;
                    }

                    Thread.Sleep(100);
                }
                while (suspend)
                {
                    ;
                }
            }

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            th1.Abort();       // on coupe le thread a la fermeture de la fenetre
        }
        
      }
   }
      
    

