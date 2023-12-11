using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace MessengerServer.messages {
    public class MessageHelper {

        public static void sendMessage(string commandName, ClientObject client, params dynamic[] args) {
            List<byte> res = encode(commandName, args.Length, args);
            try {
                client.client.GetStream().Write(res.ToArray(), 0, res.Count);
            } catch {
                client.disconnect();
                Program.clients.Remove(client);
                Console.WriteLine("Разрыв соединения: " + Program.clients.Count);
            }
        }

        public static void sendMessage(string commandName, User user, params dynamic[] args) {
            List<byte> res = encode(commandName, args.Length, args);
            try {
                user.MyClient.client.GetStream().Write(res.ToArray(), 0, res.Count);
            } catch {
                user.MyClient.disconnect();
                Program.clients.Remove(user.MyClient);
                Console.WriteLine("Разрыв соединения: " + Program.clients.Count);
            }
        }

        public static List<byte> encode(string commandName, int length, params dynamic[] args) {

            List<byte> bytes = new List<byte>();

            bytes.AddRange(Encoding.Unicode.GetBytes(commandName+"|"));
            bytes.AddRange(Encoding.Unicode.GetBytes(length+"|"));

            string t = "";

            if (args != null)
                for (int i = 0; i < args.Length; i++) {
                    dynamic arg = args[i];

                    if (arg == null)
                        continue;
                    if (i > 0) {
                        t+="|";
                    }

                    if (arg is string) {
                        t+=arg;
                    }
                    if (arg is byte[]) {
                        bytes.AddRange(Encoding.Unicode.GetBytes(t));
                        t = "";
                        bytes.AddRange(arg);
                    }
                    if (arg is byte) {
                        t += arg;
                    }
                    if (arg is User) {
                        t += arg.ToString();
                    }
                    if (arg is DateTime) {
                        t += ""+arg.Ticks;
                    }
                    if (arg is bool) {
                        t += arg ? "1" : "0";
                    }
                    if (arg is int) {
                        t+=arg;
                    }
                }
            if (t.Length > 0)
                bytes.AddRange(Encoding.Unicode.GetBytes(t));

            int size = bytes.Count;
            List<byte> result = new List<byte>();
            result.AddRange(BitConverter.GetBytes(size));
            result.AddRange(bytes); 

            return result;
        }

    }
}
