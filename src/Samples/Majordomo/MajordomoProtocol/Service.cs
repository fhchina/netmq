using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using NetMQ;
using NetMQ.zmq;

namespace MajordomoProtocol
{
    /// <summary>
    ///     a broker local representation for a service
    ///     act as a frame for worker offering the service
    ///     as well as for pending requests to workers
    /// </summary>
    internal class Service
    {
        private readonly List<Worker> m_workers;                // list of known worker for this service 
        private readonly List<NetMQMessage> m_pendingRequests;  // list of client requests for that service
        private readonly List<Worker> m_waitingWorkers;         // queue of workers waiting for requests FIFO!

        /// <summary>
        ///     the service name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     returns a sequence of waiting workers - some of which may have expired
        /// </summary>
        public IEnumerable<Worker> WaitingWorkers { get { return m_waitingWorkers; } }

        /// <summary>
        ///     returns a list of requests pending for being send to workers
        /// </summary>
        public List<NetMQMessage> PendingRequests { get { return m_pendingRequests; } }

        /// <summary>
        ///     ctor for a service
        /// </summary>
        /// <param name="name">the service name</param>
        public Service (string name)
        {
            Name = name;
            m_workers = new List<Worker> ();
            m_pendingRequests = new List<NetMQMessage> ();
            m_waitingWorkers = new List<Worker> ();
        }

        /// <summary>
        ///     returns true if wokers are waiting and requests are pending 
        ///     and false otherwise
        /// </summary>
        public bool CanDispatchRequests ()
        {
            return m_waitingWorkers.Count > 0 && m_pendingRequests.Count > 0;
        }

        /// <summary>
        ///     returns true if wokers exist and false otherwise
        /// </summary>
        public bool DoWorkersExist ()
        {
            return m_workers.Count > 0;
        }

        /// <summary>
        ///     get the longest waiting worker for this service
        ///     and remove it from the waiting list
        /// </summary>
        /// <returns>the worker or if non is available <c>null</c></returns>
        public Worker GetNextWorker ()
        {
            var worker = m_waitingWorkers.Count == 0 ? null : m_waitingWorkers[0];

            if (worker != null)
                m_waitingWorkers.Remove (worker);

            return worker;
        }

        /// <summary>
        ///     adds a worker to the waiting worker list and if it is
        ///     not known it adds it to the known workers as well
        /// </summary>
        /// <param name="worker">the worker to add</param>
        public void AddWaitingWorker (Worker worker)
        {
            if (!IsKnown (worker))
                m_workers.Add (worker);

            if (!IsWaiting (worker))
                // add to the end of the list
                // oldest is at the beginning of the list
                m_waitingWorkers.Add (worker);
        }

        /// <summary>
        ///     deletes worker from the list of known workers and
        ///     if the worker is registered for waiting removes it 
        ///     from that list as well
        /// </summary>
        /// <param name="worker">the worker to delete</param>
        public void DeleteWorker (Worker worker)
        {
            if (IsKnown (worker.Id))
                m_workers.Remove (worker);

            if (IsWaiting (worker))
                m_waitingWorkers.Remove (worker);
        }

        /// <summary>
        ///     add the request to the pending requests
        /// </summary>
        /// <param name="message">the message to send</param>
        public void AddRequest (NetMQMessage message)
        {
            // add to the end, thus the oldest is the first element
            m_pendingRequests.Add (message);
        }

        /// <summary>
        ///     return the oldest pending request or null if non exists
        /// </summary>
        public NetMQMessage GetNextRequest ()
        {
            // get one or null
            var request = m_pendingRequests.Count > 0 ? m_pendingRequests[0] : null;
            // remove from pending requests if it exists
            if (!ReferenceEquals (request, null))
                m_pendingRequests.Remove (request);

            return request;
        }

        public override int GetHashCode ()
        {
            return Name.GetHashCode ();
        }

        public override string ToString ()
        {
            return string.Format ("Name = {0} / Worker {1} - Waiting {2} - Pending REQ {3}",
                                  Name,
                                  m_workers.Count,
                                  m_waitingWorkers.Count,
                                  m_pendingRequests.Count);
        }

        public override bool Equals (object obj)
        {
            if (ReferenceEquals (obj, null))
                return false;

            var other = obj as Service;

            return !ReferenceEquals (other, null) && Name == other.Name;
        }

        private bool IsKnown (string workerName)
        {
            return m_workers.Exists (w => w.Id == workerName);
        }

        private bool IsKnown (Worker worker)
        {
            return m_workers.Contains (worker);
        }

        private bool IsWaiting (Worker worker)
        {
            return m_waitingWorkers.Contains (worker);
        }

        private bool IsWaiting (string name)
        {
            return m_waitingWorkers.Exists (w => w.Id == name);
        }
    }
}