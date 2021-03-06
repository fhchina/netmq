﻿using System;

using MajordomoProtocol;

using NetMQ;

namespace MDPWorkerExample
{
    class Program
    {
        /// <summary>
        ///     usage:  MDPWorkerExample [-v]
        /// 
        ///     implements a MDPWorker API usage
        /// </summary>
        static void Main (string[] args)
        {
            var verbose = args.Length == 1 && args[0] == "-v";

            var id = new[] { (byte) 'W', (byte) '1' };

            Console.WriteLine ("Starting the worker!");
            try
            {
                // create worker offering the service 'echo'
                using (var session = new MDPWorker ("tcp://localhost:5555", "echo", id))
                {
                    // logging info to be displayed on screen
                    if (verbose)
                        session.LogInfoReady += (s, e) => Console.WriteLine ("{0}", e.Info);

                    // there is no inital reply
                    NetMQMessage reply = null;

                    while (true)
                    {
                        // send the reply and wait for a request
                        var request = session.Receive (reply);

                        if (verbose)
                            Console.WriteLine ("Received: {0}", request);

                        // was the worker interrupted
                        if (ReferenceEquals (request, null))
                            break;
                        // echo the request
                        reply = request;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine ("ERROR:");
                Console.WriteLine ("{0}", ex.Message);
                Console.WriteLine ("{0}", ex.StackTrace);

                Console.WriteLine ("exit - any key");
                Console.ReadKey ();
            }
        }
    }
}
