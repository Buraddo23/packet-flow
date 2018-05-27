using System;
using System.Threading;
using System.Collections.Generic;

namespace WindowsFormsApp1 {
	static class Proiect {
		
		public enum tip { Bus, Ring, Star, Router }; //Tipurile de componente din sistem
		
		//Clasa abstracta din care deriva Calculatoarele, Busul si Routerul
		//Pt. explicarea functiilor vezi clasele derivate
		public class Component {
			protected string identifier;
			public List<Pipe> connections;
			public Bus b;
			protected tip grup;
			
			public void addConnection(Pipe p) {
				connections.Add(p);
			}
			
			public void addBus(Bus bus) {
				b = bus;
			}
			
			public string getId() {
				return identifier;
			}
			
			public tip getType() {
				return grup;
			}

			public void storeBuffer(Packet p, string cc) {
				Console.WriteLine("Eroare: Chemare storeBuffer pe Component");
			}
			public void loadBuffer(Packet data) {
				Console.WriteLine("Eroare: Chemare loadBuffer pe Component");
			}
		}
		
		public class Calculator : Component {
			//Constructor fara parametri pentru creerea initiala a vectorului din main
			public Calculator() {}
			
			//Constructorul propriu-zis, initializeaza calculatoare
			public Calculator (string i, tip g) {
				identifier = i;
				grup = g;
				connections = new List<Pipe>();
			}
			
			//Se apeleaza cand ajunge un pachet la destinatia finala
			public void receiveData(long i) {
				Console.WriteLine("Calculatorul {0} a primit date: {1}", identifier, i);
			}
		}
		
		public class Router : Component {
			public Router () {
				identifier = "Router";
				grup = tip.Router;
				connections = new List<Pipe>();
			}
		}
		
		//Clasa Pipe simuleaza legaturile dintre componente
		public class Pipe {
			string identifier;
			Component c1, c2; //Capetele pipe-ului
			
			public Pipe(string i, Component cc1, Component cc2, bool bi) {
				identifier = i;
				
				c1 = cc1;
				c2 = cc2;
				
				//Populam listele din componente pentru a putea accesa oricare pipe
				c1.addConnection(this);
				if (bi) c2.addConnection(this);//bi==true => pipe bidirectional
			}
			
			//Functia send pt pipe bidirectional
			public void send(Packet p, string dest) {
				Console.WriteLine("dest: {0}", dest);
				
				Thread.Sleep(2000); //Simulare 2 secunde pt transmitere
				
				//Determinarea capatului dorit
				if (c1.getId() == dest)
					p.movePacket(c1);
				else if (c2.getId() == dest)
					p.movePacket(c2);
				else Console.WriteLine("Destinatie invalida pipe {0}", identifier);
			}
			
			//Functia send pt pipe unidirectional
			public void send(Packet p) {
				Thread.Sleep(2000);
				p.movePacket(c2);
			}
			
			//Functia isComponent verifica returneaza true daca pipe-ul face legatura cu componenta c
			public bool isComponent(string c) {
				return ((c1.getId() == c) || (c2.getId() == c));
			}
			public bool isComponent(Component c) {
				return ((c1 == c) || (c2 == c));
			}
			
			public string getId() {
				return identifier;
			}
		}
		
		public class Bus : Component {
			Calculator[] c = new Calculator[6];
			Router r;
			bool bf; //buffer full (indica daca exista date in curs de transmitere prin bus)
			
			public Bus(string id, Calculator c1, Calculator c2, Calculator c3, Calculator c4, Calculator c5, Router rout) {
				identifier = id;
				
				//Busul leaga 5 calculatoare si un router
				c[1] = c1;
				c[1].addBus(this);
				c[2] = c2;
				c[2].addBus(this);
				c[3] = c3;
				c[3].addBus(this);
				c[4] = c4;
				c[4].addBus(this);
				c[5] = c5;
				c[5].addBus(this);
				
				r = rout;
				
				grup = tip.Bus;
				bf = false;
			}
			
			//incarca datele din pachet pe bus
			public new void loadBuffer(Packet data) {
				while(bf);
				
				Thread.Sleep(500);
				bf = true;
				data.movePacket(this);
			}
			
			//descarca datele/pachetul din bus pe un calculator/router
			public new void storeBuffer(Packet p, string cc) {
				while(!bf);
				
				Thread.Sleep(500);
				if (cc == "Router") {
					p.movePacket(r);
				} else {
					p.movePacket(c[int.Parse(cc)]);
				}
				bf = false;
			}
		}
		
		//Clasa ce descrie comportamentul pachetelor de date
		public class Packet {
			Calculator source, destination;
			Component actualPosition;
			long data;
			Mutex mut; //Semafor ce nu permite calcularea rutei de către mai multe threaduri (conflicte intre pachete)
			
			public Packet(Calculator c1, Calculator c2, long d) {
				source = c1;
				destination = c2;
				actualPosition = c1;
				data = d;
				
				mut = new Mutex();
				
				Console.WriteLine("Pachetul creat în {0}", source.getId());
				
				//Se apeleaza functia packetHandler intr-un thread nou
				//Astfel se permite mutarea a mai multor pachete simultan
				new Thread(new ThreadStart(packetHandler)).Start();
			}
			
