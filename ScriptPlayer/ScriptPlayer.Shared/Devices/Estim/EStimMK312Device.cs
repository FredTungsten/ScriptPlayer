using RexLabsWifiShock;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScriptPlayer.Shared.Devices.Estim
{
    /// <summary>
    /// Implementation of a generik MK312 Device, connected via RS232 or Wifi
    /// 
    /// 
    /// </summary>
    public class EStimMK312Device : Device
    {

        private MK312Device device = null; // Reference to the Device API
        private bool running = false; // Allows to terminate the threads.


        /// <summary>
        /// When a device is found, we initialize it here
        /// </summary>
        /// <param name="dev"></param>
        public EStimMK312Device(MK312Device dev)
        {
            this.device = dev;
            this.Name = "MK312 " + dev.getMK312Version() + " on " + dev.getConnectorName();

            //dev.initializeChannels();

            running = true;
            Thread thr = new Thread(new ThreadStart(SimulateToy));
            thr.Start();
        }

        /// <summary>
        /// No Idea
        /// </summary>
        /// <param name="information"></param>
        /// <returns></returns>
        public override Task Set(IntermediateCommandInformation information)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Recieves the next action to be executred
        /// </summary>
        /// <param name="information">Information about action to be executed</param>
        /// <returns></returns>
        protected override Task Set(DeviceCommandInformation information)
        {
            action_time = System.Environment.TickCount; // The current time

            current_level = information.PositionFromTransformed; // Set the current level to what the action think it was
            action_level = current_level; // The "from" level of the current action
            target_level = information.PositionToTransformed; // Set the target level to what the action tells us it should be
            target_time = action_time + information.Duration.Milliseconds; // Set the time by which the value is to be reached

            //Debug.WriteLine("" + action_level + ">" + target_level + " " + action_time + " ==> " + target_time + "("+(target_time-action_time)+")");

            return Task.CompletedTask;
        }
        
        private double target_level = 0;  // The level to be reached at target time
        private long target_time = 0; // The time (ticks) by that the target needs to be reached
        private long action_time = 0; // The time the last action was set
        private double action_level = 0;
        private double current_level = 0; // The level we are currently at
        private int device_update_interval = 1000 / 120; // The device gets updated 60 times a second

        /// <summary>
        /// Simulates the toy moving to the programmed positions
        /// </summary>
        private void SimulateToy()
        {
            while (running)
            {
                try
                {
                    long curtime = System.Environment.TickCount;

                    double new_level = 0;

                    if (curtime >= target_time)
                        new_level = target_level;
                    else
                    {
                        double full_timespan = target_time - action_time; // The timespan from action time to target time
                        double full_delta = target_level - action_level;  // The delta of the level during that timespan
                        double cur_timespan = curtime - action_time;      // The passed timespan since the action occured
                        double cur_delta = (full_delta / full_timespan) * cur_timespan; // The currently reached delta
                        new_level = action_level + cur_delta;
                    }

                    // Check boundaries
                    if (new_level<0) new_level = 0;
                    if (new_level>100) new_level = 100;

                    //Debug.WriteLine("c"+(curtime-action_time)+" "+new_level);

                    current_level = new_level; // Make the new level final

                    device.setChannelALevel((20  + (current_level * 0.8)) / 100.0);
                    device.setChannelBLevel((100 - (current_level * 0.8)) / 100.0);
                    Thread.Sleep(device_update_interval);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// No idea
        /// </summary>
        protected override void StopInternal()
        {
            try
            {
                device.resetChannels();
            }
            catch (Exception)
            {
            }
        }

        public override void Dispose()
        {
            try
            {
                running = false;
                device.disconnect();
            }
            catch (Exception)
            {
            }
        }
    }
}
