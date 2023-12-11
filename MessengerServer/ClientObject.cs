using MessengerServer.messages;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using static MessengerServer.messages.MessageHelper;

namespace MessengerServer {
    public class ClientObject {
 
        public User main = null;
        public TcpClient client = null;

        public ClientObject(TcpClient tcpClient) {
            client = tcpClient;
        }

        public void reciveData() {
            try {
                NetworkStream stream = client.GetStream();
                while (client != null && client.Connected) {
                    while (stream.DataAvailable) {
                        byte[] buffer = new byte[4];
                        stream.Read(buffer, 0, buffer.Length);
                        int size = BitConverter.ToInt32(buffer, 0);
                        buffer = new byte[size];
                        stream.Read(buffer, 0, buffer.Length);
                        if (buffer.Length > 0) {
                            //Console.WriteLine("Received: " + buffer.Length + Encoding.Unicode.GetString(buffer));

                            string[] temp = Encoding.Unicode.GetString(buffer).Split('|');
                            string funcName = temp[0];
                            string argCount = temp[1];

                            buffer = buffer.Skip(Encoding.Unicode.GetBytes(funcName + "|" + argCount + "|").Length).ToArray();

                            //Console.WriteLine(Encoding.Unicode.GetString(buffer));

                            MethodInfo m = this.GetType().GetMethod(funcName);
                            if (m != null) {
                                m.Invoke(this, new object[] { buffer });
                            }
                        }
                    }
                }
                Thread.CurrentThread.Abort();
            } catch {}
        }

        /////////
        public void editMessage(byte[] bytes) {
            StringBuilder sb = new StringBuilder();
            List<string> par = new List<string>();
            par.AddRange(sb.Append(Encoding.Unicode.GetString(bytes)).ToString().Split('|'));
            string friendName = par[0];
            string myName = main.UserName;
            par.RemoveAt(0);

            string sDate = par[0];
            DateTime date = DateTime.FromBinary(long.Parse(sDate));
            par.RemoveAt(0);

            string containImage = par[0];
            par.RemoveAt(0);

            if (containImage == "1") {

                byte[] byteImage = new byte[0];
                int textSize = 3 + friendName.Length + sDate.Length + containImage.Length;
                Array.Resize(ref byteImage, bytes.Length - textSize * 2);
                Array.Copy(bytes, textSize * 2, byteImage, 0, bytes.Length - textSize * 2);

                SqlCommand command = new SqlCommand("UPDATE [Chat_" + connectNames(myName, friendName) + "] SET Image=@newImg WHERE Sender=@sender AND Time=@time", Program.connection);
                command.Parameters.AddWithValue("newImg", byteImage);
                command.Parameters.AddWithValue("sender", myName);
                command.Parameters.AddWithValue("time", date);
                command.ExecuteNonQuery();

                MessageHelper.sendMessage("editMessageAnswer", main, friendName, date, true, byteImage);
                foreach (ClientObject co in Program.clients) {
                    if (co.main != null && co.main.UserName.Equals(friendName)) {
                        MessageHelper.sendMessage("editMessageAnswer", co.main, myName, date, true, byteImage);
                        break;
                    }
                }

            } else {
                string newMess = String.Join("|", par);

                SqlCommand command = new SqlCommand("UPDATE [Chat_" + connectNames(myName, friendName) + "] SET Message=@newMess WHERE Sender=@sender AND Time=@time", Program.connection);
                command.Parameters.AddWithValue("newMess", newMess);
                command.Parameters.AddWithValue("sender", myName);
                command.Parameters.AddWithValue("time", date);
                command.ExecuteNonQuery();

                MessageHelper.sendMessage("editMessageAnswer", main, friendName, date, false, newMess);
                foreach (ClientObject co in Program.clients) {
                    if (co.main != null && co.main.UserName.Equals(friendName)) {
                        MessageHelper.sendMessage("editMessageAnswer", co.main, myName, date, false, newMess);
                        break;
                    }
                }
            }
        }

