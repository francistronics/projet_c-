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
        double val;
        double time;
        Boolean suspend = false;
        
        
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
            Thread th1 = new Thread(asktemperature);
            th1.Start();

            //////////////////////////////rajouts////////////////////////////////
            
            serveur.Connect(IPAddress.Parse("169.254.245.198"), int.Parse("8888"));
            labeltemp.Text = "0";
            
            receivemessage.Client.ReceiveTimeout = 100;
            receivemessage.Client.Blocking = false;

            /////////////////////////gestion du graphique///////////////////////////////////////////

            GraphPane myPane = zedGraphControl1.GraphPane;
            myPane.Title.Text = "Test of Dynamic Data Update with ZedGraph\n" +
                  "(After 25 seconds the graph scrolls)";
            myPane.XAxis.Title.Text = "Time, Seconds";
            myPane.YAxis.Title.Text = "temperature value";

            // Save 1200 points.  At 50 ms sample rate, this is one minute
            // The RollingPointPairList is an efficient storage class that always
            // keeps a rolling set of point data without needing to shift any data values
            RollingPointPairList list = new RollingPointPairList(1200);

            // Initially, a curve is added with no data points (list is empty)
            // Color is blue, and there will be no symbols
            LineItem curve = myPane.AddCurve("temperature", list, Color.Gold, SymbolType.None);

            // Sample at 50ms intervals
            timer1.Interval = 50;
            timer1.Enabled = true;
            timer1.Start();

            // Just manually control the X axis range so it scrolls continuously
            // instead of discrete step-sized jumps
            myPane.XAxis.Scale.Min = 0;
            myPane.XAxis.Scale.Max = 30;
            myPane.XAxis.Scale.MinorStep = 1;
            myPane.XAxis.Scale.MajorStep = 5;

            // Scale the axes
            zedGraphControl1.AxisChange();

            // Save the beginning time for reference
            tickStart = Environment.TickCount;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            
                // Make sure that the curvelist has at least one curve
                if (zedGraphControl1.GraphPane.CurveList.Count <= 0)
                    return;

                // Get the first CurveItem in the graph
                LineItem curve = zedGraphControl1.GraphPane.CurveList[0] as LineItem;

                if (curve == null)
                    return;

                // Get the PointPairList
                IPointListEdit list = curve.Points as IPointListEdit;
                // If this is null, it means the reference at curve.Points does not
                // support IPointListEdit, so we won't be able to modify it
                if (list == null)
                    return;

                // Time is measured in seconds
                time = (Environment.TickCount - tickStart) / 1000.0;


                /////////rajout/////////////////////////////
                list.Add(time, val);
                ////////////////////////////////////////////



                // Keep the X scale at a rolling 30 second interval, with one
                // major step between the max X value and the end of the axis
                Scale xScale = zedGraphControl1.GraphPane.XAxis.Scale;
                if (time > xScale.Max - xScale.MajorStep)
                {
                    xScale.Max = time + xScale.MajorStep;
                    xScale.Min = xScale.Max - 30.0;

                }

                // Make sure the Y axis is rescaled to accommodate actual data
                zedGraphControl1.AxisChange();
                // Force a redraw
                zedGraphControl1.Invalidate();
           
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            SetSize();
        }

        // Set the size and location of the ZedGraphControl
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
                       // labeltemp.Text = ASCIIEncoding.ASCII.GetString(rcvbytes);
                        labeltemp.Text = Convert.ToString(val);
                       
                    }
                    catch (Exception ex)
                    {
                        string code = ex.HelpLink;
                    }

                    Thread.Sleep(5000);
                }
                while (suspend)
                {
                    ;
                }
            }

        }
        
      }
   }
      
    

