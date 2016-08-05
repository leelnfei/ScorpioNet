﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
namespace Scorpio.Net {
    public enum State {
        None,           //无状态
        Connecting,     //正在连接
        Connected,      //已连接状态
    }
    public class ClientSocket {
        private ScorpioConnectionFactory m_Factory;
        private State m_State = State.None;                         // 当前状态
        private ScorpioSocket m_Dispatcher;
        private Socket m_Socket = null;                             // Socket句柄
        private SocketAsyncEventArgs m_ConnectEvent = null;         // 异步连接消息
        public ClientSocket(ScorpioConnectionFactory factory) {
            m_Factory = factory;
            m_Dispatcher = new ScorpioSocket();
            m_ConnectEvent = new SocketAsyncEventArgs();
            m_ConnectEvent.Completed += ConnectionAsyncCompleted;
        }
        public void Connect(string host, int port) {
            if (m_State != State.None) return;
            m_State = State.Connecting;
            try {
                m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_Socket.NoDelay = false;
#if SCORPIO_DNSENDPOINT
                m_ConnectEvent.RemoteEndPoint = new DnsEndPoint(m_Host, m_Port);
#else
                IPAddress address;
                if (host == "localhost") {
                    m_ConnectEvent.RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
                    address = IPAddress.Parse("127.0.0.1");
                } else  if (IPAddress.TryParse(host, out address)) {
                    m_ConnectEvent.RemoteEndPoint = new IPEndPoint(address, port);
                } else {
                    address = Dns.GetHostEntry(host).AddressList[0];
                    m_ConnectEvent.RemoteEndPoint = new IPEndPoint(address, port);
                }
#endif
                m_Socket.ConnectAsync(m_ConnectEvent);
            } catch (Exception ex) {
                m_State = State.None;
                logger.error("连接服务器出错 " + host + ":" + port + " " + ex.ToString());
            };
        }
        void ConnectionAsyncCompleted(object sender, SocketAsyncEventArgs e) {
            if (e.SocketError != SocketError.Success) {
                m_State = State.None;
                logger.error("连接服务器出错 : " + e.SocketError);
                return;
            }
            m_State = State.Connected;
            var con = m_Factory.create();
            m_Dispatcher.SetSocket(m_Socket, con);
        }
    }
}