        ///////////
        public void deleteMessage(byte[] bytes) {
            StringBuilder sb = new StringBuilder();
            List<string> par = new List<string>();
            par.AddRange(sb.Append(Encoding.Unicode.GetString(bytes)).ToString().Split('|'));
            string myName = main.UserName;
            string friendName = par[0];
            par.RemoveAt(0);
            DateTime d = DateTime.FromBinary(long.Parse(par[0]));
            par.RemoveAt(0);

            SqlCommand command = new SqlCommand("DELETE FROM [Chat_" + connectNames(myName, friendName) + "] WHERE Sender=@sender AND Time=@time", Program.connection);
            command.Parameters.AddWithValue("sender", myName);
            command.Parameters.AddWithValue("time", d);
            command.ExecuteNonQuery();

            MessageHelper.sendMessage("deleteMessageAnswer", main, friendName, d);
            foreach (ClientObject co in Program.clients) {
                if (co.main != null && co.main.UserName.Equals(friendName)) {
                    MessageHelper.sendMessage("deleteMessageAnswer", co.main, myName, d);
                    break;
                }
            }
        }

        /////////
        public void sendMessage(byte[] bytes) {
            if (main == null) return;
            StringBuilder sb = new StringBuilder();
            List<string> par = new List<string>();

            string bytesAsString = sb.Append(Encoding.Unicode.GetString(bytes)).ToString();
            par.AddRange(bytesAsString.Split('|'));

            string myName = main.UserName;
            string friendName = par[0];
            par.RemoveAt(0);

            string sDate = par[0];
            DateTime date = DateTime.FromBinary(long.Parse(sDate));
            par.RemoveAt(0);

            string containImage = par[0];
            par.RemoveAt(0);

            if (containImage == "1") {

                byte[] byteImage = new byte[0];
                int textSize = 3 + friendName.Length + sDate.Length + containImage.Length;
                Array.Resize(ref byteImage, bytes.Length - textSize * 2);
                Array.Copy(bytes, textSize * 2, byteImage, 0, bytes.Length - textSize * 2);

                SqlCommand command = new SqlCommand("INSERT INTO [Chat_" + connectNames(myName, friendName) + "] (Sender,Time,Image) VALUES (@sender,@time,@img)", Program.connection);
                command.Parameters.AddWithValue("sender", myName);
                command.Parameters.AddWithValue("time", date);
                command.Parameters.AddWithValue("img", byteImage);
                command.ExecuteNonQuery();

                MessageHelper.sendMessage("sendMessageOnClient", main, true, friendName, date, true, byteImage);
                foreach (ClientObject co in Program.clients) {
                    if (co.main != null && co.main.UserName.Equals(friendName)) {
                        MessageHelper.sendMessage("sendMessageOnClient", co.main, false, myName, date, true, byteImage);
                        break;
                    }
                }
            } else {
                string mess = String.Join("|", par);

                SqlCommand command = new SqlCommand("INSERT INTO [Chat_" + connectNames(myName, friendName) + "] (Sender,Message,Time) VALUES (@sender,@mess,@time)", Program.connection);
                command.Parameters.AddWithValue("sender", myName);
                command.Parameters.AddWithValue("mess", mess);
                command.Parameters.AddWithValue("time", date);
                command.ExecuteNonQuery();

                MessageHelper.sendMessage("sendMessageOnClient", main, true, friendName, date, false, mess);
                foreach (ClientObject co in Program.clients) {
                    if (co.main != null && co.main.UserName.Equals(friendName)) {
                        MessageHelper.sendMessage("sendMessageOnClient", co.main, false, myName, date, false, mess);
                        break;
                    }
                }
            }
        }

        /////////////
        public void getMessages(byte[] bytes) {
            StringBuilder sb = new StringBuilder();
            string myName = main.UserName;
            string friendName = sb.Append(Encoding.Unicode.GetString(bytes)).ToString();

            SqlCommand command = new SqlCommand("SELECT * FROM [Chat_" + connectNames(myName, friendName) + "] ORDER BY Time", Program.connection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read()) {
                object ava = reader["Image"];
                if (!(ava is DBNull)) {
                    MessageHelper.sendMessage("getMessagesAnswer", main, ((string)reader["Sender"]).Equals(myName), friendName, (DateTime)reader["Time"], true, (byte[])ava);
                } else {
                    MessageHelper.sendMessage("getMessagesAnswer", main, ((string)reader["Sender"]).Equals(myName), friendName, (DateTime)reader["Time"], false, (string)reader["Message"]);
                }
            }
            reader.Close();
        }

