using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MessengerServer {
    public class User {

        private string userName;
        public string UserName { get { return userName; } }

        private ClientObject myClient;
        public ClientObject MyClient { get { return myClient; } }

        public User(string uName, ClientObject conn) {
            userName = uName;
            myClient = conn;
        }

    }
}