			//Functia principala ce determina calcularea rutei si parcurgerea acesteia de catre pachete
			public void packetHandler() {
				mut.WaitOne(); //Activarea semaforului pentru un singur thread
				
				if (actualPosition.getType() == destination.getType()) {
					if (destination.getType() == tip.Star) {
						HandlerStar(); //Parcurgeri in interiorul subsistemului stea
					} else if (destination.getType() == tip.Ring) {
						HandlerRing(); //Parcurgeri in interiorul subsistemului inel
					} else if (destination.getType() == tip.Bus) {
						HandlerBus(); //Parcurgeri in interiorul subsistemului magistrala
					}
				} else if (actualPosition.getType() == tip.Router) {
					HandlerRouter(); //Parcurgeri intre subsisteme
				} else {
					if (actualPosition.getType() == tip.Star) {
						
						//Parcurgeri de la stea la Router
						if (actualPosition.getId() == "10") {
							foreach (Pipe conn in actualPosition.connections) {
								if (conn.isComponent("Router")) {
									conn.send(this, "Router");
									break;
								}
							}
						} else {
							foreach (Pipe conn in actualPosition.connections) {
								if (conn.isComponent("10")) {
									conn.send(this, "10");
									break;
								}
							}
						}
						
					} else if (actualPosition.getType() == tip.Ring) {
						
						//Parcurgeri de la inel la Router
						if (actualPosition.getId() == "6") {
							actualPosition.connections[1].send(this, "Router");
						} else {
							HandlerRing();
						}
						
					} else if (actualPosition.getType() == tip.Bus) {
						//Parcurgeri de la Bus
						HandlerBus();
					}
				}
				
				mut.ReleaseMutex();//Eliberarea semaforului
			}
			
			//Muta pachetul mai departe in retea si cheama functia de finalizare sau reapeleaza serviciul de rutare
			public void movePacket(Component c) {
				actualPosition = c;
				Console.WriteLine("Pachet mutat in {0}", c.getId());
				
				if (actualPosition == destination) {
					Thread.Sleep(1000);
					destination.receiveData(data);
				} else {
					packetHandler();
				}
			}
			
			//Returneaza pozitia actuala a pachetului
			public string getPos() {
				return actualPosition.getId();
			}
			
			//Parcurgerile din subsistemul stea
			void HandlerStar() {
				string d;
				
				if (actualPosition.getId() == "10") {
					d = destination.getId();
				} else {
					d = "10";
				}
				
				foreach (Pipe conn in actualPosition.connections) {
					if (conn.isComponent(d)) {
						conn.send(this, d);
						break;
					}
				}
			}
			
			//Parcurgerea din subsistemul inel
			void HandlerRing() {
				actualPosition.connections[0].send(this);
			}
			
			//Parcurgerea din subsistemul magistrala
			void HandlerBus() {
				if (actualPosition.getId() == "Bus") {
					if (destination.getType() == tip.Bus) {
						destination.b.storeBuffer(this, destination.getId());
					} else {
						source.b.storeBuffer(this, "Router");
					}
				} else {
					actualPosition.b.loadBuffer(this);
				}
			}
			
			//Parcurgeri prin router
			void HandlerRouter() {
				string d;
				
				if (destination.getType() == tip.Star) {
					d = "10";
				} else if (destination.getType() == tip.Ring) {
					d = "6";
				} else if (destination.getType() == tip.Bus) {
					destination.b.loadBuffer(this);
					return;
				} else {
					d = "";
					Console.WriteLine("Eroare: HandlerRouter; Destinatia are tip gresit");
					return;
				}
					
				foreach (Pipe conn in actualPosition.connections) {
					if (conn.isComponent(d)) {
						conn.send(this, d);
						break;
					}
				}
			}
		}
		

		public static void Main() {
			//Calculatoarele puse in array pentru a se putea refetentia folosind un int
			Calculator[] c = new Calculator[13];
			
			for (int i = 1; i <= 5; i++)
				c[i] = new Calculator(i.ToString(), tip.Bus);
			for (int i = 6; i <= 8; i++)
				c[i] = new Calculator(i.ToString(), tip.Ring);
			for (int i = 9; i <= 12; i++)
				c[i] = new Calculator(i.ToString(), tip.Star);
				
			Router r = new Router();
			
			Bus b = new Bus("Bus", c[1], c[2], c[3], c[4], c[5], r);
			
			//Star Pipes
			Pipe p10_12 = new Pipe("10-12", c[10], c[12], true);
			Pipe p10_11 = new Pipe("10-11", c[10], c[11], true);
			Pipe p10_9  = new Pipe("10-9" , c[10], c[9] , true);
			
			//Ring Pipes
			Pipe p6_8 = new Pipe("6-8", c[6], c[8], false);
			Pipe p8_7 = new Pipe("8-7", c[8], c[7], false);
			Pipe p7_6 = new Pipe("7-6", c[7], c[6], false);
			
			//Router Pipes
			Pipe pr_10 = new Pipe("R-10", r, c[10], true);
			Pipe pr_6  = new Pipe("R-6" , r, c[6] , true);
			
			//Creeri de pachete:
			/*Packet p1 = new Packet(c[10], c[12], 255);
			Packet p2 = new Packet(c[11], c[10], 128);
			Packet p3 = new Packet(c[9] , c[11], 2);
			Packet p4 = new Packet(c[6] , c[7] , 3);
			Packet p5 = new Packet(c[8] , c[12], 4);
			Packet p6 = new Packet(c[12], c[8] , 5);
			Packet p7 = new Packet(c[9], c[11], 1);
			Packet p8 = new Packet(c[11], c[9], 2);
			new Packet(c[1], c[2], 1);
			new Packet(c[3], c[5], 3);
			new Packet(c[4], c[12], 4);
			new Packet(c[8], c[1], 8);*/
		}
	}
}