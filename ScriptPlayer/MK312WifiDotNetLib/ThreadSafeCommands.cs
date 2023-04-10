using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RexLabsWifiShock
{


    // A thread safe version of the commands class that has one thread polling a command queue
    public class ThreadSafeCommands:Commands {


        private Queue<Command> commands = new Queue<Command>(); // The command queue
        private object sync_obj = new object(); // Sync object making the command queue threadsafe

        private Boolean running = false;

        // Instances the threadsafe commands
        public ThreadSafeCommands(Protocol prot, IComm comm):base(prot, comm) {
            running = true;
            Thread thread1 = new Thread(mainQueue);
            thread1.Start();
        }

        // Sends the command into the queue and waits for the reply
        private byte query(Command cmd) {
            // Queue our command
            lock (sync_obj) {
                commands.Append(cmd);
            }

            // Wait for the reply
            long waituntil = System.Environment.TickCount + 5000; // We wait for 5 seconds until we give up
            while (cmd.retByte == -1) {
                if (System.Environment.TickCount > waituntil) throw new TimeoutException("Timeout waiting for queue command to be executed");
                Thread.Sleep(10);
            }

            if (cmd.ex != null) throw cmd.ex;

            return (byte) cmd.retByte;
        }

        // The Main Cueue that works down commands
        public void mainQueue() {
            while (running) {
                Command curCmd = null; // The current command
                lock (sync_obj) {
                    if (commands.Count > 0)
                    curCmd = commands.Dequeue();
                }
                if (curCmd == null) {
                    Thread.Sleep(10);
                    continue;
                }
                try
                {
                    if (curCmd.type == Command.CommandType.peek) {
                        curCmd.retByte = base.peek(curCmd.address);
                    }
                    if (curCmd.type == Command.CommandType.poke) {
                        base.poke(curCmd.address, curCmd.input);
                        curCmd.retByte = 0;
                    }
                }
                catch (System.Exception al)
                {
                    curCmd.ex = al;
                    curCmd.retByte = 0;
                }
            }
        }

        public override void disconnect()
        {
            running = false;
            base.disconnect();
        }

        /// Reads a memory address in the devices memory
        public new byte peek(uint address) {
            Command cmd = new Command();
            cmd.type = Command.CommandType.peek;
            cmd.address = address;
            return query(cmd);
        }

        /// Writes bytes into the devices Memory
        public new void poke(uint address, byte[] buffer) {
            Command cmd = new Command();
            cmd.type = Command.CommandType.poke;
            cmd.address = address;
            cmd.input = buffer;
            query(cmd);
        }

        /// Writes a byte into the devices memory
        public new void poke(uint address, byte b) {
            byte[] buf = new byte[1];
            buf[0] = b;

            poke(address, buf);
        }
    }



    public class Command {
        public enum CommandType {none, peek, poke};

        public CommandType type = CommandType.none; // Type of the command
        public byte[] input;  // Input data
        public uint address;  // Target address
        public int retByte = -1;  // Return value
        public Exception ex;  // Exception

    }



}