        /////////////////
        public void deleteFriend(byte[] bytes) {
            StringBuilder sb = new StringBuilder();

            string myName = main.UserName;
            string friendName = sb.Append(Encoding.Unicode.GetString(bytes)).ToString();

            SqlCommand command = new SqlCommand("DELETE FROM [Friends_" + myName + "] WHERE Friend=@fr", Program.connection);
            command.Parameters.AddWithValue("fr", friendName);
            command.ExecuteNonQuery();

            command = new SqlCommand("DELETE FROM [Friends_" + friendName + "] WHERE Friend=@fr", Program.connection);
            command.Parameters.AddWithValue("fr", myName);
            command.ExecuteNonQuery();

            command = new SqlCommand("DROP TABLE [Chat_" + connectNames(myName, friendName) + "]", Program.connection);
            command.ExecuteNonQuery();

            MessageHelper.sendMessage("deleteFriendAnswer", main, friendName);
            foreach (ClientObject co in Program.clients) {
                if (co.main != null && co.main.UserName.Equals(friendName)) {
                    MessageHelper.sendMessage("deleteFriendAnswer", co.main, myName);
                    break;
                }
            }
        }

        public string connectNames(string n1, string n2) {
            string[] arr = { n1, n2 };
            Array.Sort(arr);
            return string.Join("_", arr);
        }

        //////////////
        public void addFriend(byte[] bytes) {
            StringBuilder sb = new StringBuilder();
            string myName = main.UserName;
            string friendName = sb.Append(Encoding.Unicode.GetString(bytes)).ToString();

            SqlCommand command = new SqlCommand("INSERT INTO [Friends_" + myName + "] (Friend) VALUES (@fr)", Program.connection);
            command.Parameters.AddWithValue("fr", friendName);
            command.ExecuteNonQuery();

            command = new SqlCommand("INSERT INTO [Friends_" + friendName + "] (Friend) VALUES (@fr)", Program.connection);
            command.Parameters.AddWithValue("fr", myName);
            command.ExecuteNonQuery();


            command = new SqlCommand("CREATE TABLE [Chat_" + connectNames(myName, friendName) + "] (Sender VARCHAR(50) NOT NULL, Message VARCHAR(MAX), Time DateTime NOT NULL, Image VARBINARY(MAX))", Program.connection);
            command.ExecuteNonQuery();

            MessageHelper.sendMessage("addFriendAnswer", main, friendName, getAvatar(friendName));
            foreach (ClientObject co in Program.clients) {
                if (co.main != null && co.main.UserName.Equals(friendName)) {
                    MessageHelper.sendMessage("addFriendAnswer", co.main, myName, getAvatar(myName));
                    break;
                }
            }
        }

        public byte[] getAvatar(string name) {
            SqlCommand command = new SqlCommand("SELECT Avatar FROM Users WHERE Name=@name", Program.connection);
            command.Parameters.AddWithValue("name", name);
            return (byte[])command.ExecuteScalar();
        }

        ////////////
        public void getFriends(byte[] bytes) {
            StringBuilder sb = new StringBuilder();
            sb.Append(Encoding.Unicode.GetString(bytes));

            string myName = sb.ToString().Split("|")[0];

            SqlCommand command = new SqlCommand("SELECT Friend FROM [Friends_" + myName + "]", Program.connection);
            SqlDataReader reader = command.ExecuteReader();
            List<string> names = new List<string>();
            while (reader.Read()) {
                names.Add((string)reader["Friend"]);
            }
            reader.Close();
            foreach (string name in names) {
                byte[] ava = getAvatar(name);
                Console.WriteLine("Friend sended " + name + " " + ava.Length);
                MessageHelper.sendMessage("getFriendsAnswer", main, name, ava);
            }
        }

        public void getQuickMessages(byte[] bytes) {
            StringBuilder sb = new StringBuilder();
            sb.Append(Encoding.Unicode.GetString(bytes));

            string myName = sb.ToString().Split("|")[0];

            SqlCommand command = new SqlCommand("SELECT * FROM [Quicks_" + myName + "]", Program.connection);
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read()) {
                MessageHelper.sendMessage("getQuickMessagesAnswer", main, (int)reader["Id"], (string)reader["Message"]);
            }
            reader.Close();
        }

