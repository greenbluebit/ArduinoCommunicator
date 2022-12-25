using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace com.snowfall.ArduinoCommunicator
{
    public interface IMessageReader
    {
        void ReadMessage(string message);
    }
    public class SerialController : MonoBehaviour
    {
        [SerializeField]
        private string portName = "COM3";
        [SerializeField]
        private int baudRate = 9600;

        [Tooltip("After an error in the serial communication, or an unsuccessful " +
                 "connect, how many milliseconds we should wait.")]
        [SerializeField]
        private int reconnectionDelay = 1000;

        [Tooltip("Maximum number of unread data messages in the queue. " +
                 "New messages will be discarded.")]
        [SerializeField]
        private int maxUnreadMessages = 12;

        [SerializeField]
        private GameObject messageReaderRef;

        [Header("UnityEvents")]
        [SerializeField]
        private UnityEvent tearDownEvent;
        [SerializeField]
        private UnityEvent connectedEvent;
        [SerializeField]
        private UnityEvent disconnectedEvent;

        protected IMessageReader messageReader;
        protected Thread thread;
        protected SerialThreadLines serialThread;

        // Not something we receive from device or platform; Instead, something we use on Exception in SerialThread.cs
        public const string SERIAL_DEVICE_CONNECTED = "__Connected__";
        public const string SERIAL_DEVICE_DISCONNECTED = "__Disconnected__"; 

        void OnEnable()
        {
            serialThread = new SerialThreadLines(portName,
                                                 baudRate,
                                                 reconnectionDelay,
                                                 maxUnreadMessages);
            thread = new Thread(new ThreadStart(serialThread.RunForever));
            thread.Start();

            if(messageReaderRef != null)
            {
                messageReader = messageReaderRef.GetComponent<IMessageReader>();
            }
        }

        void OnDisable()
        {
            tearDownEvent?.Invoke();

            if (serialThread != null)
            {
                serialThread.RequestStop();
                serialThread = null;
            }

            if (thread != null)
            {
                thread.Join();
                thread = null;
            }
        }

        void Update()
        {
            if (messageReader == null)
                return;

            string message = (string)serialThread.ReadMessage();
            if (message == null)
                return;

            if (ReferenceEquals(message, SERIAL_DEVICE_CONNECTED))
                connectedEvent?.Invoke();
            else if (ReferenceEquals(message, SERIAL_DEVICE_DISCONNECTED))
                disconnectedEvent?.Invoke();
            else
                messageReader.ReadMessage(message);
        }

        public string ReadSerialMessage()
        {
            // Read the next message from the queue
            return (string)serialThread.ReadMessage();
        }

        public void SendSerialMessage(string message)
        {
            serialThread.SendMessage(message);
        }
    }
}