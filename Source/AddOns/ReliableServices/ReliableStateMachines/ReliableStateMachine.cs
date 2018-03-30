﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace Microsoft.PSharp.ReliableServices
{
    public abstract class ReliableStateMachine : Machine
    {
        /// <summary>
        /// RSM Host
        /// </summary>
        protected RsmHost Host { get; private set; }

        /// <summary>
        /// For initializing a machine, on creation or restart
        /// </summary>
        /// <returns></returns>
        protected abstract Task OnActivate();

        /// <summary>
        /// Initializes the RSM
        /// </summary>
        /// <returns></returns>
        [MachineConstructor]
        async Task RsmInitialization()
        {
            var re = this.ReceivedEvent as RsmInitEvent;
            this.Assert(re != null, "Internal error in RSM initialization");
            this.Host = re.Host;

            await OnActivate();
        }

        #region Internal callbacks

        /// <summary>
        /// Invokes user callback when a machine throws an exception.
        /// </summary>
        /// <param name="ex">The exception thrown by the machine</param>
        /// <param name="methodName">The handler (outermost) that threw the exception</param>
        /// <returns>False if the exception should continue to get thrown, true if it was handled in this method</returns>
        internal override bool OnExceptionHandler(string methodName, Exception ex)
        {
            if (ex is ExecutionCanceledException)
            {
                // internal exception, used by PsharpTester
                return false;
            }

            this.Logger.OnMachineExceptionThrown(this.Id, CurrentStateName, methodName, ex);
            Host.NotifyFailure(ex, methodName);
            OnExceptionRequestedGracefulHalt = true;
            return false;
        }

        /// <summary>
        /// Invokes user callback when a machine receives an event it cannot handle
        /// </summary>
        /// <param name="ex">The exception thrown by the machine</param>
        /// <param name="methodName">The handler (outermost) that threw the exception</param>
        /// <returns>False if the exception should continue to get thrown, true if the machine should gracefully halt</returns>
        internal override bool OnUnhandledEventExceptionHandler(string methodName, UnhandledEventException ex)
        {
            this.Logger.OnMachineExceptionThrown(this.Id, ex.CurrentStateName, methodName, ex);
            Host.NotifyFailure(ex, methodName);
            OnExceptionRequestedGracefulHalt = true;
            return true;
        }

        /// <summary>
        /// Notify state push
        /// </summary>
        /// <param name="nextState"></param>
        internal override void OnStatePush(string nextState)
        {
            Host?.NotifyStatePush(nextState);
        }

        /// <summary>
        /// Notify state pop
        /// </summary>
        internal override void OnStatePop()
        {
            Host?.NotifyStatePop();
        }

        #endregion

        protected override void OnHalt()
        {
            Host.NotifyHalt();
            base.OnHalt();
        }
    }
}