        public void updateQuickMessages(byte[] bytes) {
            StringBuilder sb = new StringBuilder();
            sb.Append(Encoding.Unicode.GetString(bytes));

            string[] par = sb.ToString().Split("|");
            string id = par[0];
            Console.WriteLine(par.Length);

            if (id != "-1") {
                if (par.Length < 2) {
                    Console.WriteLine("DELETE " + id);
                    SqlCommand command = new SqlCommand("DELETE FROM [Quicks_" + main.UserName + "] WHERE Id=@id", Program.connection);
                    command.Parameters.AddWithValue("id", int.Parse(id));
                    command.ExecuteNonQuery();
                } else {
                    string message = par[1];
                    Console.WriteLine("UPDATE " + id + " " + message);
                    SqlCommand command = new SqlCommand("UPDATE [Quicks_" + main.UserName + "] SET Message=@newText WHERE Id=@id", Program.connection);
                    command.Parameters.AddWithValue("newText", message);
                    command.Parameters.AddWithValue("id", int.Parse(id));
                    command.ExecuteNonQuery();
                }
            } else {
                string message = par[1];
                Console.WriteLine("INSERT " + message);
                SqlCommand command = new SqlCommand("INSERT INTO [Quicks_" + main.UserName + "] (Message) VALUES (@text)", Program.connection);
                command.Parameters.AddWithValue("text", message);
                command.ExecuteNonQuery();
            }
        }

        ////////
        public void checkUserName(byte[] bytes) {
            StringBuilder sb = new StringBuilder();
            sb.Append(Encoding.Unicode.GetString(bytes));

            SqlCommand command = new SqlCommand("SELECT Count(*) FROM Users WHERE Name=@name", Program.connection);
            command.Parameters.AddWithValue("name", sb.ToString());
            int counter = (int)command.ExecuteScalar();

            MessageHelper.sendMessage("searchInUsersAnswer", this, (counter==0) ? "true" : "false");
        }

        //////////////
        public void registerUser(byte[] bytes) {
            Console.Write("[SERVER: " + DateTime.Now.ToString("HH:mm:ss") + "] ");
            StringBuilder sb = new StringBuilder();
            string[] par = sb.Append(Encoding.Unicode.GetString(bytes)).ToString().Split('|');
            byte[] ava = new byte[0];
            
            if (!par[2].Equals("!")) {
                int lpsize = 2 + par[0].Length + par[1].Length;
                Array.Resize(ref ava, bytes.Length - lpsize*2);
                Array.Copy(bytes, lpsize * 2, ava, 0, bytes.Length - lpsize * 2);
            }

            try {
                SqlCommand command = new SqlCommand("INSERT INTO Users (Name,Password,Avatar) VALUES (@name,@pass,@ava)", Program.connection);
                command.Parameters.AddWithValue("name", par[0]);
                command.Parameters.AddWithValue("pass", par[1]);
                command.Parameters.AddWithValue("ava", ava);
                command.ExecuteNonQuery();
            } catch (Exception e) { 
                Console.WriteLine(e.Message);
            }
            
            try {
                SqlCommand command = new SqlCommand("CREATE TABLE [Friends_" + par[0] + "] (Friend VARCHAR(50) NOT NULL)", Program.connection);
                command.ExecuteNonQuery();
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }

            try {
                SqlCommand command = new SqlCommand("CREATE TABLE [Quicks_" + par[0] + "] (Id INT NOT NULL IDENTITY(1, 1), Message VARCHAR(2000) NOT NULL, PRIMARY KEY (Id))", Program.connection);
                command.ExecuteNonQuery();
                command = new SqlCommand("INSERT INTO [Quicks_" + par[0] + "] VALUES (@par1), (@par2), (@par3), (@par4), (@par5)", Program.connection);
                command.Parameters.AddWithValue("par1", "Сейчас занят.");
                command.Parameters.AddWithValue("par2", "Добрый день!");
                command.Parameters.AddWithValue("par3", "Спасибо!");
                command.Parameters.AddWithValue("par4", "Как дела?");
                command.Parameters.AddWithValue("par5", "Напишу когда освобожусь.");
                command.ExecuteNonQuery();
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }

            MessageHelper.sendMessage("registerAnswer", this);//Save it  ////////////////////////////////////////////////////////////////////////////////////////

            Console.WriteLine("Регистрация нового аккаунта: {login: " + par[0] + ", password: " + par[1] + ", avatar: " + ava.Length + "}");
        }

