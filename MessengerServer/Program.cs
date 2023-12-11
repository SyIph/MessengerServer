using MessengerServer.messages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MessengerServer {
    class Program {
        private static int port = 8080;
        private static string address = "localhost";
        private static string dbConnectionString = @"Server=.\SQLEXPRESS_MESSG;Database=MessengerDB;User ID=messenger_server;Password=123;TrustServerCertificate=True;";
        private static TcpListener listener;
        private static string cfgPath = "Config.txt";

        //public static List<User> users;

        public static List<ClientObject> clients;

        public static SqlConnection connection;

        static void Main(string[] args) {
            readFile();
            Console.WriteLine("Запуск сервера");
            //users = new List<User>();
            clients = new List<ClientObject>();
            connection = new SqlConnection(dbConnectionString);

            SetConsoleCtrlHandler(new HandlerRoutine(consoleClosed), true);

            try {
                connection.Open();
                ThreadPool.QueueUserWorkItem((o) => {
                    try {
                        startListener();
                        Console.WriteLine("Ожидание подключений...");
                        while (true) {
                            TcpClient client = listener.AcceptTcpClient();
                            client.ReceiveBufferSize = int.MaxValue - 1;
                            checkAllConnections();
                            ClientObject clientObject = new ClientObject(client);
                            clients.Add(clientObject);
                            Thread thread = new Thread(new ThreadStart(clientObject.reciveData));
                            thread.Start();
                        }
                    } catch (SocketException e) {
                        Debug.WriteLine("Exception: " + e);
                    }
                });

                while (true) { 
                    //Можно сделать ввод команд)
                }

            } catch (Exception ex) {
                Console.WriteLine("Ошибка при подключении к базе данных!");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Сервер остановлен");
            }
            Console.ReadLine();
        }

        public void acceptClients() { 
        
        }

        public static void checkAllConnections() {
            ThreadPool.QueueUserWorkItem((o) => {
                int i = 0;
                while (i < clients.Count) {
                    ClientObject co = clients[i];
                    if (co.checkConnection()) {
                        i++;
                    } else {
                        clients.Remove(co);
                    }
                }
            });
        }

        ////////////////////////////Отслеживание закрытия///////////////////////////////////////
        public enum CtrlTypes {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);
        private static bool consoleClosed(CtrlTypes ctrlType) {
            connection.Close();
            closeListener();
            return true;
        }
        /////////////////////////////////////////////////////////////////////////////////////////

        private static void startListener() {
            if (address == "localhost")
                listener = new TcpListener(IPAddress.Any, port);
            else
                listener = new TcpListener(IPAddress.Parse(address), port);
            listener.Start();
        }

        private static void closeListener() {
            if (listener != null) {
                listener.Stop();
                listener = null;
            }
            Console.WriteLine("Сервер остановлен");
        }

        private static void readFile() {
            StreamReader reader = new StreamReader(cfgPath);
            string sLine = "";
            while ((sLine = reader.ReadLine()) != null) {
                List<string> parsed_line = sLine.Split('=').ToList();
                string param = parsed_line[0];
                parsed_line.RemoveAt(0);
                string value = string.Join('=', parsed_line);
                value = value.Substring(1, value.Length-2);
                if (value.Length > 0)
                    switch (param) {
                        case "address":
                            address = value;
                            break;
                        case "port":
                            port = int.Parse(value);
                            break;
                        case "connection_string":
                            dbConnectionString = value;
                            break;
                        default:
                            break;
                    }
            }
            reader.Close();
        }

    }
}
