/*
  arduino temperature measure:
 This sketch receives UDP message strings, prints them to the serial port
 and sends temperature value string back to the sender
 

 
 created 10 april 2014
 by francis gahang
 
  using a lm19 sensor an a arduino uno:
  vout = analog pin 0
  vin = 5 volt pin
  Gnd = Gnd pin
  
 */


#include <SPI.h>         // needed for Arduino versions later than 0018
#include <Ethernet.h>
#include <Udp.h>         // UDP library from: bjoern@cs.stanford.edu 12/30/2008

// Enter a MAC address and IP address for your controller below.
// The IP address will be dependent on your local network:
byte mac[] = {  
  0x90, 0xA2, 0xDA, 0x0D, 0x3A, 0x92 };
byte ip[] = { 
  169,254,245,198 };

unsigned int localPort = 8888;      // local port to listen on
unsigned int Port = 13000;


// the next two variables are set when a packet is received
byte remoteIp[4];        // holds received packet's originating IP
unsigned int remotePort; // holds received packet's originating port

// buffers for receiving and sending data
char packetBuffer[UDP_TX_PACKET_MAX_SIZE]; //buffer to hold incoming packet,

char test4[] = "temperature";
char test5[] = "stop";
char temp[UDP_TX_PACKET_MAX_SIZE];

char  ReplyBuffer5[32];
float vin=0.0 ;
float tempC=0.0; // valeur de la temperature

void setup() {
  // start the Ethernet and UDP:
  pinMode(9,OUTPUT);
  Ethernet.begin(mac,ip);
  Udp.begin(localPort);

  Serial.begin(9600);
}

void loop() {

  

  int packetSize = Udp.available(); // note that this includes the UDP header

  if(packetSize)
  {
    packetSize = packetSize - 8;      // subtract the 8 byte header
    Serial.print("Received packet of size ");
    Serial.println(packetSize);

    // read the packet into packetBufffer and get the senders IP addr and port number

    Udp.readPacket(packetBuffer,UDP_TX_PACKET_MAX_SIZE, remoteIp, remotePort);
    Serial.println("Contents:");
    Serial.println(packetBuffer);
    for (int i =0; i < 4; i++)
    {
      Serial.print(remoteIp[i], DEC);
      if (i < 3)
      {
        Serial.print(".");
      }
    }
    Serial.println("");
    Serial.println(remotePort);

    for(int i=0;i<UDP_TX_PACKET_MAX_SIZE;i++) temp[i] = packetBuffer[i] ;

    control(temp);




    //(byte*)ReplyBuffer,strlen(ReplyBuffer),
    for(int i=0;i<UDP_TX_PACKET_MAX_SIZE;i++) 
    {
      packetBuffer[i] = 0;
      // temp[i]=0;
    }

    Serial.println("...........................................................");
  }
  delay(1000);

}

void control(char temp[])
{


  if( strcmp(temp,test4)==0)
  {

    do
    {
      vin = 5.0 * analogRead(0) / 1023.0;
      // Serial.print(vin);
      // Serial.print("  ");
      tempC = (1.8663 - vin) / 0.01169;// temp in celcius
      //int x =(int)tempC;
      int temp1 = (tempC - (int)tempC) * 100;
      sprintf(ReplyBuffer5,"%d,%d", (int)tempC, temp1);
      //sprintf(ReplyBuffer5, "%d",x);

      //ReplyBuffer5[0]= 40;
      Udp.sendPacket(ReplyBuffer5, remoteIp, Port);
      Serial.print(ReplyBuffer5);
      Serial.print("    <.....R.....et tempc.>.....");
      Serial.println(tempC);


      ////////////////////////////////////////////////////
      int packetSize = Udp.available(); // note that this includes the UDP header
      if(packetSize)
      {
        packetSize = packetSize - 8;      // subtract the 8 byte header
        Serial.print("Received packet of size ");
        Serial.println(packetSize);

        // read the packet into packetBufffer and get the senders IP addr and port number
        Udp.readPacket(packetBuffer,UDP_TX_PACKET_MAX_SIZE, remoteIp, remotePort);
        Serial.println("Contents:");
        Serial.println(packetBuffer);
        for (int i =0; i < 4; i++)
        {
          Serial.print(remoteIp[i], DEC);
          if (i < 3)
          {
            Serial.print(".");
          }
        }
        Serial.println("");
        Serial.println(remotePort);

        for(int i=0;i<UDP_TX_PACKET_MAX_SIZE;i++) temp[i] = packetBuffer[i] ;

        //control(temp);

        //(byte*)ReplyBuffer,strlen(ReplyBuffer),
        for(int i=0;i<UDP_TX_PACKET_MAX_SIZE;i++) 

          packetBuffer[i] = 0;



        Serial.println("...........................................................");
     
      }
      ///////////////////////////////////////////////////
      delay(1000);

    }
    while(strcmp(temp,test5)!=0); 
    // reinitialize variable
    for(int i=0;i<UDP_TX_PACKET_MAX_SIZE;i++) 
    {
      packetBuffer[i] = 0;
      temp[i]=0;
    }
  }
}