        //////////
        public void checkUser(byte[] bytes) {
            Console.Write("[SERVER: " + DateTime.Now.ToString("HH:mm:ss") + "] ");
            StringBuilder sb = new StringBuilder();
            sb.Append(Encoding.Unicode.GetString(bytes));
            List<string> par = new List<string>();
            par.AddRange(sb.ToString().Split("|"));

            SqlCommand command = new SqlCommand("SELECT Count(*) FROM Users WHERE Name=@name AND Password=@pass", Program.connection);
            command.Parameters.AddWithValue("name", par[0]);
            command.Parameters.AddWithValue("pass", par[1]);
            int counter = (int)command.ExecuteScalar();

            if (counter == 1) {
                byte[] ava = getAvatar(par[0]);
                Console.WriteLine("Успешная авторизация: {login: " + par[0] + ", password: " + par[1] + ", avatar: " + ava.Length + "}");
                MessageHelper.sendMessage("userCheckAnswer", this, par[0], par[1], ava);
                main = new User(par[0], this);
                //User.AllActiveUsers.Add(main);
            } else {
                Console.WriteLine("Попытка авторизации: {login: " + par[0] + ", password: " + par[1] + "}");
                MessageHelper.sendMessage("userCheckAnswer", this);
            }
        }

        ////////////
        public void getUsersList(byte[] bytes) {
            StringBuilder sb = new StringBuilder();
            sb.Append(Encoding.Unicode.GetString(bytes));
            string res = "";

            SqlCommand command = new SqlCommand("SELECT Name FROM Users WHERE Name LIKE '%"+sb.ToString()+"%'", Program.connection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read()) {
                string name = (string)reader["Name"];
                if (res.Length > 0) {
                    res += "|";
                }
                res += name;
            }
            reader.Close();
            if (res.Length == 0) res = "!";
            Console.WriteLine(main.MyClient == null);
            MessageHelper.sendMessage("getUserListAnswer", main, res);
        }

        public void reconnectUser(byte[] bytes) {
            //Console.Write("[SERVER: " + DateTime.Now.ToString("HH:mm:ss") + "] ");
            StringBuilder sb = new StringBuilder();
            sb.Append(Encoding.Unicode.GetString(bytes));
            List<string> par = new List<string>();
            par.AddRange(sb.ToString().Split("|"));

            SqlCommand command = new SqlCommand("SELECT Count(*) FROM Users WHERE Name=@name AND Password=@pass", Program.connection);
            command.Parameters.AddWithValue("name", par[0]);
            command.Parameters.AddWithValue("pass", par[1]);
            int counter = (int)command.ExecuteScalar();

            if (counter == 1) {
                byte[] ava = getAvatar(par[0]);
                //Console.WriteLine("Успешная авторизация: {login: " + par[0] + ", password: " + par[1] + ", avatar: " + ava.Length + "}");
                //MessageHelper.sendMessage("userCheckAnswer", client.GetStream(), par[0], par[1], ava);
                main = new User(par[0], this);
                //User.AllActiveUsers.Add(main);
            } else {
                //Console.WriteLine("Попытка авторизации: {login: " + par[0] + ", password: " + par[1] + "}");
                MessageHelper.sendMessage("reconnectUserAnswer", this);
            }
        }

        public void disconnect() {
            if (client != null && client.Connected) {
                client.Client.Shutdown(SocketShutdown.Both);
                client.Client.Close();
                client.Close();
            }
        }

        public bool checkConnection() {
            try {
                MessageHelper.sendMessage("test1/2", this);
                MessageHelper.sendMessage("test2/2", this);
                return true;
            } catch {
                disconnect();
                return false;
            }
        }
    }
}
